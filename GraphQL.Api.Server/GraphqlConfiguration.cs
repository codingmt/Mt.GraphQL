using System;
using System.Linq.Expressions;
using InternalConfig = Mt.GraphQL.Internal.Configuration;

namespace Mt.GraphQL.Api.Server
{
    /// <summary>
    /// Configures which types can be used for GraphQL and which properties can be used for filtering and ordering.
    /// </summary>
    public static class GraphqlConfiguration
    {
        /// <summary>
        /// Configures a type for GraphQL.
        /// </summary>
        public static TypeConfiguration<T> Configure<T>() where T : class =>
            new TypeConfiguration<T>(InternalConfig.GetTypeConfiguration<T>(true));
    }

    /// <summary>
    /// The configuration of a specific type.
    /// </summary>
    public class TypeConfiguration<T> where T : class
    {
        private readonly InternalConfig.InternalTypeConfig _internalTypeConfig;

        internal TypeConfiguration(InternalConfig.InternalTypeConfig internalTypeConfig)
        {
            _internalTypeConfig = internalTypeConfig;
        }

        /// <summary>
        /// Configures a property as fit for filtering and ordering.
        /// </summary>
        /// <typeparam name="TProperty">The type of property.</typeparam>
        /// <param name="property">The property selector.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public TypeConfiguration<T> AllowFilteringAndSorting<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if (!(property.Body is MemberExpression m) ||
                (m.Expression != property.Parameters[0]))
                throw new ArgumentException($"Argument property must select a property on type {typeof(T).Name}.");

            _internalTypeConfig.SetColumnIsIndexed(m.Member.Name);

            return this;
        }
    }
}
