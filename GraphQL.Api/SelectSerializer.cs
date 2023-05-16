using System;
using System.Linq;
using System.Linq.Expressions;

namespace Mt.GraphQL.Api
{
    internal class SelectSerializer<T> : ExpressionVisitor
    {
        private string? _result;

        public override Expression Visit(Expression node)
        {
            _result = null;

            if (!(node is LambdaExpression lambda))
                throw new ArgumentException("Select expression must be a lambda expression.");
            if (!(lambda.Body is NewExpression newexp))
                throw new ArgumentException("Select expression must be like x => new { x.Id, ... }");

            _result = string.Join(
                ',',
                newexp.Arguments.Select(a =>
                {
                    string? selector = null;
                    while (a is MemberExpression member)
                    {
                        if (selector != null) 
                            selector = "." + selector;
                        selector = member.Member.Name + selector;
                        a = member.Expression;
                    }

                    return selector;
                }));

            return node;
        }

        public override string ToString() => _result ?? string.Empty;
    }
}
