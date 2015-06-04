using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Sandbox.Common;
using VRage.Plugins;
using VRage.ObjectBuilders;

namespace Sandbox.Game.World
{
    public delegate void GlobalEventHandler(object sender);

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]
    public class MyGlobalEventHandler : System.Attribute
    {
        public MyDefinitionId EventDefinitionId;

        public MyGlobalEventHandler(Type objectBuilderType, string subtypeName)
        {
            MyObjectBuilderType type = objectBuilderType;
            EventDefinitionId = new MyDefinitionId(type, subtypeName);
        }
    }

    public class MyGlobalEventFactory
    {
        static readonly Dictionary<MyDefinitionId, GlobalEventHandler> m_typesToHandlers;

        static MyGlobalEventFactory()
        {
            m_typesToHandlers = new Dictionary<MyDefinitionId, GlobalEventHandler>();

            RegisterEventTypesAndHandlers(Assembly.GetAssembly(typeof(MyGlobalEventBase)));
            RegisterEventTypesAndHandlers(MyPlugins.GameAssembly);
            RegisterEventTypesAndHandlers(MyPlugins.SandboxAssembly);
        }

        private static void RegisterEventTypesAndHandlers(Assembly assembly)
        {
            if (assembly == null) return;

            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods())
                {
                    if (!method.IsPublic || !method.IsStatic) continue;

                    var descriptorArray = method.GetCustomAttributes(typeof(MyGlobalEventHandler), false);
                    if (descriptorArray != null && descriptorArray.Length > 0)
                    {
                        foreach (var descriptor in descriptorArray)
                        {
                            MyGlobalEventHandler typedDescriptor = (MyGlobalEventHandler)descriptor;

                            RegisterHandler(typedDescriptor.EventDefinitionId, MethodInfoExtensions.CreateDelegate<GlobalEventHandler>(method));
                        }
                    }
                }
            }
        }

        private static void RegisterHandler(MyDefinitionId eventDefinitionId, GlobalEventHandler handler)
        {
            Debug.Assert(!m_typesToHandlers.ContainsKey(eventDefinitionId), "One event definition id can only have one event handler!");

            m_typesToHandlers[eventDefinitionId] = handler;
        }

        public static GlobalEventHandler GetEventHandler(MyDefinitionId eventDefinitionId)
        {
            GlobalEventHandler handler = null;
            m_typesToHandlers.TryGetValue(eventDefinitionId, out handler);
            return handler;
        }

        /// <summary>
        /// Use for creation of the event in code (ensures correct data class usage)
        /// </summary>
        public static EventDataType CreateEvent<EventDataType>(MyDefinitionId id)
            where EventDataType : MyGlobalEventBase, new()
        {
            var eventDefinition = MyDefinitionManager.Static.GetEventDefinition(id);

            Type eventDataType = typeof(EventDataType);

            EventDataType globalEvent = new EventDataType();
            globalEvent.InitFromDefinition(eventDefinition);
            return globalEvent;
        }

        /// <summary>
        /// Use for deserialization from a saved game
        /// </summary>
        public static MyGlobalEventBase CreateEvent(MyObjectBuilder_GlobalEventBase ob)
        {
            if (ob.DefinitionId.TypeId == MyObjectBuilderType.Invalid)
            {
                return CreateEventObsolete(ob);
            }

            var eventDefinition = MyDefinitionManager.Static.GetEventDefinition(ob.DefinitionId);
            if (eventDefinition == null) return null;

            var globalEvent = new MyGlobalEventBase();
            globalEvent.Init(ob);
            return globalEvent;
        }

        public static MyGlobalEventBase CreateEventObsolete(MyObjectBuilder_GlobalEventBase ob)
        {
            MyGlobalEventBase globalEvent = CreateEvent<MyGlobalEventBase>(GetEventDefinitionObsolete(ob.EventType));
            globalEvent.SetActivationTime(TimeSpan.FromMilliseconds(ob.ActivationTimeMs));
            globalEvent.Enabled = ob.Enabled;
            return globalEvent;
        }

        /// <summary>
        /// Gets the definition id of the event definition that corresponds to the event that used to have the given event type
        /// </summary>
        public static MyDefinitionId GetEventDefinitionObsolete(MyGlobalEventTypeEnum eventType)
        {
            if (eventType == MyGlobalEventTypeEnum.SpawnNeutralShip ||
                eventType == MyGlobalEventTypeEnum.SpawnCargoShip)
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventDefinition), "SpawnCargoShip");
            }
            if (eventType == MyGlobalEventTypeEnum.MeteorWave)
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventDefinition), "MeteorWave");
            }
            if (eventType == MyGlobalEventTypeEnum.April2014)
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventDefinition), "April2014");
            }
            return new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventDefinition));
        }
    }
}
