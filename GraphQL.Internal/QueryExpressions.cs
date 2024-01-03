using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mt.GraphQL.Internal
{
    public class QueryExpressions
    {
        public LambdaExpression SelectExpression { get; set; }
        public LambdaExpression FilterExpression { get; set; }
        public List<(LambdaExpression member, bool descending)> OrderBy { get; } = new List<(LambdaExpression member, bool descending)>();
    }

    public class QueryExpressions<T> : QueryExpressions
    {
        private static readonly MethodInfo _selectMethod = GetMethodInfo(q => q.Select(x => x));
        private static readonly MethodInfo _toListMethod = GetMethodInfo(q => q.ToList());

        private static MethodInfo GetMethodInfo(Expression<Func<IQueryable<object>, IQueryable<object>>> method) =>
            (method.Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        private static MethodInfo GetMethodInfo(Expression<Action<IQueryable<object>>> method) =>
            (method.Body as MethodCallExpression).Method.GetGenericMethodDefinition();

        public QueryExpressions()
        {
            ParseSelect(string.Empty);    
        }

        public string GetSelect()
        {
            if (SelectExpression == null)
                return string.Empty;

            return new SelectSerializer<T>(SelectExpression).ToString();
        }

        public void ParseSelect(string expression)
        {
            try
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var resultType = Configuration.GetTypeConfiguration<T>().GetResultType();
                var properties = expression.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(propName =>
                    {
                        var (expr, t, attributes) = ParseMemberExpression(resultType, ref propName, parameter);
                        return ( expr, type: t, name: propName, attributes );
                    })
                    .ToArray();
                switch (properties.Length)
                {
                    case 0:
                        SelectExpression = null;
                        break;
                    case 1:
                        SelectExpression = properties[0].expr;
                        break;
                    default:
                        var type = TypeBuilder.GetType(typeof(T).Name, properties.Select(p => (p.name, p.type, p.attributes)).ToArray());
                        SelectExpression = Expression.Lambda(
                            CreateMemberInit(type, parameter, properties.Select(p => (p.expr, p.type, p.name)).ToArray()),
                            parameter);
                        break;
                }
            }
            catch (ConfigurationException ex)
            {
                throw new QueryInternalException(expression, ex.Message);
            }
        }

        private MemberInitExpression CreateMemberInitForType(Type targetType, Expression from, ParameterExpression param) =>
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

        private MemberInitExpression CreateMemberInit(Type targetType, ParameterExpression param, (LambdaExpression expr, Type type, string name)[] properties) =>
            Expression.MemberInit(
                Expression.New(targetType.GetConstructors().First()),
                properties.Select(p =>
                {
                    var prop = targetType.GetProperty(p.name);
                    if (!prop.PropertyType.IsClass || prop.PropertyType == typeof(string))
                        return Expression.Bind(prop, p.expr.Body);

                    if (typeof(IList).IsAssignableFrom(prop.PropertyType) && prop.PropertyType.IsGenericType)
                    {
                        var modelType = prop.PropertyType.GenericTypeArguments[0];
                        var entityType = p.expr.ReturnType.GenericTypeArguments[0];
                        var selectParameter = Expression.Parameter(entityType);
                        return Expression.Bind(prop,
                            Expression.Call(
                                null,
                                _toListMethod.MakeGenericMethod(modelType),
                                Expression.Call(
                                    null,
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

        public LambdaExpression GetActualSelectExpression()
        {
            if (SelectExpression != null) 
                return SelectExpression;

            var parameter = Expression.Parameter(typeof(T), "x");
            var resultType = Configuration.GetTypeConfiguration<T>().GetResultType();
            return Expression.Lambda(
                CreateMemberInitForType(resultType, parameter, parameter),
                parameter);
        }

        public string GetFilter()
        {
            if (FilterExpression == null)
                return string.Empty;

            return new FilterSerializer<T>(FilterExpression).ToString();
        }

        public void ParseFilter(string filter)
        {
            FilterExpression = string.IsNullOrEmpty(filter) 
                ? null 
                : new FilterDeserializer<T>().Deserialize(filter);
        }

        public string GetOrderBy() =>
            string.Join(
                ",",
                OrderBy.Select(
                    x =>
                    {
                        var result = string.Empty;
                        Expression e = x.member.Body;
                        while (e is MemberExpression m)
                        {
                            if (result.Length > 0)
                                result = "." + result;
                            result = result.Length == 0 ? m.Member.Name : $"{m.Member.Name}.{result}";
                            e = m.Expression;
                        }
                        return result + (x.descending ? " desc" : string.Empty);
                    }));

        public void ParseOrderBy(string orderBy)
        {
            try
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                OrderBy.Clear();
                OrderBy.AddRange(
                    orderBy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(part =>
                        {
                            part = part.Trim();
                            var descending = false;
                            if (part.EndsWith(" desc", StringComparison.InvariantCultureIgnoreCase))
                            {
                                descending = true;
                                part = part.Substring(0, part.Length - 5).Trim();
                            }
                            else if (part.EndsWith(" asc", StringComparison.InvariantCultureIgnoreCase))
                                part = part.Substring(0, part.Length - 4).Trim();

                            var (member, _, __) = ParseMemberExpression(typeof(T), ref part, parameter, true);
                            return (member, descending);
                        }));
            }
            catch (ConfigurationException ex)
            {
                throw new QueryInternalException(orderBy, ex.Message);
            }
        }

        private static (LambdaExpression expr, Type t, Expression[] attributes) ParseMemberExpression(Type modelType, ref string memberExpression, ParameterExpression parameter, bool validateIndexed = false)
        {
            memberExpression = memberExpression.Trim();
            var normalizedMemberExpression = string.Empty;
            var entityType = parameter.Type;
            Expression expr = parameter;
            Expression[] attributes = null;
            foreach (var part in memberExpression.Split('.'))
            {
                var modelProp = modelType.GetProperties().SingleOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                    ?? throw new QueryInternalException(memberExpression, $"Could not parse expression; {part} not found on model.");
                var entityProp = entityType.GetProperties().SingleOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                    ?? throw new QueryInternalException(memberExpression, $"Could not parse expression; {part} not found on entity.");
                if (validateIndexed)
                    Configuration.ValidateMemberIsIndexed(entityProp);
                expr = Expression.Property(expr, entityProp);
                modelType = modelProp.PropertyType;
                entityType = entityProp.PropertyType;
                attributes = modelProp.GetAttributeExpressions().ToArray();
                normalizedMemberExpression = $"{normalizedMemberExpression}.{modelProp.Name}";
            }
            memberExpression = normalizedMemberExpression.Substring(1);
            return (Expression.Lambda(expr, parameter), modelType, attributes);
        }
    }
}
