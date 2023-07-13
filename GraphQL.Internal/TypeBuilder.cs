using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Mt.GraphQL.Internal
{
    internal class TypeBuilder
    {
        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly ConcurrentDictionary<string, Type> _builtTypes;

        static TypeBuilder()
        {
            var assemblyName = new AssemblyName($"{typeof(TypeBuilder).FullName}.Dynamic");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            _builtTypes = new ConcurrentDictionary<string, Type>();
        }

        public static Type GetType((string name, Type type)[] properties)
        {
            var name = properties.Aggregate(string.Empty, (v, pi) => v + $"{pi.type.FullName} {pi.name};");
            return _builtTypes.GetOrAdd(name, _ => BuildType(properties));
        }

        private static Type BuildType((string name, Type type)[] properties)
        {
            var typename = $"T{Guid.NewGuid():N}";
            var builder = _moduleBuilder.DefineType(typename);
            var fields = new List<FieldInfo>();
            foreach (var (name, type) in properties)
            {
                var fld = builder.DefineField($"_{name[..1].ToLower()}{name[1..]}", type, FieldAttributes.Private);
                fields.Add(fld);
                var propBuilder = builder.DefineProperty(name, PropertyAttributes.HasDefault, type, new Type[0]);
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
                propBuilder.SetGetMethod(getMethodBuilder);
                propBuilder.SetSetMethod(setMethodBuilder);
            }

            var ctor = builder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                properties.Select(p => p.type).ToArray());
            {
                var ilGen = ctor.GetILGenerator();
                for (int i = 0; i < properties.Length; i++)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldarg, i + 1);
                    ilGen.Emit(OpCodes.Stfld, fields[i]);
                }
                ilGen.Emit(OpCodes.Ret);
            }

            return builder.CreateType();
        }
    }
}
