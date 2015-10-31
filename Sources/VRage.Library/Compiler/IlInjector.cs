﻿using System;
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

                // Search through the list and make sure this type is added before any type deriving from it, or any nested types within it.
                var node = sortedList.First;
                while (true)
                {
                    if (node == null)
                    {
                        sortedList.AddLast(currentType);
                        break;
                    }
                    if (currentType.IsAssignableFrom(node.Value) || node.Value.IsNested && node.Value.DeclaringType == currentType)
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
            Dictionary<string, Type> typeLookup = new Dictionary<string, Type>();
            Dictionary<ConstructorBuilder, ConstructorInfo> createdConstructors = new Dictionary<ConstructorBuilder, ConstructorInfo>();
            List<FieldBuilder> createdFields = new List<FieldBuilder>();

            // Generate the type lookup table
            foreach (var sourceType in GetTypesOrderedByGeneration(sourceTypes))
            {
                CreateType(newModule, createdTypes, typeLookup, sourceType);
            }

            // Copy methods
            foreach (var pair in createdTypes)
            {
                var newType = pair.Key;
                var sourceType = pair.Value;
                CopyFields(createdFields, sourceType, newType);
                CopyProperties(sourceType, newType);
                CopyConstructors(createdConstructors, sourceType, newType);
                CopyMethods(createdMethods, sourceType, newType, typeLookup);
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

        private static TypeBuilder CreateType(ModuleBuilder newModule, Dictionary<TypeBuilder, Type> createdTypes, Dictionary<string, Type> typeLookup, Type sourceType)
        {
            var attributes = sourceType.Attributes;

            // If this type derives from a type created in-game, it must be replaced with the new type.
            var baseType = sourceType.BaseType;
            if (baseType != null && typeLookup.ContainsKey(baseType.FullName))
            {
                Type newBaseType;
                if (typeLookup.TryGetValue(baseType.FullName, out newBaseType))
                {
                    baseType = newBaseType;
                }
            }

            // If any of the interfaces of this type is from a type created in-game, it must be replaced with the new type.
            var interfaceTypes = sourceType.GetInterfaces().ToArray();
            for (var index = 0; index < interfaceTypes.Length; index++)
            {
                Type newInterfaceType;
                if (typeLookup.TryGetValue(interfaceTypes[index].FullName, out newInterfaceType))
                {
                    interfaceTypes[index] = newInterfaceType;
                }
            }

            TypeBuilder newType;
            Type declaringType;
            // To avoid duplicate type names, we must make sure we duplicate type nesting as well.
            if (sourceType.IsNested && typeLookup.TryGetValue(sourceType.DeclaringType.FullName, out declaringType) && declaringType is TypeBuilder)
            {
                newType = ((TypeBuilder)declaringType).DefineNestedType(sourceType.Name, attributes, baseType, interfaceTypes);
            }
            else
            {
                newType = newModule.DefineType(sourceType.Name, attributes, baseType, interfaceTypes);
            }

            createdTypes.Add(newType, sourceType);
            typeLookup.Add(newType.FullName, newType);
            return newType;
        }
        private static void CopyFields(List<FieldBuilder> createdFields, Type sourceType, TypeBuilder newType)
        {
            var fields = sourceType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField | BindingFlags.Instance);
            foreach (var field in fields)
            {
                createdFields.Add(newType.DefineField(field.Name, field.FieldType, field.Attributes));
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
        private static void CopyMethods(Dictionary<MethodBuilder, MethodInfo> createdMethods, Type type, TypeBuilder newType, Dictionary<string, Type> typeLookup)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var method in methods)
            {
                if (method.DeclaringType != type)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                Type[] parameterTypes = new Type[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    parameterTypes[i] = parameters[i].ParameterType;
                }

                var definedMethod = newType.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, parameterTypes);
                if (method.IsGenericMethodDefinition)
                {
                    var typeParameters = method.GetGenericArguments();
                    var definedTypeParameters = definedMethod.DefineGenericParameters(typeParameters.Select(t => t.Name).ToArray());
                    for (var i = 0; i < definedTypeParameters.Length; i++)
                    {
                        var definedTypeParameter = definedTypeParameters[i];
                        var typeParameter = typeParameters[i];

                        definedTypeParameter.SetGenericParameterAttributes(typeParameter.GenericParameterAttributes);
                        var parameterConstraints = typeParameter.GetGenericParameterConstraints();
                        var baseConstraint = parameterConstraints.SingleOrDefault(c => c.IsClass);
                        var interfaceConstraints = parameterConstraints.Where(c => c.IsInterface).ToArray();

                        // Replace constraints to local types
                        if (baseConstraint != null)
                        {
                            Type replacedType;
                            if (typeLookup.TryGetValue(baseConstraint.FullName, out replacedType))
                                baseConstraint = replacedType;
                            definedTypeParameter.SetBaseTypeConstraint(baseConstraint);
                        }
                        if (interfaceConstraints.Length > 0)
                        {
                            for (int j = 0; j < interfaceConstraints.Length; j++)
                            {
                                Type replacedType;
                                if (typeLookup.TryGetValue(interfaceConstraints[j].FullName, out replacedType))
                                    interfaceConstraints[j] = replacedType;
                            }
                            definedTypeParameter.SetInterfaceConstraints(interfaceConstraints);
                        }
                    }
                }

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

		private static void InjectMethod(MethodBase sourceMethod, ILGenerator methodGenerator, List<FieldBuilder> fields, Dictionary<MethodBuilder, MethodInfo> methods, Dictionary<ConstructorBuilder, ConstructorInfo> constructors, Dictionary<TypeBuilder, Type> types, MethodInfo methodToInject, MethodInfo methodToInjectMethodCheck, Dictionary<string, Type> typeLookup)
        {
            ConstructInstructions(sourceMethod, methodGenerator, fields, methods, constructors, types, methodToInject ,methodToInjectMethodCheck, typeLookup);
        }

        private static void ConstructInstructions(MethodBase sourceMethod, ILGenerator methodGenerator, List<FieldBuilder> createdFields, Dictionary<MethodBuilder, MethodInfo> createdMethods, Dictionary<ConstructorBuilder, ConstructorInfo> createdConstructors, Dictionary<TypeBuilder, Type> createdTypes, MethodInfo methodToInject, MethodInfo methodToInjectMethodCheck, Dictionary<string, Type> typeLookup)
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
                            ResolveMethod(methodGenerator, createdMethods, createdConstructors, instruction, code, typeLookup);
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
                            Type typeBuilder;
                            // Make sure the type is replaced with the regenerated type if required.
                            if (!type.IsGenericParameter && typeLookup.TryGetValue(type.FullName, out typeBuilder))
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

        private static void ResolveMethod(ILGenerator generator, Dictionary<MethodBuilder, MethodInfo> methods, Dictionary<ConstructorBuilder, ConstructorInfo> constructors, VRage.Compiler.IlReader.IlInstruction instruction, System.Reflection.Emit.OpCode code, Dictionary<string, Type> typeLookup)
        {
            if (instruction.Operand is MethodInfo)
            {
                var methodInfo = instruction.Operand as MethodInfo;
                if (methodInfo.DeclaringType.IsGenericType)
                {
                    Type genericTypeDefinition;
                    var declaringType = ResolveGenericType(typeLookup, methodInfo, out genericTypeDefinition);

                    if (declaringType != null)
                    {
                        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                        if (methodInfo.IsStatic)
                            flags |= BindingFlags.Static;
                        else
                            flags |= BindingFlags.Instance;

                        var genericMethod = genericTypeDefinition.GetMethods(flags).Single(m => m.MetadataToken == methodInfo.MetadataToken);
                        methodInfo = TypeBuilder.GetMethod(declaringType, genericMethod);
                    }
                }
                
                // Handle generic calls
                if (methodInfo.IsGenericMethod)
                {
                    var methodDefinitionInfo = methodInfo.GetGenericMethodDefinition();
                    var mustRegenerateMethod = false;

                    // See if we already have the method definition
                    foreach (var met in methods)
                    {
                        if (met.Value == methodDefinitionInfo)
                        {
                            methodDefinitionInfo = met.Key;
                            mustRegenerateMethod = true;
                            break;
                        }
                    }
                    
                    // Analyze all the generics arguments and replace as needed
                    var genericArguments = methodInfo.GetGenericArguments();
                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        if (genericArguments[i].IsGenericParameter)
                            continue;
                        Type genericArgumentTypeBuilder;
                        if (typeLookup.TryGetValue(genericArguments[i].FullName, out genericArgumentTypeBuilder))
                        {
                            mustRegenerateMethod = true;
                            genericArguments[i] = genericArgumentTypeBuilder;
                        }
                    }

                    if (mustRegenerateMethod)
                    {
                        methodDefinitionInfo = methods.Where(m => m.Value == methodDefinitionInfo).Select(m => m.Key).SingleOrDefault() ?? methodDefinitionInfo;
                        generator.Emit(code, methodDefinitionInfo.MakeGenericMethod(genericArguments));
                        return;
                    }
                }
                else
                {
                    foreach (var met in methods)
                    {
                        if (met.Value == methodInfo)
                        {
                            generator.Emit(code, met.Key);
                            return;
                        }
                    }
                }
            
                generator.Emit(code, methodInfo);
                return;
            }
            
            if (instruction.Operand is ConstructorInfo)
            {
                var constructorInfo = instruction.Operand as ConstructorInfo;
                if (constructorInfo.DeclaringType.IsGenericType)
                {
                    Type genericTypeDefinition;
                    var declaringType = ResolveGenericType(typeLookup, constructorInfo, out genericTypeDefinition);

                    if (declaringType != null)
                    {
                        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                        if (constructorInfo.IsStatic)
                            flags |= BindingFlags.Static;
                        else
                            flags |= BindingFlags.Instance;

                        var genericMethod = genericTypeDefinition.GetConstructors(flags).Single(m => m.MetadataToken == constructorInfo.MetadataToken);
                        constructorInfo = TypeBuilder.GetConstructor(declaringType, genericMethod);
                    }
                }
                foreach (var met in constructors)
                {
                    if (met.Value == constructorInfo)
                    {
                        generator.Emit(code, met.Key);
                        return;
                    }
                }
            
                generator.Emit(code, constructorInfo);
                return;
            }
        }

        private static Type ResolveGenericType(Dictionary<string, Type> typeLookup, MethodBase methodInfo, out Type genericTypeDefinition)
        {
            Type declaringType;
            genericTypeDefinition = methodInfo.DeclaringType.GetGenericTypeDefinition();
            if (!typeLookup.TryGetValue(methodInfo.DeclaringType.FullName, out declaringType))
            {
                var mustRegenerateMethodInfo = false;
                var genericParameters = methodInfo.DeclaringType.GetGenericArguments();
                for (var i = 0; i < genericParameters.Length; i++)
                {
                    Type genericArgumentTypeBuilder;
                    if (typeLookup.TryGetValue(genericParameters[i].FullName, out genericArgumentTypeBuilder))
                    {
                        genericParameters[i] = genericArgumentTypeBuilder;
                        mustRegenerateMethodInfo = true;
                    }
                }
                if (mustRegenerateMethodInfo)
                {
                    declaringType = genericTypeDefinition.MakeGenericType(genericParameters);
                    typeLookup[methodInfo.DeclaringType.FullName] = declaringType;
                }
            }
            return declaringType;
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
                    generator.Emit(code, newField);
                    break;
                }
            }
        }
    }
}
