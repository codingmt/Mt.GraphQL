using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Mt.GraphQL.Internal
{
    public class Extend
    {
        public string Name { get; set; }

        public Extend[] Properties { get; set; }

        public override string ToString()
        {
            if (Properties != null && Properties.Any())
                return $"{Name}({string.Join(",", Properties.Select(p => p.ToString()))})";

            return Name;
        }

        public static Extend[] Parse(string extend)
        {
            extend = extend?.Replace(" ", "") ?? string.Empty;
            var pos = 0;
            return readExtends();

            Extend[] readExtends()
            {
                var result = new List<Extend>();
                while (pos < extend.Length)
                {
                    var len = extend.IndexOfAny(new[] { ',', '(', ')' }, pos) - pos;
                    if (len < 0) 
                        len = extend.Length - pos;
                    if (len == 0)
                        throw new InternalException($"No field name found on position {pos} in extend {extend}");
                    var name = extend.Substring(pos, len);
                    if (!Regex.IsMatch(name, @"^[a-z]+$", RegexOptions.IgnoreCase))
                        throw new InternalException($"Extension is invalid: {name}");
                    var e = new Extend { Name = name };
                    result.Add(e);
                    pos += len;
                    if (pos >= extend.Length)
                        break;
                    pos++;
                    if (extend[pos - 1] == '(')
                    {
                        e.Properties = readExtends();
                        if (pos >= extend.Length)
                            break;
                        pos++;
                    }
                    switch (extend[pos-1])
                    {
                        case ',':
                            continue;
                        case ')':
                            return result.ToArray();
                    }
                }

                return result.ToArray();
            }
        }
    }

    public static class ExtendExtensions
    {
        public static Extend[] CloneExtends(this Extend[] extends)
        {
            if (extends == null)
                return null;

            return extends
                .Select(x => new Extend { Name = x.Name, Properties = CloneExtends(x.Properties) })
                .ToArray();
        }

        public static Extend[] Add(this Extend[] extends, LambdaExpression memberExpression)
        {
            if (!(memberExpression.Body is MemberExpression me) ||
                !(me.Expression is ParameterExpression pe))
                throw new InternalException("You can only select direct members as extensions.");

            var result = extends == null 
                ? new List<Extend>() 
                : extends.ToList();
            var skipTypes = new List<Type> { pe.Type };
            result.Add(
                new Extend
                {
                    Name = me.Member.Name,
                    Properties = getProperties(((PropertyInfo)me.Member).PropertyType)
                });
            return result.ToArray();

            Extend[] getProperties(Type t)
            {
                if (t == typeof(string) || !t.IsClass)
                    return null;
                if (t.IsGenericType && 
                    typeof(ICollection<>).MakeGenericType(t.GetGenericArguments()[0]).IsAssignableFrom(t))
                    return getProperties(t.GetGenericArguments()[0]);
                skipTypes.Add(t);
                var e = t.GetPropertiesInheritedFirst()
                    .Where(
                        p =>
                        {
                            var pt = (p.PropertyType.IsGenericType &&
                                typeof(ICollection<>).MakeGenericType(p.PropertyType.GetGenericArguments()[0]).IsAssignableFrom(p.PropertyType))
                                ? p.PropertyType.GetGenericArguments()[0]
                                : p.PropertyType;
                            return !skipTypes.Contains(pt);
                        })
                    .Select(
                        p => 
                        new Extend
                        {
                            Name = p.Name,
                            Properties = getProperties(p.PropertyType)
                        })
                    .ToArray();
                skipTypes.Remove(t);
                return e;
            }
        }
    }
}
