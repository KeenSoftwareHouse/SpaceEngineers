using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using VRage.ModAPI;
using VRage.Profiler;

namespace VRage.Game.Components.Session
{
    public class MyEventBus
    {

        protected interface IRegisteredInstance
        {
        }

        protected class RegisteredInstance<T> : IRegisteredInstance
        {
            public readonly string Name;
            public readonly object Instance;
            public readonly List<T> Data;

            public RegisteredInstance(string name, object instance)
            {
                Name = name;
                Instance = instance;
                Data = new List<T>();
            }

            public override int GetHashCode()
            {
                return Instance.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Instance.Equals(obj);
            }

            public void OnTriggered(T data)
            {
                Data.Add(data);
            }
        }

        private Dictionary<string, HashSet<IRegisteredInstance>> m_registeredInstances = new Dictionary<string, HashSet<IRegisteredInstance>>();
        private Stopwatch m_stopwatch = new Stopwatch();
        private int m_executionCounter = 0;

        public void OnEntityCreated(IMyEntity entity)
        {
            m_stopwatch.Start();
            var entityType = entity.GetType();
            foreach (var eventInfo in entityType.GetEvents())
            {
                var eventName = entityType.Name + "." + eventInfo.Name;

                HashSet<IRegisteredInstance> registeredInstancesOfType;
                if (!m_registeredInstances.TryGetValue(eventName, out registeredInstancesOfType))
                {
                    registeredInstancesOfType = new HashSet<IRegisteredInstance>();
                    m_registeredInstances.Add(eventName,registeredInstancesOfType);
                }

                { // Add the event handler
                    var parameters = eventInfo.EventHandlerType
                      .GetMethod("Invoke")
                      .GetParameters()
                      .Select(parameter => Expression.Parameter(parameter.ParameterType))
                      .ToArray();

                    if (parameters.Length == 1)
                    {
                        var newInstanceType = typeof (RegisteredInstance<>).MakeGenericType(parameters[0].Type);
                        var newInstance = Activator.CreateInstance(newInstanceType, eventName, entity);
                        var onTriggered = newInstanceType.GetMethod("OnTriggered");

                        var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, newInstance, onTriggered);
                        //var handler = Expression.Lambda(
                        //    eventInfo.EventHandlerType,
                        //    Expression.Call(Expression.Constant(newInstance), onTriggered, parameters[0]),
                        //    parameters
                        //    )
                        //    .Compile();

                        eventInfo.AddEventHandler(entity, handler);
                        registeredInstancesOfType.Add((IRegisteredInstance)newInstance);
                    }
                }
            }
            m_executionCounter++;
            m_stopwatch.Stop();
        }

        public void OnEntityRemove(IMyEntity entity)
        {
            //var entityType = entity.GetType();
            //foreach (var eventInfo in entityType.GetEvents())
            //{
            //    var eventName = entityType.Name + "." + eventInfo.Name;


            //}
        }


        //private bool AddEventHandler(EventInfo eventInfo, object entity, RegisteredInstance container)
        //{
        //    var parameters = eventInfo.EventHandlerType
        //      .GetMethod("Invoke")
        //      .GetParameters()
        //      .Select(parameter => Expression.Parameter(parameter.ParameterType))
        //      .ToArray();
        //    if(parameters.Length == 1)
        //    {

        //        var _onTriggered = typeof(RegisteredInstance).GetMethod("OnTriggered");
        //        //var d = Delegate.CreateDelegate(eventInfo.EventHandlerType, _event, _onTriggered);

        //        var handler = Expression.Lambda(
        //            eventInfo.EventHandlerType,
        //            Expression.Call(Expression.Constant(container), _onTriggered, parameters[0]),
        //            parameters
        //            )
        //            .Compile();

        //        eventInfo.AddEventHandler(entity, handler);

        //        return true;
        //    }

        //    return false;
        //}

    }
}
