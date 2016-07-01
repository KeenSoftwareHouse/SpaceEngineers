using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    public class MyTypeTable
    {
        List<MySynchronizedTypeInfo> m_idToType = new List<MySynchronizedTypeInfo>();
        Dictionary<Type, MySynchronizedTypeInfo> m_typeLookup = new Dictionary<Type, MySynchronizedTypeInfo>();
        Dictionary<int, MySynchronizedTypeInfo> m_hashLookup = new Dictionary<int, MySynchronizedTypeInfo>();
        MyEventTable m_staticEventTable = new MyEventTable(null);

        public MyEventTable StaticEventTable { get { return m_staticEventTable; } }

        public bool Contains(Type type)
        {
            return m_typeLookup.ContainsKey(type);
        }

        public MySynchronizedTypeInfo Get(TypeId id)
        {
            Debug.Assert(id.Value < m_idToType.Count, "Invalid replication type ID");
            return m_idToType[(int)id.Value];
        }

        public MySynchronizedTypeInfo Get(Type type)
        {
            Debug.Assert(m_typeLookup.ContainsKey(type), "Type not found");
            return m_typeLookup[type];
        }

        public bool TryGet(Type type, out MySynchronizedTypeInfo typeInfo)
        {
            return m_typeLookup.TryGetValue(type, out typeInfo);
        }

        public MySynchronizedTypeInfo Register(Type type)
        {
            MySynchronizedTypeInfo result;
            if (!m_typeLookup.TryGetValue(type, out result))
            {
                MySynchronizedTypeInfo baseType = CreateBaseType(type);
                bool isReplicated = IsReplicated(type);
                if (isReplicated || HasEvents(type))
                {
                    result = new MySynchronizedTypeInfo(type, new TypeId((uint)m_idToType.Count), baseType, isReplicated);
                    m_idToType.Add(result);
                    m_hashLookup.Add(result.TypeHash, result);
                    m_typeLookup.Add(type, result);
                    m_staticEventTable.AddStaticEvents(type);
                }
                else if (IsSerializableClass(type)) // Stored only for dynamic serialization.
                {
                    result = new MySynchronizedTypeInfo(type, new TypeId((uint)m_idToType.Count), baseType, isReplicated);
                    m_idToType.Add(result);
                    m_hashLookup.Add(result.TypeHash, result);
                    m_typeLookup.Add(type, result);
                }
                else if (baseType != null)// Base type has some events
                {
                    result = baseType;
                    m_typeLookup.Add(type, result);
                }
                else // Not even base type has some events
                {
                    result = null;
                }
            }
            return result;
        }

        public static bool ShouldRegister(Type type)
        {
            return IsReplicated(type) || CanHaveEvents(type) || IsSerializableClass(type);
        }

        private static bool IsSerializableClass(Type type)
        {
            return type.HasAttribute<SerializableAttribute>();
        }

        static bool IsReplicated(Type type)
        {
            return !type.IsAbstract && typeof(IMyReplicable).IsAssignableFrom(type) && !type.HasAttribute<NotReplicableAttribute>();
        }

        static bool CanHaveEvents(Type type)
        {
            return Attribute.IsDefined(type, typeof(StaticEventOwnerAttribute)) || typeof(IMyEventOwner).IsAssignableFrom(type);
        }

        static bool HasEvents(Type type)
        {
            return type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static).Any(s => s.HasAttribute<EventAttribute>());
        }

        MySynchronizedTypeInfo CreateBaseType(Type type)
        {
            while (type.BaseType != null && type.BaseType != typeof(Object))
            {
                if (ShouldRegister(type.BaseType))
                {
                    return Register(type.BaseType);
                }
                else
                {
                    type = type.BaseType;
                }
            }
            return null;
        }

        /// <summary>
        /// Serializes id to hash list.
        /// Server sends the hashlist to client, client reorders type table to same order as server.
        /// </summary>
        public void Serialize(BitStream stream)
        {
            if (stream.Writing)
            {
                stream.WriteVariant((uint)m_idToType.Count);
                for (int i = 0; i < m_idToType.Count; i++)
                {
                    stream.WriteInt32(m_idToType[i].TypeHash);
                }
            }
            else
            {
                int count = (int)stream.ReadUInt32Variant();

                Debug.Assert(m_idToType.Count == count, "Number of received types does not match number of registered types");

                m_staticEventTable = new MyEventTable(null);
                for (int i = 0; i < count; i++)
                {
                    int typeHash = stream.ReadInt32();
                    Debug.Assert(m_hashLookup.ContainsKey(typeHash), "Type hash not found!");
                    var type = m_hashLookup[typeHash];
                    m_idToType[i] = type;
                    m_staticEventTable.AddStaticEvents(type.Type);
                }
            }
        }
    }
}
