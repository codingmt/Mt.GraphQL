using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mt.GraphQL.Internal
{
    public class QueryExpressions
    {
        public SelectClause SelectClause { get; set; }
        public Extend[] Extends { get; set; }
        public LambdaExpression FilterExpression { get; set; }
        public List<(LambdaExpression member, bool descending)> OrderBy { get; } = new List<(LambdaExpression member, bool descending)>();
        public bool RestrictToModel { get; set; }
    }

    public class QueryExpressions<T> : QueryExpressions
    {
        public string GetSelect() => 
            SelectClause?.ToString() ?? string.Empty;

        public void ParseSelect(string expression) => 
            SelectClause = new StringSelectClause { Expression = expression };

        public string GetExtend() => 
            Extends == null || Extends.Length == 0 
                ? string.Empty 
                : string.Join(",", Extends.Select(x => x.ToString()));

        public void ParseExtend(string value) => 
            Extends = Extend.Parse(value);

        public LambdaExpression GetActualSelectExpression(bool nullChecking)
        {
            if (SelectClause is ExpressionSelectClause e) 
                return e.Expression;

            var configuration = Configuration.GetTypeConfiguration<T>();
            var modelType = configuration.GetResultType(SelectClause?.GetProperties(), Extends);
            return SelectExpressionBuilder.CreateSelectExpression(typeof(T), modelType, nullChecking);
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
