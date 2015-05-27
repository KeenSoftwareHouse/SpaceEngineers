using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using VRage.Plugins;
using VRage.Collections;
using VRage.Library.Utils;

namespace VRage.Network
{
    class MyStateDataEntry : IComparable<MyStateDataEntry>
    {
        public float Priority;
        public uint PacketID;
        public uint NetworkID { get; private set; }
        public MySyncedClass Sync { get; private set; }

        public MyStateDataEntry(uint networkID, MySyncedClass sync)
        {
            Priority = 0;
            PacketID = 0;
            NetworkID = networkID;
            Sync = sync;
        }

        public int CompareTo(MyStateDataEntry other)
        {
            return Priority.CompareTo(other.Priority);
        }

        internal void UpdatePriority(ulong steamID)
        {
            // TODO:SK
            Priority = MyRandom.Instance.NextFloat();
        }
    }

    public class MyRakNetSyncLayer : IDisposable
    {
        public event Action<object> OnEntityDestroyed;
        public event Action<object> OnEntityCreated;

        private MyRakNetPeer m_peer;

        public const int MaxClients = 32;

        private uint m_uniqueID = 1;

        private Dictionary<uint, object> m_networkIDToObject = new Dictionary<uint, object>();
        private Dictionary<object, uint> m_objectToNetworkID = new Dictionary<object, uint>();

        private Dictionary<uint, MySyncedClass> m_stateData = new Dictionary<uint, MySyncedClass>();

        private Dictionary<ulong, List<MyStateDataEntry>> m_perPlayerStateData;

        private Queue<int> m_freeClientIndexes = new Queue<int>(MaxClients);
        private Dictionary<ulong, int> m_steamIDToClientIndex = new Dictionary<ulong, int>();
        private Dictionary<int, ulong> m_clientIndexToSteamID = new Dictionary<int, ulong>();

        public static MyRakNetSyncLayer Static;

        private List<Type> m_idToType;
        private Dictionary<Type, int> m_typeToId;

        public bool IsServer { get { return m_peer.IsServer; } }

        public ListReader<Type> GetTypeTable()
        {
            Debug.Assert(m_idToType.Count > 0);
            return new ListReader<Type>(m_idToType);
        }

        internal void SetTypeTable(List<Guid> guids)
        {
            Debug.Assert(m_idToType.Count == guids.Count);

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
                        break;
                    }
                }

