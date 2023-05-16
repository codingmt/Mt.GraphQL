using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace Mt.GraphQL.Api
{
    internal class QueryExpressions<T>
    {
        public LambdaExpression? SelectExpression { get; set; }
        public void ParseSelect(string expression)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var properties = expression.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(propName =>
                {
                    propName = propName.Trim();
                    var t = typeof(T);
                    string? p = null;
                    Expression expr = parameter;
                    foreach (var part in propName.Split('.'))
                    {
                        var prop = t.GetProperties().SingleOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                            ?? throw new QueryParseException(expression, $"Could not parse select expression; {part} not found");
                        expr = Expression.Property(expr, prop);
                        t = prop.PropertyType;
                    }
                    return new { expr, type = t, name = propName };
                })
                .ToArray();
            if (!properties.Any())
            {
                SelectExpression = null;
                return;
            }

            var type = TypeBuilder.GetType(properties.Select(p => (p.name, p.type)).ToArray());
            SelectExpression = Expression.Lambda(
                Expression.New(type.GetConstructors().First(), properties.Select(x => x.expr).ToArray()),
                parameter);
        }
        public string GetSelect()
        {
            if (SelectExpression == null)
                return string.Empty;

            var serializer = new SelectSerializer<T>();
            serializer.Visit(SelectExpression);
            return serializer.ToString();
        }

        public LambdaExpression? FilterExpression { get; set; }
        internal string? GetFilter()
        {
            if (FilterExpression == null)
                return string.Empty;

            return new FilterSerializer<T>().Serialize(FilterExpression);
        }
        internal void ParseFilter(string filter)
        {
            FilterExpression = string.IsNullOrEmpty(filter) 
                ? null 
                : new FilterDeserializer<T>().Deserialize(filter);
        }
    }
}
