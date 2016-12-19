using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.ModAPI;
using System.Threading;

namespace VRage
{
    public struct MyEntityIdentifier
    {
        class PerThreadData
        {
            public bool AllocationSuspended;
            public Dictionary<long, IMyEntity> EntityList;

            public PerThreadData(int defaultCapacity)
            {
                EntityList = new Dictionary<long, IMyEntity>(defaultCapacity);
            }
        }

        const int DEFAULT_DICTIONARY_SIZE = 32768;

        [ThreadStatic]
        static PerThreadData m_perThreadData; // Per-thread data, explicitly initialized
        static PerThreadData m_mainData; // Main data, for update thread and any other threads without explicitly initialized data

        static Dictionary<long, IMyEntity> m_entityList { get { return (m_perThreadData ?? m_mainData).EntityList; } }

        // Want to share, always accessed through interlocked, safe
#if UNSHARPER
		static long[] m_lastGeneratedIds = new long[(int)MyEnum_Range<ID_OBJECT_TYPE>.Max + 1];
#else
        static long[] m_lastGeneratedIds = new long[(int)MyEnum<ID_OBJECT_TYPE>.Range.Max + 1];
#endif
        /// <summary>
        /// Freezes allocating entity ids.
        /// This is important, because during load, no entity cannot allocate new id, because it could allocate id which already has entity which will be loaded soon.
        /// </summary>
        public static bool AllocationSuspended
        {
            get { return (m_perThreadData ?? m_mainData).AllocationSuspended; }
            set { (m_perThreadData ?? m_mainData).AllocationSuspended = value; }
        }

        public enum ID_OBJECT_TYPE : byte
        {
            UNKNOWN = 0,
            ENTITY = 1,
            IDENTITY = 2, // Previously known as: PLAYER
            FACTION = 3,
            NPC = 4,         // Obsolete, use IDENTITY instead
            SPAWN_GROUP = 5, // Obsolete, use IDENTITY instead
            ASTEROID = 6,
            PLANET = 7,
            VOXEL_PHYSICS = 8, // Generated from planet entity id and storage min hash
            PLANET_ENVIRONMENT_SECTOR = 9, // From sectorId
            PLANET_ENVIRONMENT_ITEM = 10, // From sector Id and spawner index. Temporary untill environment item refactor.
            PLANET_VOXEL_DETAIL = 11,
        }

        public enum ID_ALLOCATION_METHOD : byte
        {
            RANDOM = 0,
            SERIAL_START_WITH_1 = 1,
        }

        static MyEntityIdentifier()
        {
            m_mainData = new PerThreadData(DEFAULT_DICTIONARY_SIZE);
            m_perThreadData = m_mainData;
        }

        public static void InitPerThreadStorage(int defaultCapacity)
        {
            Debug.Assert(m_perThreadData == null || m_perThreadData == m_mainData, "Per thread storage already initialized!");
            m_perThreadData = new PerThreadData(defaultCapacity);
        }

        public static void LazyInitPerThreadStorage(int defaultCapacity)
        {
            if (m_perThreadData == null || m_perThreadData == m_mainData)
                m_perThreadData = new PerThreadData(defaultCapacity);
        }

        public static void DestroyPerThreadStorage()
        {
            Debug.Assert(m_perThreadData != m_mainData, "DestroyPerThreadStorage should not be used for main data");
            m_perThreadData = null;
        }

        public static void GetPerThreadEntities(List<IMyEntity> result)
        {
            Debug.Assert(m_perThreadData != m_mainData, "GetPerThreadEntities should not be used for main data");
            foreach (var e in m_perThreadData.EntityList)
                result.Add(e.Value);
        }

        public static void ClearPerThreadEntities()
        {
            Debug.Assert(m_perThreadData != m_mainData, "ClearPerThreadEntities should not be used for main data");
            m_perThreadData.EntityList.Clear();
        }

        public static void Reset()
        {
            Array.Clear(m_lastGeneratedIds, 0, m_lastGeneratedIds.Length);
        }

#if !UNSHARPER
        /// <summary>
        /// This method is used when loading existing entity IDs to track the last generated ID
        /// </summary>
        public static void MarkIdUsed(long id)
        {
            long num = GetIdUniqueNumber(id);
            ID_OBJECT_TYPE type = GetIdObjectType(id);

            MyUtils.InterlockedMax(ref m_lastGeneratedIds[(byte)type], num);
        }
#endif

        /// <summary>
        /// Registers entity with given ID. Do not call this directly, it is called automatically
        /// when EntityID is first time assigned.
        /// </summary>
        /// <param name="entity"></param>
        public static void AddEntityWithId(IMyEntity entity)
        {
            Debug.Assert(entity != null, "Adding null entity. This can't happen.");
            Debug.Assert(!m_entityList.ContainsKey(entity.EntityId), "Entity with this key (" + entity.EntityId + ") already exists in entity list! This can't happen.");
            Debug.Assert(!m_entityList.ContainsValue(entity), "Entity is already registered by different ID. This can't happen.");
            m_entityList.Add(entity.EntityId, entity);
        }

