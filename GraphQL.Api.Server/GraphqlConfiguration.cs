using System;
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
        /// Configures a type for GraphQL.
        /// </summary>
        public static TypeConfiguration<T> Configure<T>() where T : class =>
            new TypeConfiguration<T>(InternalConfig.GetTypeConfiguration<T>(true));

        /// <summary>
        /// Configures a type for GraphQL.
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

        /// <summary>
        /// Excludes a property or nested property from the GraphQL model.
        /// </summary>
        /// <typeparam name="TProperty">The type of property.</typeparam>
        /// <param name="property">The property selector.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public TypeConfiguration<T> ExcludeProperty<TProperty>(Expression<Func<T, TProperty>> property)
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

            _internalTypeConfig.ExcludeColumn(member.Substring(1));

            return this;
        }

        /// <summary>
        /// Configures a property as fit for filtering and ordering.
        /// </summary>
        /// <typeparam name="TProperty">The type of property.</typeparam>
        /// <typeparam name="TAttribute">The type of attribute.</typeparam>
        /// <param name="property">The property selector.</param>
        /// <param name="attribute"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
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
}
