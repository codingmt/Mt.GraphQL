using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mt.GraphQL.Internal
{
    public static class ReflectionExtensions
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

        public static PropertyInfo[] GetPropertiesInheritedFirst(this Type fromType)
        {
            var declaringTypes = new Dictionary<Type, int>();
            var t = fromType;
            while (t != null && t != typeof(object))
            {
                declaringTypes.Add(t, declaringTypes.Count);
                t = t.BaseType;
            }

            return fromType.GetProperties()
                .OrderByDescending(p => declaringTypes.TryGetValue(p.DeclaringType, out var i) ? i : 0)
                .ToArray();
        }

        public static bool IsCollectionType(this Type type) => type.IsCollectionType(out var _);

        public static bool IsCollectionType(this Type type, out Type itemType)
        {
            itemType = null;
            if (!type.IsGenericType)
                return false;
            itemType = type.GetGenericArguments()[0];
            return typeof(ICollection<>).MakeGenericType(itemType).IsAssignableFrom(type);
        }

        public static bool IsReadWriteAutoProperty(this PropertyInfo propertyInfo) =>
            propertyInfo.CanRead &&
            propertyInfo.CanWrite &&
            propertyInfo.GetMethod.GetCustomAttribute<CompilerGeneratedAttribute>() != null &&
            propertyInfo.SetMethod.GetCustomAttribute<CompilerGeneratedAttribute>() != null;
    }
}
