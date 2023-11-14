using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mt.GraphQL.Internal
{
    public static class Configuration
    {
        private static readonly ConcurrentDictionary<Type, InternalTypeConfig> _typeConfigurations 
            = new ConcurrentDictionary<Type, InternalTypeConfig>();

        public static InternalTypeConfig GetTypeConfiguration<T>(bool configured = false) =>
            GetTypeConfiguration(typeof(T), configured);

        private static InternalTypeConfig GetTypeConfiguration(Type type, bool configured = false) =>
                _typeConfigurations.GetOrAdd(type, 
                    t =>
                    {
                        if (t.Namespace == TypeBuilder.Namespace)
                            throw new Exception("Creating a config for a constructed type.");

                        return new InternalTypeConfig(t, configured);
                    });

        public static void ValidateMemberIsIndexed(PropertyInfo property)
        {
            var typeConfiguration = GetTypeConfiguration(property.DeclaringType);

            if (typeConfiguration.Configured && !typeConfiguration.IsColumnIndexed(property.Name))
                throw ConfigurationException.ColumnNotIndexed(property);
        }

        public class InternalTypeConfig
        {
            private readonly Type _type;
            private Type _resultType = null;

            private readonly List<string> _indexedColumns = new List<string>();
            private readonly List<string> _excludedColumns = new List<string>();
            private readonly List<(string columnName, Expression attribute)> _columnAttributes = new List<(string, Expression)> ();
            
            public bool Configured { get; }

            public InternalTypeConfig(Type type, bool configured)
            {
                _type = type;
                Configured = configured;
            }

            public bool IsColumnIndexed(string name) => 
                _indexedColumns.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));

            public void SetColumnIsIndexed(string name) =>
                _indexedColumns.Add(name);

            public void ExcludeColumn(string name) =>
                _excludedColumns.Add(name);

            public void ApplyAttribute(string columnName, Expression attribute) =>
                _columnAttributes.Add((columnName, attribute));

            internal Type GetResultType() => _resultType ?? (_resultType = ConstructType(_type));

            private Type ConstructType(Type type, string path = "", List<Type> parentTypes = null)
            {
                parentTypes = parentTypes ?? new List<Type> ();
                parentTypes.Add(type);

                var properties = type.GetProperties()
                    .Where(p => !_excludedColumns.Any(c => c.Equals($"{path}{p.Name}", StringComparison.OrdinalIgnoreCase)))
                    .Select(p => ( name: p.Name, type: p.PropertyType, attributes: p.GetAttributeExpressions().ToList() ))
                    .ToArray();

                for (var i = properties.Length - 1; i >= 0; i--)
                {
                    var property = properties[i];
                    property.attributes.AddRange(
                        _columnAttributes
                            .Where(x => x.columnName.Equals(property.name, StringComparison.OrdinalIgnoreCase))
                            .Select(x => x.attribute));

                    if (property.type == typeof(string))
                    {
                        // do nothing
                    }
                    else if (property.type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(property.type))
                    {
                        var itemType = property.type.GetGenericArguments()[0];
                        if (itemType.IsClass)
                        {
                            if (parentTypes.Contains(itemType))
                            {
                                properties = properties.Where(p => p != property).ToArray();
                                continue;
                            }

                            itemType = ConstructType(itemType, $"{property.name}.", parentTypes);
                            property.type = typeof(List<>).MakeGenericType(itemType);
                        }
                    }
                    else if (property.type.IsClass)
                    {
                        if (parentTypes.Contains(property.type))
                        {
                            properties = properties.Where(p => p != property).ToArray();
                            continue;
                        }

                        property.type = ConstructType(property.type, $"{property.name}.", parentTypes);
                    }

                    properties[i] = property;
                }

                parentTypes.Remove(type);

                return TypeBuilder.GetType(
                    type.Name, 
                    properties
                        .Select(p => (p.name, p.type, attributes: p.attributes.ToArray()))
                        .ToArray());
            }
        }
    }
}
