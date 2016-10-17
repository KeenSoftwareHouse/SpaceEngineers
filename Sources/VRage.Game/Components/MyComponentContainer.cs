using VRage.Game.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;

namespace VRage.Game.Components
{
    public class MyComponentContainer
    {
        private Dictionary<Type, MyComponentBase> m_components = new Dictionary<Type, MyComponentBase>();

        public void Add<T>(T component) where T : MyComponentBase
        {
            {
                Type t = typeof(T);
                Add(t, component);            
            }
        }

        public void Add(Type type, MyComponentBase component)
        {
            //System.Diagnostics.Debug.Assert(component == null || component.ContainerBase == null, "Component needs to be removed from a container before adding to a new one!");
            System.Diagnostics.Debug.Assert(typeof(MyComponentBase).IsAssignableFrom(type), "Unsupported type of component!");
            if (!typeof(MyComponentBase).IsAssignableFrom(type))
                return;

            if (component != null)
            {
                System.Diagnostics.Debug.Assert(type.IsAssignableFrom(component.GetType()), "Component added with wrong type!");
                if (!type.IsAssignableFrom(component.GetType()))
                    return;
            }

            {
                //TODO: componentTypeFromAttribute cannot be null when all components has [MyComponentType(typeof(...))] attribute.
                var componentTypeFromAttribute = MyComponentTypeFactory.GetComponentType(type);
                if (componentTypeFromAttribute != null && componentTypeFromAttribute != type)
                {
                    // Failed when component type from attribute is not the same as the given type. Means that [MyComponentType(typeof(...))]
                    // should be specified for the component class (it is probably used for base class now).
                    System.Diagnostics.Debug.Fail("Component: " + component.GetType() + " is set to container as type: " + type + " but type from attribute is: " + componentTypeFromAttribute);
                }
            }

            MyComponentBase containedComponent;
            if (m_components.TryGetValue(type, out containedComponent))
            {
                //System.Diagnostics.Debug.Assert(containedComponent != component, "Adding a component to a container twice!");

                if (containedComponent is IMyComponentAggregate)
                {
                    (containedComponent as IMyComponentAggregate).AddComponent(component);
                    return;
                }
                else if (component is IMyComponentAggregate)
                {
                    Remove(type);
                    (component as IMyComponentAggregate).AddComponent(containedComponent);
                    m_components[type] = component;
                    component.SetContainer(this);
                    OnComponentAdded(type, component);
                    return;
                }
            }

            Remove(type);
            if (component != null)
            {
                m_components[type] = component;
                component.SetContainer(this);
                OnComponentAdded(type, component);
            }
        }

        public void Remove<T>() where T : MyComponentBase
        {
            {
                Type t = typeof(T);
                Remove(t);
            }
        }

        public void Remove(Type t)
        {
            MyComponentBase c;
            if (m_components.TryGetValue(t, out c))
            {
                RemoveComponentInternal(t, c);
            }
        }

        private void RemoveComponentInternal(Type t, MyComponentBase c)
        {
            c.SetContainer(null);
            m_components.Remove(t);
            OnComponentRemoved(t, c);
        }

        public void Remove(Type t, MyComponentBase component)
        {
            MyComponentBase storedComponent = null;
            m_components.TryGetValue(t, out storedComponent);
            if (storedComponent == null)
            {
                System.Diagnostics.Debug.Assert(false, "Removing component from a container, but that container does not contain the component!");
                return;
            }

            IMyComponentAggregate storedAggregate = storedComponent as IMyComponentAggregate;
            if (storedAggregate == null)
            {
                System.Diagnostics.Debug.Assert(storedComponent == component, "Removing component from a container, but that container does not contain the component!");
                RemoveComponentInternal(t, component);
            }
            else
            {
                bool removed = storedAggregate.RemoveComponent(component);
                System.Diagnostics.Debug.Assert(removed, "Component could not be removed because it was not present in the container!");
            }
        }

        public T Get<T>() where T : MyComponentBase
        {
            {
                MyComponentBase c;
                m_components.TryGetValue(typeof(T), out c);
                return (T)c;
            }
        }

