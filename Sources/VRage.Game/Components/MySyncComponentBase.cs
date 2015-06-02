using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.ObjectBuilders;

namespace VRage.Components
{
    public abstract class MySyncComponentBase : MyComponentBase
    {
        public abstract void SendCloseRequest();
        public abstract void Tick();
        public abstract void UpdatePosition();
        public abstract bool UpdatesOnlyOnServer { get; set; }
    }
}
