using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Game.Entity;
using VRageMath;

namespace VRage.Game.VisualScripting
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class VisualScriptingMiscData : Attribute
    {
        public readonly string Group;
        public readonly string Comment;

        public VisualScriptingMiscData(string group, string comment = null)
        {
            Group = group;
            Comment = comment;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class VisualScriptingMember : Attribute
    {
        public readonly bool Sequential;
        public readonly bool Reserved;
        public VisualScriptingMember(bool isSequenceDependent = false, bool reserved = false)
        {
            Sequential = isSequenceDependent;
            Reserved = reserved;
        }
    }

    [AttributeUsage(AttributeTargets.Delegate, AllowMultiple = true)]
    public class VisualScriptingEvent : Attribute
    {
        public readonly bool [] IsKey;

        public bool HasKeys { get
        {
            foreach (bool b in IsKey)
                if(b) return true;

            return false;
        }
        }

        public VisualScriptingEvent(bool firstParam = false)
        {
            IsKey = new [] {firstParam};
        }

        public VisualScriptingEvent(bool [] @params)
        {
            IsKey = @params;
        }
    }

    public static class MyVisualScriptingProxy
    {
        #region Internals

        private static readonly Dictionary<string, MethodInfo> m_visualScriptingMethodsBySignature = new Dictionary<string, MethodInfo>();
        private static readonly Dictionary<Type, HashSet<MethodInfo>>  m_whitelistedMethods = new Dictionary<Type, HashSet<MethodInfo>>();
        private static readonly Dictionary<MethodInfo, bool> m_whitelistedMethodsSequenceDependency = new Dictionary<MethodInfo, bool>(); 
        private static readonly Dictionary<string,FieldInfo> m_visualScriptingEventFields = new Dictionary<string, FieldInfo>();
        private static readonly Dictionary<string,Type> m_registeredTypes = new Dictionary<string, Type>(); 
        private static readonly List<Type> m_supportedTypes = new List<Type>();
        private static bool m_initialized = false;

        public static List<FieldInfo> EventFields { get { return m_visualScriptingEventFields.Values.ToList(); } }

        public static List<Type> SupportedTypes
        {
            get { return m_supportedTypes; }
        }

        /// <summary>
        /// Loads reflection data.
        /// </summary>
        public static void Init()
        {
            if (m_initialized)
                return;

            m_supportedTypes.Add(typeof(int));
            m_supportedTypes.Add(typeof(float));
            m_supportedTypes.Add(typeof(string));
            m_supportedTypes.Add(typeof(Vector3D));
            m_supportedTypes.Add(typeof(bool));
            m_supportedTypes.Add(typeof(long));
            m_supportedTypes.Add(typeof(List<bool>));
            m_supportedTypes.Add(typeof(List<int>));
            m_supportedTypes.Add(typeof(List<float>));
            m_supportedTypes.Add(typeof(List<string>));
            m_supportedTypes.Add(typeof(List<long>));
            m_supportedTypes.Add(typeof(List<MyEntity>));
            m_supportedTypes.Add(typeof(MyEntity));

            MyVisualScriptLogicProvider.Init();

            m_initialized = true;
        }

        private static void RegisterMethod(Type declaringType, MethodInfo method, VisualScriptingMember attribute, bool ?overrideSequenceDependency = null)
        {
            if (declaringType.IsGenericType)
            {
                declaringType = declaringType.GetGenericTypeDefinition();
            }

            if (!m_whitelistedMethods.ContainsKey(declaringType))
            {
                m_whitelistedMethods[declaringType] = new HashSet<MethodInfo>();
            }
            // Register current type
            m_whitelistedMethods[declaringType].Add(method);
            m_whitelistedMethodsSequenceDependency[method] = overrideSequenceDependency ?? attribute.Sequential;

            // Add to base types
            foreach (var data in m_whitelistedMethods)
            {
                if (data.Key.IsAssignableFrom(declaringType))
                {
                    // Add methods of base type to derived types
                    data.Value.Add(method);
                }
                else if(declaringType.IsAssignableFrom(data.Key))
                {
                    // Add methods of base types
                    var set = m_whitelistedMethods[declaringType];
                    foreach (var info in data.Value)
                    {
                        set.Add(info);
                    }
                }
            }
        }

        public static void RegisterType(Type type)
        {
            var signature = type.Signature();
            if(m_registeredTypes.ContainsKey(signature)) return;

            m_registeredTypes.Add(signature, type);
        }

        public static void WhitelistExtensions(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var methodInfo in methods)
            {
                var vsAttribute = methodInfo.GetCustomAttribute<VisualScriptingMember>();
                if (vsAttribute != null && methodInfo.IsDefined(typeof(ExtensionAttribute), false))
                {
                    var declaringType = methodInfo.GetParameters()[0].ParameterType;
                    RegisterMethod(declaringType, methodInfo, vsAttribute);
                }
            }

            m_registeredTypes[type.Signature()] = type;
        }

        public static void WhitelistMethod(MethodInfo method, bool sequenceDependent)
        {
            var declaringType = method.DeclaringType;
            if(declaringType == null) return;
            RegisterMethod(declaringType, method, null, sequenceDependent);
        }

        public static IEnumerable<MethodInfo> GetWhitelistedMethods(Type type)
        {
            if(type == null) return null;

            // Try looking for specific type
            HashSet<MethodInfo> res;
            if (m_whitelistedMethods.TryGetValue(type, out res))
            {
                return res;
            }

            // not found, try looking for generic 
            if (type.IsGenericType)
            {
                // Get generic type
                var genericType = type.GetGenericTypeDefinition();
                // Get type parameters
                var typeParameter = type.GetGenericArguments();
                // Add specific version of generic to whitelisted
                if(m_whitelistedMethods.TryGetValue(genericType, out res))
                {
                    var wlMethods = new HashSet<MethodInfo>();
                    m_whitelistedMethods[type] = wlMethods;

                    // Create specific versions of all methods 
                    foreach (var genericMethodInfo in res)
                    {
                        MethodInfo specificMethod = null;
                        if (genericMethodInfo.IsDefined(typeof(ExtensionAttribute)))
                        {
                            specificMethod = genericMethodInfo.MakeGenericMethod(typeParameter);
                        }
                        else
                        {
                            // Todo: Create a better recreation of method infos from generic method infos
                            specificMethod = type.GetMethod(genericMethodInfo.Name);
                        }
                        wlMethods.Add(specificMethod);
                        // Add sequence dependency information
                        var sequenceDependent = m_whitelistedMethodsSequenceDependency[genericMethodInfo];
                        m_whitelistedMethodsSequenceDependency[specificMethod] = sequenceDependent;
                        // Register method signature in signature dictionary
                        m_visualScriptingMethodsBySignature[specificMethod.Signature()] = specificMethod;
                    }

                    return wlMethods;
                }
            }

            return null;
        } 

        public static void RegisterLogicProvider(Type type)
        {
            var methods = type.GetMethods();

            foreach (var methodInfo in methods)
            {
                var attribute = methodInfo.GetCustomAttribute<VisualScriptingMember>();
                if (attribute != null)
                {
                    var signature = methodInfo.Signature();
                    if (!m_visualScriptingMethodsBySignature.ContainsKey(signature))
                        m_visualScriptingMethodsBySignature.Add(signature, methodInfo);
                }
            }

            var fields = type.GetFields();
            foreach (var fieldInfo in fields)
            {
                var attribute = fieldInfo.FieldType.GetCustomAttribute<VisualScriptingEvent>();
                if (attribute != null && fieldInfo.FieldType.IsSubclassOf(typeof(MulticastDelegate)))
                    if (!m_visualScriptingEventFields.ContainsKey(fieldInfo.Signature()))
                        m_visualScriptingEventFields.Add(fieldInfo.Signature(), fieldInfo);
            }
        }

        /// <summary>
        /// Looks for given type using executing assembly.
        /// </summary>
        /// <param name="typeFullName"></param>
        /// <returns></returns>
        public static Type GetType(string typeFullName)
        {
            if(typeFullName == null || typeFullName.Length == 0)
                Debugger.Break();
            
            Type type;
            if(m_registeredTypes.TryGetValue(typeFullName, out type))
                return type;

            type = Type.GetType(typeFullName);
            if (type != null) return type;
            var assembly = typeof(Vector3D).Assembly;
            type = assembly.GetType(typeFullName);
            return type;
        }

        /// <summary>
        /// Looks for methodInfo about method with given signature.
        /// </summary>
        /// <param name="signature">Full signature of a method.</param>
        /// <returns>null if not found.</returns>
        public static MethodInfo GetMethod(string signature)
        {
            MethodInfo info;
            m_visualScriptingMethodsBySignature.TryGetValue(signature, out info);
            return info;
        }

        public static MethodInfo GetMethod(Type type, string signature)
        {
            // Type was not whitelisted and though it is not in registered in signatures yet.
            if (!m_whitelistedMethods.ContainsKey(type))
            {
                // Only whitelisted types can be 
                // used + this methods registers the signatures
                GetWhitelistedMethods(type);
           } 

            return GetMethod(signature);
        }

        /// <summary>
        /// All attributed methods from VisualScriptingProxy.
        /// </summary>
        /// <returns></returns>
        public static List<MethodInfo> GetMethods()
        {
            var results = new List<MethodInfo>();

            foreach (var keyValuePair in m_visualScriptingMethodsBySignature)
            {
                results.Add(keyValuePair.Value);
            }

            return results;
        }

        /// <summary>
        /// Returns event field with specified signature.
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        public static FieldInfo GetField(string signature)
        {
            FieldInfo info;
            m_visualScriptingEventFields.TryGetValue(signature, out info);
            return info;
        }

        public static string Signature(this FieldInfo info)
        {
            return info.DeclaringType.Namespace + "." + info.DeclaringType.Name + "." + info.Name;
        }

        public static bool TryToRecoverMethodInfo(ref string oldSignature, Type declaringType, Type extensionType, out MethodInfo info)
        {
            info = null;

            var index = 0;
            for(;
                index < oldSignature.Length && index < declaringType.FullName.Length &&
                oldSignature[index] == declaringType.FullName[index];
                index++)
            {    
            }

            oldSignature = oldSignature.Remove(0, index + 1);
            oldSignature = oldSignature.Remove(oldSignature.IndexOf('('));

            if (extensionType != null && extensionType.IsGenericType)
            {
                var args = extensionType.GetGenericArguments();
                var genericMethod = declaringType.GetMethod(oldSignature);
                if(genericMethod != null)
                {
                    info = genericMethod.MakeGenericMethod(args);
                }
            }
            else
            {
                info = declaringType.GetMethod(oldSignature);
            }

            if (info != null)
            {
                oldSignature = info.Signature();
            }

            return info != null;
        }

        public static string Signature(this MethodInfo info)
        {
            var sb = new StringBuilder(info.DeclaringType.Signature());
            var parameters = info.GetParameters();

            sb.Append('.').Append(info.Name).Append('(');

            for (var index = 0; index < parameters.Length; index++)
            {
                if(parameters[index].ParameterType.IsGenericType)
                {
                    sb.Append(parameters[index].ParameterType.Signature());
                }
                else
                {
                    sb.Append(parameters[index].ParameterType.Name);
                }

                sb.Append(' ').Append(parameters[index].Name);
                if(index < parameters.Length - 1)
                    sb.Append(", ");
            }
            sb.Append(')');

            return sb.ToString();
        }

        public static string MethodGroup(this MethodInfo info)
        {
            var attribute = info.GetCustomAttribute<VisualScriptingMiscData>();
            if(attribute != null)
                return attribute.Group;

            return null;
        }

        public static string Signature(this Type type)
        {
            return type.FullName;
        }

        public static bool IsSequenceDependent(this MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<VisualScriptingMember>();
            if (attribute == null && !method.IsStatic)
            {
                bool sequenceDependent = true;
                if(m_whitelistedMethodsSequenceDependency.TryGetValue(method, out sequenceDependent))
                    return sequenceDependent;

                return true;
            }

            return attribute == null || attribute.Sequential;
        }

        public static string ReadableName(this Type type)
        {
            if (type == null)
            {
                Debugger.Break();
                return null;
            }

            if (type == typeof(bool))
                return "Bool";
            if (type == typeof(int))
                return "Int";
            if (type == typeof(string))
                return "String";
            if (type == typeof(float))
                return "Float";
            if (type == typeof(long))
                return "Long";
            if (type.IsGenericType)
            {
                var nameSb = new StringBuilder(type.Name.Remove(type.Name.IndexOf('`')));
                var genericArguments = type.GetGenericArguments();

                nameSb.Append(" - ");
                foreach (var argument in genericArguments)
                {
                    nameSb.Append(argument.ReadableName());
                    nameSb.Append(",");
                }

                nameSb.Remove(nameSb.Length-1,1);
                return nameSb.ToString();
            }


            return type.Name;
        }

        #endregion
    }
}
