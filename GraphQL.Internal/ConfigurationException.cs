using System;
using System.Reflection;

namespace Mt.GraphQL.Internal
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }

        internal static ConfigurationException TypeNotConfigured(Type type) =>
            new ConfigurationException($"Type {type.Name} is not configured.");

        internal static ConfigurationException ColumnNotIndexed(PropertyInfo property) =>
            new ConfigurationException($"Column {property.DeclaringType.Name}.{property.Name} cannot be used for filtering and ordering.");
    }
}
