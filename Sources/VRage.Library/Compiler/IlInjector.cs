using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using VRage.Library.Utils;

namespace VRage.Compiler
{
    public class ScriptOutOfRangeException : Exception
    {
    }

    public class IlInjector
    {
        static int m_numInstructions = 0;
        static int m_numMaxInstructions = 0;

        public static void RestartCountingInstructions(int maxInstructions)
        {
            m_numInstructions = 0;
            m_numMaxInstructions = maxInstructions;
        }

        public static void CountInstructions()
        {
            m_numInstructions++;
            if (m_numInstructions > m_numMaxInstructions)
            {
                throw new ScriptOutOfRangeException();
            }
        }

		static int m_numMethodCalls = 0;
		static int m_numMaxMethodCalls= 0;

		public static void RestartCountingMethods(int maxMethodCalls)
		{
			m_numMethodCalls = 0;
			m_numMaxMethodCalls = maxMethodCalls;
		}

		public static void CountMethodCalls()
		{
			m_numMethodCalls++;
			if (m_numMethodCalls > m_numMaxMethodCalls)
			{
				throw new ScriptOutOfRangeException();
			}
		}

        private static IlReader m_reader = new IlReader();

        public static Assembly InjectCodeToAssembly(string newAssemblyName, Assembly inputAssembly, MethodInfo method,MethodInfo methodToInjectMethodCheck, bool save = false)
        {
            AssemblyName assemblyName = new AssemblyName(newAssemblyName);

            AssemblyBuilder newAssembly = Thread.GetDomain().DefineDynamicAssembly(assemblyName, save ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);

            ModuleBuilder newModule;
            if (save)
            {
                newModule = newAssembly.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
            }
            else
            {
                newModule = newAssembly.DefineDynamicModule(assemblyName.Name);
            }
			InjectTypes(inputAssembly.GetTypes(), newModule, method, methodToInjectMethodCheck);

            if (save)
            {
                newAssembly.Save(assemblyName.Name + ".dll");
            }

            return newAssembly;
        }

        private static IEnumerable<Type> GetTypesOrderedByGeneration(Type[] sourceTypes)
        {
            var queue = new Queue<Type>(sourceTypes);
            var sortedList = new LinkedList<Type>();

            while (queue.Count > 0)
            {
                var currentType = queue.Dequeue();

                // Search through the list and make sure this type is added before any type deriving from it.
                var node = sortedList.First;
                while (true)
                {
                    if (node == null)
                    {
                        sortedList.AddLast(currentType);
                        break;
                    }
                    if (currentType.IsAssignableFrom(node.Value))
                    {
                        sortedList.AddBefore(node, currentType);
                        break;
                    }
                    node = node.Next;
                }
            }
            return sortedList;
        }

        private static void InjectTypes(Type[] sourceTypes, ModuleBuilder newModule, MethodInfo methodToInject,MethodInfo methodToInjectMethodCheck)
        {

            Dictionary<TypeBuilder, Type> createdTypes = new Dictionary<TypeBuilder, Type>();
            Dictionary<MethodBuilder, MethodInfo> createdMethods = new Dictionary<MethodBuilder, MethodInfo>(InstanceComparer<MethodBuilder>.Default);
            Dictionary<string, TypeBuilder> typeLookup = new Dictionary<string, TypeBuilder>();
            Dictionary<ConstructorBuilder, ConstructorInfo> createdConstructors = new Dictionary<ConstructorBuilder, ConstructorInfo>();
            List<FieldBuilder> createdFields = new List<FieldBuilder>();

            foreach (var sourceType in GetTypesOrderedByGeneration(sourceTypes))
            {
                TypeBuilder newType = CreateType(newModule, createdTypes, typeLookup, sourceType);

                CopyFields(createdFields, sourceType, newType, createdTypes);
                CopyProperties(sourceType, newType);
                CopyConstructors(createdConstructors, sourceType, newType);
                CopyMethods(createdMethods, sourceType, newType);
            }

            foreach (var type in createdTypes)
            {
                foreach (var newMethod in createdMethods)
                {
                    if (newMethod.Key.DeclaringType == type.Key)
                    {
                        InjectMethod(newMethod.Value, newMethod.Key.GetILGenerator(), createdFields, createdMethods, createdConstructors, createdTypes, methodToInject,methodToInjectMethodCheck, typeLookup);
                    }
                }
                foreach (var newConstructor in createdConstructors)
                {
                    if (newConstructor.Key.DeclaringType == type.Key)
                    {
                        InjectMethod(newConstructor.Value, newConstructor.Key.GetILGenerator(), createdFields, createdMethods, createdConstructors, createdTypes, methodToInject,methodToInjectMethodCheck, typeLookup);
                    }
                }
                type.Key.CreateType();
            }
        }

