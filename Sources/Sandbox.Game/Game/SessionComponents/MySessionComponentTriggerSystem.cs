using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

using CachingHashSet = VRage.Collections.CachingHashSet<Sandbox.Game.Components.MyTriggerComponent>;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MySessionComponentTriggerSystem : MySessionComponentBase
    {
        private readonly Dictionary<MyEntity, CachingHashSet> m_triggers
            = new Dictionary<MyEntity, CachingHashSet>();

        private readonly FastResourceLock m_dictionaryLock = new FastResourceLock();

        public static MySessionComponentTriggerSystem Static;

        public override void LoadData()
        {
            base.LoadData();
            Static = this;
        }

        public MyEntity GetTriggersEntity(string triggerName, out MyTriggerComponent foundTrigger)
        {
            foundTrigger = null;

            foreach (var pair in m_triggers)
            {
                foreach (var component in pair.Value)
                {
                    var areaTrigger = component as MyAreaTriggerComponent;
                    if (areaTrigger != null && areaTrigger.Name == triggerName)
                    {
                        foundTrigger = component;
                        return pair.Key;
                    }
                }
            }

            return null;
        }

        public void AddTrigger(MyTriggerComponent trigger)
        {
            Debug.Assert(trigger != null, "Horrible Assertion! Call a programmer! Hurry!");
            MySandboxGame.AssertUpdateThread();

            if(Contains(trigger)) return;

            using(m_dictionaryLock.AcquireExclusiveUsing())
            {
                CachingHashSet triggerSet;
                if(m_triggers.TryGetValue((MyEntity)trigger.Entity, out triggerSet))
                {
                    triggerSet.Add(trigger);
                }
                else
                {
                    m_triggers[(MyEntity)trigger.Entity] = new CachingHashSet { trigger };
                }
            }
        }

        public void RemoveTrigger(MyEntity entity, MyTriggerComponent trigger)
        {
            using(m_dictionaryLock.AcquireExclusiveUsing())
            {
                CachingHashSet triggerSet;
                if (m_triggers.TryGetValue(entity, out triggerSet))
                {
                    triggerSet.Remove(trigger);
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            using (m_dictionaryLock.AcquireSharedUsing())
            {
                foreach (var triggerSet in m_triggers.Values)
                {
                    triggerSet.ApplyChanges();
                    foreach (var trigger in triggerSet)
                    {
                        trigger.Update();
                    }
                }
            }
        }

        public override void Draw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER)
            {
                using (m_dictionaryLock.AcquireSharedUsing())
                {
                    foreach (var triggerSet in m_triggers.Values)
                    {
                        foreach (var trigger in triggerSet)
                        {
                            trigger.DebugDraw();
                        }
                    }
                }
            }
        }

        public bool IsAnyTriggerActive(MyEntity entity)//true if there are no triggers
        {
            using (m_dictionaryLock.AcquireSharedUsing())
            {
                if (m_triggers.ContainsKey(entity))
                {
                    foreach (var trigger in m_triggers[entity])
                    {
                        if (trigger.Enabled)
                            return true;
                    }
                    return (m_triggers[entity].Count == 0);
                }
            }
            return true;
        }

        public bool Contains(MyTriggerComponent trigger)
        {
            using (m_dictionaryLock.AcquireSharedUsing())
            {
                foreach (var triggerSet in m_triggers.Values)
                {
                    if (triggerSet.Contains(trigger))
                        return true;
                }
            }
            return false;
        }

        public List<MyTriggerComponent> GetIntersectingTriggers(Vector3D position)
        {
            var results = new List<MyTriggerComponent>();

            using (m_dictionaryLock.AcquireSharedUsing())
            {
                foreach (var triggerList in m_triggers.Values)
                {
                    foreach (var trigger in triggerList)
                    {
                        if (trigger.Contains(position))
                            results.Add(trigger);
                    }
                }
            }

            return results;
        }
    }
}
