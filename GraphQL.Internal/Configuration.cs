﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mt.GraphQL.Internal
{
    public static class Configuration
    {
        private static readonly ConcurrentDictionary<Type, InternalTypeConfig> _typeConfigurations 
            = new ConcurrentDictionary<Type, InternalTypeConfig>();
        private static readonly ConcurrentDictionary<Type, InternalTypeConfig> _baseTypeConfigurations 
            = new ConcurrentDictionary<Type, InternalTypeConfig>();

        private static int _defaultMaxPageSize;
        public static int DefaultMaxPageSize
        {
            get => _defaultMaxPageSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _defaultMaxPageSize = value;
            }
        }

        public static InternalTypeConfig GetTypeConfiguration<T>(bool configured = false) =>
            GetTypeConfiguration(typeof(T), configured);

        private static InternalTypeConfig GetTypeConfiguration(Type type, bool configured = false) =>
            _typeConfigurations.GetOrAdd(type, 
                t =>
                {
                    if (t.Namespace == TypeBuilder.Namespace)
                        throw new Exception("Creating a config for a constructed type.");

                    var baseConfigurations = _baseTypeConfigurations
                        .Where(x => x.Key.IsAssignableFrom(type))
                        .Select(x => x.Value)
                        .ToArray();

                    return new InternalTypeConfig(t, configured || baseConfigurations.Any(), baseConfigurations);
                });

        public static InternalTypeConfig GetBaseTypeConfiguration<T>() =>
            GetBaseTypeConfiguration(typeof(T));

        private static InternalTypeConfig GetBaseTypeConfiguration(Type type) =>
            _baseTypeConfigurations.GetOrAdd(type,
                t =>
                {
                    if (t.Namespace == TypeBuilder.Namespace)
                        throw new Exception("Creating a config for a constructed type.");

                    return new InternalTypeConfig(t, true);
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
            public int? MaxPageSize { get; set; }
            public string DefaultOrderBy { get; set; }

            public InternalTypeConfig(Type type, bool configured, params InternalTypeConfig[] copyFrom)
            {
                _type = type;
                Configured = configured;

                _indexedColumns = copyFrom.SelectMany(x => x._indexedColumns).Distinct().ToList();
                _excludedColumns = copyFrom.SelectMany(x => x._excludedColumns).Distinct().ToList();
                _columnAttributes = copyFrom.SelectMany(x => x._columnAttributes).ToList();
                MaxPageSize = copyFrom.LastOrDefault(x => x.MaxPageSize.HasValue)?.MaxPageSize;

                DefaultOrderBy = type.GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                    ?.Name ?? copyFrom.LastOrDefault(x => !string.IsNullOrEmpty(x.DefaultOrderBy))?.DefaultOrderBy;
            }

            public bool IsColumnIndexed(string name) => 
                _indexedColumns.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));

            public void SetColumnIsIndexed(string name) =>
                _indexedColumns.Add(name);

            public void ExcludeColumn(string name) =>
                _excludedColumns.Add(name);

            public void ApplyAttribute(string columnName, Expression attribute) =>
                _columnAttributes.Add((columnName, attribute));

            public int? GetPageSize(int? take)
            {
                var maxPageSize = MaxPageSize ?? DefaultMaxPageSize;
                return maxPageSize == 0 
                    ? take 
                    : maxPageSize > take 
                        ? take
                        : maxPageSize;
            }

            internal Type GetResultType() => _resultType ?? (_resultType = ConstructType(_type));

            private Type ConstructType(Type type, string path = "", List<Type> parentTypes = null)
            {
                parentTypes = parentTypes ?? new List<Type> ();
                parentTypes.Add(type);

                var properties = type.GetPropertiesInheritedFirst()
                    .Where(p => !_excludedColumns.Any(c => c.Equals($"{path}{p.Name}", StringComparison.OrdinalIgnoreCase)))
                    .Select(p => ( name: p.Name, type: p.PropertyType, attributes: p.GetAttributeExpressions().ToList() ))
                    .ToArray();

                for (var i = properties.Length - 1; i >= 0; i--)
                {
                    var property = properties[i];
                    property.attributes.AddRange(
                        GetTypeConfiguration(type)._columnAttributes
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
