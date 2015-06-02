using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace VRage.Components
{
    public abstract class MyComponentBase
    {
       // public IMyEntity Entity { get { return Container != null ? Container.Entity : null; } }  // to be obsolete once components are finished
        public MyComponentContainer Container { get; set; }

        public virtual void OnAddedToContainer(MyComponentContainer container)
        {
            Container = container;
        }

        public virtual void OnRemovedFromContainer(MyComponentContainer container)
        {
            Container = null;
        }

        public virtual T GetAs<T>() where T : MyComponentBase
        {
            return this as T;
        }
    }
}
