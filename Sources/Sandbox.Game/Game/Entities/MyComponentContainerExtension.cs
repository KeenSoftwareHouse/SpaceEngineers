using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// TODO: This should be later ideally some factory rather than just an extension on the MyComponentContainer
    /// </summary>
    public static class MyComponentContainerExtension
    {
        /// <summary>
        /// Tries to retrieve entity definition of the entity owning this container, check if the definition has some DefaultComponents,
        /// tries to retrieve these components definitions, create these components instances and add them
        /// 
        /// TODO: This should be ideally a behavior of the MyEntityComponentContainer when it is initialized (deserialized).. or by the factory, for now, this is an extension method
        /// </summary>        
        public static void InitComponents(this MyComponentContainer container, MyObjectBuilderType type, MyStringHash subtypeName, MyObjectBuilder_ComponentContainer builder)
        {
            if (MyDefinitionManager.Static != null)
            {               
                MyContainerDefinition definition = null;

                bool IsFirstInit = builder == null;

                if (TryGetContainerDefinition(type, subtypeName, out definition))
                {
                    container.Init(definition);
                    
                    if (definition.DefaultComponents != null)
                    {
                        foreach (var component in definition.DefaultComponents)
                        {
                            MyComponentDefinitionBase componentDefinition = null;
                            MyObjectBuilder_ComponentBase componentBuilder = FindComponentBuilder(component, builder);
                            bool IsComponentSerialized = componentBuilder != null;
                            Type componentType = null;
                            MyComponentBase componentInstance = null;

                            var componentSubtype = subtypeName;
                            if (component.SubtypeId.HasValue)
                            {
                                componentSubtype = component.SubtypeId.Value;
                            }

                            // Create component instance
                            if (TryGetComponentDefinition(component.BuilderType, componentSubtype, out componentDefinition))
                            {
                                componentInstance = MyComponentFactory.CreateInstanceByTypeId(componentDefinition.Id.TypeId);
                                componentInstance.Init(componentDefinition);
                            }
                            else if (component.IsValid())
                            {
                                if (!component.BuilderType.IsNull)
                                {
                                    componentInstance = MyComponentFactory.CreateInstanceByTypeId(component.BuilderType);
                                }
                                else
                                {
                                    componentInstance = MyComponentFactory.CreateInstanceByType(component.InstanceType);
                                }
                            }

                            // Check component type from attributes.
                            if (componentInstance != null)
                            {
                                var componentTypeFromAttr = MyComponentTypeFactory.GetComponentType(componentInstance.GetType());
                                if (componentTypeFromAttr != null)
                                {
                                    componentType = componentTypeFromAttr;
                                }
                                else
                                {
                                    if (componentDefinition != null)
                                        System.Diagnostics.Debug.Fail("Unknown component type - component type attribute not specified for component class: " + componentInstance.GetType());
                                }
                            }

                            //TODO: this should be avoided! Component type MUST be set via MyComponentType attribute.
                            if (componentType == null && componentInstance != null)
                            {
                                componentType = componentInstance.GetType();
                            }
                            
                            // If everything passed, go on..
                            if (componentInstance != null && componentType != null)
                            {
                                bool componentShouldBeAdded = IsFirstInit || IsComponentSerialized || component.ForceCreate;

                                if (componentShouldBeAdded)
                                {
                                    if (componentBuilder != null)
                                    {
                                        componentInstance.Deserialize(componentBuilder);
                                    }

                                    // Add only fully initialized component..
                                    container.Add(componentType, componentInstance);
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.Fail("Component instance wasn't created or it's base type couldn't been determined!");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Got definition for container, but DefaultComponents is null!");
                    }
                }               
                
                // This may rewrite already once deserialized data, but will also add missing components in definition
                container.Deserialize(builder);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Trying to init enabled components on entity, but definition manager is null");
            }
        }

        public static MyObjectBuilder_ComponentBase FindComponentBuilder(MyContainerDefinition.DefaultComponent component, MyObjectBuilder_ComponentContainer builder)
        {
            MyObjectBuilder_ComponentBase componentBuilder = null;

            if (builder != null && component.IsValid())
            {
                MyObjectBuilderType componentObType = null;
                
                if (!component.BuilderType.IsNull)
                {   
                    var componentData = builder.Components.Find(x => x.Component.TypeId == component.BuilderType);

                    if (componentData != null)
                    {
                        componentBuilder = componentData.Component;
                    }
                }
            }

            return componentBuilder;
        }

        public static bool TryGetContainerDefinition(MyObjectBuilderType type, MyStringHash subtypeName, out MyContainerDefinition definition)
        {
            definition = null;

            if (MyDefinitionManager.Static != null)
            {                
                MyDefinitionId containerSubtypeId = new MyDefinitionId(type, subtypeName);
                if (MyDefinitionManager.Static.TryGetContainerDefinition(containerSubtypeId, out definition))
                    return true;

                // Fallback to EntityBase type
                if (subtypeName != MyStringHash.NullOrEmpty)
                {
                    MyDefinitionId containerSubtypeId_Fallback = new MyDefinitionId(typeof(MyObjectBuilder_EntityBase), subtypeName);
                    if (MyDefinitionManager.Static.TryGetContainerDefinition(containerSubtypeId_Fallback, out definition))
                        return true;
                }

                MyDefinitionId containerDefaultId = new MyDefinitionId(type);
                if (MyDefinitionManager.Static.TryGetContainerDefinition(containerDefaultId, out definition))
                    return true;

            }

            return false;
        }

        public static bool TryGetComponentDefinition(MyObjectBuilderType type, MyStringHash subtypeName, out MyComponentDefinitionBase componentDefinition)
        {
            componentDefinition = null;

            if (MyDefinitionManager.Static != null)
            {
                MyDefinitionId subtypeDefinition = new MyDefinitionId(type, subtypeName);
                if (MyDefinitionManager.Static.TryGetEntityComponentDefinition(subtypeDefinition, out componentDefinition))
                    return true;

                // Fallback to EntityBase type
                if (subtypeName != MyStringHash.NullOrEmpty)
                {
                    MyDefinitionId subtypeDefinition_Fallback = new MyDefinitionId(typeof(MyObjectBuilder_EntityBase), subtypeName);
                    if (MyDefinitionManager.Static.TryGetEntityComponentDefinition(subtypeDefinition_Fallback, out componentDefinition))
                        return true;
                }

                MyDefinitionId defaultDefinition = new MyDefinitionId(type);
                if (MyDefinitionManager.Static.TryGetEntityComponentDefinition(defaultDefinition, out componentDefinition))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// This will retrieve component types in the entity container. This method allocates, use only for debugging etc.
        /// </summary>
        /// <returns>true if success</returns>
        public static bool TryGetEntityComponentTypes(long entityId, out List<Type> components)
        {
            MyEntity entity;
            components = null;
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {
                components = new List<Type>();
                foreach (var component in entity.Components.GetComponentTypes())
                {
                    if (component != null)
                    {
                        components.Add(component);
                    }
                }

                if (components.Count > 0)
                    return true;
            }
            return false;
        }

        public static bool TryRemoveComponent(long entityId, Type componentType)
        {
            MyEntity entity;            
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {
                entity.Components.Remove(componentType);
                return true;
            }
            return false;
        }

        /// <summary>
        /// This will look for the component definition and if found, it will create its instance and add to the entity with the give id
        /// </summary>
        /// <returns>true on success</returns>
        public static bool TryAddComponent(long entityId, MyDefinitionId componentDefinitionId)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {
                MyComponentDefinitionBase componentDefinition;
                if (TryGetComponentDefinition(componentDefinitionId.TypeId, componentDefinitionId.SubtypeId, out componentDefinition))
                {
                    var componentInstance = MyComponentFactory.CreateInstanceByTypeId(componentDefinition.Id.TypeId);
                    var componentType = MyComponentTypeFactory.GetComponentType(componentInstance.GetType());
                    if (componentType == null)
                    {
                        System.Diagnostics.Debug.Fail("Unknown component type - component type attribute not specified for component class: " + componentInstance.GetType());
                        return false;
                    }

                    componentInstance.Init(componentDefinition);
                    entity.Components.Add(componentType, componentInstance);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// This will try to parse strings to types and create an instance of the component type. Don't use this in retail code, use for debug, modding etc.
        /// </summary>
        /// <param name="entityId">Id of entity which should get the component</param>
        /// <param name="instanceTypeStr">Type of the component instance, no the base type</param>
        /// <param name="componentTypeStr">The base type of the component to be added</param>
        /// <returns>true on success</returns>
        public static bool TryAddComponent(long entityId, string instanceTypeStr, string componentTypeStr)
        {
            MyEntity entity;
            Type instanceType = null;
            Type componentType = null;

            try
            {
                instanceType = Type.GetType(instanceTypeStr, true);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.Fail(String.Format("Can not parse defined component type {0}",  instanceTypeStr));
            }

            try
            {
                instanceType = Type.GetType(componentTypeStr, true);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.Fail(String.Format("Can not parse defined component type {0}", componentTypeStr));
            }

            if (MyEntities.TryGetEntityById(entityId, out entity) && instanceType != null)
            {
                var componentInstance = MyComponentFactory.CreateInstanceByType(instanceType);

                MyComponentDefinitionBase componentDefinition;
                if (entity.DefinitionId.HasValue && TryGetComponentDefinition(componentInstance.GetType(), entity.DefinitionId.Value.SubtypeId, out componentDefinition))
                {
                    componentInstance.Init(componentDefinition);
                }
                
                entity.Components.Add(componentType, componentInstance);
                
                return true;
            }
            return false;
        }
    }
}
