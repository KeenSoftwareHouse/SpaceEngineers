using Havok;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MySessionComponentEntityTrigger : MySessionComponentBase
    {
        List<MyUpdateTriggerComponent> m_values = new List<MyUpdateTriggerComponent>();
        MyConcurrentDictionary<MyEntity, MyUpdateTriggerComponent> m_triggers = new MyConcurrentDictionary<MyEntity, MyUpdateTriggerComponent>();

        public static MySessionComponentEntityTrigger Static;

        public override void LoadData()
        {
            base.LoadData();
            Static = this;
        }

        public void AddTrigger(MyEntity entity, int triggerSize)
        {
            if (entity == null)
                return;
            MyUpdateTriggerComponent t;
            if (m_triggers.TryGetValue(entity, out t))
            {
                t.Size = triggerSize;
            }
            else
            {
                t = new MyUpdateTriggerComponent(triggerSize);
                entity.Components.Add(t);
            }
        }

        public void AddTrigger(MyUpdateTriggerComponent trigger)
        {
            Debug.Assert(!m_triggers.ContainsKey((MyEntity)trigger.Entity));
            m_triggers[(MyEntity)trigger.Entity] = trigger;
        }

        public void RemoveTrigger(MyEntity entity)
        {
            MyUpdateTriggerComponent t;
            if(m_triggers.TryGetValue(entity, out t))
            {
                m_triggers.Remove(entity);
            }
        }

        public override void UpdateAfterSimulation()
        {
            m_values.Clear();
            base.UpdateAfterSimulation();

            m_triggers.GetValues(m_values);
            foreach (var trigger in m_values)
            {
                trigger.Update();
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER)
            {
                foreach (var trigger in m_values)
                {
                    trigger.DebugDraw();
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
        }

        public bool IsActive(MyEntity entity)
        {
            MyUpdateTriggerComponent trigger;
            if(m_triggers.TryGetValue(entity,out trigger))
            {
                return trigger.IsEnabled();
            }
            return true;
        }
    }
}