                if (!found)
                {
                    var msg = string.Format("Replication type not found, unknown type GUID: {0}", guid);
                    Debug.Assert(found, msg);
                    throw new KeyNotFoundException(msg);
                }
            }

            Debug.Assert(m_idToType.Count == ordered.Count);

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
            m_perPlayerStateData = new Dictionary<ulong, List<MyStateDataEntry>>();

            for (int i = 0; i < MaxClients; i++)
            {
                m_freeClientIndexes.Enqueue(i);
            }

            m_peer.OnClientLeft += m_peer_OnClientLeft;
            m_peer.OnClientJoined += m_peer_OnClientJoined;

            RegisterFromAssembly(peer.GetType().Assembly);

            RegisterFromAssembly(assembly);

            if (MyPlugins.GameAssembly != null)
                RegisterFromAssembly(MyPlugins.GameAssembly);

            if (MyPlugins.UserAssembly != null)
                RegisterFromAssembly(MyPlugins.UserAssembly);

            Static = this;
        }

        void m_peer_OnClientLeft(ulong steamID)
        {
            Debug.Assert(m_perPlayerStateData.ContainsKey(steamID));
            m_perPlayerStateData.Remove(steamID);

            Debug.Assert(m_steamIDToClientIndex.ContainsKey(steamID));
            var clientIndex = m_steamIDToClientIndex[steamID];
            m_steamIDToClientIndex.Remove(steamID);

            Debug.Assert(m_clientIndexToSteamID.ContainsKey(clientIndex));
            m_clientIndexToSteamID.Remove(clientIndex);
        }

        void m_peer_OnClientJoined(ulong steamID)
        {
            Debug.Assert(!m_perPlayerStateData.ContainsKey(steamID));
            var playerStateData = new List<MyStateDataEntry>();
            m_perPlayerStateData.Add(steamID, playerStateData);

            Debug.Assert(m_freeClientIndexes.Count > 0);
            int clientIndex = m_freeClientIndexes.Dequeue();

            Debug.Assert(!m_steamIDToClientIndex.ContainsKey(steamID));
            m_steamIDToClientIndex.Add(steamID, clientIndex);

            Debug.Assert(!m_clientIndexToSteamID.ContainsKey(clientIndex));
            m_clientIndexToSteamID.Add(clientIndex, steamID);
        }

        //TODO:SK: Split the message to allow client to load the state data incrementaly
        internal void SerializeStateData(ulong steamID, BitStream bs)
        {
            ProfilerShort.Begin("MyRakNetSyncLayer::SerializeStateData");
            Debug.Assert(m_perPlayerStateData.ContainsKey(steamID));
            var playerStateData = m_perPlayerStateData[steamID];

            playerStateData.Capacity = m_stateData.Count;

            int clientIndex = GetClientIndexFromSteamID(steamID);

            bs.Write(m_stateData.Count);
            foreach (var pair in m_stateData)
            {
                var networkID = pair.Key;
                var sync = pair.Value;
                playerStateData.Add(new MyStateDataEntry(networkID, sync));

                bs.WriteCompressed(sync.TypeID);
                bs.WriteCompressed(networkID);
                sync.SerializeDefault(bs, clientIndex);
            }
            ProfilerShort.End();
        }

        internal void DeserializeStateData(BitStream bs)
        {
            ProfilerShort.Begin("MyRakNetSyncLayer::DeserializeStateData");
            bool success;
            int count;

            success = bs.Read(out count);
            Debug.Assert(success, "Failed to read state count");

            for (int i = 0; i < count; i++)
            {
                ProcessReplicationCreate(bs);
            }
            ProfilerShort.End();
        }

        public void UnloadData()
        {
            if (Static != null)
            {
                Static.Dispose();
                Static = null;
            }
        }

        // TODO:SK performance
        public void Update()
        {
            Debug.Assert(IsServer);
            ProfilerShort.Begin("MyRakNetSyncLayer::Update");
            foreach (var pair in m_perPlayerStateData)
            {
                var steamID = pair.Key;
                var stateData = pair.Value;
                foreach (var entry in stateData)
                {
                    entry.UpdatePriority(steamID);
                }

                stateData.Sort();

                int clientIndex = GetClientIndexFromSteamID(steamID);

                foreach (var entry in stateData)
                {
                    if (entry.PacketID != 0)
                    {
                        continue;
                    }

                    if (entry.Sync.IsDirty(clientIndex))
                    {
                        Sync(entry.NetworkID, entry.Sync, clientIndex);
                    }
                }
            }
            ProfilerShort.End();
        }

        private int GetClientIndexFromSteamID(ulong steamID)
        {
            Debug.Assert(m_steamIDToClientIndex.ContainsKey(steamID));
            return m_steamIDToClientIndex[steamID];
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
            m_stateData.Clear();
            m_idToType.Clear();
            m_typeToId.Clear();
        }

        private static void RegisterSynced(uint networkID, MySyncedClass sync)
        {
            Debug.Assert(networkID != 0); // ??
            Debug.Assert(Static != null);
            Debug.Assert(!Static.m_stateData.ContainsKey(networkID));

            Static.m_stateData.Add(networkID, sync);
            foreach (var pair in Static.m_perPlayerStateData)
            {
                var steamID = pair.Key;
                var stateData = pair.Value;

                stateData.Add(new MyStateDataEntry(networkID, sync));
            }
        }

        private static void Sync(uint networkID, MySyncedClass sync, int clientIndex)
        {
            Debug.Assert(networkID != 0);
            Debug.Assert(Static != null);
            Debug.Assert(Static.m_peer is MyRakNetServer);

            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.SYNC_FIELD);
            bs.WriteCompressed(networkID);
            sync.Serialize(bs, clientIndex);

            var steamID = Static.m_clientIndexToSteamID[clientIndex];

            // TODO:SK use UNRELIABLE_WITH_ACK_RECEIPT
            var packetID = ((MyRakNetServer)Static.m_peer).SendMessage(bs, steamID, PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum.UNRELIABLE);
        }

        internal void ProcessSync(BitStream bs)
        {
            uint networkID;
            bool success = bs.ReadCompressed(out networkID);
            Debug.Assert(success, "Failed to read networkID");

            MySyncedClass mySyncedObject = GetNetworkedSync(networkID);
            if (mySyncedObject != null)
            {
                mySyncedObject.Deserialize(bs);
            }
        }

        public static void Destroy(object obj)
        {
            Debug.Assert(obj != null);
            Debug.Assert(Static != null);
            Debug.Assert(Static.m_peer is MyRakNetServer);
            Debug.Assert(Static.m_objectToNetworkID.ContainsKey(obj));

            uint networkID = Static.m_objectToNetworkID[obj];

            Debug.Assert(Static.m_networkIDToObject.ContainsKey(networkID));
            Debug.Assert(Static.m_stateData.ContainsKey(networkID));

            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.REPLICATION_DESTROY);
            bs.WriteCompressed(networkID);

            Static.RemoveNetworkedObject(networkID, obj);

            ((MyRakNetServer)Static.m_peer).BroadcastMessage(bs, PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum.RELIABLE);
        }

        private void AddNetworkedObject(uint networkID, object obj, MySyncedClass sync)
        {
            m_objectToNetworkID.Add(obj, networkID);
            m_networkIDToObject.Add(networkID, obj);
            RegisterSynced(networkID, sync);
        }

        private object RemoveNetworkedObject(uint networkID)
        {
            Debug.Assert(m_networkIDToObject.ContainsKey(networkID));
            var obj = m_networkIDToObject[networkID];
            RemoveNetworkedObject(networkID, obj);
            return obj;
        }

        private void RemoveNetworkedObject(uint networkID, object obj)
        {
            m_objectToNetworkID.Remove(obj);
            m_networkIDToObject.Remove(networkID);
            m_stateData.Remove(networkID);
        }

        // TODO:SK this will be bad if we actually get over uint size (http://bitsquid.blogspot.cz/2014/08/building-data-oriented-entity-system.html)
        private uint GetNetworkUniqueID()
        {
            uint uniqueID;
            while (m_networkIDToObject.ContainsKey(uniqueID = m_uniqueID++)) ;
            Debug.Assert(uniqueID != 0);
            return uniqueID;
        }

        public static void Replicate(object obj)
        {
            Debug.Assert(Static != null);
            Debug.Assert(Static.m_peer is MyRakNetServer);
            Debug.Assert(Static.m_idToType.Contains(obj.GetType()));

            int typeID = Static.m_typeToId[obj.GetType()];

            uint networkID = Static.GetNetworkUniqueID();

            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.REPLICATION_CREATE);
            bs.WriteCompressed(typeID); // TODO:SK better compression
            bs.WriteCompressed(networkID);

            MySyncedClass sync = GetSyncedClass(obj);
            sync.TypeID = typeID;

            Static.AddNetworkedObject(networkID, obj, sync);

            sync.SerializeDefault(bs);

            ((MyRakNetServer)Static.m_peer).BroadcastMessage(bs, PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum.RELIABLE);
        }

        private static SortedDictionary<int, object> tmpFields = new SortedDictionary<int, object>();

        // TODO:SK reflection should be cached if possible
        private static MySyncedClass GetSyncedClass(object obj, Type type = null)
        {
            Debug.Assert(tmpFields.Count == 0);
            MySyncedClass sync = null;
            MySyncedClass baseSync = null;

            type = type ?? obj.GetType();

            Type baseType = type.BaseType;

            if (baseType != null && baseType != typeof(object))
            {
                baseSync = GetSyncedClass(obj, baseType);
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

            if (baseSync != null && tmpFields.Count == 0)
            {
                return baseSync;
            }
            else
            {
                sync = new MySyncedClass();
                if (baseSync != null)
                {
                    sync.Add(baseSync);
                }
                foreach (var value in tmpFields.Values)
                {
                    sync.Add((IMySyncedValue)value);
                }
            }
            tmpFields.Clear();

            return sync;
        }

        internal void ProcessReplicationCreate(BitStream bs)
        {
            bool success;

            int typeID;
            success = bs.ReadCompressed(out typeID);
            Debug.Assert(success, "Failed to read replication type ID");
            Debug.Assert(typeID < m_idToType.Count, "Invalid replication type ID");

            Type type = m_idToType[typeID];

            uint networkID;
            success = bs.ReadCompressed(out networkID);
            Debug.Assert(success, "Failed to read networkID");

            object obj = Activator.CreateInstance(type);
            MySyncedClass sync = GetSyncedClass(obj);
            sync.TypeID = typeID;
            sync.Deserialize(bs);
            // TODO:SK should init here

            AddNetworkedObject(networkID, obj, sync);

            var handle = OnEntityCreated;
            if (handle != null)
            {
                handle(obj);
            }
        }

        internal void ProcessReplicationDestroy(BitStream bs)
        {
            bool success;

            uint networkID;
            success = bs.ReadCompressed(out networkID);
            Debug.Assert(success, "Failed to read networkID");

            var obj = RemoveNetworkedObject(networkID);

            var handle = OnEntityDestroyed;
            if (handle != null)
            {
                handle(obj);
            }
        }

        private MySyncedClass GetNetworkedSync(uint networkID)
        {
            if (m_stateData.ContainsKey(networkID))
            {
                return m_stateData[networkID];
            }
            return null;
        }
    }
}
