using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace VRage.Compiler
{
#if XB1
#else
    public class IlChecker
    {
        public static Dictionary<Type, HashSet<MemberInfo>> AllowedOperands = new Dictionary<Type, HashSet<MemberInfo>>();
        public static Dictionary<Assembly, HashSet<String>> AllowedNamespacesCommon = new Dictionary<Assembly, HashSet<string>>();
        public static Dictionary<Assembly, HashSet<String>> AllowedNamespacesModAPI = new Dictionary<Assembly, HashSet<string>>();

        static IlChecker()
        {
            // Ensure AllowedOperands is not null
            if (AllowedOperands == null) AllowedOperands = new Dictionary<Type, HashSet<MemberInfo>>();

            AllowedOperands.Add(typeof(System.Object), null);
            AllowedOperands.Add(typeof(System.IDisposable), null);

            AllowNamespaceOfTypeCommon(typeof(System.Collections.IEnumerator));
            AllowNamespaceOfTypeCommon(typeof(System.Collections.Generic.IEnumerable<>));
            AllowNamespaceOfTypeCommon(typeof(System.Collections.Generic.HashSet<>));
            AllowNamespaceOfTypeCommon(typeof(System.Collections.Generic.Queue<>));
            AllowNamespaceOfTypeCommon(typeof(System.Collections.Generic.ListExtensions));
            AllowNamespaceOfTypeCommon(typeof(System.Linq.Enumerable));
            AllowNamespaceOfTypeCommon(typeof(System.Text.StringBuilder));
            AllowNamespaceOfTypeCommon(typeof(System.Text.RegularExpressions.Regex));
            AllowNamespaceOfTypeModAPI(typeof(System.Timers.Timer));
            AllowNamespaceOfTypeCommon(typeof(System.Globalization.Calendar));

            //AllowedOperands.Add(typeof(System.MulticastDelegate), null); //delegates allowed directly in checking, delegates are harmless since you have to call or store something in it which is also checked
            AllowedOperands.Add(typeof(System.Text.StringBuilder), null);
            AllowedOperands.Add(typeof(System.String), null);
            AllowedOperands.Add(typeof(System.Math), null);
            AllowedOperands.Add(typeof(System.Enum), null);
            AllowedOperands.Add(typeof(System.Int32), null);
            AllowedOperands.Add(typeof(System.Int16), null);
            AllowedOperands.Add(typeof(System.Int64), null);
            AllowedOperands.Add(typeof(System.UInt32), null);
            AllowedOperands.Add(typeof(System.UInt16), null);
            AllowedOperands.Add(typeof(System.UInt64), null);
            AllowedOperands.Add(typeof(System.Double), null);
            AllowedOperands.Add(typeof(System.Single), null);
            AllowedOperands.Add(typeof(System.Boolean), null);
            AllowedOperands.Add(typeof(System.Char), null);
            AllowedOperands.Add(typeof(System.Byte), null); 
            AllowedOperands.Add(typeof(System.SByte), null);
            AllowedOperands.Add(typeof(System.Decimal), null);
            AllowedOperands.Add(typeof(System.DateTime), null);
            AllowedOperands.Add(typeof(System.TimeSpan), null);
            AllowedOperands.Add(typeof(System.Array),null);
            AllowedOperands.Add(typeof(System.DateTimeOffset), null);

            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlElementAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlAttributeAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlArrayAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlArrayItemAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlAnyAttributeAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlAnyElementAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlAnyElementAttributes), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlArrayItemAttributes), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlAttributeEventArgs), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlAttributeOverrides), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlAttributes), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlChoiceIdentifierAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlElementAttributes), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlElementEventArgs), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlEnumAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlIgnoreAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlIncludeAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlRootAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlTextAttribute), null);
            AllowedOperands.Add(typeof(System.Xml.Serialization.XmlTypeAttribute), null);

            var members = new HashSet<MemberInfo>();
            members.Add(typeof(System.Reflection.MemberInfo).GetProperty("Name").GetGetMethod());
            AllowedOperands.Add(typeof(System.Reflection.MemberInfo), members);
            AllowedOperands.Add(typeof(System.Runtime.CompilerServices.RuntimeHelpers), null);
            AllowedOperands.Add(typeof(System.IO.Stream), null);
            //AllowedOperands.Add(typeof(System.IO.StreamWriter), null);//can be consructed with path
            //AllowedOperands.Add(typeof(System.IO.StreamReader), null);
            AllowedOperands.Add(typeof(System.IO.TextWriter), null);
            AllowedOperands.Add(typeof(System.IO.TextReader), null);
            AllowedOperands.Add(typeof(System.IO.BinaryReader), null);
            AllowedOperands.Add(typeof(System.IO.BinaryWriter), null);
            //AllowedOperands.Add(typeof(System.Runtime.CompilerServices.CompilerHelper), null); // We use this in tests
            members = new HashSet<MemberInfo>();
            members.Add(typeof(Type).GetMethod("GetTypeFromHandle"));
            AllowedOperands.Add(typeof(System.Type), members);

            var rt = typeof(Type).Assembly.GetType("System.RuntimeType");
            AllowedOperands[rt] = new HashSet<MemberInfo>() 
            {
                rt.GetMethod("op_Inequality"),
                rt.GetMethod("GetFields", new Type[] { typeof(System.Reflection.BindingFlags) }),
            };

            AllowedOperands[typeof(Type)] = new HashSet<MemberInfo>()
            {
                typeof(Type).GetMethod("GetFields", new Type[] { typeof(System.Reflection.BindingFlags) }),
                typeof(Type).GetMethod("IsEquivalentTo"),
                typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public),
                typeof(Type).GetMethod("op_Equality"),
            };

            var rtField = typeof(Type).Assembly.GetType("System.Reflection.RtFieldInfo");
            AllowedOperands[rtField] = new HashSet<MemberInfo>()
            {
                rtField.GetMethod("UnsafeGetValue", BindingFlags.NonPublic | BindingFlags.Instance),
            };

            AllowedOperands[typeof(NullReferenceException)] = null;
            AllowedOperands[typeof(ArgumentException)] = null;
            AllowedOperands[typeof(ArgumentNullException)] = null;
            AllowedOperands[typeof(InvalidOperationException)] = null;
            AllowedOperands[typeof(FormatException)] = null;
            AllowedOperands.Add(typeof(System.Exception), null);
            AllowedOperands.Add(typeof(System.DivideByZeroException), null);
            AllowedOperands.Add(typeof(System.InvalidCastException), null);
            AllowedOperands.Add(typeof(System.IO.FileNotFoundException), null);
            AllowedOperands.Add(typeof(NotSupportedException), null);

            var t = typeof(MethodInfo).Assembly.GetType("System.Reflection.RuntimeMethodInfo");
            //AllowedOperands[t] = new List<MemberInfo>() { t.GetMethod("Equals") };
            AllowedOperands[typeof(ValueType)] = new HashSet<MemberInfo>() 
            { 
                typeof(ValueType).GetMethod("Equals"), 
                typeof(ValueType).GetMethod("GetHashCode"),
                typeof(ValueType).GetMethod("ToString"),
                typeof(ValueType).GetMethod("CanCompareBits", BindingFlags.NonPublic | BindingFlags.Static),
                typeof(ValueType).GetMethod("FastEqualsCheck", BindingFlags.NonPublic | BindingFlags.Static),
            };

            var env = typeof(Environment);
            AllowedOperands[env] = new HashSet<MemberInfo>()
            {
                env.GetMethod("GetResourceString", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string), typeof(object[]) }, null),
                env.GetMethod("GetResourceString", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string) }, null),
                // NotSupportedException and CurrentManagedThreadId are required by 'yield return'
                env.GetProperty("CurrentManagedThreadId", BindingFlags.Static | BindingFlags.Public).GetGetMethod(), 
                env.GetProperty("NewLine", BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
                env.GetProperty("ProcessorCount", BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
            };

            AllowedOperands[typeof(System.IO.Path)] = null;
            AllowedOperands[typeof(System.Random)] = null;
            AllowedOperands[typeof(System.Convert)] = null; //Enum needs it, also usefull tool, they can convert object to type but cannot call what isnt whitelisted and Method.Invoke isnt whitelisted
            AllowedOperands.Add(typeof(System.Nullable<>), null);
            AllowedOperands.Add(typeof(StringComparer), null);
            AllowedOperands.Add(typeof(System.IComparable<>), null);

            AllowedOperands.Add(typeof(System.BitConverter), null);     // Useful for serializing custom messages/data in non-xml format
            AllowedOperands.Add(typeof(System.FlagsAttribute), null);
        }

        /// <summary>
        /// Checks if the passed type is valid, ie. not null, assembly is not null and namespace is not null.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if type is valid, false if it isn't.</returns>
        private static bool IsTypeValid(Type type)
        {
            if (type == null)
            {
                System.Diagnostics.Debug.Fail("Invalid type: Type is null!");
                return false;
            }

            if (type.Assembly == null)
            {
                System.Diagnostics.Debug.Fail("Invalid type: Type Assembly is null! (" + type.ToString() + ")");
                return false;
            }

            if (type.Namespace == null)
            {
                System.Diagnostics.Debug.Fail("Invalid type: Type Namespace is null! (" + type.ToString() + ")");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds the namespace of the specified type to the specified dictionary.
        /// </summary>
        /// <param name="type">Type whose namespace needs to be added.</param>
        /// <param name="targetDictionary">Dictionary which needs to be modified.</param>
        /// <returns>True if it was added, false otherwise.</returns>
        private static bool AddNamespaceOfTypeToDictionary(Type type, Dictionary<Assembly, HashSet<String>> targetDictionary)
        {
            // Ensure type is valid
            if (!IsTypeValid(type)) return false;
            if (targetDictionary == null) return false;

            if (!targetDictionary.ContainsKey(type.Assembly) || targetDictionary[type.Assembly] == null)
                targetDictionary.Add(type.Assembly, new HashSet<string>());
            targetDictionary[type.Assembly].Add(type.Namespace);

            return true;
        }

        /// <summary>
        /// Allows the namespace of the type for ModAPI only.
        /// </summary>
        /// <param name="type">Type whose namespace needs to be whitelisted for ModAPI.</param>
        /// <returns>True if it was added, false otherwise.</returns>
        public static bool AllowNamespaceOfTypeModAPI(Type type)
        {
            // Ensure AllowedNamespacesModAPI exists
            if (AllowedNamespacesModAPI == null) AllowedNamespacesModAPI = new Dictionary<Assembly, HashSet<string>>();

            // Add namespace for assembly
            return AddNamespaceOfTypeToDictionary(type, AllowedNamespacesModAPI);
        }

        /// <summary>
        /// Allows the namespace of the type for both ModAPI and ingame programmable blocks.
        /// </summary>
        /// <param name="type">Type whose namespace needs to be whitelisted for common listing.</param>
        /// <returns>True if it was added, false otherwise.</returns>
        public static bool AllowNamespaceOfTypeCommon(Type type)
        {
            // Ensure AllowedNamespacesModAPI exists
            if (AllowedNamespacesCommon == null) AllowedNamespacesCommon = new Dictionary<Assembly, HashSet<string>>();

            // Add namespace for assembly
            return AddNamespaceOfTypeToDictionary(type, AllowedNamespacesCommon);
        }
      
        /// <summary>
        /// Checks list of IL instructions against dangerous types
        /// </summary>
        /// <param name="dangerousTypeNames">Full names of dangerous types</param>
        public static bool CheckIl(List<IlReader.IlInstruction> instructions, out Type failed, bool isIngameScript, Dictionary<Type, HashSet<MemberInfo>> allowedTypes = null)
        {
            failed = null;
            foreach (var pair in allowedTypes) //alllows calls across user scripts
                if (!AllowedOperands.Contains(pair))
                    AllowedOperands.Add(pair.Key, pair.Value);

            foreach (var i in instructions)
            {
                var methodinfo = (i.Operand as MethodInfo);
                if (methodinfo != null && HasMethodInvalidAtrributes(methodinfo.Attributes))
                {
                    return false;
                }
                if (!CheckMember(i.Operand as MemberInfo,isIngameScript) || i.OpCode == System.Reflection.Emit.OpCodes.Calli)
                {
                    failed = ((MemberInfo)i.Operand).DeclaringType;
                    return false;
                }
            }
            return true;
        }

        private static bool CheckMember(MemberInfo memberInfo,bool isIngameScript)
        {
            if (memberInfo == null)
                return true;
            return CheckTypeAndMember(memberInfo.DeclaringType,isIngameScript, memberInfo);
        }

        public static bool CheckTypeAndMember(Type type,bool isIngameScript , MemberInfo memberInfo = null)
        {
            if (type == null) //implicit operators +,-,=,...
                return true;
            if (IsDelegate(type)) //all delegates, asctions, functions
                return true;
            if (!type.IsGenericTypeDefinition && type.IsGenericType && CheckGenericType(type.GetGenericTypeDefinition(), memberInfo,isIngameScript))
                return true;
            if (CheckNamespace(type,isIngameScript) || CheckOperand(type, memberInfo, AllowedOperands))
                return true;
            return false;
        }
        private static bool IsDelegate(Type type)
        {
            var multicastDelType = typeof(MulticastDelegate);
            return multicastDelType.IsAssignableFrom(type.BaseType) || type == multicastDelType || type == multicastDelType.BaseType;
        }

        private static bool CheckNamespace(Type type, bool isIngameScript)
        {
            if (type == null)
                return false;
            bool found = AllowedNamespacesCommon.ContainsKey(type.Assembly) && AllowedNamespacesCommon[type.Assembly].Contains(type.Namespace);
            if (found == false && isIngameScript == false)
            {
                found = AllowedNamespacesModAPI.ContainsKey(type.Assembly) && AllowedNamespacesModAPI[type.Assembly].Contains(type.Namespace);
            }
            return found;
        }

        private static bool CheckOperand(Type type, MemberInfo memberInfo, Dictionary<Type, HashSet<MemberInfo>> op)
        {
            if (op == null)
                return false;
            return op.ContainsKey(type) && (memberInfo == null || op[type] == null || op[type].Contains(memberInfo));
        }

        private static bool CheckGenericType(Type declType, MemberInfo memberInfo, bool isIngameScript)
        {
            if(CheckTypeAndMember(declType,isIngameScript, memberInfo))
            {
                if (memberInfo != null)
                {
                    foreach (var type in memberInfo.DeclaringType.GetGenericArguments())
                    {
                        if (type.IsGenericParameter == false && !CheckTypeAndMember(type,isIngameScript))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public static bool HasMethodInvalidAtrributes(MethodAttributes Attributes)
        {
            return (Attributes & (MethodAttributes.PinvokeImpl | MethodAttributes.UnmanagedExport)) != 0;
        }
        public static bool IsMethodFromParent(Type classType,MethodBase method)
        {
            return classType.IsSubclassOf(method.DeclaringType);
        }

        public static void Clear()
        {
            AllowedOperands.Clear();
            AllowedNamespacesCommon.Clear();
            AllowedNamespacesModAPI.Clear();
        }
    }
#endif
}

