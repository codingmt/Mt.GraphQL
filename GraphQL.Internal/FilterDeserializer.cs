using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Mt.GraphQL.Internal
{
    public class FilterDeserializer<T>
    {
        private static readonly IFormatProvider _formatProvider = CultureInfo.InvariantCulture;

        private static readonly Regex _groupOpenRegex = 
            new Regex(@"\s*\(", RegexOptions.IgnoreCase);
        private static readonly Regex _groupCloseRegex = 
            new Regex(@"\s*\)", RegexOptions.IgnoreCase);
        private static readonly Regex _groupNextRegex = 
            new Regex(@"\s*,", RegexOptions.IgnoreCase);
        private static readonly Regex _functionRegex = 
            new Regex(@"\s*(?<name>[a-z]+)\s*\(", RegexOptions.IgnoreCase);
        private static readonly Regex _functionParameterDelimiterRegex = 
            new Regex(@"\s*[,\)]", RegexOptions.IgnoreCase);
        private static readonly Regex _memberRegex = 
            new Regex(@"\s*(?<member>[a-z]\w*(\.[a-z]\w*)*)", RegexOptions.IgnoreCase);
        private static readonly Regex _constantRegex = 
            new Regex(@"\s*((?<null>null)|(?<bool>true|false)|(?<number>\d+(\.\d*)?)|(?<date>'\d{4}-\d{1,2}-\d{1,2}')|(?<datetime>'\d{4}-\d{1,2}-\d{1,2}T\d{1,2}:\d\d:\d\d')|(?<string>'([^']|'')*'))", RegexOptions.IgnoreCase);
        private static readonly Regex _operatorRegex = 
            new Regex(@"\s*(?<operator>[a-z]+)\s", RegexOptions.IgnoreCase);

        private static readonly MethodInfo _stringContains =
            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string), typeof(StringComparison) });
        private static readonly MethodInfo _stringStartsWith =
            typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string), typeof(StringComparison) });
        private static readonly MethodInfo _stringEndsWith =
            typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string), typeof(StringComparison) });
        private static readonly MethodInfo _arrayContains =
            typeof(Enumerable).GetMethods().Single(x => x.Name == nameof(string.Contains) && x.GetParameters().Length == 2);

        private static readonly ParameterExpression _parameter = 
            Expression.Parameter(typeof(T), "x");

        private string _filter = string.Empty;
        private int _position;

        public LambdaExpression? Deserialize(string filter)
        {
            _filter = filter?.Trim() ?? string.Empty;
            _position = 0;
            Expression? result = null;
            while (_position < _filter.Length)
            {
                result = ReadBooleanPart(result);
            }
            return result == null ? null : Expression.Lambda<Func<T, bool>>(result, _parameter);
        }

        private Expression? ReadBooleanPart(Expression? related = null)
        {
            var result = related;

            do
            {
                result = ReadPart(result);
            } while (result != null && result.Type != typeof(bool));

            return result;
        }

        private Expression? ReadPart(Expression? related = null)
        {
            if (_position >= _filter.Length)
                return null;

            if (related == null)
            {
                // parse group open
                {
                    var match = _groupOpenRegex.Match(_filter, _position);
                    if (match.Success && match.Index == _position)
                    {
                        _position += match.Length;
                        Expression? result = null;
                        do
                        {
                            result = ReadBooleanPart(result);

                            match = _groupCloseRegex.Match(_filter, _position);
                            if (match.Success && match.Index == _position)
                            {
                                _position += match.Length;
                                return result;
                            }

                            if (_position == _filter.Length)
                                throw new QueryParseException(_filter, "Closing parenthesis not found");
                        } while (true);
                    }
                }

                // parse function call
                {
                    var match = _functionRegex.Match(_filter, _position);
                    if (match.Success && match.Index == _position)
                    {
                        _position += match.Length;
                        var functionName = match.Groups["name"].Value.ToLower();
                        var parameters = new List<Expression>();
                        while (true)
                        {
                            match = _groupCloseRegex.Match(_filter, _position);
                            if (match.Success && match.Index == _position)
                            {
                                _position += match.Length;
                                break;
                            }

                            var parameter = ReadPart(parameters.FirstOrDefault()) ??
                                throw new QueryParseException(_filter, $"Could not parse parameter at position {_position}");
                            parameters.Add(parameter);

                            match = _functionParameterDelimiterRegex.Match(_filter, _position);
                            if (!match.Success || match.Index != _position)
                                throw new QueryParseException(_filter, $"Expected delimiter or closing bracket at position {_position}");
                            _position += match.Length;
                            if (match.Value.Trim() == ")")
                                break;
                        }

                        switch (functionName)
                        {
                            case "contains":
                                if (parameters.Count != 2)
                                    throw new QueryParseException(_filter, $"Expected 2 parameters for function '{functionName}'.");
                                return Expression.Call(parameters[0], _stringContains, parameters[1], Expression.Constant(StringComparison.OrdinalIgnoreCase));
                            case "startswith":
                                if (parameters.Count != 2)
                                    throw new QueryParseException(_filter, $"Expected 2 parameters for function '{functionName}'.");
                                return Expression.Call(parameters[0], _stringStartsWith, parameters[1], Expression.Constant(StringComparison.OrdinalIgnoreCase));
                            case "endswith":
                                if (parameters.Count != 2)
                                    throw new QueryParseException(_filter, $"Expected 2 parameters for function '{functionName}'.");
                                return Expression.Call(parameters[0], _stringEndsWith, parameters[1], Expression.Constant(StringComparison.OrdinalIgnoreCase));
                            case "not":
                                if (parameters.Count != 1 || parameters[0].Type != typeof(bool))
                                    throw new QueryParseException(_filter, $"Expected 1 boolean parameter for function '{functionName}'.");
                                return Expression.Not(parameters[0]);
                            default:
                                throw new QueryParseException(_filter, $"Unknown function '{functionName}'");
                        }
                    }
                }

                // parse member
                {
                    var match = _memberRegex.Match(_filter, _position);
                    if (match.Success && match.Index == _position)
                    {
                        var ownerType = typeof(T);
                        Expression result = _parameter;
                        foreach (var member in match.Groups["member"].Value.Split("."))
                        {
                            var property = ownerType.GetProperties().SingleOrDefault(p => p.Name == member)
                                ?? throw new QueryParseException(_filter, $"Property {member} was not found on type {ownerType.Name}");
                            result = Expression.Property(result, property);
                            ownerType = property.PropertyType;
                        }

                        _position += match.Length;
                        return result;
                    }
                } 
            }

            if (related != null)
            {
                // parse constant
                {
                    if (ReadConstant(related, out var constant))
                        return constant;
                }

                // parse operator
                if (related != null)
                {
                    var match = _operatorRegex.Match(_filter, _position);
                    if (match.Success && match.Index == _position)
                    {
                        var op = match.Groups["operator"].Value.ToLower();
                        _position += match.Length;

                        if (op == "in")
                        {
                            var array = ReadItems(related);
                            var containsMethod = _arrayContains.MakeGenericMethod(related.Type);
                            return Expression.Call(containsMethod, array, related);
                        }
                        else
                        {
                            var right = op == "or" || op == "and"
                                ? ReadBooleanPart()
                                : ReadPart(related);
                            switch (op)
                            {
                                case "and":
                                    return Expression.AndAlso(related, right);
                                case "or":
                                    return Expression.OrElse(related, right);
                                case "eq":
                                    return Expression.Equal(related, right);
                                case "ne":
                                    return Expression.NotEqual(related, right);
                                case "lt":
                                    return Expression.LessThan(related, right);
                                case "le":
                                    return Expression.LessThanOrEqual(related, right);
                                case "gt":
                                    return Expression.GreaterThan(related, right);
                                case "ge":
                                    return Expression.GreaterThanOrEqual(related, right);
                                default:
                                    throw new QueryParseException(_filter, $"Unknown operator {op}");
                            }
                        }
                    }
                }
            }

            throw new QueryParseException(_filter, $"Could not parse filter from position {_position}");
        }

        private bool ReadConstant(Expression related, out Expression? constant)
        {
            constant = null;

            var match = _constantRegex.Match(_filter, _position);
            if (!match.Success || match.Index != _position)
                return false;

            var type = ((related is MemberExpression memberExpression) ? memberExpression.Type
                : (related is MethodCallExpression methodCallExpression) ? methodCallExpression.Method.ReturnType
                : null)
                ?? throw new QueryParseException(_filter, $"Could not determine data type at position {_position}");

            try
            {
                if (match.Groups["null"].Success)
                    constant = Expression.Constant(null);
                else if (match.Groups["bool"].Success && (type == typeof(bool) || type == typeof(bool?)))
                    constant = Expression.Constant(match.Groups["bool"].Value.ToLower() == "true");
                else if (match.Groups["number"].Success)
                {
                    if (type == typeof(int) || type == typeof(int?))
                        constant = Expression.Constant(int.Parse(match.Groups["number"].Value));
                    else if (type == typeof(long) || type == typeof(long?))
                        constant = Expression.Constant(long.Parse(match.Groups["number"].Value));
                    else if (type == typeof(decimal) || type == typeof(decimal?))
                        constant = Expression.Constant(decimal.Parse(match.Groups["number"].Value, _formatProvider));
                    else if (type == typeof(float) || type == typeof(float?))
                        constant = Expression.Constant(float.Parse(match.Groups["number"].Value, _formatProvider));
                    else if (type == typeof(double) || type == typeof(double?))
                        constant = Expression.Constant(double.Parse(match.Groups["number"].Value, _formatProvider));
                }
                else if (match.Groups["date"].Success && type != typeof(string))
                    constant = Expression.Constant(DateTime.ParseExact(match.Groups["date"].Value, @"\'yyyy-M-d\'", null));
                else if (match.Groups["datetime"].Success && type != typeof(string))
                    constant = Expression.Constant(DateTime.ParseExact(match.Groups["datetime"].Value, @"\'yyyy-M-d\TH:mm:ss\'", null));
                else if ((match.Groups["date"].Success || match.Groups["datetime"].Success || match.Groups["string"].Success) && type == typeof(string))
                    constant = Expression.Constant(match.Value[1..^1].Replace("''", "'"));
            }
            catch (Exception ex)
            {
                throw new QueryParseException(_filter, $"Error parsing constant value {match.Value}", ex);
            }

            if (constant == null)
                throw new QueryParseException(_filter, $"Could not parse {type.Name} constant with value: {match.Value}");

            if (constant.Type != type)
                constant = Expression.Convert(constant, type);

            _position += match.Length;
            return true;
        }

        private NewArrayExpression ReadItems(Expression related)
        {
            var parentheses = false;
            // group open?
            {
                var match = _groupOpenRegex.Match(_filter, _position);
                if (match.Success && match.Index == _position)
                {
                    _position += match.Length;
                    parentheses = true;
                }
            }

            var items = new List<Expression>();
            while (true)
            {
                if (!ReadConstant(related, out var constant) || constant == null)
                    throw new QueryParseException(_filter, $"Expected constant at position {_position}");
                items.Add(constant);

                // next item?
                {
                    var match = _groupNextRegex.Match(_filter, _position);
                    if (match.Success && match.Index == _position)
                        _position += match.Length;
                    else
                        break;
                }
            }

            if (parentheses)
            {
                var match = _groupCloseRegex.Match(_filter, _position);
                if (match.Success && match.Index == _position)
                    _position += match.Length;
                else
                    throw new QueryParseException(_filter, $"Expected closing parenthesis at position {_position}");
            }

            return Expression.NewArrayInit(related.Type, items.ToArray());
        }
    }
}
