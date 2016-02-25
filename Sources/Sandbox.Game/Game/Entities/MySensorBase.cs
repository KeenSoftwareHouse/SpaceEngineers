using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Entities
{
    internal delegate void SensorFilterHandler(MySensorBase sender, MyEntity detectedEntity, ref bool processEntity);
    internal delegate void EntitySensorHandler(MySensorBase sender, MyEntity entity);

    internal class MySensorBase : MyEntity
    {
        public enum EventType : byte
        {
            None,
            Add,
            Delete,
        }

        class DetectedEntityInfo
        {
            public bool Moved;
            public EventType EventType;
        }

        Stack<DetectedEntityInfo> m_unusedInfos = new Stack<DetectedEntityInfo>();
        Dictionary<MyEntity, DetectedEntityInfo> m_detectedEntities = new Dictionary<MyEntity, DetectedEntityInfo>(new InstanceComparer<MyEntity>());
        List<MyEntity> m_deleteList = new List<MyEntity>();

        Action<MyPositionComponentBase> m_entityPositionChanged;
        Action<MyEntity> m_entityClosed;

        public MySensorBase()
        {
            Save = false;
            this.m_entityPositionChanged = new Action<MyPositionComponentBase>(entity_OnPositionChanged);
            this.m_entityClosed = new Action<MyEntity>(entity_OnClose);
        }

        public MyEntity GetClosestEntity(Vector3 position)
        {
            MyEntity result = null;
            var minDistSq = double.MaxValue;

            foreach (var e in m_detectedEntities)
            {
                var distSq = (position - e.Key.PositionComp.GetPosition()).LengthSquared();
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    result = e.Key;
                }
            }
            return result;
        }

        public event SensorFilterHandler Filter;

        public event EntitySensorHandler EntityEntered;
        public event EntitySensorHandler EntityMoved;
        public event EntitySensorHandler EntityLeft;

        DetectedEntityInfo GetInfo()
        {
            if (m_unusedInfos.Count == 0)
                return new DetectedEntityInfo();
            else
                return m_unusedInfos.Pop();
        }

        protected void TrackEntity(MyEntity entity)
        {
            System.Diagnostics.Debug.Assert(!entity.Closed);

            if (FilterEntity(entity))
                return;

            DetectedEntityInfo info;
            if (!m_detectedEntities.TryGetValue(entity, out info)) // New entity
            {
                entity.PositionComp.OnPositionChanged += m_entityPositionChanged;
                entity.OnClose += m_entityClosed;
                info = GetInfo();
                info.Moved = false;
                info.EventType = EventType.Add;
                m_detectedEntities[entity] = info;
            }
            else if (info.EventType == EventType.Delete) // When contact came from existing entity, it means it still overlaps, change "Delete" to "None"
            {
                info.EventType = EventType.None;
            }
        }

        protected bool FilterEntity(MyEntity entity)
        {
            var handler = Filter;
            if (handler != null)
            {
                bool processEntity = true;
                handler(this, entity, ref processEntity);
                if (!processEntity)
                    return true;
            }
            return false;
        }

        public bool AnyEntityWithState(EventType type)
        {
            return m_detectedEntities.Any(s => s.Value.EventType == type);
        }

        public bool HasAnyMoved()
        {
            return m_detectedEntities.Any(s => s.Value.Moved);
        }

        void UntrackEntity(MyEntity entity)
        {
            entity.PositionComp.OnPositionChanged -= m_entityPositionChanged;
            entity.OnClose -= m_entityClosed;
        }

        void entity_OnClose(MyEntity obj)
        {
            DetectedEntityInfo info;
            if (m_detectedEntities.TryGetValue(obj, out info))
            {
                info.EventType = EventType.Delete;
            }
        }

        void entity_OnPositionChanged(MyPositionComponentBase entity)
        {
            DetectedEntityInfo info;
            if (m_detectedEntities.TryGetValue(entity.Container.Entity as MyEntity, out info))
            {
                info.Moved = true;
            }
        }

        void raise_EntityEntered(MyEntity entity)
        {
            var handler = EntityEntered;
            if (handler != null) handler(this, entity);
        }

        void raise_EntityMoved(MyEntity entity)
        {
            var handler = EntityMoved;
            if (handler != null) handler(this, entity);
        }

        void raise_EntityLeft(MyEntity entity)
        {
            var handler = EntityLeft;
            if (handler != null) handler(this, entity);
        }

        public void RaiseAllMove()
        {
            var handler = EntityMoved;
            if (handler != null)
            {
                foreach (var e in m_detectedEntities)
                {
                    handler(this, e.Key);
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            foreach (var pair in m_detectedEntities)
            {
                if (pair.Value.EventType == EventType.Delete)
                {
                    // We can prevent raising EntityLeft when entity is closed, if we want to
                    UntrackEntity(pair.Key);
                    raise_EntityLeft(pair.Key);
                    m_deleteList.Add(pair.Key);
                    m_unusedInfos.Push(pair.Value);
                    continue;
                }
                else if (pair.Value.EventType == EventType.Add)
                {
                    raise_EntityEntered(pair.Key);
                }
                else if (pair.Value.Moved)
                {
                    raise_EntityMoved(pair.Key);
                }

                // Set all to delete (in next frame, entities which does not collide will be deleted)
                // Reset state
                pair.Value.Moved = false;
                pair.Value.EventType = EventType.Delete;
            }
            foreach (var e in m_deleteList)
            {
                m_detectedEntities.Remove(e);
            }
            m_deleteList.Clear();
        }
    }
}
