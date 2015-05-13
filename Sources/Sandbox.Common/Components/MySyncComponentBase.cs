using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.Components
{
    public abstract class MySyncComponentBase : MyComponentBase
    {
        public abstract void SendCloseRequest();
        public abstract void Tick();
        public abstract void UpdatePosition();
        public abstract bool UpdatesOnlyOnServer { get; set; }
    }
}
