using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Common.Components
{
    public class MyComponentContainer
    {
        public Sandbox.ModAPI.IMyEntity Entity { get; private set; } //only temporary until the conversion to components is done
        public event Action<Type, MyComponentBase> ComponentAdded;
        public event Action<Type, MyComponentBase> ComponentRemoved;

        private Dictionary<Type, MyComponentBase> m_components = new Dictionary<Type, MyComponentBase>();

		private static List<MyComponentBase> m_tmpComponentList = new List<MyComponentBase>();

        public MyComponentContainer(Sandbox.ModAPI.IMyEntity entity)
        {
            Entity = entity;
        }

        public void Add<T>(MyComponentBase component) where T : MyComponentBase
        {
#if DEBUG
            using (Stats.Generic.Measure("ComponentContainer.Add", VRage.Stats.MyStatTypeEnum.Counter, 1000, numDecimals: 3))
            using (Stats.Generic.Measure("ComponentContainer.AddMs", VRage.Stats.MyStatTypeEnum.Max, 1000, numDecimals: 3))
#endif
            {
                Type t = typeof(T);
                Remove<T>();
                if (component != null)
                {
                    m_components[t] = component;
                    component.OnAddedToContainer(this);
                    var handle = ComponentAdded;
                    if (handle != null) handle(t, component);
                }
            }
        }

        public void Remove<T>() where T : MyComponentBase
        {
#if DEBUG
            using (Stats.Generic.Measure("ComponentContainer.Remove", VRage.Stats.MyStatTypeEnum.Counter, 1000, numDecimals: 3))
            using (Stats.Generic.Measure("ComponentContainer.RemoveMs", VRage.Stats.MyStatTypeEnum.Max, 1000, numDecimals: 3))
#endif
            {
                Type t = typeof(T);
                MyComponentBase c;
                if (m_components.TryGetValue(t, out c))
                {
                    c.OnRemovedFromContainer(this);
                    m_components.Remove(t);
                    var handle = ComponentRemoved;
                    if (handle != null) handle(t, c);
                }
            }
        }

        public T Get<T>() where T : MyComponentBase
        {
#if DEBUG
            using (Stats.Generic.Measure("ComponentContainer.Get", VRage.Stats.MyStatTypeEnum.Counter, 1000, numDecimals: 3))
            using (Stats.Generic.Measure("ComponentContainer.GetMs", VRage.Stats.MyStatTypeEnum.Max, 1000, numDecimals: 3))
#endif
            {
                MyComponentBase c;
                m_components.TryGetValue(typeof(T), out c);
                return c as T;
            }
        }

        public bool TryGet<T>(out T component) where T : MyComponentBase
        {
            MyComponentBase c;
            var retVal = m_components.TryGetValue(typeof(T), out c);
            component = c as T;
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
			try
			{
				foreach(var component in m_components)
				{
					m_tmpComponentList.Add(component.Value);
				}
				m_components.Clear();

				foreach(var component in m_tmpComponentList)
				{
					component.OnRemovedFromContainer(this);
					var handle = ComponentRemoved;
					if (handle != null) handle(component.GetType(), component);
				}
				
			}
			finally
			{
				m_tmpComponentList.Clear();
			}
			
		}
	}
}
