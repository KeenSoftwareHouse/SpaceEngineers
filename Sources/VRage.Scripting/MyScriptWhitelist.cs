using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sandbox.ModAPI;
using VRage.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VRage.Scripting
{
    /// <summary>
    /// Exceptions during registration of whitelisted type members
    /// </summary>
    [Serializable]
    public class MyWhitelistException : Exception
    {
        public MyWhitelistException()
        {}

        public MyWhitelistException(string message) : base(message)
        {}

        public MyWhitelistException(string message, Exception inner) : base(message, inner)
        {}

        protected MyWhitelistException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {}
    }

    /// <summary>
    ///     The script whitelist contains information about which types and type members are allowed in the
    ///     various types of scripts.
    /// </summary>
    public class MyScriptWhitelist : IMyScriptBlacklist
    {
        readonly HashSet<string> m_ingameBlacklist = new HashSet<string>();
        readonly MyScriptCompiler m_scriptCompiler;
        readonly Dictionary<string, MyWhitelistTarget> m_whitelist = new Dictionary<string, MyWhitelistTarget>();

        public MyScriptWhitelist(MyScriptCompiler scriptCompiler)
        {
            m_scriptCompiler = scriptCompiler;
            using (var handle = this.OpenBatch())
            {
                handle.AllowNamespaceOfTypes(MyWhitelistTarget.Both,
                    typeof(System.Collections.IEnumerator),
                    typeof(System.Collections.Generic.IEnumerable<>),
                    typeof(System.Collections.Generic.HashSet<>),
                    typeof(System.Collections.Generic.Queue<>),
                    typeof(System.Collections.Concurrent.ConcurrentDictionary<,>),
                    typeof(System.Collections.Concurrent.ConcurrentBag<>),
                    typeof(System.Linq.Enumerable),
                    typeof(System.Text.StringBuilder),
                    typeof(System.Text.RegularExpressions.Regex),
                    typeof(System.Globalization.Calendar)
                );

                // Are we _sure_ about this one? Seems scary to say the least...
                handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi, typeof(System.Timers.Timer));

                handle.AllowTypes(MyWhitelistTarget.ModApi,
                    typeof(System.Diagnostics.TraceEventType),
                    typeof(AssemblyProductAttribute),
                    typeof(AssemblyDescriptionAttribute),
                    typeof(AssemblyConfigurationAttribute),
                    typeof(AssemblyCompanyAttribute),
                    typeof(AssemblyCultureAttribute),
                    typeof(AssemblyVersionAttribute),
                    typeof(AssemblyFileVersionAttribute),
                    typeof(AssemblyCopyrightAttribute),
                    typeof(AssemblyTrademarkAttribute),
                    typeof(AssemblyTitleAttribute),
                    typeof(ComVisibleAttribute),
                    typeof(DefaultValueAttribute),
                    typeof(SerializableAttribute),
                    typeof(GuidAttribute),
                    typeof(StructLayoutAttribute),
                    typeof(LayoutKind),
                    typeof(Guid)
                );
                
                // TODO: Evaluate whether any of the following may be better off whitelisted for modAPI only

                handle.AllowTypes(MyWhitelistTarget.Both,
                    //typeof(System.MulticastDelegate), //delegates allowed directly in checking, delegates are harmless since you have to call or store something in it which is also checked
                    typeof(object),
                    typeof(System.IDisposable),
                    typeof(string),
                    typeof(System.StringComparison),
                    typeof(System.Math),
                    typeof(System.Enum),
                    typeof(int),
                    typeof(short),
                    typeof(long),
                    typeof(uint),
                    typeof(ushort),
                    typeof(ulong),
                    typeof(double),
                    typeof(float),
                    typeof(bool),
                    typeof(char),
                    typeof(byte),
                    typeof(sbyte),
                    typeof(decimal),
                    typeof(System.DateTime),
                    typeof(System.TimeSpan),
                    typeof(System.Array),
                    typeof(System.Xml.Serialization.XmlElementAttribute),
                    typeof(System.Xml.Serialization.XmlAttributeAttribute),
                    typeof(System.Xml.Serialization.XmlArrayAttribute),
                    typeof(System.Xml.Serialization.XmlArrayItemAttribute),
                    typeof(System.Xml.Serialization.XmlAnyAttributeAttribute),
                    typeof(System.Xml.Serialization.XmlAnyElementAttribute),
                    typeof(System.Xml.Serialization.XmlAnyElementAttributes),
                    typeof(System.Xml.Serialization.XmlArrayItemAttributes),
                    typeof(System.Xml.Serialization.XmlAttributeEventArgs),
                    typeof(System.Xml.Serialization.XmlAttributeOverrides),
                    typeof(System.Xml.Serialization.XmlAttributes),
                    typeof(System.Xml.Serialization.XmlChoiceIdentifierAttribute),
                    typeof(System.Xml.Serialization.XmlElementAttributes),
                    typeof(System.Xml.Serialization.XmlElementEventArgs),
                    typeof(System.Xml.Serialization.XmlEnumAttribute),
                    typeof(System.Xml.Serialization.XmlIgnoreAttribute),
                    typeof(System.Xml.Serialization.XmlIncludeAttribute),
                    typeof(System.Xml.Serialization.XmlRootAttribute),
                    typeof(System.Xml.Serialization.XmlTextAttribute),
                    typeof(System.Xml.Serialization.XmlTypeAttribute),
                    typeof(System.Runtime.CompilerServices.RuntimeHelpers),
                    typeof(System.IO.Stream),
                    //typeof(System.IO.StreamWriter),//can be constructed with path
                    //typeof(System.IO.StreamReader),
                    typeof(System.IO.TextWriter),
                    typeof(System.IO.TextReader),
                    typeof(System.IO.BinaryReader),
                    typeof(System.IO.BinaryWriter),
                    typeof(NullReferenceException),
                    typeof(ArgumentException),
                    typeof(ArgumentNullException),
                    typeof(InvalidOperationException),
                    typeof(FormatException),
                    typeof(System.Exception),
                    typeof(System.DivideByZeroException),
                    typeof(System.InvalidCastException),
                    typeof(System.IO.FileNotFoundException),
                    typeof(NotSupportedException),
                    typeof(System.Nullable<>),
                    typeof(StringComparer),
                    typeof(System.IEquatable<>),
                    typeof(System.IComparable),
                    typeof(System.IComparable<>),
                    typeof(System.BitConverter), // Useful for serializing custom messages/data in non-xml format
                    typeof(System.FlagsAttribute),
                    typeof(System.IO.Path),
                    typeof(System.Random),
                    //typeof(System.Runtime.CompilerServices.CompilerHelper), // We use this in tests
                    typeof(System.Convert), //Enum needs it, also usefull tool, they can convert object to type but cannot call what isnt whitelisted and Method.Invoke isnt whitelisted
                    typeof(StringSplitOptions),
                    typeof(DateTimeKind),
                    typeof(MidpointRounding),
                    typeof(EventArgs)
                );

                handle.AllowMembers(MyWhitelistTarget.Both,
                    typeof(System.Reflection.MemberInfo).GetProperty("Name"));

                handle.AllowMembers(MyWhitelistTarget.Both,
                    typeof(Type).GetProperty("FullName"),
                    typeof(Type).GetMethod("GetTypeFromHandle"),
                    typeof(Type).GetMethod("GetFields", new[] {typeof(System.Reflection.BindingFlags)}),
                    typeof(Type).GetMethod("IsEquivalentTo"),
                    typeof(Type).GetMethod("op_Equality"),
                    typeof(Type).GetMethod("ToString")
                    );

                //var t = typeof(MethodInfo).Assembly.GetType("System.Reflection.RuntimeMethodInfo");
                //AllowedOperands[t] = new List<MemberInfo>() { t.GetMethod("Equals") };

                handle.AllowMembers(MyWhitelistTarget.Both,
                    typeof(ValueType).GetMethod("Equals"),
                    typeof(ValueType).GetMethod("GetHashCode"),
                    typeof(ValueType).GetMethod("ToString")
                    );

                handle.AllowMembers(MyWhitelistTarget.Both,
                    typeof(Environment).GetProperty("CurrentManagedThreadId", BindingFlags.Static | BindingFlags.Public),
                    typeof(Environment).GetProperty("NewLine", BindingFlags.Static | BindingFlags.Public),
                    typeof(Environment).GetProperty("ProcessorCount", BindingFlags.Static | BindingFlags.Public)
                    );

                // TODO: The following may not be necessary at all
                var rt = typeof(Type).Assembly.GetType("System.RuntimeType");
                handle.AllowMembers(MyWhitelistTarget.Both,
                    rt.GetMethod("op_Inequality"),
                    rt.GetMethod("GetFields", new[] {typeof(System.Reflection.BindingFlags)})
                    );

                //var rtField = typeof(Type).Assembly.GetType("System.Reflection.RtFieldInfo");
                //handle.AllowMembers(WhitelistTarget.Both,
                //    rtField.GetMethod("UnsafeGetValue", BindingFlags.NonPublic | BindingFlags.Instance)
                //    );
            }
        }

        /// <summary>
        ///     Opens the whitelist, allowing for addition of new members.
        /// </summary>
        /// <returns></returns>
        public IMyWhitelistBatch OpenBatch()
        {
            return new MyWhitelistBatch(this);
        }

        internal bool IsWhitelisted(ISymbol symbol, MyWhitelistTarget target)
        {
            var typeSymbol = symbol as INamedTypeSymbol;
            if (typeSymbol != null)
            {
                return IsWhitelisted(typeSymbol, target) != TypeKeyQuantity.None;
            }

            if (symbol.IsMemberSymbol())
            {
                return IsMemberWhitelisted(symbol, target);
            }

            // This is not a symbol we need concern ourselves with.
            return true;
        }

        bool IsBlacklisted(ISymbol symbol)
        {
            if (symbol.IsMemberSymbol())
            {
                if (m_ingameBlacklist.Contains(symbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly)))
                    return true;
                symbol = symbol.ContainingType;
            }

            var typeSymbol = symbol as ITypeSymbol;
            while (typeSymbol != null)
            {
                if (m_ingameBlacklist.Contains(typeSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers)))
                    return true;
                typeSymbol = typeSymbol.ContainingType;
            }
            return false;
        }

        TypeKeyQuantity IsWhitelisted(INamespaceSymbol namespaceSymbol, MyWhitelistTarget target)
        {
            MyWhitelistTarget allowedTarget;
            if (m_whitelist.TryGetValue(namespaceSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers), out allowedTarget)
                && allowedTarget.HasFlag(target))
            {
                return TypeKeyQuantity.AllMembers;
            }
            return TypeKeyQuantity.None;
        }

        TypeKeyQuantity IsWhitelisted(INamedTypeSymbol typeSymbol, MyWhitelistTarget target)
        {
            // Delegates are allowed directly in checking, as they are harmless since you have to call 
            // or store something in it which is also checked.
            if (IsDelegate(typeSymbol))
            {
                return TypeKeyQuantity.AllMembers;
            }

            if (target == MyWhitelistTarget.Ingame && IsBlacklisted(typeSymbol))
            {
                return TypeKeyQuantity.None;
            }

            var result = IsWhitelisted(typeSymbol.ContainingNamespace, target);
            if (result == TypeKeyQuantity.AllMembers)
            {
                return result;
            }

            MyWhitelistTarget allowedTarget;
            if (m_whitelist.TryGetValue(typeSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers), out allowedTarget)
                && allowedTarget.HasFlag(target))
            {
                return TypeKeyQuantity.AllMembers;
            }

            if (m_whitelist.TryGetValue(typeSymbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly), out allowedTarget)
                && allowedTarget.HasFlag(target))
            {
                return TypeKeyQuantity.ThisOnly;
            }

            return TypeKeyQuantity.None;
        }

        bool IsDelegate(INamedTypeSymbol typeSymbol)
        {
            while (typeSymbol != null)
            {
                if (typeSymbol.SpecialType == SpecialType.System_Delegate || typeSymbol.SpecialType == SpecialType.System_MulticastDelegate)
                {
                    return true;
                }
                typeSymbol = typeSymbol.BaseType;
            }
            return false;
        }

        bool IsMemberWhitelisted(ISymbol memberSymbol, MyWhitelistTarget target)
        {
            while (true)
            {
                if (target == MyWhitelistTarget.Ingame && IsBlacklisted(memberSymbol))
                {
                    return false;
                }

                var result = IsWhitelisted(memberSymbol.ContainingType, target);
                if (result == TypeKeyQuantity.AllMembers)
                {
                    return true;
                }

                MyWhitelistTarget allowedTarget;
                if (m_whitelist.TryGetValue(memberSymbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly), out allowedTarget) && allowedTarget.HasFlag(target))
                {
                    return true;
                }

                if (memberSymbol.IsOverride)
                {
                    memberSymbol = memberSymbol.GetOverriddenSymbol();
                    if (memberSymbol != null)
                    {
                        continue;
                    }
                }

                return false;
            }
        }

        CSharpCompilation CreateCompilation()
        {
            return m_scriptCompiler.CreateCompilation(null, null, false);
        }

        void RegisterMember(MyWhitelistTarget target, ISymbol symbol, MemberInfo member)
        {
            if (!(symbol is IEventSymbol || symbol is IFieldSymbol || symbol is IPropertySymbol || symbol is IMethodSymbol))
            {
                throw new MyWhitelistException("Unsupported symbol type " + symbol);
            }

            var namespaceSymbol = symbol.ContainingNamespace;
            if (namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace)
            {
                var namespaceKey = namespaceSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers);
                MyWhitelistTarget existingTarget;
                if (m_whitelist.TryGetValue(namespaceKey, out existingTarget) && existingTarget >= target)
                {
                    throw new MyWhitelistException("The member " + member + " is covered by the " + namespaceKey + " rule");
                }
            }

            var typeSymbol = symbol.ContainingType;
            while (typeSymbol != null)
            {
                var typeKey = typeSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers);
                MyWhitelistTarget existingTarget;
                if (m_whitelist.TryGetValue(typeKey, out existingTarget) && existingTarget >= target)
                {
                    throw new MyWhitelistException("The member " + member + " is covered by the " + typeKey + " rule");
                }

                // If there is no previous registration of the containing type, or the current registration
                // is less restrictive than the one we're currently asking for, it needs to be updated.
                // (this will only allow the reference of a type, but none of its members).
                typeKey = typeSymbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly);
                if (!m_whitelist.TryGetValue(typeKey, out existingTarget) || existingTarget < target)
                {
                    m_whitelist[typeKey] = target;
                }

                typeSymbol = typeSymbol.ContainingType;
            }

            var whitelistKey = symbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly);
            if (m_whitelist.ContainsKey(whitelistKey))
            {
                throw new MyWhitelistException("Duplicate registration of the whitelist key " + whitelistKey + " retrieved from " + member);
            }
            m_whitelist.Add(whitelistKey, target);
        }

        void Register(MyWhitelistTarget target, INamespaceSymbol symbol, Type type)
        {
            var whitelistKey = symbol.GetWhitelistKey(TypeKeyQuantity.AllMembers);
            if (m_whitelist.ContainsKey(whitelistKey))
            {
                throw new MyWhitelistException("Duplicate registration of the whitelist key " + whitelistKey + " retrieved from " + type);
            }
            m_whitelist.Add(whitelistKey, target);
        }

        void Register(MyWhitelistTarget target, ITypeSymbol symbol, Type type)
        {
            var namespaceSymbol = symbol.ContainingNamespace;
            if (namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace)
            {
                var namespaceKey = namespaceSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers);
                MyWhitelistTarget existingTarget;
                if (m_whitelist.TryGetValue(namespaceKey, out existingTarget) && existingTarget >= target)
                {
                    throw new MyWhitelistException("The type " + type + " is covered by the " + namespaceKey + " rule");
                }
            }

            var whitelistKey = symbol.GetWhitelistKey(TypeKeyQuantity.AllMembers);
            if (m_whitelist.ContainsKey(whitelistKey))
            {
                throw new MyWhitelistException("Duplicate registration of the whitelist key " + whitelistKey + " retrieved from " + type);
            }
            m_whitelist.Add(whitelistKey, target);
        }

        /// <summary>
        ///     Clears the whitelist.
        /// </summary>
        public void Clear()
        {
            // This one feels a bit weird, can't really see the need but it was in the old one so I guess I'll keep it here as well.
            m_whitelist.Clear();
            m_ingameBlacklist.Clear();
        }

        public DictionaryReader<string, MyWhitelistTarget> GetWhitelist()
        {
            return new DictionaryReader<string, MyWhitelistTarget>(m_whitelist);
        }

        public HashSetReader<string> GetBlacklistedIngameEntries()
        {
            return m_ingameBlacklist;
        }

        public IMyScriptBlacklistBatch OpenIngameBlacklistBatch()
        {
            return new MyScriptBlacklistBatch(this);
        }

        abstract class Batch : IDisposable
        {
            readonly Dictionary<string, IAssemblySymbol> m_assemblyMap;
            bool m_isDisposed;

            protected Batch(MyScriptWhitelist whitelist)
            {
                Whitelist = whitelist;
                var compilation = Whitelist.CreateCompilation();
                m_assemblyMap = compilation.References
                    .Select(compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .ToDictionary(symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            ~Batch()
            {
                m_isDisposed = true;
                Debug.Assert(false, "Undisposed whitelist batch!");
            }

            protected MyScriptWhitelist Whitelist { get; private set; }

            [DebuggerNonUserCode]
            protected void AssertVitality()
            {
                if (m_isDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
            }

            protected INamedTypeSymbol ResolveTypeSymbol(Type type)
            {
                IAssemblySymbol assemblySymbol;
                if (!m_assemblyMap.TryGetValue(type.Assembly.FullName, out assemblySymbol))
                {
                    throw new MyWhitelistException(string.Format("Cannot add {0} to the batch because {1} has not been added to the compiler.",
                        type.FullName, type.Assembly.FullName));
                }
                var typeSymbol = assemblySymbol.GetTypeByMetadataName(type.FullName);
                if (typeSymbol == null)
                {
                    throw new MyWhitelistException(string.Format("Cannot add {0} to the batch because its symbol variant could not be found.",
                        type.FullName));
                }
                return typeSymbol;
            }

            public void Dispose()
            {
                if (m_isDisposed)
                {
                    return;
                }
                m_isDisposed = true;
                OnDispose();
                GC.SuppressFinalize(this);
            }

            protected virtual void OnDispose()
            {}
        }

        class MyScriptBlacklistBatch : Batch, IMyScriptBlacklistBatch
        {
            public MyScriptBlacklistBatch(MyScriptWhitelist whitelist) : base(whitelist)
            {}

            public void AddNamespaceOfTypes(params Type[] types)
            {
                if (types.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one type");
                }
                AssertVitality();
                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    if (type == null)
                    {
                        throw new MyWhitelistException("The type in index " + index + " is null");
                    }
                    var typeSymbol = ResolveTypeSymbol(type);
                    var namespaceSymbol = typeSymbol.ContainingNamespace;
                    if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                    {
                        continue;
                    }

                    Whitelist.m_ingameBlacklist.Add(namespaceSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers));
                }
            }

            public void RemoveNamespaceOfTypes(params Type[] types)
            {
                if (types.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one type");
                }
                AssertVitality();
                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    if (type == null)
                    {
                        throw new MyWhitelistException("The type in index " + index + " is null");
                    }
                    var typeSymbol = ResolveTypeSymbol(type);
                    var namespaceSymbol = typeSymbol.ContainingNamespace;
                    if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                    {
                        continue;
                    }

                    Whitelist.m_ingameBlacklist.Remove(namespaceSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers));
                }
            }

            public void AddTypes(params Type[] types)
            {
                if (types.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one type");
                }
                AssertVitality();
                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    if (type == null)
                    {
                        throw new MyWhitelistException("The type in index " + index + " is null");
                    }
                    var typeSymbol = ResolveTypeSymbol(type);

                    Whitelist.m_ingameBlacklist.Add(typeSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers));
                }
            }

            public void RemoveTypes(params Type[] types)
            {
                if (types.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one type");
                }
                AssertVitality();
                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    if (type == null)
                    {
                        throw new MyWhitelistException("The type in index " + index + " is null");
                    }
                    var typeSymbol = ResolveTypeSymbol(type);

                    Whitelist.m_ingameBlacklist.Remove(typeSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers));
                }
            }

            public void AddMembers(Type type, params string[] memberNames)
            {
                if (type == null)
                {
                    throw new MyWhitelistException("Must specify the target type");
                }
                if (memberNames.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one member name");
                }
                AssertVitality();
                var members = new List<string>();
                GetMemberWhitelistKeys(type, memberNames, members);
                for (var index = 0; index < members.Count; index++)
                {
                    var member = members[index];
                    Whitelist.m_ingameBlacklist.Add(member);
                }
            }

            public void RemoveMembers(Type type, params string[] memberNames)
            {
                if (type == null)
                {
                    throw new MyWhitelistException("Must specify the target type");
                }
                if (memberNames.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one member name");
                }
                AssertVitality();
                var members = new List<string>();
                GetMemberWhitelistKeys(type, memberNames, members);
                for (var index = 0; index < members.Count; index++)
                {
                    var member = members[index];
                    Whitelist.m_ingameBlacklist.Remove(member);
                }
            }

            void GetMemberWhitelistKeys(Type type, string[] memberNames, List<string> members)
            {
                var typeSymbol = ResolveTypeSymbol(type);
                for (var index = 0; index < memberNames.Length; index++)
                {
                    var memberName = memberNames[index];
                    var count = members.Count;
                    foreach (var memberSymbol in typeSymbol.GetMembers())
                    {
                        if (memberSymbol.Name != memberName)
                        {
                            continue;
                        }
                        switch (memberSymbol.DeclaredAccessibility)
                        {
                            case Accessibility.ProtectedOrInternal:
                            case Accessibility.Protected:
                            case Accessibility.Public:
                                break;
                            default:
                                continue;
                        }
                        members.Add(memberSymbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly));
                    }
                    if (count == members.Count)
                    {
                        throw new MyWhitelistException("Cannot find any members named " + memberName);
                    }
                }
            }
        }

        class MyWhitelistBatch : Batch, IMyWhitelistBatch
        {
            public MyWhitelistBatch(MyScriptWhitelist whitelist) : base(whitelist)
            {}

            /// <summary>
            ///     Adds the entire namespace of one or more given types.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="types"></param>
            public void AllowNamespaceOfTypes(MyWhitelistTarget target, params Type[] types)
            {
                if (types.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one type");
                }
                AssertVitality();
                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    if (type == null)
                    {
                        throw new MyWhitelistException("The type in index " + index + " is null");
                    }
                    var typeSymbol = ResolveTypeSymbol(type);
                    var namespaceSymbol = typeSymbol.ContainingNamespace;
                    if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                    {
                        continue;
                    }

                    Whitelist.Register(target, namespaceSymbol, type);
                }
            }

            /// <summary>
            ///     Adds one or more specific types and all their members to the whitelist.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="types"></param>
            public void AllowTypes(MyWhitelistTarget target, params Type[] types)
            {
                if (types.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one type");
                }
                AssertVitality();
                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    if (type == null)
                    {
                        throw new MyWhitelistException("The type in index " + index + " is null");
                    }
                    var typeSymbol = ResolveTypeSymbol(type);

                    Whitelist.Register(target, typeSymbol, type);
                }
            }

            /// <summary>
            ///     Adds only the specified members to the whitelist.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="members"></param>
            public void AllowMembers(MyWhitelistTarget target, params MemberInfo[] members)
            {
                if (members.IsNullOrEmpty())
                {
                    throw new MyWhitelistException("Needs at least one member");
                }
                AssertVitality();
                for (var index = 0; index < members.Length; index++)
                {
                    var member = members[index];
                    if (member == null)
                    {
                        throw new MyWhitelistException("Element " + index + " is null");
                    }

                    var typeSymbol = ResolveTypeSymbol(member.DeclaringType);
                    var candidates = typeSymbol.GetMembers().Where(m => m.MetadataName == member.Name).ToList();
                    var method = member as MethodInfo;
                    ParameterInfo[] methodParameters = null;
                    if (method != null)
                    {
                        // Sanity check. I don't think this is actually possible.
                        Debug.Assert(candidates.All(m => m is IMethodSymbol), "Illogical failure: Found more than one non-method with the same name?!?");
                        methodParameters = method.GetParameters();
                        candidates.RemoveAll(s => ((IMethodSymbol)s).Parameters.Length != methodParameters.Length);
                        if (method.IsGenericMethodDefinition)
                        {
                            candidates.RemoveAll(s => !((IMethodSymbol)s).IsGenericMethod);
                        }
                        else
                        {
                            candidates.RemoveAll(s => ((IMethodSymbol)s).IsGenericMethod);
                        }

                        if (method.IsSpecialName && method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                        {
                            throw new MyWhitelistException("Whitelist the actual properties, not their access methods");
                        }
                    }

                    switch (candidates.Count)
                    {
                        case 0:
                            throw new MyWhitelistException(string.Format("Cannot add {0} to the whitelist because its symbol variant could not be found.", member));

                        case 1:
                            Whitelist.RegisterMember(target, candidates[0], member);
                            break;

                        default:
                            // Sanity check. I don't think this is actually possible.
                            Debug.Assert(method != null, "Illogical failure: Found more than one non-method with the same name?!?");

                            var methodSymbol = FindMethodOverload(candidates, methodParameters);
                            if (methodSymbol == null)
                            {
                                throw new MyWhitelistException(string.Format("Cannot add {0} to the whitelist because its symbol variant could not be found.", member));
                            }

                            Whitelist.RegisterMember(target, methodSymbol, member);
                            break;
                    }
                }
            }

            IMethodSymbol FindMethodOverload(IEnumerable<ISymbol> candidates, ParameterInfo[] methodParameters)
            {
                foreach (var candidate in candidates)
                {
                    var methodSymbol = (IMethodSymbol)candidate;
                    var candidateParameters = methodSymbol.Parameters;
                    var success = true;
                    for (var i = 0; i < candidateParameters.Length; i++)
                    {
                        var parameter = methodParameters[i];
                        var parameterType = parameter.ParameterType;
                        var candidateParameter = candidateParameters[i];
                        var candidateParameterType = candidateParameter.Type;

                        if (parameter.IsOut)
                        {
                            if (candidateParameter.RefKind != RefKind.Out)
                            {
                                success = false;
                                break;
                            }
                        }

                        if (parameterType.IsByRef)
                        {
                            if (candidateParameter.RefKind != RefKind.Ref)
                            {
                                success = false;
                                break;
                            }
                            parameterType = parameterType.GetElementType();
                        }

                        if (parameterType.IsPointer)
                        {
                            if (!(candidateParameterType is IPointerTypeSymbol))
                            {
                                success = false;
                                break;
                            }
                            candidateParameterType = ((IPointerTypeSymbol)candidateParameterType).PointedAtType;
                            parameterType = parameterType.GetElementType();
                        }

                        if (parameterType.IsArray)
                        {
                            if (!(candidateParameterType is IArrayTypeSymbol))
                            {
                                success = false;
                                break;
                            }
                            candidateParameterType = ((IArrayTypeSymbol)candidateParameterType).ElementType;
                            parameterType = parameterType.GetElementType();
                        }

                        if (!Equals(ResolveTypeSymbol(parameterType), candidateParameterType))
                        {
                            success = false;
                            break;
                        }
                    }
                    if (success)
                    {
                        return methodSymbol;
                    }
                }
                return null;
            }
        }
    }
}