        public bool TryGet<T>(out T component) where T : MyComponentBase
        {
            MyComponentBase c;
            var retVal = m_components.TryGetValue(typeof(T), out c);
            component = (T)c;
            return retVal;
        }

        public bool TryGet(Type type, out MyComponentBase component)
        {            
            return m_components.TryGetValue(type, out component);
        }

        public bool Has<T>() where T : MyComponentBase
        {
            return m_components.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Returns if any component is assignable from type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Contains(Type type)
        {
            foreach (var key in m_components.Keys)
            {
                if (type.IsAssignableFrom(key))
                    return true;
            }
            return false;
        }

		public void Clear()
		{
            if (m_components.Count > 0)
            {
                var tmpComponentList = new List<MyComponentBase>();

                try
                {
                    foreach (var component in m_components)
                    {
                        tmpComponentList.Add(component.Value);
                        component.Value.SetContainer(null);
                    }
                    m_components.Clear();

                    foreach (var component in tmpComponentList)
                    {
                        OnComponentRemoved(component.GetType(), component);
                    }

                }
                finally
                {
                    tmpComponentList.Clear();
                }
            }
		}

        public void OnAddedToScene()
        {
            foreach (var component in m_components)
            {
                component.Value.OnAddedToScene();
            }
        }

        public void OnRemovedFromScene()
        {
            foreach (var component in m_components)
            {
                component.Value.OnRemovedFromScene();
            }
        }

        public virtual void Init(MyContainerDefinition definition) { }

        protected virtual void OnComponentAdded(Type t, MyComponentBase component) {}

        protected virtual void OnComponentRemoved(Type t, MyComponentBase component) { }

        public MyObjectBuilder_ComponentContainer Serialize(bool copy = false)
        {
            var tmpSerializedComponents = new List<KeyValuePair<Type, MyComponentBase>>(8);

            tmpSerializedComponents.Clear();
            foreach (var component in m_components)
            {
                if (component.Value.IsSerialized())
                {
                    tmpSerializedComponents.Add(component);
                }
            }

            if (tmpSerializedComponents.Count == 0) return null;

            var builder = new MyObjectBuilder_ComponentContainer();
            foreach (var component in tmpSerializedComponents)
            {
                MyObjectBuilder_ComponentBase componentBuilder = component.Value.Serialize(copy);
                if (componentBuilder != null)
                {
                    var data = new MyObjectBuilder_ComponentContainer.ComponentData();
                    data.TypeId = component.Key.Name;
                    data.Component = componentBuilder;
                    builder.Components.Add(data);
                }
            }

            tmpSerializedComponents.Clear();
            return builder;
        }

		public void Deserialize(MyObjectBuilder_ComponentContainer builder)
		{
			if (builder == null || builder.Components == null)
				return;

            foreach (var data in builder.Components)
			{
                MyComponentBase instance = null;
                var createdInstanceType = MyComponentFactory.GetCreatedInstanceType(data.Component.TypeId);

                // Old component deserialized type.
                var dictType = MyComponentTypeFactory.GetType(data.TypeId);
                // Component type can be set as attribute now
                var dictTypeFromAttr = MyComponentTypeFactory.GetComponentType(createdInstanceType);
                if (dictTypeFromAttr != null)
                    dictType = dictTypeFromAttr;

                bool hasComponent = TryGet(dictType, out instance);
                if (hasComponent)
                {
                    // If component is found then check also type because some components have default instances (MyNullGameLogicComponent)
                    if (createdInstanceType != instance.GetType() && createdInstanceType != typeof(MyHierarchyComponentBase))
                        hasComponent = false;
                }
                
                if (!hasComponent)
                {
                    instance = MyComponentFactory.CreateInstanceByTypeId(data.Component.TypeId);
                }
				
                instance.Deserialize(data.Component);

                if (!hasComponent)
                {
                    Add(dictType, instance);
                }
			}
		}

        public Dictionary<Type, MyComponentBase>.ValueCollection.Enumerator GetEnumerator()
        {
            return m_components.Values.GetEnumerator();
        }

        public Dictionary<Type, MyComponentBase>.KeyCollection GetComponentTypes()
        {
            return m_components.Keys;
        }
	}
}
