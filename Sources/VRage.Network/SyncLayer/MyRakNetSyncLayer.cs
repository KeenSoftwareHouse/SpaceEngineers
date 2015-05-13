using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using VRage.Plugins;
using VRage.Collections;

namespace VRage.Network
{
    public class MyRakNetSyncLayer : IDisposable
    {
        public event Action<ulong> OnEntityDestroyed;
        public event Action<object, ulong> OnEntityCreated;

        private MyRakNetPeer m_peer;

        private Dictionary<ulong, MySyncedClass> m_registered = new Dictionary<ulong, MySyncedClass>();
        public static MyRakNetSyncLayer Static;

        private List<Type> m_idToType;
        private Dictionary<Type, int> m_typeToId;

        public ListReader<Type> GetTypeTable()
        {
            Debug.Assert(m_idToType.Count > 0);
            return new ListReader<Type>(m_idToType);
        }

        internal void SetTypeTable(List<Guid> guids)
        {
            List<Type> ordered = new List<Type>(guids.Count);
            foreach (var guid in guids)
            {
                bool found = false;
                foreach (var type in m_idToType)
                {
                    if (type.GUID == guid)
                    {
                        ordered.Add(type);
                        found = true;
                    }
                }

                if (!found)
                {
                    var msg = string.Format("Replication type not found, unknown type GUID: {0}", guid);
                    Debug.Assert(found, msg);
                    throw new KeyNotFoundException(msg);
                }
            }
            m_idToType = ordered;

            m_typeToId.Clear();
            for (int i = 0; i < m_idToType.Count; i++)
            {
                m_typeToId[m_idToType[i]] = i;
            }
        }

        public void LoadData(MyRakNetPeer peer, Assembly assembly)
        {
            m_peer = peer;

            m_idToType = new List<Type>();
            m_typeToId = new Dictionary<Type, int>();

            RegisterFromAssembly(peer.GetType().Assembly);

            RegisterFromAssembly(assembly);

            if (MyPlugins.GameAssembly != null)
                RegisterFromAssembly(MyPlugins.GameAssembly);

            if (MyPlugins.UserAssembly != null)
                RegisterFromAssembly(MyPlugins.UserAssembly);

            Static = this;
        }

        public void UnloadData()
        {
            if (Static != null)
            {
                Static.Dispose();
                Static = null;
            }
        }

        public void Update()
        {
            foreach (var syncedObject in m_registered.Values)
            {
                if (syncedObject.IsDirty)
                {
                    Sync(syncedObject);
                }
            }
        }

