using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mt.GraphQL.Internal
{
    public static class Configuration
    {
        public static bool IsConfigured => _typeConfigurations.Any();

        private static readonly ConcurrentDictionary<Type, InternalTypeConfig> _typeConfigurations 
            = new ConcurrentDictionary<Type, InternalTypeConfig>();

        public static InternalTypeConfig GetTypeConfiguration<T>(bool createIfNotPresent = false) =>
            GetTypeConfiguration(typeof(T), createIfNotPresent);

        public static InternalTypeConfig GetTypeConfiguration(Type type, bool createIfNotPresent = false) =>
            createIfNotPresent
                ? _typeConfigurations.GetOrAdd(type, t => new InternalTypeConfig())
                : _typeConfigurations.TryGetValue(type, out var c) ? c : null;

        public static void ValidateMember(PropertyInfo property)
        {
            if (!IsConfigured)
                return;

            var typeConfiguration = 
                GetTypeConfiguration(property.DeclaringType)
                ?? throw ConfigurationException.TypeNotConfigured(property.DeclaringType);

            if (!typeConfiguration.IsColumnIndexed(property.Name))
                throw ConfigurationException.ColumnNotIndexed(property);
        }

        public class InternalTypeConfig
        {
            private readonly List<string> _indexedColumns = new List<string>();

            public bool IsColumnIndexed(string name) => 
                _indexedColumns.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));

            public void SetColumnIsIndexed(string name) =>
                _indexedColumns.Add(name);
        }
    }
}
