using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace VRage.Components
{
    public abstract class MyComponentBase
    {
        public IMyEntity Entity { get { return CurrentContainer != null ? CurrentContainer.Entity : null; } }  // to be obsolete once components are finished
        public MyComponentContainer CurrentContainer { get; set; }

        public virtual void OnAddedToContainer(MyComponentContainer container)
        {
            CurrentContainer = container;
        }

        public virtual void OnRemovedFromContainer(MyComponentContainer container)
        {

            CurrentContainer = null;
        }
    }
}