        private void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attribute = Attribute.GetCustomAttribute(type, typeof(SynchronizedAttribute)) as SynchronizedAttribute;
                if (attribute != null)
                {
                    m_idToType.Add(type);
                    m_typeToId[type] = m_idToType.Count - 1;
                }
            }
        }

        public void Dispose()
        {
            m_registered.Clear();
            m_idToType.Clear();
            m_typeToId.Clear();
        }

        public static void RegisterSynced(MySyncedClass mySyncedClass)
        {
            Debug.Assert(mySyncedClass.entityId != 0);
            Debug.Assert(Static != null);
            Debug.Assert(!Static.m_registered.ContainsKey(mySyncedClass.entityId));
            Debug.Assert(!Static.m_registered.ContainsValue(mySyncedClass));

            Static.m_registered.Add(mySyncedClass.entityId, mySyncedClass);
        }

        internal static void Sync(MySyncedClass mySyncedClass)
        {
            Debug.Assert(mySyncedClass.entityId != 0);
            Debug.Assert(Static != null);
            Debug.Assert(Static.m_peer is MyRakNetServer);

            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.SYNC_FIELD);
            bs.Write((long)mySyncedClass.entityId);
            mySyncedClass.Serialize(bs);

            ((MyRakNetServer)Static.m_peer).BroadcastMessage(bs, PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum.UNRELIABLE, 0, RakNetGUID.UNASSIGNED_RAKNET_GUID);
        }

        internal void ProcessSync(BitStream bs)
        {
            long tmpLong;
            bool success = bs.Read(out tmpLong);
            Debug.Assert(success, "Failed to read entityID");
            ulong entityID = (ulong)tmpLong;

            MySyncedClass mySyncedObject = GetEntitySyncFields(entityID);
            if (mySyncedObject != null)
            {
                mySyncedObject.Deserialize(bs);
            }
        }

        public static void Destroy(ulong entityID)
        {
            Debug.Assert(Static != null);
            Debug.Assert(Static.m_peer is MyRakNetServer);
            Debug.Assert(Static.m_registered.ContainsKey(entityID));

            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.REPLICATION_DESTROY);
            bs.Write((long)entityID);

            Static.m_registered.Remove(entityID);

            ((MyRakNetServer)Static.m_peer).BroadcastMessage(bs, PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum.RELIABLE, 0, RakNetGUID.UNASSIGNED_RAKNET_GUID);
        }

        public static void Replicate(object obj, ulong entityID)
        {
            Debug.Assert(Static != null);
            Debug.Assert(Static.m_peer is MyRakNetServer);
            Debug.Assert(!Static.m_registered.ContainsKey(entityID));
            Debug.Assert(Static.m_idToType.Contains(obj.GetType()));

            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.REPLICATION_CREATE);
            bs.WriteCompressed(Static.m_typeToId[obj.GetType()]);
            bs.Write((long)entityID);

            MySyncedClass syncedClass = GetSyncedClass(obj);
            syncedClass.entityId = entityID;
            syncedClass.Serialize(bs);

            Static.m_registered.Add(entityID, syncedClass);

            ((MyRakNetServer)Static.m_peer).BroadcastMessage(bs, PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum.RELIABLE, 0, RakNetGUID.UNASSIGNED_RAKNET_GUID);
        }

        private static SortedDictionary<int, object> tmpFields = new SortedDictionary<int, object>();

        // TODO:sk reflection should be cached if possible
        private static MySyncedClass GetSyncedClass(object obj, Type type = null)
        {
            Debug.Assert(tmpFields.Count == 0);
            MySyncedClass sync = new MySyncedClass();

            type = type ?? obj.GetType();

            Type baseType = type.BaseType;

            if (baseType != null && baseType != typeof(object))
            {
                sync.Add(GetSyncedClass(obj, baseType));
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                //var baseFieldType = field.FieldType.BaseType;
                //if (baseFieldType != null && baseFieldType.IsGenericType && baseFieldType.GetGenericTypeDefinition() == typeof(MySyncedBase<>))

                var fieldAttributes = field.GetCustomAttributes(false);

                foreach (var fieldAttribute in fieldAttributes)
                {
                    StateDataAttribute stateData = fieldAttribute as StateDataAttribute;
                    if (stateData != null)
                    {
                        int order = stateData.Order;
                        Debug.Assert(!tmpFields.ContainsKey(order));

                        object value = field.GetValue(obj);
                        Debug.Assert(value != null, "Uninitialized synced variable");
                        Debug.Assert(!tmpFields.ContainsValue(value));

                        tmpFields.Add(order, value);
                    }
                }
            }

            foreach (var value in tmpFields.Values)
            {
                sync.Add((IMySyncedValue)value);
            }
            tmpFields.Clear();

            return sync;
        }

        internal void ProcessReplication(BitStream bs)
        {
            bool success;

            int typeID;
            success = bs.ReadCompressed(out typeID);
            Debug.Assert(success, "Failed to read replication type ID");
            Debug.Assert(typeID < m_idToType.Count, "Invalid replication type ID");

            Type type = m_idToType[typeID];

            long tmpLong;
            success = bs.Read(out tmpLong);
            Debug.Assert(success, "Failed to read entityID");
            ulong entityID = (ulong)tmpLong;

            object obj = Activator.CreateInstance(type);
            // should init here
            MySyncedClass sync = GetSyncedClass(obj);
            sync.entityId = entityID;
            sync.Deserialize(bs);

            m_registered.Add(entityID, sync);

            var handle = OnEntityCreated;
            if (handle != null)
            {
                handle(obj, entityID);
            }
        }

        internal void ReplicationDestroy(BitStream bs)
        {
            bool success;

            long tmpLong;
            success = bs.Read(out tmpLong);
            Debug.Assert(success, "Failed to read entityID");
            ulong entityID = (ulong)tmpLong;

            m_registered.Remove(entityID);

            var handle = OnEntityDestroyed;
            if (handle != null)
            {
                handle(entityID);
            }
        }

        private MySyncedClass GetEntitySyncFields(ulong entityID)
        {
            if (m_registered.ContainsKey(entityID))
            {
                return m_registered[entityID];
            }
            return null;
        }
    }
}
