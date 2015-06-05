using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Components
{
    public abstract class MySyncComponentBase : MyEntityComponentBase
    {
        public override MyStringId Name
        {
            get { return DefaultNames.Sync; }
        }

        public abstract void SendCloseRequest();
        public abstract void Tick();
        public abstract void UpdatePosition();
        public abstract bool UpdatesOnlyOnServer { get; set; }
    }
}
