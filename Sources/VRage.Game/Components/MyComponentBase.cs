using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Components
{
    public interface IMyComponentBase
    {
        void SetContainer(IMyComponentContainer container);

        void OnAddedToContainer();
        void OnRemovedFromContainer();

        void OnAddedToScene();
        void OnRemovedFromScene();
    }

    public abstract class MyComponentBase<C> : IMyComponentBase where C : IMyComponentContainer
    {
        public C Container { get; set; }

        public void SetContainer(IMyComponentContainer container)
        {
            Container = (C)container;
        }

        public virtual void OnAddedToContainer()
        {
        }

        public virtual void OnRemovedFromContainer()
        {
        }

        public virtual T GetAs<T>() where T : MyComponentBase<C>
        {
            return this as T;
        }

        public virtual void OnAddedToScene()
        {
        }

        public virtual void OnRemovedFromScene()
        {
        }
    }
}
