using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MySessionComponentTriggerSystem : MySessionComponentBase
    {
        private Dictionary<MyEntity, List<MyTriggerComponent>> m_triggers = new Dictionary<MyEntity, List<MyTriggerComponent>>();
        private List<MyTriggerComponent> m_addCache = new List<MyTriggerComponent>();
        private List<MyEntity> m_removeCacheEntity = new List<MyEntity>();
        private List<MyTuple<MyEntity, MyTriggerComponent>> m_removeCacheTrigger = new List<MyTuple<MyEntity, MyTriggerComponent>>();
        private bool m_updateLock = false;

        public static MySessionComponentTriggerSystem Static;

        public override void LoadData()
        {
            base.LoadData();
            Static = this;
        }

        public MyEntity GetTriggerEntity(string entityName, out MyTriggerComponent foundTrigger)
        {
            foundTrigger = null;
            bool orig = m_updateLock;
            m_updateLock = true;
            foreach (var triggerList in m_triggers.Values)
            {
                foreach (var trigger in triggerList)
                {
                    if (trigger is MyAreaTriggerComponent && ((MyAreaTriggerComponent)trigger).Name.Equals(entityName))
                    {
                        foundTrigger = trigger;
                        return (MyEntity)trigger.Entity;
                    }
                        
                }
            }
            m_updateLock = orig;
            return null;
        }

        public void AddTrigger(MyTriggerComponent trigger)
        {
            if (m_updateLock)
                m_addCache.Add(trigger);
            else
                AddTriggerCached(trigger);
        }

        private void AddTriggerCached(MyTriggerComponent trigger)
        {
            if (!m_triggers.ContainsKey((MyEntity)trigger.Entity))
            {
                m_triggers.Add((MyEntity)trigger.Entity, new List<MyTriggerComponent> { trigger });
            }
            else
            {
                Debug.Assert(!m_triggers[(MyEntity)trigger.Entity].Contains(trigger));
                m_triggers[(MyEntity)trigger.Entity].Add(trigger);
            }
        }

        public void RemoveTrigger(MyEntity entity, MyTriggerComponent trigger)
        {
            if (m_updateLock)
                m_removeCacheTrigger.Add(new MyTuple<MyEntity, MyTriggerComponent>(entity, trigger));
            else
                RemoveTriggerCached(entity, trigger);
        }

        private void RemoveTriggerCached(MyEntity entity, MyTriggerComponent trigger)
        {
            if (m_triggers.ContainsKey(entity) && m_triggers[entity].Contains(trigger))
            {
                if (m_triggers[entity].Count == 1)
                {
                    m_triggers[entity].Clear();
                    m_triggers.Remove(entity);
                }
                else
                    m_triggers[entity].Remove(trigger);
            }
        }

        public void RemoveAllTriggers(MyEntity entity)
        {
            if (m_updateLock)
                m_removeCacheEntity.Add(entity);
            else
                RemoveAllTriggersCached(entity);
        }

        private void RemoveAllTriggersCached(MyEntity entity)
        {
            if (m_triggers.ContainsKey(entity))
            {
                m_triggers[entity].Clear();
                m_triggers.Remove(entity);
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            //cached data
            for (int i = 0; i < m_addCache.Count; i++)
            {
                AddTriggerCached(m_addCache[i]);
            }
            m_addCache.Clear();
            for (int i = 0; i < m_removeCacheTrigger.Count; i++)
            {
                RemoveTriggerCached(m_removeCacheTrigger[i].Item1, m_removeCacheTrigger[i].Item2);
            }
            m_removeCacheTrigger.Clear();
            for (int i = 0; i < m_removeCacheEntity.Count; i++)
            {
                RemoveAllTriggersCached(m_removeCacheEntity[i]);
            }
            m_removeCacheEntity.Clear();

            //update
            m_updateLock = true;
            foreach (var triggerList in m_triggers.Values)
            {
                foreach (var trigger in triggerList)
                {
                    trigger.Update();
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER)
            {
                foreach (var triggerList in m_triggers.Values)
                {
                    foreach (var trigger in triggerList)
                    {
                        trigger.DebugDraw();
                    }
                }
            }
            m_updateLock = false;
        }

        public override void Draw()
        {
            base.Draw();
        }

        public bool IsAnyTriggerActive(MyEntity entity)//true if there are no triggers
        {
            m_updateLock = true;
            if(m_triggers.ContainsKey(entity))
            {
                foreach (var trigger in m_triggers[entity])
                {
                    if (trigger.Enabled)
                        return true;
                }
                return (m_triggers[entity].Count == 0);
            }
            return true;
        }

        public bool Contains(MyTriggerComponent trigger)
        {
            m_updateLock = true;

            foreach (var tuple in m_removeCacheTrigger)
            {
                if (tuple.Item2 == trigger)
                {
                    return false;
                }
            }

            if(m_addCache.Contains(trigger))
                return true;

            foreach (var triggerList in m_triggers.Values)
            {
                if (triggerList.Contains(trigger))
                    return true;
            }
            return false;
        }

        public List<MyTriggerComponent> GetIntersectingTriggers(Vector3D position)
        {
            var results = new List<MyTriggerComponent>();
            m_updateLock = true;

            foreach (var triggerList in m_triggers.Values)
            {
                foreach (var trigger in triggerList)
                {
                    if (trigger.Contains(position))
                        results.Add(trigger);
                }
            }

            return results;
        }
    }
}
