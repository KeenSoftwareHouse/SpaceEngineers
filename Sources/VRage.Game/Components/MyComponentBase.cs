using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRage.ModAPI;

namespace VRage.Components
{
    public interface IMyComponentBase
    {
        MyStringId Name { get; }

        void SetContainer(IMyComponentContainer container);

        void OnAddedToContainer();

        void OnRemovedFromContainer();
    }

    public abstract class MyComponentBase<C> : IMyComponentBase where C : IMyComponentContainer
    {
        public C Container { get; set; }

        public abstract MyStringId Name { get; }

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
    }
}
