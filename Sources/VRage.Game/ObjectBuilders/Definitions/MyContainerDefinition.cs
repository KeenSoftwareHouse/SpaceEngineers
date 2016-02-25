using System;
using System.Collections.Generic;
using VRage.Game.Definitions;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game
{
    [MyDefinitionType(typeof(MyObjectBuilder_ContainerDefinition))]
    public class MyContainerDefinition : MyDefinitionBase
    {
        public class DefaultComponent
        {
            public MyObjectBuilderType BuilderType = null;
                        
            public Type InstanceType = null;

            public bool ForceCreate = false;

            public MyStringHash? SubtypeId;

            public bool IsValid()
            {
                return InstanceType != null || !BuilderType.IsNull;
            }
        }

        public List<DefaultComponent> DefaultComponents = new List<DefaultComponent>();
        public EntityFlags? Flags;

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_ContainerDefinition)base.GetObjectBuilder();
            ob.Flags = Flags;

            if (DefaultComponents != null && DefaultComponents.Count > 0)
            {
                ob.DefaultComponents = new MyObjectBuilder_ContainerDefinition.DefaultComponentBuilder[DefaultComponents.Count];
                int i = 0;
                foreach (var component in DefaultComponents)
                {
                    if (!component.BuilderType.IsNull)
                    {
                        ob.DefaultComponents[i].BuilderType = component.BuilderType.ToString();
                    }

                    if (component.InstanceType != null)
                    {
                        ob.DefaultComponents[i].InstanceType = component.InstanceType.Name;
                    }

                    if (component.SubtypeId.HasValue)
                    {
                        ob.DefaultComponents[i].SubtypeId = component.SubtypeId.Value.ToString();
                    }

                    ob.DefaultComponents[i].ForceCreate = component.ForceCreate;

                    i++;
                }
            }

            return ob;
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var definitionBuilder = (builder as MyObjectBuilder_ContainerDefinition);
            Flags = definitionBuilder.Flags;

            if (definitionBuilder.DefaultComponents != null && definitionBuilder.DefaultComponents.Length > 0)
            {
                if (DefaultComponents == null)
                {
                    DefaultComponents = new List<DefaultComponent>();
                }
                foreach (var component in definitionBuilder.DefaultComponents)
                {
                    DefaultComponent defComponent = new DefaultComponent();
                                        
                    try
                    {
                        if (component.BuilderType != null)
                        {
                            MyObjectBuilderType obType;
                            obType = MyObjectBuilderType.Parse(component.BuilderType);
                            defComponent.BuilderType = obType;
                        }
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.Fail(String.Format("Can not parse defined component builder type {0} for container {1}", component.BuilderType.ToString(), Id.ToString()));
                        MyLog.Default.WriteLine(String.Format("Container definition error: can not parse defined component type {0} for container {1}", component, Id.ToString()));
                    }

                    try
                    {
                        if (component.InstanceType != null)
                        {
                            Type runtimeType = Type.GetType(component.InstanceType, true);
                            defComponent.InstanceType = runtimeType;
                        }
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.Fail(String.Format("Can not parse defined component instance type {0} for container {1}", component.InstanceType.ToString(), Id.ToString()));
                        MyLog.Default.WriteLine(String.Format("Container definition error: can not parse defined component type {0} for container {1}", component, Id.ToString()));
                    }

                    defComponent.ForceCreate = component.ForceCreate;

                    if (component.SubtypeId != null)
                    {
                        defComponent.SubtypeId = MyStringHash.GetOrCompute(component.SubtypeId);
                    }

                    if (defComponent.IsValid())
                    {
                        DefaultComponents.Add(defComponent);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail(String.Format("Defined component {0} for container {1} is invalid, none builder type or instance type is defined! Skipping it.", component, Id.ToString()));
                        MyLog.Default.WriteLine(String.Format("Defined component {0} for container {1} is invalid, none builder type or instance type is defined! Skipping it.", component, Id.ToString()));
                    }
                }
            }
        }

        /// <summary>
        /// This will search through definitions to find if Default Components contains the searched component either as BuilderType, InstanceType, or ComponentType
        /// </summary>
        /// <param name="component">Name of the type to search for in defined default components</param>
        /// <returns>true if is defined component with the matching BuilderType, InstanceType or ComponentType </returns>
        public bool HasDefaultComponent(string component)
        {
            foreach (var defaultComponent in DefaultComponents)
            {
                if ((!defaultComponent.BuilderType.IsNull && defaultComponent.BuilderType.ToString() == component) || 
                    (defaultComponent.InstanceType != null && defaultComponent.InstanceType.ToString() == component))
                    return true;
            }

            return false;
        }
    }
}
