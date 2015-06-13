﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;

namespace VRage.Components
{

    public interface IMyComponentContainer
    {
    }

    public class MyComponentContainer<B> : IMyComponentContainer where B : IMyComponentBase 
    {
        public event Action<Type, B> ComponentAdded;
        public event Action<Type, B> ComponentRemoved;

        private Dictionary<Type, B> m_components = new Dictionary<Type, B>();

        public void Add<T>(B component) where T : B
        {
            {
                Type t = typeof(T);
                Remove<T>();
                if (component != null)
                {
                    m_components[t] = component;
                    component.SetContainer(this);
                    component.OnAddedToContainer();
                    var handle = ComponentAdded;
                    if (handle != null) handle(t, component);
                }
            }
        }

        public void Remove<T>() where T : B
        {
            {
                Type t = typeof(T);
                B c;
                if (m_components.TryGetValue(t, out c))
                {
                    c.OnRemovedFromContainer();
                    c.SetContainer(null);
                    m_components.Remove(t);
                    var handle = ComponentRemoved;
                    if (handle != null) handle(t, c);
                }
            }
        }

        public T Get<T>() where T : B
        {
            {
                B c;
                m_components.TryGetValue(typeof(T), out c);
                return (T)c;
            }
        }

        public bool TryGet<T>(out T component) where T : B
        {
            B c;
            var retVal = m_components.TryGetValue(typeof(T), out c);
            component = (T)c;
            return retVal;
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
                var tmpComponentList = new List<B>();

                try
                {
                    foreach (var component in m_components)
                    {
                        tmpComponentList.Add(component.Value);
                    }
                    m_components.Clear();

                    foreach (var component in tmpComponentList)
                    {
                        component.OnRemovedFromContainer();
                        var handle = ComponentRemoved;
                        if (handle != null) handle(component.GetType(), component);
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
	}
}
