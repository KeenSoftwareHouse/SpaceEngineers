using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Profiler;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.World
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyGlobalEvents : MySessionComponentBase
    {
        private static SortedSet<MyGlobalEventBase> m_globalEvents = new SortedSet<MyGlobalEventBase>();
        public static bool EventsEmpty
        {
            get
            {
                return m_globalEvents.Count == 0;
            }
        }

        private int m_elapsedTimeInMilliseconds = 0;
        private int m_previousTime = 0;

        static readonly int GLOBAL_EVENT_UPDATE_RATIO_IN_MS = 2000;

        public override void LoadData()
        {
            m_globalEvents.Clear();

            base.LoadData();
        }

        protected override void UnloadData()
        {
            m_globalEvents.Clear();

            base.UnloadData();
        }

        public void Init(MyObjectBuilder_GlobalEvents objectBuilder)
        {
            foreach (var eventBuilder in objectBuilder.Events)
            {
                m_globalEvents.Add(MyGlobalEventFactory.CreateEvent(eventBuilder));
            }
        }

        public static MyObjectBuilder_GlobalEvents GetObjectBuilder()
        {
            MyObjectBuilder_GlobalEvents objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_GlobalEvents>();

            foreach (var globalEvent in m_globalEvents)
            {
                objectBuilder.Events.Add(globalEvent.GetObjectBuilder());
            }

            return objectBuilder;
        }

        public override void BeforeStart()
        {
            m_previousTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public override void UpdateBeforeSimulation()
        {
            // Events are executed on the server only
            if (!Sync.IsServer)
                return;

            m_elapsedTimeInMilliseconds += MySandboxGame.TotalGamePlayTimeInMilliseconds - m_previousTime;
            m_previousTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            if (m_elapsedTimeInMilliseconds < GLOBAL_EVENT_UPDATE_RATIO_IN_MS)
                return;

            // Subtract elapsed time from the activation time of all events
            foreach (MyGlobalEventBase e in m_globalEvents)
            {
                e.SetActivationTime(TimeSpan.FromTicks(e.ActivationTime.Ticks - m_elapsedTimeInMilliseconds*TimeSpan.TicksPerMillisecond));
            }

            // Execute elapsed events
            MyGlobalEventBase globalEvent = m_globalEvents.FirstOrDefault();
            while (globalEvent != null && globalEvent.IsInPast)
            {
                m_globalEvents.Remove(globalEvent);

                if (globalEvent.Enabled)
                {
                    ProfilerShort.Begin(globalEvent.Definition.Id.ToString());
                    StartGlobalEvent(globalEvent);
                    ProfilerShort.End();
                }

                // Reschedule periodic events. Test whether the handler did not reschedule the event
                if (globalEvent.IsPeriodic)
                {
                    if (globalEvent.RemoveAfterHandlerExit)
                    {
                        m_globalEvents.Remove(globalEvent);
                    }
                    else if (!m_globalEvents.Contains(globalEvent))
                    {
                        globalEvent.RecalculateActivationTime();
                        AddGlobalEvent(globalEvent);
                    }
                }

                globalEvent = m_globalEvents.FirstOrDefault();
            }

            m_elapsedTimeInMilliseconds = 0;
        }

        public override void Draw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_EVENTS)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 500.0f), "Upcoming events:", Color.White, 1.0f);
                StringBuilder sb = new StringBuilder();
                float position = 530.0f;
                foreach (var globalEvent in m_globalEvents)
                {
                    int hours = (int)(globalEvent.ActivationTime.TotalHours);
                    int minutes = globalEvent.ActivationTime.Minutes;
                    int seconds = globalEvent.ActivationTime.Seconds;

                    sb.Clear();
                    sb.AppendFormat("{0}:{1:D2}:{2:D2}", hours, minutes, seconds);
                    sb.AppendFormat(" {0}: {1}", globalEvent.Enabled ? "ENABLED" : "--OFF--", globalEvent.Definition.DisplayNameString ?? globalEvent.Definition.Id.SubtypeName);

                    MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, position), sb.ToString(), globalEvent.Enabled ? Color.White : Color.Gray, 0.8f);

                    position += 20.0f;
                }
            }
        }

        public static MyGlobalEventBase GetEventById(MyDefinitionId defId)
        {
            foreach (var globalEvent in m_globalEvents)
            {
                if (globalEvent.Definition.Id == defId)
                    return globalEvent;
            }

            return null;
        }

        private static Predicate<MyGlobalEventBase> m_removalPredicate = RemovalPredicate;
        private static MyDefinitionId m_defIdToRemove;
        private static bool RemovalPredicate(MyGlobalEventBase globalEvent)
        {
            return globalEvent.Definition.Id == m_defIdToRemove;
        }

        public static void RemoveEventsById(MyDefinitionId defIdToRemove)
        {
            m_defIdToRemove = defIdToRemove;
            m_globalEvents.RemoveWhere(m_removalPredicate);
        }

        public static void AddGlobalEvent(MyGlobalEventBase globalEvent)
        {
            m_globalEvents.Add(globalEvent);
        }

        public static void RemoveGlobalEvent(MyGlobalEventBase globalEvent)
        {
            m_globalEvents.Remove(globalEvent);
        }

        public static void RescheduleEvent(MyGlobalEventBase globalEvent, TimeSpan time)
        {
            m_globalEvents.Remove(globalEvent);
            globalEvent.SetActivationTime(time);
            m_globalEvents.Add(globalEvent);
        }

        public static void LoadEvents(MyObjectBuilder_GlobalEvents eventsBuilder)
        {
            if (eventsBuilder == null) return;

            foreach (var globalEventBuilder in eventsBuilder.Events)
            {
                MyGlobalEventBase globalEvent = MyGlobalEventFactory.CreateEvent(globalEventBuilder);

                Debug.Assert(globalEvent == null || globalEvent.IsHandlerValid, "Event handler could not be found on load. Call a programmer please! You can ignore this, if you don't mind the given event not happening.");
                if (globalEvent != null && globalEvent.IsHandlerValid)
                    m_globalEvents.Add(globalEvent);
            }
        }

        private void StartGlobalEvent(MyGlobalEventBase globalEvent)
        {
            AddGlobalEventToEventLog(globalEvent);
			if (globalEvent.IsHandlerValid)
			{
				globalEvent.Action.Invoke(this, new object[] { globalEvent });
			}
        }

        private void AddGlobalEventToEventLog(MyGlobalEventBase globalEvent)
        {
            MySandboxGame.Log.WriteLine("MyGlobalEvents.StartGlobalEvent: " + globalEvent.Definition.Id.ToString());
        }

        public static void EnableEvents()
        {
            foreach (var globalEvent in m_globalEvents)
            {
                globalEvent.Enabled = true;
            }
        }

        internal static void DisableEvents()
        {
            foreach (var globalEvent in m_globalEvents)
            {
                globalEvent.Enabled = false;
            }
        }
    }
}
