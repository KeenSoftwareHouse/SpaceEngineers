using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Common.Utils;
using VRage.Trace;

namespace Sandbox.Game.Entities
{
    public struct MyEntityIdentifier
    {
        const int DEFAULT_DICTIONARY_SIZE = 32768;

        static Dictionary<long, MyEntity> m_entityList = new Dictionary<long, MyEntity>(DEFAULT_DICTIONARY_SIZE);
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
                if (MyFakes.ENABLE_ENTITY_ID_ALLOCATION_SUSPENSION_LOG)
                {
                    System.Diagnostics.StackTrace trace = new StackTrace();
                    MySandboxGame.Log.WriteLine(trace.GetFrame(1).GetMethod() + (value ? ": Allocation suspended" : ": Allocation resumed"));
                }
            }
        }

        /// <summary>
        /// Registers entity with given ID. Do not call this directly, it is called automatically
        /// when EntityID is first time assigned.
        /// </summary>
        /// <param name="entity"></param>
        public static void AddEntityWithId(MyEntity entity)
        {
            Debug.Assert(entity != null, "Adding null entity. This can't happen.");
            Debug.Assert(!m_entityList.ContainsKey(entity.EntityId), "Entity with this key (" + entity.EntityId + ") already exists in entity list! This can't happen.");
            Debug.Assert(!m_entityList.ContainsValue(entity), "Entity is already registered by different ID. This can't happen.");
            m_entityList.Add(entity.EntityId, entity);
        }

        public enum ID_OBJECT_TYPE : byte 
        {
            UNKNOWN = 0,
            ENTITY = 1,
            PLAYER = 2,
            FACTION = 3,
            NPC = 4,
            SPAWN_GROUP = 5,
        }

        public static bool IsIdentityObjectType(ID_OBJECT_TYPE identityType)
        {
            return identityType == ID_OBJECT_TYPE.PLAYER || identityType == ID_OBJECT_TYPE.NPC || identityType == ID_OBJECT_TYPE.SPAWN_GROUP;
        }

        public enum ID_RESERVED : byte
        {
            UNKNOWN = 0,
        }

        /// <summary>
        /// Allocated new entity ID (won't add to list)
        /// Entity with this ID should be added immediatelly
        /// </summary>
        public static long AllocateId(ID_OBJECT_TYPE objectType = ID_OBJECT_TYPE.ENTITY)
        {
            // We can use the MyRandom RNG, because its instance is per-thread
            return MyRandom.Instance.NextLong() & 0x0000FFFFFFFFFFFF | ((long)objectType << 56);
        }

        //Use only for debugginf/info - older versions can have any random Id!
        public static ID_OBJECT_TYPE GetIdObjectType(long id)
        {
            return (ID_OBJECT_TYPE)(id >> 56);
        }

        public static void RemoveEntity(long entityId)
        {
            Debug.Assert(m_entityList.ContainsKey(entityId), "Attempting to remove already removed entity. This shouldn't happen.");
            m_entityList.Remove(entityId);
        }

        public static bool TryGetEntity(long entityId, out MyEntity entity)
        {
            return m_entityList.TryGetValue(entityId, out entity);
        }

        public static bool TryGetEntity<T>(long entityId, out T entity) where T : MyEntity
        {
            MyEntity e;
            bool result = TryGetEntity(entityId, out e);
            entity = e as T;
            return result && entity != null;
        }

        public static MyEntity GetEntityById(long entityId)
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
        public static void SwapRegisteredEntityId(MyEntity entity, long oldId, long newId)
        {
            Debug.Assert(m_entityList.ContainsKey(oldId), "Old ID of the entity does not exist. This can't happen.");
            Debug.Assert(!m_entityList.ContainsKey(newId), "New ID of the entity already exists. This can't happen.");
            Debug.Assert(entity != null, "Entity is null. This can't happen.");
            Debug.Assert(m_entityList[oldId] == entity, "Entity assigned to old ID is different. This can't happen.");
            Debug.Assert(m_entityList.ContainsValue(entity), "Entity is not in the list. This can't happen.");

            RemoveEntity(oldId);
            m_entityList[newId] = entity;
        }

        public static void Clear()
        {
            m_entityList.Clear();
        }
    }
}