        /// <summary>
        /// Allocated new entity ID (won't add to list)
        /// Entity with this ID should be added immediatelly
        /// </summary>
        public static long AllocateId(ID_OBJECT_TYPE objectType = ID_OBJECT_TYPE.ENTITY, ID_ALLOCATION_METHOD generationMethod = ID_ALLOCATION_METHOD.RANDOM)
        {
            Debug.Assert(objectType != ID_OBJECT_TYPE.NPC, "NPC identity IDs are obsolete!");
            Debug.Assert(objectType != ID_OBJECT_TYPE.SPAWN_GROUP, "SPAWN_GROUP identity IDs are obsolete!");

            long generatedNumber;
            if (generationMethod == ID_ALLOCATION_METHOD.RANDOM)
            {
                // We can use the MyRandom RNG, because its instance is per-thread
                generatedNumber = MyRandom.Instance.NextLong() & 0x00FFFFFFFFFFFFFF;
            }
            else
            {
                Debug.Assert(generationMethod == ID_ALLOCATION_METHOD.SERIAL_START_WITH_1, "Unknown entity ID generation method!");
                generatedNumber = Interlocked.Increment(ref m_lastGeneratedIds[(byte)objectType]);
            }

            return ConstructId(objectType, generatedNumber);
        }

        //Use only for debugginf/info - older versions can have any random Id!
        public static ID_OBJECT_TYPE GetIdObjectType(long id)
        {
            return (ID_OBJECT_TYPE)(id >> 56);
        }

        public static long GetIdUniqueNumber(long id)
        {
            return id & 0x00FFFFFFFFFFFFFF;
        }

        /**
         * Construct an ID using the hash from a string.
         */
        public static long ConstructIdFromString(ID_OBJECT_TYPE type, string uniqueString)
        {
            Debug.Assert(!string.IsNullOrEmpty(uniqueString), "Unique string was incorrect!");
            long eid = uniqueString.GetHashCode64();
            eid = (eid >> 8) + eid + (eid << 13);
            return (eid & 0x00FFFFFFFFFFFFFF) | ((long)type << 56);
        }

        public static long ConstructId(ID_OBJECT_TYPE type, long uniqueNumber)
        {
            Debug.Assert(((ulong)uniqueNumber & 0xFF00000000000000) == 0, "Unique number was incorrect!");
            return (uniqueNumber & 0x00FFFFFFFFFFFFFF) | ((long)type << 56);
        }

        public static long FixObsoleteIdentityType(long id)
        {
            if (GetIdObjectType(id) == ID_OBJECT_TYPE.NPC || GetIdObjectType(id) == ID_OBJECT_TYPE.SPAWN_GROUP)
            {
                id = ConstructId(ID_OBJECT_TYPE.IDENTITY, GetIdUniqueNumber(id));
            }

            return id;
        }

        public static void RemoveEntity(long entityId)
        {
            //    Debug.Assert(m_entityList.ContainsKey(entityId), "Attempting to remove already removed entity. This shouldn't happen.");
            m_entityList.Remove(entityId);
        }

        public static IMyEntity GetEntityById(long entityId)
        {
            IMyEntity entity;
            bool found = m_entityList.TryGetValue(entityId, out entity);
            // If called from thread other than main, we have to explicitly search the main entity dictionary as well
            if (!found && m_perThreadData != null)
                found = m_mainData.EntityList.TryGetValue(entityId, out entity);
            return entity;
        }

        public static bool TryGetEntity(long entityId, out IMyEntity entity)
        {
            bool found = m_entityList.TryGetValue(entityId, out entity);
            // If called from thread other than main, we have to explicitly search the main entity dictionary as well
            if (!found && m_perThreadData != null)
                found = m_mainData.EntityList.TryGetValue(entityId, out entity);
            return found;
        }

        public static bool TryGetEntity<T>(long entityId, out T entity) where T : class ,IMyEntity
        {
            IMyEntity e;
            bool result = TryGetEntity(entityId, out e);
            entity = e as T;
            return result && entity != null;
        }

        public static bool ExistsById(long entityId)
        {
            return m_entityList.ContainsKey(entityId) || (m_perThreadData != null && m_mainData.EntityList.ContainsKey(entityId));
        }

        /// <summary>
        /// Changes ID by which an entity is registered. Do not call this directly, it is called automatically when
        /// EntityID changes.
        /// </summary>
        /// <param name="entity">Entity whose ID has changed.</param>
        /// <param name="oldId">Old ID of the entity.</param>
        /// <param name="newId">New ID of the entity.</param>
        public static void SwapRegisteredEntityId(IMyEntity entity, long oldId, long newId)
        {
            //Debug.Assert(m_entityList.ContainsKey(oldId), "Old ID of the entity does not exist. This can't happen.");
            //Debug.Assert(!m_entityList.ContainsKey(newId), "New ID of the entity already exists. This can't happen.");
            //Debug.Assert(entity != null, "Entity is null. This can't happen.");
            //Debug.Assert(m_entityList[oldId] == entity, "Entity assigned to old ID is different. This can't happen.");
            //Debug.Assert(m_entityList.ContainsValue(entity), "Entity is not in the list. This can't happen.");

            var result = m_entityList[oldId];
            m_entityList.Remove(oldId);
            m_entityList[newId] = result;
            Debug.Assert(result == entity, "Entity had different EntityId");
        }

        public static void Clear()
        {
            m_entityList.Clear();
        }
    }
}
