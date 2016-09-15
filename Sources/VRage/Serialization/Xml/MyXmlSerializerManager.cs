using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VRage
{
    public class MyXmlSerializerManager
    {
        private static readonly HashSet<Type> m_serializableBaseTypes = new HashSet<Type>();
        private static readonly Dictionary<Type, XmlSerializer> m_serializersByType = new Dictionary<Type, XmlSerializer>();
        private static readonly Dictionary<string, XmlSerializer> m_serializersBySerializedName = new Dictionary<string, XmlSerializer>();
        private static readonly Dictionary<Type, string> m_serializedNameByType = new Dictionary<Type, string>();

        private static HashSet<Assembly> m_registeredAssemblies = new HashSet<Assembly>();

        public static void RegisterSerializer(Type type)
        {
            if (!m_serializersByType.ContainsKey(type))
                RegisterType(type, true, false);
        }

        public static void RegisterSerializableBaseType(Type type)
        {
            m_serializableBaseTypes.Add(type);
        }

        public static void RegisterFromAssembly(Assembly assembly)
        {
            if (assembly == null) return;
            if (m_registeredAssemblies.Contains(assembly)) return;
            m_registeredAssemblies.Add(assembly);

            foreach (Type type in assembly.GetTypes())
            {
                try
                {
                    if (m_serializersByType.ContainsKey(type))
                        continue;

                    RegisterType(type);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Error creating XML serializer for type " + type.Name, e);
                }
            }
        }

        public static XmlSerializer GetSerializer(Type type)
        {
            return m_serializersByType[type];
        }

        public static XmlSerializer GetOrCreateSerializer(Type type)
        {
            XmlSerializer ret;
            if (!m_serializersByType.TryGetValue(type, out ret))
                ret = RegisterType(type, true);

            return ret;
        }

        public static string GetSerializedName(Type type)
        {
            return m_serializedNameByType[type];
        }

        public static bool TryGetSerializer(string serializedName, out XmlSerializer serializer)
        {
            return m_serializersBySerializedName.TryGetValue(serializedName, out serializer);
        }

        public static XmlSerializer GetSerializer(string serializedName)
        {
            return m_serializersBySerializedName[serializedName];
        }

        public static bool IsSerializerAvailable(string name)
        {
            return m_serializersBySerializedName.ContainsKey(name);
        }

        /// <param name="forceRegister">Force registration for types without XmlType
        /// attribute or not object builders</param>
        private static XmlSerializer RegisterType(Type type, bool forceRegister = false, bool checkAttributes = true)
        {
            string serializedName = null;
            if (checkAttributes)
            {
                var xmlTypes = type.GetCustomAttributes(typeof(XmlTypeAttribute), false);
                if (xmlTypes.Length > 0)
                {
                    var descriptor = (XmlTypeAttribute)xmlTypes[0];

                    serializedName = type.Name;

                    if (!string.IsNullOrEmpty(descriptor.TypeName))
                        serializedName = descriptor.TypeName;
                }
                else
                {
                    foreach (var baseType in m_serializableBaseTypes)
                    {
                        if (baseType.IsAssignableFrom(type))
                        {
                            serializedName = type.Name;
                            break;
                        }
                    }
                }
            }

            if (serializedName == null)
            {
                if (!forceRegister)
                    return null;

                serializedName = type.Name;
            }

            XmlSerializer serializer;
            serializer = new XmlSerializer(type);
            m_serializersByType.Add(type, serializer);
            m_serializersBySerializedName.Add(serializedName, serializer);
            m_serializedNameByType.Add(type, serializedName);
            return serializer;
        }
    }
}
