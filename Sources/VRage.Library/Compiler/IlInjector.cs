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
                CopyConstructors(createdConstructors, typePair.Key, typePair.Value, typeLookup);
                CopyMethods(createdMethods, typePair.Key, typePair.Value, typeLookup);
                CopyProperties(createdMethods, typePair.Key, typePair.Value, typeLookup);
            }

            foreach (var newMethod in createdMethods)
            {
                InjectMethod(newMethod.Key, newMethod.Value.GetILGenerator(), createdFields, createdMethods, createdConstructors, methodToInject,methodToInjectMethodCheck, typeLookup);
            }
            foreach (var newConstructor in createdConstructors)
            {
                InjectMethod(newConstructor.Key, newConstructor.Value.GetILGenerator(), createdFields, createdMethods, createdConstructors, methodToInject,methodToInjectMethodCheck, typeLookup);
            }

            // Once everything is hooked up, we can create our types.
            CreateTypesInOrder(typeLookup);
        }

        private static void CreateTypesInOrder(Dictionary<Type, TypeBuilder> typeLookup)
        {
            // Note that if a type A has a field of type B and B is a value type, then B MUST be created before A. This is
            // presumably so the compiler knows the size of the type in memory. Since value types cannot circularly contain
            // each other this should always be resolvable.
            // If a type fails to create due to a dependency it may render the entire assembly unusable, so let any
            // exceptions bubble up.

            while(typeLookup.Count > 0)
            {
                var pair = typeLookup.First();
                CreateDependencies(typeLookup, pair.Key, pair.Value);
            }
        }

        private static void CreateDependencies(Dictionary<Type, TypeBuilder> typeLookup, Type sourceType, TypeBuilder newType)
        {
            // Infinite recursion should be impossible since value types cannot contain themselves and for
            // reference types we do not recurse, but for added safety we remove the type first.
            typeLookup.Remove(sourceType);

            var fields = sourceType.GetFields(BindingFlags.Static |BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField | BindingFlags.Instance);
            foreach(var field in fields)
            {
                if(!field.FieldType.IsValueType) continue; // If it's not a value type we don't need to create it yet.
                TypeBuilder newFieldType;
                if(!typeLookup.TryGetValue(field.FieldType, out newFieldType)) continue; // Already created, or not one we're building.
                
                CreateDependencies(typeLookup, field.FieldType, newFieldType);
            }
            newType.CreateType();
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
            if(type.HasElementType)
            {
                var elementType = MaybeSubstituteType(typeLookup, type.GetElementType());
                if(elementType == type.GetElementType()) return type;

                if(type.IsByRef) return elementType.MakeByRefType();
                if(type.IsArray) return elementType.MakeArrayType(type.GetArrayRank());

                // We never expect to see this, but completeness...
                if(type.IsPointer) return elementType.MakePointerType();

                Debug.Fail(String.Format("Type {0} claimed HasElementType but is not a pointer, array, or ByRef.", type));
            }

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
                if (field.DeclaringType != sourceType)
                {
                    continue;
                }
                
                createdFields.Add(field, newType.DefineField(field.Name, MaybeSubstituteType(typeLookup, field.FieldType), field.Attributes));
            }
        }
        private static void CopyProperties(Dictionary<MethodInfo, MethodBuilder> createdMethods, Type sourceType, TypeBuilder newType, Dictionary<Type, TypeBuilder> typeLookup)
        {
            var properties = sourceType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.DeclaringType != sourceType)
                {
                    continue;
                }
                
                var definedProperty = newType.DefineProperty(property.Name, property.Attributes, MaybeSubstituteType(typeLookup, property.PropertyType), Type.EmptyTypes);
                if(property.GetGetMethod(true) != null)
                {
                    MethodBuilder getter;
                    if(createdMethods.TryGetValue(property.GetGetMethod(true), out getter)) definedProperty.SetGetMethod(getter);
                }
                if(property.GetSetMethod(true) != null)
                {
                    MethodBuilder setter;
                    if(createdMethods.TryGetValue(property.GetSetMethod(true), out setter)) definedProperty.SetSetMethod(setter);
                }
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
                for(var i = 0; i < parameters.Length; i++)
                {
                    parameterTypes[i] = MaybeSubstituteType(typeLookup, parameters[i].ParameterType);
                }
                
                var definedMethod = newType.DefineMethod(method.Name, method.Attributes, method.CallingConvention, MaybeSubstituteType(typeLookup, method.ReturnType), parameterTypes);
                if(method.IsGenericMethodDefinition)
                {
                    var genericArgs = method.GetGenericArguments();
                    var names = genericArgs.Select(a => a.Name).ToArray();
                    var definedGenericArgs = definedMethod.DefineGenericParameters(names);
                    for(var i = 0; i < genericArgs.Length; i++)
                    {
                        var a = genericArgs[i];
                        var d = definedGenericArgs[i];
                        if(a.BaseType != null && a.BaseType != typeof(object)) d.SetBaseTypeConstraint(a.BaseType);
                    }
                }
                
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
                        ResolveField(instruction.Operand as FieldInfo, typeLookup, createdFields, methodGenerator, code);
                        break;
                    case OperandType.InlineMethod:
                        try
                        {
                            ResolveMethodOrConstructor(methodGenerator, typeLookup, createdMethods, createdConstructors, instruction, code);
                        }
                        catch
                        {
                            ResolveField(instruction.Operand as FieldInfo, typeLookup, createdFields, methodGenerator, code);
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

        private static void ResolveMethodOrConstructor(ILGenerator generator, Dictionary<Type, TypeBuilder> typeLookup, Dictionary<MethodInfo, MethodBuilder> methods, Dictionary<ConstructorInfo, ConstructorBuilder> constructors, VRage.Compiler.IlReader.IlInstruction instruction, System.Reflection.Emit.OpCode code)
        {
            if (instruction.Operand is MethodInfo)
            {
                var actualMethod = ResolveMethodInfo(typeLookup, methods, (MethodInfo)instruction.Operand);
                generator.Emit(code, actualMethod);
                return;
            }
            if (instruction.Operand is ConstructorInfo)
            {
                var actualConstructor = ResolveConstructorInfo(typeLookup, constructors, (ConstructorInfo)instruction.Operand);
                generator.Emit(code, actualConstructor);
                return;
            }
        }

        private static MethodInfo ResolveMethodInfo(Dictionary<Type, TypeBuilder> typeLookup, Dictionary<MethodInfo, MethodBuilder> methods, MethodInfo method)
        {
            // It's unlikely any of this will work properly with generic types specified in the script.
            // Should be fine with List<T> and its ilk, though.

            if (!method.IsGenericMethod)
            {
                // Fast path. Non-generic method defined inside the script.
                MethodBuilder replacementMethod;
                if (methods.TryGetValue(method, out replacementMethod)) return replacementMethod;
                
                // If the declaring type does not depend on script-defined types, we're done here.
                Type replacementDeclaringType;
                if(!NeedsDeclaringTypeSubstitution(typeLookup, method, out replacementDeclaringType)) return method;
            
                // Method on a generic type which depends on script-defined types. Identify the method's declaration
                // on the type's definition, then resolve it within the context of the replacement type.
                var declaredMethod = (MethodInfo)method.DeclaringType.Module.ResolveMethod(method.MetadataToken);
                return TypeBuilder.GetMethod(replacementDeclaringType, declaredMethod);
            }
            else
            {
                // Generic method. Rewrite type parameters before continuing:
                var args = method.GetGenericArguments();
                var types = new Type[args.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    types[i] = MaybeSubstituteType(typeLookup, args[i]);
                }
                var methodDefinition = method.GetGenericMethodDefinition();
                MethodBuilder replacementMethod;
                MethodInfo rewrittenMethod;
                if (methods.TryGetValue(methodDefinition, out replacementMethod))
                {
                    rewrittenMethod = replacementMethod.MakeGenericMethod(types);
                }
                else 
                {
                    rewrittenMethod = methodDefinition.MakeGenericMethod(types);
                }

                // If the declaring type does not depend on script-defined types, we're done here.
                Type replacementDeclaringType;
                if(!NeedsDeclaringTypeSubstitution(typeLookup, rewrittenMethod, out replacementDeclaringType)) return rewrittenMethod;
            
                // Possibly-generic method on a generic type which depends on script-defined types. We have already
                // dealt with any generic parameters on the method itself. Now we must identify the method's declaration
                // on the type's definition, then resolve it within the context of the replacement type.

                Debug.Assert(replacementDeclaringType.IsGenericType);
            
                var declaredMethod = (MethodInfo)rewrittenMethod.DeclaringType.Module.ResolveMethod(rewrittenMethod.MetadataToken);
                return TypeBuilder.GetMethod(replacementDeclaringType, declaredMethod);
            }
        }

        private static ConstructorInfo ResolveConstructorInfo(Dictionary<Type, TypeBuilder> typeLookup, Dictionary<ConstructorInfo, ConstructorBuilder> constructors, ConstructorInfo constructor)
        {
            // It's unlikely any of this will work properly with generic types defined in the script.
            // Should be fine with List<T> and its ilk, though.

            // Fast path. Constructor defined inside the script.
            ConstructorBuilder replacementConstructor;
            if(constructors.TryGetValue(constructor, out replacementConstructor)) return replacementConstructor;

            // If the declaring type does not depend on script-defined types, we're done here.
            Type replacementDeclaringType;
            if(!NeedsDeclaringTypeSubstitution(typeLookup, constructor, out replacementDeclaringType)) return constructor;
            
            // Constructor on a generic type which depends on script-defined types. We must identify the constructor's
            // declaration on the type's definition, then resolve it within the context of the replacement type.

            Debug.Assert(replacementDeclaringType.IsGenericType);
            
            var declaredConstructor = (ConstructorInfo)constructor.DeclaringType.Module.ResolveMethod(constructor.MetadataToken);
            return TypeBuilder.GetConstructor(replacementDeclaringType, declaredConstructor);
        }

        private static void ResolveLocalVariable(ILGenerator generator, Dictionary<Type, TypeBuilder> typeLookup)
        {
            foreach (LocalVariableInfo local in m_reader.Locals)
            {
                generator.DeclareLocal(MaybeSubstituteType(typeLookup, local.LocalType));
            }
        }
        private static void ResolveField(FieldInfo field, Dictionary<Type, TypeBuilder> typeLookup, Dictionary<FieldInfo, FieldBuilder> fields, ILGenerator generator, OpCode code)
        {
            var actualField = ResolveFieldInfo(typeLookup, fields, field);
            generator.Emit(code, actualField);
        }    
        private static FieldInfo ResolveFieldInfo(Dictionary<Type, TypeBuilder> typeLookup, Dictionary<FieldInfo, FieldBuilder> fields, FieldInfo field)
        {
            // Fast path. Field defined inside the script.
            FieldBuilder replacementField;
            if(fields.TryGetValue(field, out replacementField)) return replacementField;
            
            // If the declaring type does not depend on script-defined types, we're done here.
            Type replacementDeclaringType;
            if(!NeedsDeclaringTypeSubstitution(typeLookup, field, out replacementDeclaringType)) return field;
            
            Debug.Assert(replacementDeclaringType.IsGenericType);
            
            var declaredField = field.DeclaringType.Module.ResolveField(field.MetadataToken);
            return TypeBuilder.GetField(replacementDeclaringType, declaredField);
        }    

        private static bool NeedsDeclaringTypeSubstitution(Dictionary<Type, TypeBuilder> typeLookup, MemberInfo member, out Type replacementDeclaringType)
        {
            replacementDeclaringType = MaybeSubstituteType(typeLookup, member.DeclaringType);
            if(replacementDeclaringType == member.DeclaringType) return false; // No substitution required.
            return true;
        }
    }
}
