using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using InternalConfig = Mt.GraphQL.Internal.Configuration;

namespace Mt.GraphQL.Api.Server
{
    /// <summary>
    /// Configures which types can be used for GraphQL and which properties can be used for filtering and ordering.
    /// </summary>
    public static class GraphqlConfiguration
    {
        /// <summary>
        /// Loads GraphQL configuration from the configuration manager.
        /// </summary>
        public static void FromConfiguration(ConfigurationManager configurationManager)
        {
            var config = new ConfigurationModel();
            configurationManager.GetSection(nameof(GraphqlConfiguration)).Bind(config);
            config.Apply();
        }

        /// <summary>
        /// Configures a type for GraphQL.
        /// </summary>
        public static TypeConfiguration<T> Configure<T>() where T : class =>
            new TypeConfiguration<T>(InternalConfig.GetTypeConfiguration<T>(true));

        /// <summary>
        /// Configures a base type for GraphQL.
        /// </summary>
        public static TypeConfiguration<T> ConfigureBase<T>() where T : class =>
            new TypeConfiguration<T>(InternalConfig.GetBaseTypeConfiguration<T>());

        /// <summary>
        /// The max page size used when not configured on a type. Use 0 to disable.
        /// </summary>
        public static int DefaultMaxPageSize 
        { 
            get => InternalConfig.DefaultMaxPageSize; 
            set => InternalConfig.DefaultMaxPageSize = value; 
        }
    }

    /// <summary>
    /// The configuration of a specific type.
    /// </summary>
    public class TypeConfiguration<T> where T : class
    {
        internal readonly InternalConfig.InternalTypeConfig _internalTypeConfig;

        internal TypeConfiguration(InternalConfig.InternalTypeConfig internalTypeConfig)
        {
            _internalTypeConfig = internalTypeConfig;
        }

        /// <summary>
        /// Configures a property as fit for filtering and ordering.
        /// </summary>
        /// <typeparam name="TProperty">The type of property.</typeparam>
        /// <param name="property">The property selector.</param>
        /// <param name="allow">Filtering and sorting is allowed.</param>
        public TypeConfiguration<T> AllowFilteringAndSorting<TProperty>(Expression<Func<T, TProperty>> property, bool allow = true)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if (!(property.Body is MemberExpression m) ||
                (m.Expression != property.Parameters[0]))
                throw new ArgumentException($"Argument property must select a property on type {typeof(T).Name}.");

            _internalTypeConfig.SetColumnIsIndexed(m.Member.Name, allow);

            return this;
        }

        /// <summary>
        /// Exclude all properties of this type.
        /// </summary>
        public TypeConfiguration<T> ExcludeAllProperties()
        {
            var t = typeof(T);
            foreach (var p in t.GetProperties())
                if (p.DeclaringType == t)
                    _internalTypeConfig.ExcludeColumn(p.Name, true);

            return this;
        }

        /// <summary>
        /// Excludes a property or nested property from the GraphQL model.
        /// </summary>
        /// <typeparam name="TProperty">The type of property.</typeparam>
        /// <param name="property">The property selector.</param>
        /// <param name="exclude">Exclude the property.</param>
        public TypeConfiguration<T> ExcludeProperty<TProperty>(Expression<Func<T, TProperty>> property, bool exclude = true)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var body = property.Body;
            var member = string.Empty;
            do
            {
                switch (body)
                {
                    case MemberExpression m:
                        member = $".{m.Member.Name}{member}";
                        body = m.Expression;
                        break;
                    case MethodCallExpression call:
                        body = call.Method.IsStatic ? call.Arguments[0] : call.Object;
                        break;
                    default:
                        throw new ArgumentException($"Argument property must select a property or nested property on type {typeof(T).Name}.");
                }
            } while (!(body is ParameterExpression));

            if (body != property.Parameters[0])
                throw new ArgumentException($"Argument property must select a property on type {typeof(T).Name}.");

            _internalTypeConfig.ExcludeColumn(member.Substring(1), exclude);

