using VRage.Game.ObjectBuilders.ComponentSystem;

namespace VRage.Game.Components
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
        public virtual void SetContainer(MyComponentContainer container)
        {
            if (m_container != null)
                OnBeforeRemovedFromContainer();

            m_container = container;
            var aggregate = this as IMyComponentAggregate;
            if (aggregate != null)
            {
                foreach (var child in aggregate.ChildList.Reader)
                {
                    System.Diagnostics.Debug.Assert(child != this, "Error, the child is the same as this object!");
                    child.SetContainer(container);
                }
            }

            if (container != null)
                OnAddedToContainer();
        }
#if !UNSHARPER
        public virtual T GetAs<T>() where T : MyComponentBase
        {
            return this as T;
        }
#endif
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

        /// <summary>
        /// CH: TOOD: Be careful! This does not get called if the component is added to a container that is in the scene already!
        /// </summary>
        public virtual void OnAddedToScene()
        {
        }

        /// <summary>
        /// CH: TOOD: Be careful! This does not get called if the component is removed from a container that is still in the scene!
        /// </summary>
        public virtual void OnRemovedFromScene()
        {
        }

        public virtual MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            return MyComponentFactory.CreateObjectBuilder(this);
        }

        public virtual void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
        }

        public virtual void Init(MyComponentDefinitionBase definition) { }

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
