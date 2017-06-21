using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using System.Diagnostics;
using Sandbox.Game.EntityComponents.Systems;
using VRage.Game;


namespace Sandbox.Game.Components
{
    [MyComponentType(typeof(MyTimerComponent))]
    [MyComponentBuilder(typeof(MyObjectBuilder_TimerComponent))]
    public class MyTimerComponent : MyEntityComponentBase
    {
        public bool Repeat = false;
        public float TimeToEvent;
        public Action<MyEntityComponentContainer> EventToTrigger;
        private float m_setTimeMin;
        private float m_originTimeMin;
        public bool TimerEnabled = true;
        public bool RemoveEntityOnTimer = false;
        private bool m_resetOrigin;

        public override string ComponentTypeDebugString
        {
            get { return "Timer"; }
        }

        public void SetRemoveEntityTimer(float timeMin)
        {
            RemoveEntityOnTimer = true;
            SetTimer(timeMin, GetRemoveEntityOnTimerEvent());
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
            System.Diagnostics.Debug.Assert(Sandbox.Game.World.MySession.Static != null, "This component must be used together with MySession time!");
            TimeToEvent = m_setTimeMin;
            TimerEnabled = true;
            m_originTimeMin = (float)Sandbox.Game.World.MySession.Static.ElapsedGameTime.TotalMinutes;
        }

        public void Update()
        {
            if (!TimerEnabled)
            {
                return;
            }

            var currentTime = (float)Sandbox.Game.World.MySession.Static.ElapsedGameTime.TotalMinutes;
            if (m_resetOrigin)
            {
                m_originTimeMin = currentTime - m_setTimeMin + TimeToEvent;
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
                    m_originTimeMin = (float)Sandbox.Game.World.MySession.Static.ElapsedGameTime.TotalMinutes;
                }
                else
                {
                    TimerEnabled = false;
                }
            }
        }

        public override void OnAddedToContainer()
        {
            System.Diagnostics.Debug.Assert(Sandbox.Game.World.MySession.Static != null, "This component must be used together with MySession time!");
            base.OnAddedToContainer();
            if (TimerEnabled)
            {
                m_resetOrigin = true;
            }

            Debug.Assert(MyTimerComponentSystem.Static != null);
            MyTimerComponentSystem.Static.Register(this);
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            if (MyTimerComponentSystem.Static != null)
                MyTimerComponentSystem.Static.Unregister(this);
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
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
                EventToTrigger = GetRemoveEntityOnTimerEvent();
            }
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            var timerComponentDefinition = definition as MyTimerComponentDefinition;
            Debug.Assert(timerComponentDefinition != null);
            if (timerComponentDefinition != null)
            {
                TimerEnabled = timerComponentDefinition.TimeToRemoveMin > 0;
                m_setTimeMin = timerComponentDefinition.TimeToRemoveMin;
                TimeToEvent = m_setTimeMin;
                RemoveEntityOnTimer = timerComponentDefinition.TimeToRemoveMin > 0;

                if (RemoveEntityOnTimer)
                {
                    EventToTrigger = GetRemoveEntityOnTimerEvent();
                }
            }
        }

        private static Action<MyEntityComponentContainer> GetRemoveEntityOnTimerEvent()
        {
            return (MyEntityComponentContainer container) => { container.Entity.SyncObject.SendCloseRequest(); };
        }

    }
}
