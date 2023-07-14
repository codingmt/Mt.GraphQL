using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
        public string GetSelect()
        {
            if (SelectExpression == null)
                return string.Empty;

            return new SelectSerializer<T>(SelectExpression).ToString();
        }

        public void ParseSelect(string expression)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var properties = expression.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(propName =>
                {
                    var (expr, t) = ParseMemberExpression(ref propName, parameter);
                    return new { expr, type = t, name = propName };
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
                    var type = TypeBuilder.GetType(properties.Select(p => (p.name, p.type)).ToArray());
                    SelectExpression = Expression.Lambda(
                        Expression.MemberInit(
                            Expression.New(type.GetConstructors().First()),
                            properties.Select(p => Expression.Bind(type.GetProperty(p.name), p.expr.Body))),
                        parameter);
                    break;
            }
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

                        var (member, _) = ParseMemberExpression(ref part, parameter);
                        return (member, descending);
                    }));
        }

        private static (LambdaExpression expr, Type t) ParseMemberExpression(ref string memberExpression, ParameterExpression parameter)
        {
            memberExpression = memberExpression.Trim();
            var normalizedMemberExpression = string.Empty;
            var t = typeof(T);
            Expression expr = parameter;
            foreach (var part in memberExpression.Split('.'))
            {
                var prop = t.GetProperties().SingleOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                    ?? throw new QueryInternalException(memberExpression, $"Could not parse expression; {part} not found");
                expr = Expression.Property(expr, prop);
                t = prop.PropertyType;
                normalizedMemberExpression = $"{normalizedMemberExpression}.{prop.Name}";
            }
            memberExpression = normalizedMemberExpression.Substring(1);
            return (Expression.Lambda(expr, parameter), t);
        }
    }
}
