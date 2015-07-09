using Sandbox.Common.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;

namespace VRage.Components
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

        private void Add(Type type, MyComponentBase component)
        {
            MyComponentBase containedComponent;
            if (m_components.TryGetValue(type, out containedComponent))
            {
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

        private void Remove(Type t)
        {
            MyComponentBase c;
            if (m_components.TryGetValue(t, out c))
            {
                c.SetContainer(null);
                m_components.Remove(t);
                OnComponentRemoved(t, c);
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

        protected virtual void OnComponentAdded(Type t, MyComponentBase component) {}

        protected virtual void OnComponentRemoved(Type t, MyComponentBase component) { }

        public MyObjectBuilder_ComponentContainer Serialize()
        {
            var builder = new MyObjectBuilder_ComponentContainer();
            var componentsData = new List<MyObjectBuilder_ComponentContainer.ComponentData>(m_components.Count);
            int i = 0;
            foreach (var component in m_components)
            {
				MyObjectBuilder_ComponentBase componentBuilder = null;
				if (component.Value.IsSerialized())
					componentBuilder = component.Value.Serialize();
                if (componentBuilder != null)
                {
                    var data = new MyObjectBuilder_ComponentContainer.ComponentData();
                    data.TypeId = component.Key.Name;
                    data.Component = componentBuilder;
                    componentsData.Add(data);
                    i++;
                }
            }
            if (componentsData.Count > 0)
                builder.Components = componentsData.ToArray();
            return builder;
        }

		public void Deserialize(MyObjectBuilder_ComponentContainer builder)
		{
			if (builder == null || builder.Components == null)
				return;

			var componentsData = builder.Components;
			foreach (var data in componentsData)
			{
				var instance = MyComponentFactory.CreateInstance(data.Component.GetType());
				instance.Deserialize(data.Component);
				var dictType = MyComponentTypeFactory.GetType(data.TypeId);
				Add(dictType, instance);
			}
		}

        public Dictionary<Type, MyComponentBase>.ValueCollection.Enumerator GetEnumerator()
        {
            return m_components.Values.GetEnumerator();
        }
	}
}
