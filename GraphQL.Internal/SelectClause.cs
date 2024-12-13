using System;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mt.GraphQL.Internal
{
    public abstract class SelectClause
    {
        public abstract SelectClause Clone();
        public abstract override string ToString();
        public string[] GetProperties() => ToString().Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
    }

    public abstract class ExpressionSelectClause : SelectClause 
    {
        protected string _stringExpression;
        protected LambdaExpression _expression;

        public LambdaExpression Expression
        {
            get => _expression;
            set
            {
                _expression = value;
                _stringExpression = null;
            }
        }

        public override string ToString()
        {
            if (_stringExpression == null)
                ProcessExpression();

            return _stringExpression;
        }

        protected abstract void ProcessExpression();
    }

    public class ExpressionSelectClause<T, TResult> : ExpressionSelectClause
    {
        private Func<JsonNode, TResult> _resultMapping;

        public Func<JsonNode, TResult> ResultMapping
        {
            get
            {
                if (_resultMapping == null ) 
                    ProcessExpression();

                return _resultMapping;
            }
        }

        public ExpressionSelectClause(Expression<Func<T, TResult>> expression)
        {
            Expression = expression;
        }

        private ExpressionSelectClause()
        { }

        public override SelectClause Clone() => 
            new ExpressionSelectClause<T, TResult>
            {
                Expression = Expression,
                _resultMapping = ResultMapping
            };

        protected override void ProcessExpression()
        {
            var visitor = new SelectSerializer<T, TResult>(_expression);
            _stringExpression = visitor.ToString();
            _resultMapping = visitor.ResultMapping;
        }
    }

    public class StringSelectClause : SelectClause
    {
        public string Expression { get; set; }

        public override SelectClause Clone() =>
            new StringSelectClause
            {
                Expression = Expression
            };

        public override string ToString() => Expression;
    }
}