        private static TypeBuilder CreateType(ModuleBuilder newModule, Dictionary<TypeBuilder, Type> createdTypes, Dictionary<string, TypeBuilder> typeLookup, Type sourceType)
        {
            var attributes = sourceType.Attributes;
            if ((attributes & TypeAttributes.NestedPublic) == TypeAttributes.NestedPublic)
            {
                attributes &= ~TypeAttributes.NestedPublic;
                attributes |= TypeAttributes.Public;
            }

            if ((attributes & TypeAttributes.NestedPrivate) == TypeAttributes.NestedPrivate)
            {
                attributes &= ~TypeAttributes.NestedPrivate;
                attributes |= TypeAttributes.NotPublic;
            }

            // If this type derives from a type created in-game, it must be replaced with the new type.
            var baseType = sourceType.BaseType;
            if (baseType != null && typeLookup.ContainsKey(baseType.Name))
            {
                TypeBuilder newBaseType;
                if (typeLookup.TryGetValue(baseType.Name, out newBaseType))
                {
                    baseType = newBaseType;
                }
            }

            // If any of the interfaces of this type is from a type created in-game, it must be replaced with the new type.
            var interfaceTypes = sourceType.GetInterfaces().ToArray();
            for (var index = 0; index < interfaceTypes.Length; index++)
            {
                TypeBuilder newInterfaceType;
                if (typeLookup.TryGetValue(interfaceTypes[index].Name, out newInterfaceType))
                {
                    interfaceTypes[index] = newInterfaceType;
                }
            }
            TypeBuilder newType = newModule.DefineType(sourceType.Name, attributes, baseType, interfaceTypes);
            if (sourceType.IsEnum)
            {
                // If this is an enum, we need to define a special field which defines the base type of this enum.
                var typeField = sourceType.GetField("value__", BindingFlags.Public | BindingFlags.Instance);
                newType.DefineField(typeField.Name, typeField.FieldType, typeField.Attributes);
            }
            createdTypes.Add(newType, sourceType);
            typeLookup.Add(newType.FullName, newType);
            return newType;
        }
        private static void CopyFields(List<FieldBuilder> createdFields, Type sourceType, TypeBuilder newType, Dictionary<TypeBuilder, Type> createdTypes)
        {
            var fields = sourceType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // The type designation field for enums has already been copied
                if (sourceType.IsEnum && field.Name == "value__")
                    continue;

                // Resolve the correct types
                var resolvedFieldType = createdTypes
                    .Where(pair => pair.Value == field.FieldType)
                    .Select(t => (Type)t.Key).FirstOrDefault()
                    ?? field.FieldType;
                var newField = newType.DefineField(field.Name, resolvedFieldType, field.Attributes);
                if (newType.IsEnum && field.IsStatic)
                {
                    // Copy the constant value to enable correct output for enum ToString()
                    newField.SetConstant(field.GetRawConstantValue());
                }
            }
        }
        private static void CopyProperties(Type sourceType, TypeBuilder newType)
        {
            var properties = sourceType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Instance);
            foreach (var property in properties)
            {
                newType.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.PropertyType, Type.EmptyTypes);
            }
        }
        private static void CopyMethods(Dictionary<MethodBuilder, MethodInfo> createdMethods, Type type, TypeBuilder newType)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic|BindingFlags.Static);

            foreach (var method in methods)
            {
                if (method.DeclaringType != type)
                {
                    continue;
                }
                
                var parameters = method.GetParameters();
                Type[] parameterTypes = new Type[parameters.Length];
                int i = 0;
                foreach (var parameter in parameters)
                {
                    parameterTypes[i++] = parameter.ParameterType;
                }
                
                var definedMethod = newType.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, parameterTypes);

                createdMethods.Add(definedMethod, method);
            }
        }
        private static void CopyConstructors(Dictionary<ConstructorBuilder, ConstructorInfo> createdConstructors, Type type, TypeBuilder newType)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var method in constructors)
            {
                if (method.DeclaringType != type)
                {
                    continue;
                }
                var parameters = method.GetParameters();
                Type[] paramaterTypes = new Type[parameters.Length];
                int i = 0;
                foreach (var parameter in parameters)
                {
                    paramaterTypes[i++] = parameter.ParameterType;
                }
                createdConstructors.Add(newType.DefineConstructor(method.Attributes, method.CallingConvention, paramaterTypes), method);
            }
        }

		private static void InjectMethod(MethodBase sourceMethod, ILGenerator methodGenerator, List<FieldBuilder> fields, Dictionary<MethodBuilder, MethodInfo> methods, Dictionary<ConstructorBuilder, ConstructorInfo> constructors, Dictionary<TypeBuilder, Type> types, MethodInfo methodToInject, MethodInfo methodToInjectMethodCheck, Dictionary<string, TypeBuilder> typeLookup)
        {
            ConstructInstructions(sourceMethod, methodGenerator, fields, methods, constructors, types, methodToInject ,methodToInjectMethodCheck, typeLookup);
        }

        private static void ConstructInstructions(MethodBase sourceMethod, ILGenerator methodGenerator, List<FieldBuilder> createdFields, Dictionary<MethodBuilder, MethodInfo> createdMethods, Dictionary<ConstructorBuilder, ConstructorInfo> createdConstructors, Dictionary<TypeBuilder, Type> createdTypes, MethodInfo methodToInject, MethodInfo methodToInjectMethodCheck, Dictionary<string, TypeBuilder> typeLookup)
        {
            List<VRage.Compiler.IlReader.IlInstruction> instructions = m_reader.ReadInstructions(sourceMethod);
            ResolveTypes(methodGenerator, createdTypes);

            Dictionary<long, Label> labels = new Dictionary<long, Label>();
            foreach (VRage.Compiler.IlReader.IlInstruction instr in instructions)
            {
                labels[instr.Offset] = methodGenerator.DefineLabel();
            }
			methodGenerator.Emit(OpCodes.Call, methodToInjectMethodCheck);
            methodGenerator.Emit(OpCodes.Call, methodToInject);
            // get the operation code of the current instruction
            foreach (var instruction in instructions)
            {
                methodGenerator.MarkLabel(labels[instruction.Offset]);
                var code = instruction.OpCode;

                if (code == OpCodes.Switch)
                {
                    methodGenerator.Emit(OpCodes.Call, methodToInject); 
                    var op = (instruction.Operand as int[]).Select(off => labels[off]).ToArray(); 
                    methodGenerator.Emit(OpCodes.Switch, op); 
                } 
                else if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                {
                    code = SwitchShortOpCodes(code);
                    methodGenerator.Emit(OpCodes.Call, methodToInject);
                    methodGenerator.Emit(code, labels[Convert.ToInt32(instruction.Operand)]);
                    continue;
                }
                switch (instruction.OpCode.OperandType)
                {
                    case OperandType.InlineField:
                        ResolveField(instruction.Operand as FieldInfo, createdFields, methodGenerator, code);
                        break;
                    case OperandType.InlineMethod:
                        try
                        {
                            ResolveMethod(methodGenerator, createdMethods, createdConstructors, instruction, code);
                        }
                        catch
                        {
                            ResolveField(instruction.Operand as FieldInfo, createdFields, methodGenerator, code);
                        }
                        break;
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        try
                        {
                            var type = instruction.Operand as Type;
                            TypeBuilder typeBuilder;
                            // Make sure the type is replaced with the regenerated type if required.
                            if (typeLookup.TryGetValue(type.Name, out typeBuilder))
                            {
                                methodGenerator.Emit(code, typeBuilder);
                            }
                            else
                            {
                                methodGenerator.Emit(code, type);
                            }
                        }
                        catch
                        {

                        }
                        break;
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineSig:
                    case OperandType.InlineI:
                        {
                            methodGenerator.Emit(code, Convert.ToInt32(instruction.Operand));
                            break;
                        }
                    case OperandType.InlineI8:
                        {
                            methodGenerator.Emit(code, Convert.ToInt64(instruction.Operand));
                            break;
                        }
                    case OperandType.InlineNone:
                        {
                            methodGenerator.Emit(code);
                            break;
                        }
                    case OperandType.InlineR:
                        {
                            methodGenerator.Emit(code, Convert.ToDouble(instruction.Operand));
                            break;
                        }
                    case OperandType.InlineString:
                        {
                            methodGenerator.Emit(code, instruction.Operand as string);
                            break;
                        }
                    case OperandType.InlineSwitch:
                        {
                            break;
                        }
                    case OperandType.InlineVar:
                        {
                            methodGenerator.Emit(code, Convert.ToUInt16(instruction.LocalVariableIndex));
                            break;
                        }

                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                        {
                            methodGenerator.Emit(code, Convert.ToSByte(instruction.Operand));
                            break;
                        }
                    case OperandType.ShortInlineR:
                        {
                            methodGenerator.Emit(code, Convert.ToSingle(instruction.Operand));
                            break;
                        }
                    case OperandType.ShortInlineVar:
                        {
                            methodGenerator.Emit(code, Convert.ToByte(instruction.LocalVariableIndex));
                            break;
                        }
                    default:
                        {
                            throw new Exception("Unknown operand type.");
                        }
                }
            }
        }

        private static System.Reflection.Emit.OpCode SwitchShortOpCodes(System.Reflection.Emit.OpCode code)       
        {
            if (code == OpCodes.Bge_Un_S)
            {
                code = OpCodes.Bge_Un;
            }
            if (code == OpCodes.Bne_Un_S)
            {
                code = OpCodes.Bne_Un;
            }
            if (code == OpCodes.Ble_Un_S)
            {
                code = OpCodes.Ble_Un;
            }
            if (code == OpCodes.Ble_S)
            {
                code = OpCodes.Ble;
            }
            if (code == OpCodes.Blt_S)
            {
                code = OpCodes.Blt;
            }
            if (code == OpCodes.Blt_Un_S)
            {
                code = OpCodes.Blt_Un;
            }
            if (code == OpCodes.Beq_S)
            {
                code = OpCodes.Beq;
            }
            if (code == OpCodes.Br_S)
            {
                code = OpCodes.Br;
            }
            if (code == OpCodes.Brtrue_S)
            {
                code = OpCodes.Brtrue;
            }
            if (code == OpCodes.Brfalse_S)
            {
                code = OpCodes.Brfalse;
            }
            if (code == OpCodes.Leave_S)
            {
                code = OpCodes.Leave;
            }
            if (code == OpCodes.Bge_S)
            {
                code = OpCodes.Bge;
            }
            return code;
        }

        private static void ResolveMethod(ILGenerator generator, Dictionary<MethodBuilder, MethodInfo> methods, Dictionary<ConstructorBuilder, ConstructorInfo> constructors, VRage.Compiler.IlReader.IlInstruction instruction, System.Reflection.Emit.OpCode code)
        {
            bool found = false;
            var method = instruction.Operand as MethodBase;
            if (instruction.Operand is MethodInfo)
            {
                var methodInfo = instruction.Operand as MethodInfo;
                foreach (var met in methods)
                {
                    if (met.Value == methodInfo)
                    {
                        generator.Emit(code, met.Key);
                        found = true;
                        break;
                    }
                }
            }
            if (instruction.Operand is ConstructorInfo)
            {
                var methodInfo = instruction.Operand as ConstructorInfo;
                foreach (var met in constructors)
                {
                    if (met.Value == methodInfo)
                    {
                        generator.Emit(code, met.Key);
                        found = true;
                        break;
                    }
                }
            }
            if (false == found)
            {
                if (method is MethodInfo)
                {
                    generator.Emit(code, method as MethodInfo);
                }
                else if (method is ConstructorInfo)
                {
                    generator.Emit(code, method as ConstructorInfo);
                }
            }
        }
        private static void ResolveTypes(ILGenerator generator, Dictionary<TypeBuilder, Type> types)
        {
            foreach (LocalVariableInfo local in m_reader.Locals)
            {
                bool found = false;
                foreach (var type in types)
                {
                    if (type.Value == local.LocalType)
                    {
                        generator.DeclareLocal(type.Key);
                        found = true;
                        break;
                    }
                }
                if (found == false)
                {
                    generator.DeclareLocal(local.LocalType);
                }
            }
        }
        private static void ResolveField(FieldInfo field, List<FieldBuilder> fields, ILGenerator generator, OpCode code)
        {
            foreach (var newField in fields)
            {
                if (newField.DeclaringType.Name == field.DeclaringType.Name && newField.Name == field.Name)
                {
                    // We found a replacement field reference
                    generator.Emit(code, newField);
                    return;
                }
            }
            // Generate the exact field reference
            generator.Emit(code, field);
        }
    }
}
