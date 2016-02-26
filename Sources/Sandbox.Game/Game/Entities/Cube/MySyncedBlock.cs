using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Sync;
using VRage.Network;

namespace Sandbox.Game.Entities.Cube
{
    public class MySyncedBlock : MyCubeBlock, IMyEventProxy
    {
        public readonly SyncType SyncType;

        public event Action<SyncBase> SyncPropertyChanged
        {
            add { SyncType.PropertyChanged += value; }
            remove { SyncType.PropertyChanged -= value; }
        }

        public MySyncedBlock()
        {
            SyncType = SyncHelpers.Compose(this);
        }
    }
}
