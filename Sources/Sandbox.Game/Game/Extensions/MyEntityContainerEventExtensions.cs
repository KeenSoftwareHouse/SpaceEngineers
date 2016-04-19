using Sandbox.Game;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace VRage.Game.Components
{
    /// <summary>
    /// TODO: This should change in future. Cestmir should already know how to change this to some kind of dispatcher that will inform components
    /// Until then, this is now used to inform MyEntityDurabilityComponent if present in the container about changes
    /// </summary>
    public static class MyEntityContainerEventExtensions
    {
        #region Fields

        /// <summary>
        /// This dictionary holds entityId as key, and value is the dictionary of registered events to be listened
        /// </summary>
        private static Dictionary<long, RegisteredEvents> RegisteredListeners = new Dictionary<long, RegisteredEvents>();

        /// <summary>
        /// This dictionary holds listeners (components) that are attached to other entities entities and listen on another entity
        /// </summary>
        private static Dictionary<MyComponentBase, List<long>> ExternalListeners = new Dictionary<MyComponentBase, List<long>>();

        /// <summary>
        /// This hashset contains entities, which's events are registered by components / handlers that don't belong to their containers.
        /// </summary>
        private static HashSet<long> ExternalyListenedEntities = new HashSet<long>();

        private static List<RegisteredComponent> m_tmpList = new List<RegisteredComponent>();

        private static List<MyComponentBase> m_tmpCompList = new List<MyComponentBase>();

        private static bool ProcessingEvents;

        private static bool HasPostponedOperations;

        private static List<Tuple<MyEntityComponentBase, MyEntity, MyStringHash, EntityEventHandler>> PostponedRegistration = new List<Tuple<MyEntityComponentBase, MyEntity, MyStringHash, EntityEventHandler>>();

        private static List<Tuple<MyEntityComponentBase, MyEntity, MyStringHash>> PostponedUnregistration = new List<Tuple<MyEntityComponentBase,MyEntity,MyStringHash>>();

        private static List<long> PostPonedRegisteredListenersRemoval = new List<long>();
        
        /// <summary>
        /// This counter is used to count how many events invoke other entity events, if this amount is too high, we have to rework the mechanism and hav postopned invokation..
        /// </summary>
        private static int m_debugCounter;

        #endregion
        
        #region Event Params Classes

        /// <summary>
        /// Base class for passing parameters, derive for it to pass different params, keep the names consistent ie. EntityEventType = Hit => params should be type of HitParams
        /// </summary>
        public class EntityEventParams
        {
            // TODO: Maybe this should be a struct? Or find other way to pass parameters..

        }

        /// <summary>
        /// Params to be passed with ControlAcquiredEvent..
        /// </summary>
        public class ControlAcquiredParams : EntityEventParams
        {
            public ControlAcquiredParams(MyEntity owner)
            {
                Owner = owner;
            }

            public MyEntity Owner;
        }

        /// <summary>
        /// Params to be passed with ControlAcquiredEvent..
        /// </summary>
        public class ControlReleasedParams : EntityEventParams
        {
            public ControlReleasedParams(MyEntity owner)
            {
                Owner = owner;
            }

            public MyEntity Owner;
        }

        /// <summary>
        /// This class object is passed as argument with ModelChanged entity event
        /// </summary>
        public class ModelChangedParams : MyEntityContainerEventExtensions.EntityEventParams
        {
            public ModelChangedParams(string model, Vector3 size, float mass, float volume, string displayName, string[] icons)
            {
                Model = model;
                Size = size;
                Mass = mass;
                Volume = volume;
                DisplayName = displayName;
                Icons = icons;
            }

            public Vector3 Size; // in metres
            public float Mass; // in kg
            public float Volume; // in m3
            public string Model;
            public string DisplayName;
            public string[] Icons;
        }

        /// <summary>
        /// This class is used to inform about changes in inventory
        /// </summary>
        public class InventoryChangedParams : EntityEventParams
        {
            public InventoryChangedParams(uint itemId, MyInventoryBase inventory, float amount)
            {
                ItemId = itemId;
                Inventory = inventory;
                Amount = amount;
            }

            public uint ItemId;
            public float Amount;
            public MyInventoryBase Inventory;
        }

        /// <summary>
        /// Params to pass hitted entity
        /// </summary>
        public class HitParams : EntityEventParams
        {
            public HitParams(MyStringHash hitAction, MyStringHash hitEntity)
            {
                HitEntity = hitEntity;
                HitAction = hitAction;
            }

            public MyStringHash HitEntity;
            public MyStringHash HitAction;
        }

        #endregion

        #region Helper Classes and Declarations

        /// <summary>
        /// Handler to be called on event
        /// </summary>
        /// <param name="eventParams">These params can be also another type derived from this for different event types </param>
        public delegate void EntityEventHandler(EntityEventParams eventParams);

        /// <summary>
        /// This holds basically the delegate to be invoked but also a component for easier deregistration..
        /// </summary>
        private class RegisteredComponent
        {
            public RegisteredComponent(MyComponentBase component, EntityEventHandler handler)
            {
                Component = component;
                Handler = handler;
            }

            public MyComponentBase Component;

            public EntityEventHandler Handler;            
        }

        /// <summary>
        /// This class is a dictionary of registered handlers for different events that happened on the entity
        /// </summary>
        private class RegisteredEvents : Dictionary<MyStringHash, List<RegisteredComponent>> 
        {
            public RegisteredEvents(MyStringHash eventType, MyComponentBase component, EntityEventHandler handler)
            {
                this[eventType] = new List<RegisteredComponent>();
                this[eventType].Add( new RegisteredComponent(component, handler));
            }            
        };

        public static void InitEntityEvents()
        {
            RegisteredListeners = new Dictionary<long, RegisteredEvents>();

            ExternalListeners = new Dictionary<MyComponentBase, List<long>>();

            ExternalyListenedEntities = new HashSet<long>();
            
            PostponedRegistration = new List<Tuple<MyEntityComponentBase, MyEntity, MyStringHash, EntityEventHandler>>();

            PostponedUnregistration = new List<Tuple<MyEntityComponentBase, MyEntity, MyStringHash>>();

            ProcessingEvents = false;

            HasPostponedOperations = false;
        }
        
        #endregion

        /// <summary>
        /// This will register the component to listen to some events..
        /// </summary>
        /// <param name="component">Component that is being registered</param>
        /// <param name="eventType">type of event</param>
        /// <param name="handler">handler to be called</param>
        public static void RegisterForEntityEvent(this MyEntityComponentBase component, MyStringHash eventType, EntityEventHandler handler)
        {
            if (ProcessingEvents)
            {
                AddPostponedRegistration(component, component.Entity as MyEntity, eventType, handler);
                return;
            }

            if (component.Entity == null)
            {
                System.Diagnostics.Debug.Fail("You can't register this component for events, when you don't know on which entity it should be listening to..");
                return;
            }

            component.BeforeRemovedFromContainer += RegisteredComponentBeforeRemovedFromContainer;
            component.Entity.OnClose += RegisteredEntityOnClose;

            if (RegisteredListeners.ContainsKey(component.Entity.EntityId))
            {
                var registeredForEntity = RegisteredListeners[component.Entity.EntityId];
                if (registeredForEntity.ContainsKey(eventType))
                {
                    var entry = registeredForEntity[eventType].Find(x => x.Handler == handler);
                    if (entry == null)
                    {
                        registeredForEntity[eventType].Add(new RegisteredComponent(component, handler));
                    }
                }
                else
                {
                    registeredForEntity[eventType] = new List<RegisteredComponent>();
                    registeredForEntity[eventType].Add(new RegisteredComponent(component, handler));
                }
            }
            else
            {
                RegisteredListeners[component.Entity.EntityId] = new RegisteredEvents(eventType, component, handler);
            }
        }

        /// <summary>
        /// This will register the component to listen to some events on entity that is other than entity containing this component
        /// </summary>
        /// <param name="entity">Entity on which we listen to events</param>
        /// <param name="component">Component that is being registered</param>
        /// <param name="eventType">type of event</param>
        /// <param name="handler">handler to be called</param>
        public static void RegisterForEntityEvent(this MyEntityComponentBase component, MyEntity entity, MyStringHash eventType, EntityEventHandler handler)
        {
            if (ProcessingEvents)
            {
                AddPostponedRegistration(component, entity, eventType, handler);
                return;
            }

            if (component.Entity == entity)
            {
                RegisterForEntityEvent(component, eventType, handler);
                return;
            }

            if (entity == null)
            {
                System.Diagnostics.Debug.Fail("You can't register this component for events, when you don't know on which entity it should be listening to..");
                return;
            }

            component.BeforeRemovedFromContainer += RegisteredComponentBeforeRemovedFromContainer;
            entity.OnClose += RegisteredEntityOnClose;

            if (RegisteredListeners.ContainsKey(entity.EntityId))
            {
                var registeredForEntity = RegisteredListeners[entity.EntityId];
                if (registeredForEntity.ContainsKey(eventType))
                {
                    var entry = registeredForEntity[eventType].Find(x => x.Handler == handler);
                    if (entry == null)
                    {
                        registeredForEntity[eventType].Add(new RegisteredComponent(component, handler));
                    }
                }
                else
                {
                    registeredForEntity[eventType] = new List<RegisteredComponent>();
                    registeredForEntity[eventType].Add(new RegisteredComponent(component, handler));
                }
            }
            else
            {
                RegisteredListeners[entity.EntityId] = new RegisteredEvents(eventType, component, handler);
            }


            if (ExternalListeners.ContainsKey(component) && !ExternalListeners[component].Contains(entity.EntityId))
            {
                ExternalListeners[component].Add(entity.EntityId);
            }
            else
            {
                ExternalListeners[component] = new List<long>() {entity.EntityId};                
            }

            ExternalyListenedEntities.Add(entity.EntityId);
        }

        /// <summary>
        /// This will unregister the component to listen to some events on entity that is other than entity containing this component
        /// </summary>
        /// <param name="entity">Entity on which we listen to events</param>
        /// <param name="component">Component that is being registered</param>
        /// <param name="eventType">type of event</param>
        /// <param name="handler">handler to be called</param>
        public static void UnregisterForEntityEvent(this MyEntityComponentBase component, MyEntity entity, MyStringHash eventType)
        {
            if (ProcessingEvents)
            {
                AddPostponedUnregistration(component, entity, eventType);
                return;
            }

            if (entity == null)
            {
                System.Diagnostics.Debug.Fail("You can't register this component for events, when you don't know on which entity it should be listening to..");
                return;
            }

            // TODO: Unregister events for components, or even bettter rework these EntityEvents to be used without event handlers..
            //bool componentIsRegistered = true;
            bool entityIsRegistered = true;
            
            if (RegisteredListeners.ContainsKey(entity.EntityId))
            {
                if (RegisteredListeners[entity.EntityId].ContainsKey(eventType))
                {
                    RegisteredListeners[entity.EntityId][eventType].RemoveAll(x => x.Component == component);
                    if (RegisteredListeners[entity.EntityId][eventType].Count == 0)
                    {
                        RegisteredListeners[entity.EntityId].Remove(eventType);
                    }
                }
                
                if (RegisteredListeners[entity.EntityId].Count == 0)
                {
                    RegisteredListeners.Remove(entity.EntityId);
                    ExternalyListenedEntities.Remove(entity.EntityId);
                    entityIsRegistered = false;
                }
            }


            if (ExternalListeners.ContainsKey(component) && ExternalListeners[component].Contains(entity.EntityId))
            {
                ExternalListeners[component].Remove(entity.EntityId);
                if (ExternalListeners[component].Count == 0)
                {
                    ExternalListeners.Remove(component);
                }
            }

            if (!entityIsRegistered)
            {                
                entity.OnClose -= RegisteredEntityOnClose;
            }
         
            //if (!componentIsRegistered)
            //{
            //    component.BeforeRemovedFromContainer -= RegisteredComponentBeforeRemovedFromContainer;
            //}
        }
        
        /// <summary>
        /// When entity is being closed, we need to clean it's records for events
        /// </summary>
        /// <param name="entity">entity being removed</param>
        private static void RegisteredEntityOnClose(VRage.ModAPI.IMyEntity entity)
        {
            entity.OnClose -= RegisteredEntityOnClose;
                        
            if (RegisteredListeners.ContainsKey(entity.EntityId))
            {
                if (ProcessingEvents)
                {
                    AddPostponedListenerRemoval(entity.EntityId);
                    
                }
                else
                {
                    RegisteredListeners.Remove(entity.EntityId);
                }
            }

            // Worst case, this entity is registered also elsewheree
            if (ExternalyListenedEntities.Contains(entity.EntityId))
            {
                ExternalyListenedEntities.Remove(entity.EntityId);

                m_tmpCompList.Clear();

                foreach (var entry in ExternalListeners)
                {
                    entry.Value.Remove(entity.EntityId);
                    if (entry.Value.Count == 0)
                    {
                        m_tmpCompList.Add(entry.Key);
                    }
                }

                foreach (var removing in m_tmpCompList)
                {
                    ExternalListeners.Remove(removing);
                }
            }
        }

        /// <summary>
        /// When component is removed, clean it's records
        /// </summary>
        /// <param name="component">component being removed from its container (entity) </param>
        private static void RegisteredComponentBeforeRemovedFromContainer(MyEntityComponentBase component)
        {
            component.BeforeRemovedFromContainer -= RegisteredComponentBeforeRemovedFromContainer;

            if (component.Entity == null)
                return;

            if (RegisteredListeners.ContainsKey(component.Entity.EntityId))
            {
                m_tmpList.Clear();

                foreach (var entry in RegisteredListeners[component.Entity.EntityId])
                {
                    entry.Value.RemoveAll(x => x.Component == component);
                }
            }

            if (ExternalListeners.ContainsKey(component))
            {
                foreach (var externalEntityId in ExternalListeners[component])
                {
                    if (RegisteredListeners.ContainsKey(externalEntityId))
                    {
                        foreach (var entry in RegisteredListeners[externalEntityId])
                        {
                            entry.Value.RemoveAll(x => x.Component == component);
                        }
                    }
                }

                ExternalListeners.Remove(component);
            }
        }

        /// <summary>
        /// Call this to raise event on entity, that will be processed by registered components
        /// </summary>
        /// <param name="entity">this is entity on which is this being invoked</param>
        /// <param name="eventType">type of event</param>
        /// <param name="eventParams">event params or derived type</param>
        public static void RaiseEntityEvent(this MyEntity entity, MyStringHash eventType, EntityEventParams eventParams)
        {
            if (entity.Components == null)  // no components that could listen..
                return;

            long entityId = entity.EntityId;

            InvokeEventOnListeners(entityId, eventType, eventParams);
        }

        /// <summary>
        /// Call this to raise event on entity, that will be processed by registered components
        /// </summary>
        /// <param name="entity">this is entity on which is this being invoked</param>
        /// <param name="eventType">type of event</param>
        /// <param name="eventParams">event params or derived type</param>
        public static void RaiseEntityEventOn(MyEntity entity, MyStringHash eventType, EntityEventParams eventParams)
        {
            if (entity.Components == null)  // no components that could listen..
                return;

            long entityId = entity.EntityId;

            InvokeEventOnListeners(entityId, eventType, eventParams);
        }

        /// <summary>
        /// Call this to raise event on entity, that will be processed by registered components
        /// </summary>
        /// <param name="component">component upon which container this is going to be invoke</param>
        /// <param name="eventType">type of event</param>
        /// <param name="eventParams">event params or derived type</param>
        public static void RaiseEntityEvent(this MyEntityComponentBase component, MyStringHash eventType, EntityEventParams eventParams)
        {
            if (component.Entity == null)   // this component is raising event, but it's entity don't exists..
                return;

            long entityId = component.Entity.EntityId;

            InvokeEventOnListeners(entityId, eventType, eventParams);
        }

        /// <summary>
        /// This just iterates through registered listeners and informs them..
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="eventType"></param>
        /// <param name="eventParams"></param>
        private static void InvokeEventOnListeners(long entityId, MyStringHash eventType, EntityEventParams eventParams)
        {
            var previousValue = ProcessingEvents;

            if (previousValue)
            {
                m_debugCounter++;
                System.Diagnostics.Debug.Assert(m_debugCounter < 4, "Invokation of entity events are generating more than 3 other entity events and this call get's too long. We should rework the mechanism to some queue and have postponed invokation instead..");
            }

            if (m_debugCounter > 5)
            {
                System.Diagnostics.Debug.Fail("Loop on entity events detected, ignoring event " + eventType.String + " and returning.. ");
                return;
            }

            ProcessingEvents = true;

            if (RegisteredListeners.ContainsKey(entityId))
            {
                if (RegisteredListeners[entityId].ContainsKey(eventType))
                {
                    foreach (var registered in RegisteredListeners[entityId][eventType])
                    {
                        // TODO: This is now in safe block, this should be propably removed later, when this is tested and safe
                        try
                        {
                            registered.Handler(eventParams);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.Fail(String.Format("Invoking registered method for entity event {0}  failed. {1} ", eventType.ToString(), e.Message));
                        }
                    }
                }
            }

            ProcessingEvents = previousValue;

            if (!ProcessingEvents)
            {
                m_debugCounter = 0;
            }

            if (HasPostponedOperations && !ProcessingEvents)
            {
                ProcessPostponedRegistrations();
            }
        }

        private static void ProcessPostponedRegistrations()
        {
            foreach (var regParams in PostponedRegistration)
            {
                RegisterForEntityEvent(regParams.Item1, regParams.Item2, regParams.Item3, regParams.Item4);
            }
            foreach (var unregParams in PostponedUnregistration)
            {
                UnregisterForEntityEvent(unregParams.Item1, unregParams.Item2, unregParams.Item3);
            }
            foreach (var regEnt in PostPonedRegisteredListenersRemoval)
            {
                RegisteredListeners.Remove(regEnt);
            }

            PostponedRegistration.Clear();
            PostponedUnregistration.Clear();
            PostPonedRegisteredListenersRemoval.Clear();
            HasPostponedOperations = false;
        }

        private static void AddPostponedRegistration(MyEntityComponentBase component, MyEntity entity, MyStringHash eventType, EntityEventHandler handler)
        {
            PostponedRegistration.Add(new Tuple<MyEntityComponentBase, MyEntity, MyStringHash, EntityEventHandler>(component, entity, eventType, handler));
            HasPostponedOperations = true;
        }

        private static void AddPostponedUnregistration(MyEntityComponentBase component, MyEntity entity, MyStringHash eventType)
        {
            PostponedUnregistration.Add(new Tuple<MyEntityComponentBase, MyEntity, MyStringHash>(component, entity, eventType));
            HasPostponedOperations = true;
        }

        private static void AddPostponedListenerRemoval(long id)
        {
            PostPonedRegisteredListenersRemoval.Add(id);
            HasPostponedOperations = true;
        }

    }
}
