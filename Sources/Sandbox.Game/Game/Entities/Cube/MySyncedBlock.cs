using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Network;
using VRage.Sync;

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
#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType = SyncHelpers.Compose(this);
#else // XB1
            SyncType = new SyncType(new List<SyncBase>());
#endif // XB1
        }
    }
}
