using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Utils;
using VRage.Trace;
using VRage.Library.Utils;
using VRage.ModAPI;

namespace VRage
{
    public struct MyEntityIdentifier
    {
        const int DEFAULT_DICTIONARY_SIZE = 32768;

        static Dictionary<long, IMyEntity> m_entityList = new Dictionary<long, IMyEntity>(DEFAULT_DICTIONARY_SIZE);
        static bool m_allocationSuspended = false;

        /// <summary>
        /// Freezes allocating entity ids.
        /// This is important, because during load, no entity cannot allocate new id, because it could allocate id which already has entity which will be loaded soon.
        /// </summary>
        public static bool AllocationSuspended
        {
            get
            {
                return m_allocationSuspended;
            }
            set
            {
                m_allocationSuspended = value;
            }
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
        }

        public enum ID_ALLOCATION_METHOD : byte
        {
            RANDOM = 0,
            SERIAL_START_WITH_1 = 1,
        }

        private static long[] m_lastGeneratedIds = null;

        static MyEntityIdentifier()
        {
            m_lastGeneratedIds = new long[(int)MyEnum<ID_OBJECT_TYPE>.MaxValue.Value + 1];
        }

        public static void Reset()
        {
            for (int i = 0; i < (int)MyEnum<ID_OBJECT_TYPE>.MaxValue.Value + 1; ++i)
            {
                m_lastGeneratedIds[i] = 0;
            }
        }

        /// <summary>
        /// This method is used when loading existing entity IDs to track the last generated ID
        /// </summary>
        public static void MarkIdUsed(long id)
        {
            long num = GetIdUniqueNumber(id);
            ID_OBJECT_TYPE type = GetIdObjectType(id);

            if (m_lastGeneratedIds[(byte)type] < num)
                m_lastGeneratedIds[(byte)type] = num;
        }

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
                generatedNumber = m_lastGeneratedIds[(byte)objectType] + 1;
                m_lastGeneratedIds[(byte)objectType] = generatedNumber;
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

        public static long ConstructId(ID_OBJECT_TYPE type, long uniqueNumber)
        {
            Debug.Assert(((ulong)uniqueNumber & 0xFF00000000000000) == 0, "Unique number was incorrect!");
            return (uniqueNumber & 0x00FFFFFFFFFFFFFF) | ((long)ID_OBJECT_TYPE.IDENTITY << 56);
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

        public static bool TryGetEntity(long entityId, out IMyEntity entity)
        {
            return m_entityList.TryGetValue(entityId, out entity);
        }

        public static bool TryGetEntity<T>(long entityId, out T entity) where T : class ,IMyEntity
        {
            IMyEntity e;
            bool result = TryGetEntity(entityId, out e);
            entity = e as T;
            return result && entity != null;
        }

        public static IMyEntity GetEntityById(long entityId)
        {
            return m_entityList[entityId];
        }

        public static bool ExistsById(long entityId)
        {
            return m_entityList.ContainsKey(entityId);
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

            RemoveEntity(oldId);
            m_entityList[newId] = entity;
        }

        public static void Clear()
        {
            m_entityList.Clear();
        }
    }
}
