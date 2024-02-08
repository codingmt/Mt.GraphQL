using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mt.GraphQL.Internal
{
    internal static class SelectExpressionBuilder
    {
        private static readonly MethodInfo _selectMethod = GetMethodInfo(q => q.Select(x => x));
        private static readonly MethodInfo _toListMethod = GetMethodInfo(q => q.ToList());

        private static MethodInfo GetMethodInfo(Expression<Func<IQueryable<object>, IQueryable<object>>> method) =>
            (method.Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        private static MethodInfo GetMethodInfo(Expression<Action<IQueryable<object>>> method) =>
            (method.Body as MethodCallExpression).Method.GetGenericMethodDefinition();

        private static readonly ConcurrentDictionary<Type, LambdaExpression> _createdExpressions = 
            new ConcurrentDictionary<Type, LambdaExpression>();

        public static LambdaExpression CreateSelectExpression(Type entityType, Type modelType) =>
            _createdExpressions.GetOrAdd(
                modelType,
                _ =>
                {
                    var parameter = Expression.Parameter(entityType, "x");
                    return Expression.Lambda(
                        CreateMemberInitForType(modelType, parameter, parameter),
                        parameter);
                });

        private static MemberInitExpression CreateMemberInitForType(Type targetType, Expression from, ParameterExpression param) =>
            CreateMemberInit(
                targetType,
                param,
                targetType.GetProperties()
                    .Select(pp =>
                    {
                        var name = pp.Name;
                        var expr = Expression.Lambda(
                            Expression.Property(from, from.Type.GetProperty(pp.Name)),
                            param);
                        return (expr, type: pp.PropertyType, name);
                    })
                    .ToArray());

        private static MemberInitExpression CreateMemberInit(Type targetType, ParameterExpression param, (LambdaExpression expr, Type type, string name)[] properties) =>
            Expression.MemberInit(
                Expression.New(targetType.GetConstructors().First()),
                properties.Select(p =>
                {
                    var prop = targetType.GetProperty(p.name);

                    // Regular properties
                    if (!prop.PropertyType.IsClass || prop.PropertyType == typeof(string))
                        return Expression.Bind(prop, p.expr.Body);

                    // One-to-many navigation property
                    if (typeof(IList).IsAssignableFrom(prop.PropertyType) && prop.PropertyType.IsGenericType)
                    {
                        var modelType = prop.PropertyType.GenericTypeArguments[0];
                        var entityType = p.expr.ReturnType.GenericTypeArguments[0];
                        var selectParameter = Expression.Parameter(entityType);
                        var staticMethodTarget = (Expression)null;
                        return Expression.Bind(prop,
                            Expression.Call(
                                staticMethodTarget,
                                _toListMethod.MakeGenericMethod(modelType),
                                Expression.Call(
                                    staticMethodTarget,
                                    _selectMethod.MakeGenericMethod(entityType, modelType),
                                    Expression.Convert(
                                        p.expr.Body,
                                        typeof(IQueryable<>).MakeGenericType(p.expr.ReturnType.GenericTypeArguments[0])),
                                    Expression.Lambda(
                                        CreateMemberInitForType(modelType, selectParameter, selectParameter),
                                        selectParameter))
                                ));
                    }

                    return Expression.Bind(
                        prop,
                        CreateMemberInitForType(
                            prop.PropertyType,
                            p.expr.Body,
                            param));
                }));
    }
}