            return this;
        }

        /// <summary>
        /// Configures a navigation property or a nested navigation property as an extension. This means that it is not returned by default, it has to be requested explicitly.
        /// </summary>
        /// <typeparam name="TProperty">The type of navigation property.</typeparam>
        /// <param name="property">The navigation property.</param>
        /// <param name="isExtension">The property is an extension.</param>
        public TypeConfiguration<T> IsExtension<TProperty>(Expression<Func<T, TProperty>> property, bool isExtension = true)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var body = property.Body;
            var member = string.Empty;
            do
            {
                switch (body)
                {
                    case MemberExpression m:
                        if (m.Member is PropertyInfo pi && !pi.PropertyType.IsClass && !pi.PropertyType.IsInterface && pi.PropertyType != typeof(string))
                            throw new ArgumentException($"Argument property must be a navigation property.");
                        member = $".{m.Member.Name}{member}";
                        body = m.Expression;
                        break;
                    case MethodCallExpression call:
                        body = call.Method.IsStatic ? call.Arguments[0] : call.Object;
                        break;
                    default:
                        throw new ArgumentException($"Argument property must select a property or nested property on type {typeof(T).Name}.");
                }
            } while (!(body is ParameterExpression));

            if (body != property.Parameters[0])
                throw new ArgumentException($"Argument property must select a property on type {typeof(T).Name}.");

            _internalTypeConfig.IsExtension(member.Substring(1), isExtension);

            return this;
        }

        /// <summary>
        /// Configures an attribute on a property.
        /// </summary>
        /// <typeparam name="TProperty">The type of property.</typeparam>
        /// <typeparam name="TAttribute">The type of attribute.</typeparam>
        /// <param name="property">The property selector.</param>
        /// <param name="attribute">The attribute expression.</param>
        public TypeConfiguration<T> ApplyAttribute<TProperty, TAttribute>(
            Expression<Func<T, TProperty>> property, 
            Expression<Func<TAttribute>> attribute)
            where TAttribute : Attribute
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if (!(property.Body is MemberExpression m) ||
                (m.Expression != property.Parameters[0]))
                throw new ArgumentException($"Argument property must select a property on type {typeof(T).Name}.");

            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));
            switch (attribute.Body)
            {
                case NewExpression n:
                    validateNewExpression(n);
                    break;
                case MemberInitExpression i:
                    validateNewExpression(i.NewExpression);
                    var faultyBinding = i.Bindings.FirstOrDefault(b => 
                        !(b is MemberAssignment ma) || 
                        !(b.Member is PropertyInfo) ||
                        !(ma.Expression is ConstantExpression));
                    if (faultyBinding != null)
                        throw new ArgumentException($"Argument attribute has a constructor binding that is not property bound to a constant ({faultyBinding.Member.Name}).");
                    break;
                default:
                    throw new ArgumentException("Argument attribute must construct an attribute.");
            }

            _internalTypeConfig.ApplyAttribute(m.Member.Name, attribute.Body);

            return this;

            void validateNewExpression(NewExpression newExpression)
            {
                if (!typeof(Attribute).IsAssignableFrom(newExpression.Constructor.DeclaringType))
                    throw new ArgumentException("Argument attribute must construct an attribute.");
                if (newExpression.Arguments.Any(a => !(a is ConstantExpression)))
                    throw new ArgumentException("Argument attribute constructor arguments can only contain constants.");
            }
        }

        /// <summary>
        /// Sets the maximum page size for a type. Set to 0 to disable max page size.
        /// </summary>
        public TypeConfiguration<T> MaxPageSize(int maxPageSize)
        {
            if (maxPageSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxPageSize));

            _internalTypeConfig.MaxPageSize = maxPageSize;
            return this;
        }

        /// <summary>
        /// Sets the property to use for ordering by default.
        /// </summary>
        public TypeConfiguration<T> DefaultOrderBy<TMember>(Expression<Func<T, TMember>> member)
        {
            if (!(member.Body is MemberExpression m) ||
                (m.Expression != member.Parameters[0]))
                throw new ArgumentException($"Argument member must select a property on type {typeof(T).Name}.");

            _internalTypeConfig.DefaultOrderBy = m.Member.Name;

            return this;
        }
    }

    /// <summary>
    /// Model for reading the GraphQL configuration from a configuration file.
    /// </summary>
    public class ConfigurationModel
    {
        /// <summary>
        /// The max page size used when not configured on a type. Use 0 to disable.
        /// </summary>
        public int DefaultMaxPageSize { get; set; }

        /// <summary>
        /// Base type configurations.
        /// </summary>
        public Dictionary<string, TypeConfigurationModel> BaseConfigurations { get; set; }

        /// <summary>
        /// Type configurations.
        /// </summary>
        public Dictionary<string, TypeConfigurationModel> TypeConfigurations { get; set; }

        /// <summary>
        /// Model for base type or type configurations.
        /// </summary>
        public class TypeConfigurationModel
        {
            /// <summary>
            /// The maximum page size for a type. Set to 0 to disable max page size.
            /// </summary>
            public int? MaxPageSize { get; set; }

            /// <summary>
            /// The property to use for ordering by default.
            /// </summary>
            public string DefaultOrderBy { get; set; }

            /// <summary>
            /// Exclude all properties of this type.
            /// </summary>
            public bool? ExcludeAllProperties { get; set; }

            /// <summary>
            /// List of property configuration models.
            /// </summary>
            public Dictionary<string, PropertyConfigurationModel> Properties { get; set; }

            /// <summary>
            /// Model for property configuration.
            /// </summary>
            public class PropertyConfigurationModel
            {
                /// <summary>
                /// Configure as fit for filtering and ordering.
                /// </summary>
                public bool? AllowFilteringAndSorting { get; set; }
                /// <summary>
                /// Excludes the property or nested property from the GraphQL model.
                /// </summary>
                public bool? Exclude { get; set; }
                /// <summary>
                /// Configures a navigation property or a nested navigation property as an extension. This means that it is not returned by default, it has to be requested explicitly.
                /// </summary>
                public bool? IsExtension { get; set; }

                internal void Apply(string name, InternalConfig.InternalTypeConfig config)
                {
                    if (AllowFilteringAndSorting.HasValue)
                        config.SetColumnIsIndexed(name, AllowFilteringAndSorting.Value);
                    if (Exclude.HasValue)
                        config.ExcludeColumn(name, Exclude.Value);
                    if (IsExtension.HasValue)
                        config.IsExtension(name, IsExtension.Value);
                }
            }

            internal void ApplyBase<T>() 
                where T: class
            {
                var config = GraphqlConfiguration.ConfigureBase<T>();
                ApplyInternal(config);
            }

            internal void Apply<T>() 
                where T: class
            {
                var config = GraphqlConfiguration.Configure<T>();
                ApplyInternal(config);
            }

            private void ApplyInternal<T>(TypeConfiguration<T> config)
                where T: class
            {
                if (MaxPageSize.HasValue)
                    config.MaxPageSize(MaxPageSize.Value);

                if (ExcludeAllProperties == true)
                    config.ExcludeAllProperties();

                if (!string.IsNullOrEmpty(DefaultOrderBy))
                {
                    var t = typeof(T);
                    var p = t.GetProperties().FirstOrDefault(x => 
                        x.Name.Equals(DefaultOrderBy, StringComparison.OrdinalIgnoreCase) && 
                        x.DeclaringType == t)
                        ?? throw new Exception($"No DefaultOrderBy property {DefaultOrderBy} found on type {t.FullName}.");
                    config._internalTypeConfig.DefaultOrderBy = p.Name;
                }

                foreach (var kvp in Properties)
                    try
                    {
                        kvp.Value.Apply(kvp.Key, config._internalTypeConfig);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error configuring property {kvp.Key} on type {typeof(T).FullName}.", ex);
                    }
            }
        }

        internal void Apply()
        {
            if (DefaultMaxPageSize >= 0)
                GraphqlConfiguration.DefaultMaxPageSize = DefaultMaxPageSize;

            if (BaseConfigurations == null && TypeConfigurations == null)
                return;

            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToArray();
            Func<string, Type> findType = name => 
                allTypes.FirstOrDefault(t => t.AssemblyQualifiedName.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                allTypes.FirstOrDefault(t => t.FullName.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                allTypes.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (BaseConfigurations != null)
            {
                var method = typeof(TypeConfigurationModel).GetMethod(nameof(TypeConfigurationModel.ApplyBase), BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var config in BaseConfigurations)
                {
                    var type = findType(config.Key)
                        ?? throw new Exception($"Type {config.Key} was not found.");
                    try
                    {
                        method.MakeGenericMethod(type).Invoke(config.Value, new object[0]);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error configuring base type {type.FullName}.", ex);
                    }
                }
            }

            if (TypeConfigurations != null)
            {
                var method = typeof(TypeConfigurationModel).GetMethod(nameof(TypeConfigurationModel.Apply), BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var config in TypeConfigurations)
                {
                    var type = findType(config.Key)
                        ?? throw new Exception($"Type {config.Key} was not found.");
                    try
                    {
                        method.MakeGenericMethod(type).Invoke(config.Value, new object[0]);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error configuring type {type.FullName}.", ex);
                    }
                }
            }
        }
    }
}
