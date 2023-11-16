using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        public Func<JToken, TTo> ResultMapping { get; private set; }

        public override Expression Visit(Expression node)
        {
            var result = base.Visit(node);

            var lambda = node as LambdaExpression
                ?? throw new ArgumentException("Node must be a LambdaExpression.");

            if (lambda.Body is NewExpression)
                ResultMapping = CreateResultMappingForAnonymousType();
            else if (lambda.Body is MemberExpression)
                ResultMapping = CreateResultMappingForMember();
            else
                throw new Exception("Failed to create result mapping.");

            return result;
        }

        private Func<JToken, TTo> CreateResultMappingForAnonymousType()
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
                    var member = Members[i].ToLower()[0] + Members[i].Substring(1);
                    return (Func<JToken, object>)createGetPropertyFunctionMethod.MakeGenericMethod(p.PropertyType).Invoke(null, new object[] { member });
                })
                .ToArray();

            return
                jToken =>
                (TTo)constructor.Invoke(getPropertyFunctions.Select(f => f(jToken)).ToArray());
        }

        private Func<JToken, TTo> CreateResultMappingForMember()
        {
            #pragma warning disable CS8603 // Possible null reference return.
            if (typeof(TTo).IsClass)
                return jToken => jToken.ToObject<TTo>();

            return jToken => jToken.Value<TTo>();
            #pragma warning restore CS8603 // Possible null reference return.
        }

        private static Func<JToken, object> CreateGetPropertyFunction<T>(string jsonMemberName) =>
            #pragma warning disable CS8604 // Possible null reference argument.
            jToken => jToken[jsonMemberName].Value<T>();
            #pragma warning restore CS8604 // Possible null reference argument.
    }
}
