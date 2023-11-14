using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mt.GraphQL.Internal
{
    internal static class ReflectionExtensions
    {
        public static IEnumerable<Expression> GetAttributeExpressions(this PropertyInfo propertyInfo) =>
            propertyInfo.GetCustomAttributesData()
                .Select(a =>
                {
                    var construct = Expression.New(a.Constructor, a.ConstructorArguments.Select(ca => Expression.Constant(ca.Value, ca.ArgumentType)));
                    return a.NamedArguments.Any()
                        ? (Expression)Expression.MemberInit(
                            construct,
                            a.NamedArguments.Select(na =>
                                Expression.Bind(na.MemberInfo, Expression.Constant(na.TypedValue.Value, na.TypedValue.ArgumentType))))
                        : construct;
                });
    }
}
