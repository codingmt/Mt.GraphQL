using Mt.GraphQL.Internal;
using System;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Lets the client know that a model's property is an extension property. This means that it is not returned by the server unless explicitly selected using Select() or Extend().
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExtensionAttribute : ExtensionAttributeBase
    { }
}
