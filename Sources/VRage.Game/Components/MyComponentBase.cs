using Sandbox.Common.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Components
{
    public abstract class MyComponentBase
    {
        private MyComponentContainer m_container;

        /// <summary>
        /// This cannot be named Container to not conflict with the definition of Container in MyEntityComponentBase.
        /// </summary>
        public MyComponentContainer ContainerBase
        {
            get { return m_container; }
        }

        /// <summary>
        /// Sets the container of this component.
        /// Note that the component is not added to the container here! Therefore, use MyComponentContainer.Add(...) method and it
        /// will in turn call this method. Actually, you should seldom have the need to call this method yourself.
        /// </summary>
        /// <param name="container">The new container of the component</param>
        public void SetContainer(MyComponentContainer container)
        {
            if (container == null)
                OnBeforeRemovedFromContainer();

            m_container = container;
            var aggregate = this as IMyComponentAggregate;
            if (aggregate != null)
            {
                foreach (var child in aggregate.ChildList.Reader)
                {
                    child.SetContainer(container);
                }
            }

            if (container != null)
                OnAddedToContainer();
        }

        public virtual T GetAs<T>() where T : MyComponentBase
        {
            return this as T;
        }

        /// <summary>
        /// Gets called after the container of this component changes
        /// </summary>
        public virtual void OnAddedToContainer()
        {
        }

        /// <summary>
        /// Gets called before the removal of this component from a container
        /// </summary>
        public virtual void OnBeforeRemovedFromContainer()
        {
        }

        public virtual void OnAddedToScene()
        {
        }

        public virtual void OnRemovedFromScene()
        {
        }

        public virtual MyObjectBuilder_ComponentBase Serialize()
        {
            return MyComponentFactory.CreateObjectBuilder(this);
        }

        public virtual void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
        }

		/// <summary>
		/// Tells the component container serializer whether this component should be saved
		/// </summary>
		/// <returns></returns>
		public virtual bool IsSerialized()
		{
			return false;
		}
    }
}
