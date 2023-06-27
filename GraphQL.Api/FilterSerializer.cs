using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Mt.GraphQL.Api
{
    internal class FilterSerializer<T> : ExpressionVisitor
    {
        private static readonly MethodInfo _stringStartsWith =
            typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string), typeof(StringComparison) });
        private static readonly MethodInfo _stringStartsWith1 =
            typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) });
        private static readonly MethodInfo _stringEndsWith =
            typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string), typeof(StringComparison) });
        private static readonly MethodInfo _stringEndsWith1 =
            typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) });
        private static readonly MethodInfo _stringContains =
            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string), typeof(StringComparison) });
        private static readonly MethodInfo _stringContains1 =
            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
        private static readonly MethodInfo _arrayContains =
            typeof(Enumerable).GetMethods().Single(x => x.Name == nameof(string.Contains) && x.GetParameters().Length == 2);

        private readonly StringBuilder _result = new StringBuilder();

        private ParameterExpression? _parameter;
        private bool _evaluatingAnd = false;

        public FilterSerializer(Expression filter)
        {
            if (!(filter is LambdaExpression lambda))
                throw new ArgumentException("Filter should be a lambda expression.");
            if (lambda.Parameters.Count != 1 ||
                lambda.Parameters[0].Type != typeof(T))
                throw new ArgumentException($"Expected a lambda with 1 parameter of type {typeof(T).FullName}.");
            if (lambda.ReturnType != typeof(bool))
                throw new ArgumentException("Expected a lambda with return type Boolean.");


            _parameter = lambda.Parameters[0];
            _result.Clear();
            Visit(filter);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var wasEvaluatingAnd = _evaluatingAnd;

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    Visit(node.Left);
                    _result.Append(" eq ");
                    Visit(node.Right);
                    break;
                case ExpressionType.NotEqual:
                    Visit(node.Left);
                    _result.Append(" ne ");
                    Visit(node.Right);
                    break;
                case ExpressionType.LessThan:
                    Visit(node.Left);
                    _result.Append(" lt ");
                    Visit(node.Right);
                    break;
                case ExpressionType.LessThanOrEqual:
                    Visit(node.Left);
                    _result.Append(" le ");
                    Visit(node.Right);
                    break;
                case ExpressionType.GreaterThan:
                    Visit(node.Left);
                    _result.Append(" gt ");
                    Visit(node.Right);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    Visit(node.Left);
                    _result.Append(" ge ");
                    Visit(node.Right);
                    break;
                case ExpressionType.AndAlso:
                    _evaluatingAnd = true;
                    Visit(node.Left);
                    _result.Append(" and ");
                    Visit(node.Right);
                    _evaluatingAnd = wasEvaluatingAnd;
                    break;
                case ExpressionType.OrElse:
                    if (wasEvaluatingAnd)
                    {
                        _result.Append("(");
                        _evaluatingAnd = false;
                    }
                    Visit(node.Left);
                    _result.Append(" or ");
                    Visit(node.Right);
                    if (wasEvaluatingAnd)
                    {
                        _result.Append(")");
                        _evaluatingAnd = true;
                    }
                    break;
                default:
                    throw new NotSupportedException($"Binary expression {node.NodeType} is not supported");
            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    _result.Append("not(");
                    Visit(node.Operand);
                    _result.Append(")");
                    break;
                case ExpressionType.Convert:
                    if (node.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
                        throw new NotSupportedException($"Conversion to type {node.Type.Name} is not supported");
                    AppendConstant(Expression.Lambda(node).Compile().DynamicInvoke());
                    return node;
                default:
                    throw new NotSupportedException($"Unary expression {node.NodeType} is not supported");
            }
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.NodeType == ExpressionType.MemberAccess && node.Expression == null)
            {
                AppendConstant(Expression.Lambda(node).Compile().DynamicInvoke());
                return node;
            }

            var owner = node.Expression;
            if (owner is ConstantExpression)
            {
                AppendConstant(Expression.Lambda(node).Compile().DynamicInvoke());
                return node;
            }

            var member = node.Member.Name;
            while (owner is MemberExpression ownerMember)
            {
                member = $"{ownerMember.Member.Name}.{member}";
                owner = ownerMember.Expression;
            }

            _result.Append(member);

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method == _stringContains ||
                node.Method == _stringContains1)
            {
                _result.Append("contains(");
                Visit(node.Object);
                _result.Append(',');
                Visit(node.Arguments[0]);
                _result.Append(")");
            }
            else if (node.Method == _stringStartsWith ||
                node.Method == _stringStartsWith1)
            {
                _result.Append("startsWith(");
                Visit(node.Object);
                _result.Append(',');
                Visit(node.Arguments[0]);
                _result.Append(")");
            }
            else if (node.Method == _stringEndsWith ||
                node.Method == _stringEndsWith1)
            {
                _result.Append("endsWith(");
                Visit(node.Object);
                _result.Append(',');
                Visit(node.Arguments[0]);
                _result.Append(")");
            }
            else if (node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == _arrayContains)
            {
                Visit(node.Arguments[1]);
                _result.Append(" in (");
                Visit(node.Arguments[0]);
                _result.Append(")");
            }
            else
                throw new NotSupportedException($"Method {node.Method.DeclaringType.Name}.{node.Method.Name} is not supported.");

            return node;
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                if (i > 0)
                    _result.Append(", ");

                Visit(node.Expressions[i]);
            }

            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Type != typeof(DateTime))
                throw new NotSupportedException($"Creating a new {node.Type.Name} is not supported.");

            var dt = Expression.Lambda<Func<DateTime>>(node).Compile()();
            AppendDateTime(dt);

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            AppendConstant(node.Value);
            return node;
        }

        public override string ToString() => _result.ToString();

        private void AppendConstant(object constant)
        {
            if (constant is DateTime dt)
                AppendDateTime(dt);
            else if (constant is float s)
                _result.Append(s.ToString("G"));
            else if (constant is double d)
                _result.Append(d.ToString("G"));
            else if (constant is decimal dec)
                _result.Append(dec.ToString("G"));
            else if (constant is string str)
                _result.Append($"'{str.Replace("'", "''")}'");
            else if (constant is bool b)
                _result.Append(b ? "true" : "false");
            else if (constant is int || constant is long)
                _result.Append(constant.ToString());
            else if (constant is Array a)
                for (int i = 0; i < a.Length; i++)
                {
                    if (i > 0)
                        _result.Append(", ");

                    AppendConstant(a.GetValue(i));
                }
            else
                throw new NotSupportedException($"Constant of type {constant?.GetType()?.Name ?? "null"} is not supported.");
        }

        private void AppendDateTime(DateTime dt) => 
            _result.Append(dt.ToString(dt.Date == dt ? @"\'yyyy-MM-dd\'" : @"\'yyyy-MM-dd\THH:mm:ss\'"));
    }
}
