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

        private static readonly ConcurrentDictionary<string, LambdaExpression> _createdExpressions = 
            new ConcurrentDictionary<string, LambdaExpression>();

        public static LambdaExpression CreateSelectExpression(Type entityType, Type modelType, bool nullChecking) =>
            _createdExpressions.GetOrAdd(
                $"{modelType.FullName} {nullChecking}",
                _ =>
                {
                    var parameter = Expression.Parameter(entityType, "x");
                    Expression body = CreateMemberInitForType(modelType, parameter, parameter, nullChecking);
                    if (nullChecking)
                        body = CheckForNull(parameter, body);
                    return Expression.Lambda(body, parameter);
                });

        private static MemberInitExpression CreateMemberInitForType(Type targetType, Expression from, ParameterExpression param, bool nullChecking) =>
            CreateMemberInit(
                targetType,
                param,
                targetType.GetProperties()
                    .Select(pp =>
                    {
                        var name = pp.Name;
                        Expression expr = from;
                        foreach (var namepart in name.Trim().Split('.'))
                        {
                            var property = Configuration.GetTypeConfiguration(expr.Type).GetProperty(namepart)
                                ?? throw new InternalException($"Property {namepart} not found on type {expr.Type.Name}.");
                            expr = Expression.Property(expr, property);
                        }
                        var lambda = Expression.Lambda(expr, param);
                        return (expr: lambda, type: pp.PropertyType, name);
                    })
                    .ToArray(),
                nullChecking);

        private static MemberInitExpression CreateMemberInit(Type targetType, ParameterExpression param, (LambdaExpression expr, Type type, string name)[] properties, bool nullChecking) =>
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
                                        CreateMemberInitForType(modelType, selectParameter, selectParameter, nullChecking),
                                        selectParameter))
                                ));
                    }

                    Expression value = CreateMemberInitForType(prop.PropertyType, p.expr.Body, param, nullChecking);
                    if (nullChecking)
                        value = CheckForNull(p.expr.Body, value);

                    return Expression.Bind(prop, value);
                }));

        private static Expression CheckForNull(Expression condition, Expression conditional) =>
            Expression.Condition(
                Expression.Equal(condition, Expression.Constant(null, condition.Type)), 
                Expression.Constant(null, conditional.Type), 
                conditional);
    }
}
