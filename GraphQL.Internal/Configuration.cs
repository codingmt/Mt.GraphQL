using System;
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

        public static InternalTypeConfig GetTypeConfiguration(Type type, bool configured = false) =>
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
            private readonly Dictionary<string, PropertyConfig> _properties;
            private readonly List<string> _excludedColumns = new List<string>(); // can contain nested columns (Customer.Contacts)
            
            public bool Configured { get; }
            public int? MaxPageSize { get; set; }
            public string DefaultOrderBy { get; set; }

            public InternalTypeConfig(Type type, bool configured, params InternalTypeConfig[] copyFrom)
            {
                _type = type;
                _properties = type.GetPropertiesInheritedFirst()
                    .Select(p => new PropertyConfig(p))
                    .ToDictionary(p => p.Property.Name.ToLower());
                Configured = configured;

                foreach (var property in copyFrom.SelectMany(x => x._properties.Values))
                {
                    var p = _properties[property.Name.ToLower()];
                    p.IsIndexed |= property.IsIndexed;
                    p.IsExtension |= property.IsExtension;
                    p.Attributes.AddRange(property.Attributes);
                }

                MaxPageSize = copyFrom.LastOrDefault(x => x.MaxPageSize.HasValue)?.MaxPageSize;

                DefaultOrderBy = type.GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                    ?.Name ?? copyFrom.LastOrDefault(x => !string.IsNullOrEmpty(x.DefaultOrderBy))?.DefaultOrderBy;
            }

            public PropertyInfo GetProperty(string name) => 
                _properties.TryGetValue(name.ToLower(), out var p) ? p.Property : null;

            public bool IsColumnIndexed(string name) =>
                _properties[name.ToLower()].IsIndexed;

            public void SetColumnIsIndexed(string name) =>
                _properties[name.ToLower()].IsIndexed = true;

            public void ExcludeColumn(string name)
            {
                if (_properties.TryGetValue(name.ToLower(), out var property))
                    property.IsExcluded = true;
                else
                    _excludedColumns.Add(name.ToLower());
            }

            public void IsExtension(string name) =>
                _properties[name.ToLower()].IsExtension = true;

            public void ApplyAttribute(string columnName, Expression attribute) =>
                _properties[columnName.ToLower()].Attributes.Add(attribute);

            public int? GetPageSize(int? take)
            {
                var maxPageSize = MaxPageSize ?? DefaultMaxPageSize;
                return maxPageSize == 0 
                    ? take 
                    : maxPageSize > take 
                        ? take
                        : maxPageSize;
            }

            /// <summary>
            /// Returns a model class.
            /// </summary>
            /// <param name="properties">
            ///   The selected properties. 
            ///   Can be empty or null, in which case all properties, that are visible by default, will be used. 
            ///   Can contain nested property names like Customer.Id
            /// </param>
            /// <param name="extends">The extends to add to the model.</param>
            /// <returns>A model class</returns>
            public Type GetResultType(string[] properties, Extend[] extends)
            {
                // Create default set of properties
                if (properties == null || properties.Length == 0)
                    properties = _properties.Values
                        .Where(p => !p.IsExcluded && !p.IsExtension)
                        .Select(p => p.Name)
                        .ToArray();
                // Lowercase all names
                properties.Select(p => p.ToLower()).ToArray();

                // Keep track of the parent types to avoid loops
                var parentTypes = new List<Type> { _type };

                return getResultType(_type, string.Empty, properties, extends);

                Type getResultType(Type _fromType, string path, string[] propertyNames, Extend[] typeExtends)
                {
                    if (typeExtends == null)
                        typeExtends = new Extend[0];

                    var typeConfig = _fromType == _type ? this : GetTypeConfiguration(_fromType);
                    var extendsCanContainRegularProperties = parentTypes.Count > 1;
                    var propertyInfos = collectPropertyConfigs(extendsCanContainRegularProperties)
                        .Select(pc => (name: pc.Name, type: pc.Property.PropertyType, attributes: pc.Attributes.ToArray()))
                        .ToArray();

                    for (var i = propertyInfos.Length - 1; i >= 0; i--)
                    {
                        var propertyInfo = propertyInfos[i];

                        if (propertyInfo.type == typeof(string))
                        {
                            // no need to create nested type
                        }
                        else if (propertyInfo.type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propertyInfo.type))
                        {
                            var itemType = propertyInfo.type.GetGenericArguments()[0];
                            if (itemType.IsClass)
                            {
                                if (parentTypes.Contains(itemType))
                                {
                                    propertyInfos = propertyInfos.Where(p => p != propertyInfo).ToArray();
                                    continue;
                                }

                                var nested = getNestedType(itemType, propertyInfo.name);
                                propertyInfo.type = typeof(List<>).MakeGenericType(nested);
                            }
                        }
                        else if (propertyInfo.type.IsClass)
                        {
                            if (parentTypes.Contains(propertyInfo.type))
                            {
                                propertyInfos = propertyInfos.Where(p => p != propertyInfo).ToArray();
                                continue;
                            }

                            propertyInfo.type = getNestedType(propertyInfo.type, propertyInfo.name);
                        }

                        propertyInfos[i] = propertyInfo;
                    }

                    return TypeBuilder.GetType(_fromType.Name, propertyInfos, typeExtends);



                    Type getNestedType(Type __fromType, string propertyName)
                    {
                        parentTypes.Add(__fromType);
                        var _extend = typeExtends?.FirstOrDefault(e => e.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                        var _itemType = getResultType(
                            __fromType,
                            $"{path}{propertyName}.", _extend?.Properties?.Select(p => p.Name)?.ToArray(),
                            _extend?.Properties);
                        parentTypes.Remove(__fromType);
                        return _itemType;
                    }

                    List<PropertyConfig> collectPropertyConfigs(bool cleanupExtends)
                    {
                        var _propertyConfigs = new List<PropertyConfig>();
                        if (propertyNames == null || propertyNames.Length == 0)
                            _propertyConfigs = typeConfig._properties.Values
                                .Where(p =>
                                    !p.IsExcluded &&
                                    (!p.IsExtension || typeExtends.Any(e => e.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase))))
                                .ToList();
                        else
                            foreach (var propertyName in propertyNames)
                            {
                                var cfg = typeConfig;
                                PropertyConfig propertyConfig = null;
                                foreach (var propertyNamePart in propertyName.Trim().Split('.')) 
                                { 
                                    if (!cfg._properties.TryGetValue(propertyNamePart.ToLower(), out propertyConfig) ||
                                        propertyConfig.IsExcluded)
                                        throw new InternalException($"Property {propertyName} is not available on {_fromType.Name}.");

                                    if (propertyConfig.Property.PropertyType != typeof(string) && propertyConfig.Property.PropertyType.IsClass)
                                    {
                                        if (typeof(ICollection).IsAssignableFrom(propertyConfig.Property.PropertyType))
                                        {
                                            if (!propertyConfig.Property.PropertyType.IsGenericType)
                                                throw new InternalException($"Generic type expected for property {propertyName}");
                                            cfg = GetTypeConfiguration(propertyConfig.Property.PropertyType.GetGenericArguments()[0]);
                                        }
                                        else
                                            cfg = GetTypeConfiguration(propertyConfig.Property.PropertyType);
                                    }
                                }

                                if (propertyConfig == null)
                                    throw new InternalException($"Property {propertyName} is not available on {_fromType.Name}.");

                                _propertyConfigs.Add(new PropertyConfig(propertyConfig.Property, propertyName));
                            }

                        foreach (var typeExtend in typeExtends.ToArray())
                        {
                            if (!typeConfig._properties.TryGetValue(typeExtend.Name.ToLower(), out var propertyConfig) || propertyConfig.IsExcluded)
                                throw new InternalException($"Property {typeExtend.Name} is not available on type {_fromType.Name}.");

                            if (!propertyConfig.IsExtension)
                            {
                                if (cleanupExtends)
                                {
                                    typeExtends = typeExtends.Where(e => e != typeExtend).ToArray();
                                    continue;
                                }
                                
                                throw new InternalException($"Extension {typeExtend.Name} is not available on {_fromType.Name}.");
                            }

                            if (_propertyConfigs.Any(pc => pc.Name.Equals(typeExtend.Name, StringComparison.OrdinalIgnoreCase)))
                                continue;

                            _propertyConfigs.Add(propertyConfig);
                        }

                        return _propertyConfigs;
                    }
                }
            }
        }

        public class PropertyConfig
        {
            public string Name { get; }

            public PropertyInfo Property { get; }

            public bool IsExcluded { get; set; }

            public bool IsIndexed { get; set; }

            public bool IsExtension { get; set; }

            public List<Expression> Attributes { get; } = new List<Expression>();

            public PropertyConfig(PropertyInfo property)
            {
                Name = property.Name;
                Property = property;
            }

            public PropertyConfig(PropertyInfo property, string name)
            {
                Property = property;
                Name = name;
            }
        }
    }
}
