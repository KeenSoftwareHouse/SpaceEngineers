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
            Dictionary<MethodInfo, MethodBuilder> createdMethods = new Dictionary<MethodInfo, MethodBuilder>(InstanceComparer<MethodInfo>.Default);
            Dictionary<Type, TypeBuilder> typeLookup = new Dictionary<Type, TypeBuilder>();
            Dictionary<ConstructorInfo, ConstructorBuilder> createdConstructors = new Dictionary<ConstructorInfo, ConstructorBuilder>();
            Dictionary<FieldInfo, FieldBuilder> createdFields = new Dictionary<FieldInfo, FieldBuilder>();

            // Create all types first.
            foreach (var sourceType in GetTypesOrderedByGeneration(sourceTypes))
            {
                CreateType(newModule, typeLookup, sourceType);
            }

            // Once we are able to resolve every source type to its replacement we can copy
            // the members across, replacing type usages as we go:
            foreach (var typePair in typeLookup)
            {
                CopyFields(createdFields, typePair.Key, typePair.Value, typeLookup);
                CopyProperties(typePair.Key, typePair.Value, typeLookup);
                CopyConstructors(createdConstructors, typePair.Key, typePair.Value, typeLookup);
                CopyMethods(createdMethods, typePair.Key, typePair.Value, typeLookup);
            }

            foreach (var type in typeLookup)
            {
                foreach (var newMethod in createdMethods)
                {
                    if (newMethod.Value.DeclaringType == type.Value)
                    {
                        InjectMethod(newMethod.Key, newMethod.Value.GetILGenerator(), createdFields, createdMethods, createdConstructors, methodToInject,methodToInjectMethodCheck, typeLookup);
                    }
                }
                foreach (var newConstructor in createdConstructors)
                {
                    if (newConstructor.Value.DeclaringType == type.Value)
                    {
                        InjectMethod(newConstructor.Key, newConstructor.Value.GetILGenerator(), createdFields, createdMethods, createdConstructors, methodToInject,methodToInjectMethodCheck, typeLookup);
                    }
                }
                type.Value.CreateType();
            }
        }

        private static TypeBuilder CreateType(ModuleBuilder newModule, Dictionary<Type, TypeBuilder> typeLookup, Type sourceType)
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
            var baseType = MaybeSubstituteType(typeLookup, sourceType.BaseType);

            // If any of the interfaces of this type is from a type created in-game, it must be replaced with the new type.
            var interfaceTypes = sourceType.GetInterfaces().ToArray();
            for (var index = 0; index < interfaceTypes.Length; index++)
            {
                interfaceTypes[index] = MaybeSubstituteType(typeLookup, interfaceTypes[index]);
            }

            TypeBuilder newType = newModule.DefineType(sourceType.Name, attributes, baseType, interfaceTypes);
            typeLookup.Add(sourceType, newType);
            return newType;
        }
        private static Type MaybeSubstituteType(Dictionary<Type, TypeBuilder> typeLookup, Type type)
        {
            if(type == null) return null;
            if(!type.IsGenericTypeDefinition && type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    // Argument hierarchy is bounded and acyclic, so this cannot recurse infinitely:
                    genericArguments[i] = MaybeSubstituteType(typeLookup, genericArguments[i]);
                }
                // Generic definition is a single type, so this cannot recurse infinitely:
                var definition = MaybeSubstituteType(typeLookup, type.GetGenericTypeDefinition());
                return definition.MakeGenericType(genericArguments);
            }

            TypeBuilder replacementType;
            if(typeLookup.TryGetValue(type, out replacementType)) return replacementType;
            return type;
        }
        private static void CopyFields(Dictionary<FieldInfo, FieldBuilder> createdFields, Type sourceType, TypeBuilder newType, Dictionary<Type, TypeBuilder> typeLookup)
        {
            var fields = sourceType.GetFields(BindingFlags.Static |BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField | BindingFlags.Instance);
            foreach (var field in fields)
            {
                createdFields.Add(field, newType.DefineField(field.Name, MaybeSubstituteType(typeLookup, field.FieldType), field.Attributes));
            }
        }
        private static void CopyProperties(Type sourceType, TypeBuilder newType, Dictionary<Type, TypeBuilder> typeLookup)
        {
            var properties = sourceType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Instance);
            foreach (var property in properties)
            {
                newType.DefineProperty(property.Name, PropertyAttributes.HasDefault, MaybeSubstituteType(typeLookup, property.PropertyType), Type.EmptyTypes);
            }
        }
        private static void CopyMethods(Dictionary<MethodInfo, MethodBuilder> createdMethods, Type type, TypeBuilder newType, Dictionary<Type, TypeBuilder> typeLookup)
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
                    parameterTypes[i++] = MaybeSubstituteType(typeLookup, parameter.ParameterType);
                }
                
                var definedMethod = newType.DefineMethod(method.Name, method.Attributes, method.CallingConvention, MaybeSubstituteType(typeLookup, method.ReturnType), parameterTypes);

                createdMethods.Add(method, definedMethod);
            }
        }
        private static void CopyConstructors(Dictionary<ConstructorInfo, ConstructorBuilder> createdConstructors, Type type, TypeBuilder newType, Dictionary<Type, TypeBuilder> typeLookup)
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
                    paramaterTypes[i++] = MaybeSubstituteType(typeLookup, parameter.ParameterType);
                }
                createdConstructors.Add(method, newType.DefineConstructor(method.Attributes, method.CallingConvention, paramaterTypes));
            }
        }

		private static void InjectMethod(MethodBase sourceMethod, ILGenerator methodGenerator, Dictionary<FieldInfo, FieldBuilder> fields, Dictionary<MethodInfo, MethodBuilder> methods, Dictionary<ConstructorInfo, ConstructorBuilder> constructors, MethodInfo methodToInject, MethodInfo methodToInjectMethodCheck, Dictionary<Type, TypeBuilder> typeLookup)
        {
            ConstructInstructions(sourceMethod, methodGenerator, fields, methods, constructors, methodToInject ,methodToInjectMethodCheck, typeLookup);
        }

        private static void ConstructInstructions(MethodBase sourceMethod, ILGenerator methodGenerator, Dictionary<FieldInfo, FieldBuilder> createdFields, Dictionary<MethodInfo, MethodBuilder> createdMethods, Dictionary<ConstructorInfo, ConstructorBuilder> createdConstructors, MethodInfo methodToInject, MethodInfo methodToInjectMethodCheck, Dictionary<Type, TypeBuilder> typeLookup)
        {
            List<VRage.Compiler.IlReader.IlInstruction> instructions = m_reader.ReadInstructions(sourceMethod);
            ResolveLocalVariable(methodGenerator, typeLookup);

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
                            ResolveMethodOrConstructor(methodGenerator, createdMethods, createdConstructors, instruction, code);
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
                            methodGenerator.Emit(code, MaybeSubstituteType(typeLookup, type));
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

        private static void ResolveMethodOrConstructor(ILGenerator generator, Dictionary<MethodInfo, MethodBuilder> methods, Dictionary<ConstructorInfo, ConstructorBuilder> constructors, VRage.Compiler.IlReader.IlInstruction instruction, System.Reflection.Emit.OpCode code)
        {
            if (instruction.Operand is MethodInfo)
            {
                var actualMethod = ResolveMethodInfo(methods, (MethodInfo)instruction.Operand);
                generator.Emit(code, actualMethod);
                return;
            }
            if (instruction.Operand is ConstructorInfo)
            {
                var actualConstructor = ResolveConstructorInfo(constructors, (ConstructorInfo)instruction.Operand);
                generator.Emit(code, actualConstructor);
                return;
            }
        }

        private static MethodInfo ResolveMethodInfo(Dictionary<MethodInfo, MethodBuilder> methods, MethodInfo method)
        {
            MethodBuilder replacementMethod;
            if(methods.TryGetValue(method, out replacementMethod)) return replacementMethod;
            return method;
        }
        private static ConstructorInfo ResolveConstructorInfo(Dictionary<ConstructorInfo, ConstructorBuilder> constructors, ConstructorInfo constructor)
        {
            ConstructorBuilder replacementConstructor;
            if(constructors.TryGetValue(constructor, out replacementConstructor)) return replacementConstructor;
            return constructor;
        }

        private static void ResolveLocalVariable(ILGenerator generator, Dictionary<Type, TypeBuilder> typeLookup)
        {
            foreach (LocalVariableInfo local in m_reader.Locals)
            {
                generator.DeclareLocal(MaybeSubstituteType(typeLookup, local.LocalType));
            }
        }
        private static void ResolveField(FieldInfo field, Dictionary<FieldInfo, FieldBuilder> fields, ILGenerator generator, OpCode code)
        {
            var actualField = ResolveFieldInfo(fields, field);
            generator.Emit(code, actualField);
        }    
        private static FieldInfo ResolveFieldInfo(Dictionary<FieldInfo, FieldBuilder> fields, FieldInfo field)
        {
            FieldBuilder replacementField;
            if(fields.TryGetValue(field, out replacementField)) return replacementField;
            return field;
        }    
    }
}
