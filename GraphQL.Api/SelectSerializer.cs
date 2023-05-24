using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mt.GraphQL.Api
{
    internal class SelectSerializer<TFrom> : ExpressionVisitor
    {
        protected string[] Members { get; private set; } = new string[0];

        public override Expression Visit(Expression node)
        {
            if (!(node is LambdaExpression lambda))
                throw new ArgumentException("Select expression must be a lambda expression.");
            if (!(lambda.Body is NewExpression newexp))
                throw new ArgumentException("Select expression must be like x => new { x.Id, ... }");

            Members = newexp.Arguments
                .Select(a =>
                    {
                        string selector = string.Empty;
                        while (a is MemberExpression member)
                        {
                            if (selector.Length > 0) 
                                selector = "." + selector;
                            selector = member.Member.Name + selector;
                            a = member.Expression;
                        }

                        return selector;
                    })
                .ToArray();

            return node;
        }

        public override string ToString() => string.Join(',', Members);
    }

    internal class SelectSerializer<TFrom, TTo> : SelectSerializer<TFrom>
    {
        public Func<JToken, TTo>? ResultMapping { get; private set; }

        public override Expression Visit(Expression node)
        {
            var result = base.Visit(node);

            ResultMapping = CreateResultMappingForAnonymousType() 
                ?? throw new Exception("Failed to create result mapping.");

            return result;
        }

        private Func<JToken, TTo>? CreateResultMappingForAnonymousType()
        {
            var properties = typeof(TTo).GetProperties();
            var constructor = typeof(TTo).GetConstructor(properties.Select(p => p.PropertyType).ToArray());
            if (constructor == null) 
                return null;

            var createGetPropertyFunctionMethod = typeof(SelectSerializer<TFrom, TTo>)
                .GetMethod(nameof(CreateGetPropertyFunction), BindingFlags.NonPublic | BindingFlags.Static);
            var getPropertyFunctions = properties
                .Select((p, i) => 
                    (Func<JToken, object>) createGetPropertyFunctionMethod.MakeGenericMethod(p.PropertyType).Invoke(null, new object[] { Members[i].Replace('.', '_') }))
                .ToArray();

            return
                jToken =>
                (TTo)constructor.Invoke(getPropertyFunctions.Select(f => f(jToken)).ToArray());
        }

        private static Func<JToken, object> CreateGetPropertyFunction<T>(string jsonMemberName) =>
            jToken => jToken[jsonMemberName].Value<T>();

        private void CreateResultMapping()
        {
            var createPropertyActionMethod = typeof(SelectSerializer<TFrom, TTo>)
                .GetMethod(nameof(CreatePropertyAction), BindingFlags.NonPublic | BindingFlags.Static);
            var setPropertyActions = typeof(TTo).GetProperties()
                .Select((p, i) =>
                {
                    var setter = p.GetSetMethod();
                    var jsonMemberName = Members[i];
                    return (Action<JToken, TTo>) createPropertyActionMethod.MakeGenericMethod(p.PropertyType).Invoke(null, new object[] { setter, jsonMemberName });
                })
                .ToArray();
            var setPropertiesAction = CombineActions(setPropertyActions);

            ResultMapping = jToken =>
            {
                var result = (TTo)Activator.CreateInstance(typeof(TTo));
                setPropertiesAction(jToken, result);
                return result;
            };
        }

        private static Action<JToken, TTo> CreatePropertyAction<T>(MethodInfo setter, string jsonMemberName) =>
            (jToken, tTo) =>
            {
                setter.Invoke(tTo, new object[] { jToken[jsonMemberName].Value<T>() });
            };

        private static Action<JToken, TTo> CombineActions(Action<JToken, TTo>[] actions)
        {
            var token = Expression.Parameter(typeof(JToken));
            var to = Expression.Parameter(typeof(TTo));

            var setProperties = Expression.Block(actions.Select(a => Expression.Call(null, a.Method, token, to)));
            
            return Expression.Lambda<Action<JToken, TTo>>(setProperties, token, to).Compile();
        }
    }
}
