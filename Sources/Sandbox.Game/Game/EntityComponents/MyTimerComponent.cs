using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;


namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_TimerComponent))]
    public class MyTimerComponent : MyGameLogicComponent
    {
        public bool Repeat = false;
        public float TimeToEvent;
        public Action<MyEntityComponentContainer> EventToTrigger;
        private float m_setTimeMin;
        private float m_originTimeMin;
        public bool TimerEnabled = true;
        public bool RemoveEntityOnTimer = false;
        private bool m_resetOrigin;

        public void SetRemoveEntityTimer(float timeMin)
        {
            RemoveEntityOnTimer = true;
            SetTimer(timeMin, (MyEntityComponentContainer container) => { container.Entity.Close(); } );
        }

        public void SetTimer(float timeMin, Action<MyEntityComponentContainer> triggerEvent, bool start = true, bool repeat = false)
        {
            TimeToEvent = -1;
            m_setTimeMin = timeMin;
            Repeat = repeat;
            EventToTrigger = triggerEvent;
            TimerEnabled = false;

            if (start)
            {
                StartTiming();
            }
        }

        public void ClearEvent()
        {
            EventToTrigger = null;
        }

        private void StartTiming()
        {
            System.Diagnostics.Debug.Assert(MyPerGameSettings.GetElapsedMinutes != null, "This component must be used together with time!");
            TimeToEvent = m_setTimeMin;
            TimerEnabled = true;
            m_originTimeMin = MyPerGameSettings.GetElapsedMinutes();
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            if (!TimerEnabled)
            {
                return;
            }
            var currentTime = MyPerGameSettings.GetElapsedMinutes();
            if (m_resetOrigin)
            {              
                m_originTimeMin = currentTime + m_setTimeMin - TimeToEvent;
                m_resetOrigin = false;
            }
            TimeToEvent = m_originTimeMin + m_setTimeMin - currentTime;
            if (TimeToEvent <= 0)
            {
                if (EventToTrigger != null)
                {
                    EventToTrigger(Container);
                }
                if (Repeat)
                {
                    m_originTimeMin = MyPerGameSettings.GetElapsedMinutes();
                }
                else
                {
                    TimerEnabled = false;
                }
            }
        }

        public override void OnAddedToContainer()
        {
            System.Diagnostics.Debug.Assert(MyPerGameSettings.GetElapsedMinutes != null, "This timing func not defined!");
            base.OnAddedToContainer();
            if (TimerEnabled)
            {
                m_resetOrigin = true;
            }
            Entity.NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override MyObjectBuilder_ComponentBase Serialize()
        {
            var builder = MyComponentFactory.CreateObjectBuilder(this) as MyObjectBuilder_TimerComponent;
            builder.Repeat = Repeat;
            builder.TimeToEvent = TimeToEvent;
            builder.SetTimeMinutes = m_setTimeMin;
            builder.TimerEnabled = TimerEnabled;
            builder.RemoveEntityOnTimer = RemoveEntityOnTimer;
            return builder;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase baseBuilder)
        {
            var builder = baseBuilder as MyObjectBuilder_TimerComponent;
            Repeat = builder.Repeat;
            TimeToEvent = builder.TimeToEvent;
            m_setTimeMin = builder.SetTimeMinutes;
            TimerEnabled = builder.TimerEnabled;
            RemoveEntityOnTimer = builder.RemoveEntityOnTimer;
            if (RemoveEntityOnTimer)
            {
                EventToTrigger = (MyEntityComponentContainer container) => { container.Entity.Close(); };
            }
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override VRage.ObjectBuilders.MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return null;
        }
    }
}
