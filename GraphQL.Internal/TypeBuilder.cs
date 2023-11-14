using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace Mt.GraphQL.Internal
{
    internal class TypeBuilder
    {
        public static string Namespace { get; }
        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly ConcurrentDictionary<string, Type> _builtTypes;

        static TypeBuilder()
        {
            Namespace = $"{typeof(TypeBuilder).FullName}.Dynamic";
            var assemblyName = new AssemblyName(Namespace);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            _builtTypes = new ConcurrentDictionary<string, Type>();
        }

        public static Type GetType(string typeName, (string name, Type type, Expression[] attributes)[] properties)
        {
            var name = properties.Aggregate($"{typeName} ", (v, pi) => v + $"{pi.type.FullName} {pi.name};");
            return _builtTypes.GetOrAdd(name, _ => BuildType(typeName, properties));
        }

        private static Type BuildType(string typeName, (string name, Type type, Expression[] attributes)[] properties)
        {
            var typename = $"T{Guid.NewGuid():N}";
            var builder = _moduleBuilder.DefineType(typename);

            // Set XmlType attribute to serialize to correct type name when serializing to XML
            builder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    typeof(XmlTypeAttribute).GetConstructor(new Type[0]),
                    new object[0],
                    new[] { typeof(XmlTypeAttribute).GetProperty(nameof(XmlTypeAttribute.TypeName)) },
                    new object[] { typeName }));

            var fields = new List<FieldInfo>();
            foreach (var (name, type, attributes) in properties)
            {
                var fld = builder.DefineField($"_{name.Substring(0, 1).ToLower()}{name.Substring(1)}", type, FieldAttributes.Private);
                fields.Add(fld);
                var propBuilder = builder.DefineProperty(name, PropertyAttributes.HasDefault, type, new Type[0]);

                foreach (var attr in attributes)
                {
                    CustomAttributeBuilder attributeBuilder;
                    switch (attr)
                    {
                        case NewExpression n:
                            attributeBuilder = new CustomAttributeBuilder(n.Constructor, getConstructorArguments(n));
                            break;
                        case MemberInitExpression i:
                            var bindings = i.Bindings.Cast<MemberAssignment>();
                            attributeBuilder = new CustomAttributeBuilder(
                                i.NewExpression.Constructor, 
                                getConstructorArguments(i.NewExpression),
                                bindings.Select(b => b.Member).Cast<PropertyInfo>().ToArray(),
                                bindings.Select(b => b.Expression).Cast<ConstantExpression>().Select(c => c.Value).ToArray());
                            break;
                        default:
                            throw new NotImplementedException($"Attribute type {attr.GetType().Name} is not implemented.");
                    }

                    propBuilder.SetCustomAttribute(attributeBuilder);

                    object[] getConstructorArguments(NewExpression newExpression) =>
                        newExpression.Arguments
                            .Select(
                                arg =>
                                {
                                    if (!(arg is ConstantExpression c))
                                        throw new ArgumentException($"Expression {arg} is not a ConstantExpression.");
                                    return c.Value;
                                })
                            .ToArray();
                }

                var getMethodBuilder = builder.DefineMethod(
                    $"get_{name}", 
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    type, new Type[0]);
                {
                    var ilGen = getMethodBuilder.GetILGenerator();
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, fld);
                    ilGen.Emit(OpCodes.Ret);
                }
                propBuilder.SetGetMethod(getMethodBuilder);

                var setMethodBuilder = builder.DefineMethod(
                    $"set_{name}", 
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    null, new[] { type });
                {
                    var ilGen = setMethodBuilder.GetILGenerator();
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(OpCodes.Stfld, fld);
                    ilGen.Emit(OpCodes.Ret);
                }
                propBuilder.SetSetMethod(setMethodBuilder);
            }

            return builder.CreateTypeInfo().AsType();
        }
    }
}
