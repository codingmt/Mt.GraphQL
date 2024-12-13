using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mt.GraphQL.Internal
{
    public class SelectSerializer<TFrom> : ExpressionVisitor
    {
        public SelectSerializer(Expression expression)
        {
            Visit(expression);
        }

        protected string[] Members { get; private set; } = new string[0];

        public override Expression Visit(Expression node)
        {
            if (!(node is LambdaExpression lambda))
                throw new ArgumentException("Select expression must be a lambda expression.");

            if (lambda.Body is NewExpression newexp)
                Members = newexp.Arguments
                    .Select(getMember)
                    .ToArray();
            else if (lambda.Body is MemberInitExpression memberInit)
                Members = memberInit.Bindings
                    .Select(b => b.Member.Name)
                    .ToArray();
            else if (lambda.Body is MemberExpression member)
                Members = new[] { getMember(member) };
            else
                throw new ArgumentException("Select expression must be like x => new { x.<member>, ... } or x => x.<member>");

            return node;

            string getMember(Expression exp)
            {
                string selector = string.Empty;
                while (exp is MemberExpression member)
                {
                    if (selector.Length > 0)
                        selector = "." + selector;
                    selector = member.Member.Name + selector;
                    exp = member.Expression;
                }

                return selector;
            }
        }

        public override string ToString() => string.Join(",", Members);
    }

    public class SelectSerializer<TFrom, TTo> : SelectSerializer<TFrom>
    {
        public SelectSerializer(Expression expression) : base(expression)
        { }

        public Func<JsonNode, TTo> ResultMapping { get; private set; }

        public override Expression Visit(Expression node)
        {
            var result = base.Visit(node);

            var lambda = node as LambdaExpression
                ?? throw new ArgumentException("Node must be a LambdaExpression.");

            if (lambda.Body is NewExpression)
                ResultMapping = CreateResultMappingForAnonymousType();
            else if (lambda.Body is MemberExpression me)
                ResultMapping = CreateResultMappingForMember(me.Member.Name);
            else
                throw new Exception("Failed to create result mapping.");

            return result;
        }

        private Func<JsonNode, TTo> CreateResultMappingForAnonymousType()
        {
            var properties = typeof(TTo).GetProperties();
            var constructor = typeof(TTo).GetConstructor(properties.Select(p => p.PropertyType).ToArray());
            if (constructor == null) 
                return null;

            var createGetPropertyFunctionMethod = typeof(SelectSerializer<TFrom, TTo>)
                .GetMethod(nameof(CreateGetPropertyFunction), BindingFlags.NonPublic | BindingFlags.Static);
            var getPropertyFunctions = properties
                .Select((p, i) => 
                {
                    var memberCamelCase = Members[i].ToLower()[0] + Members[i].Substring(1);
                    var memberPascalCase = Members[i].ToUpper()[0] + Members[i].Substring(1);
                    return (Func<JsonNode, object>)createGetPropertyFunctionMethod
                        .MakeGenericMethod(p.PropertyType)
                        .Invoke(null, new object[] { memberCamelCase, memberPascalCase });
                })
                .ToArray();

            return
                jToken =>
                (TTo)constructor.Invoke(getPropertyFunctions.Select(f => f(jToken)).ToArray());
        }

        private Func<JsonNode, TTo> CreateResultMappingForMember(string memberName)
        {
            var type = TypeBuilder.GetType("SingleMember", new[] { (name: memberName, type: typeof(TTo), attributes: new Expression[0]) }, new Extend[0]);
            var getMethod = type.GetProperty(memberName).GetMethod;
            Func<object, TTo> getter = o => (TTo)getMethod.Invoke(o, null);

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return jsonNode => getter(JsonSerializer.Deserialize(jsonNode, type, options)); 
        }

        private static Func<JsonNode, object> CreateGetPropertyFunction<T>(string jsonMemberNameCamelCase, string jsonMemberNamePascalCase) =>
            jsonNode => (jsonNode[jsonMemberNameCamelCase] ?? jsonNode[jsonMemberNamePascalCase]).GetValue<T>();
    }
}
