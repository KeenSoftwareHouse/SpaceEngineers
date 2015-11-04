using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.ObjectBuilders;

namespace VRage.Components
{
    public abstract class MySyncComponentBase : MyEntityComponentBase
    {
        public abstract void SendCloseRequest();
        public abstract void MarkPhysicsDirty();

        public override string ComponentTypeDebugString
        {
            get { return "Sync"; }
        }
    }
}
