using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;

namespace Sandbox.Game.Replicables
{
    public abstract class MyEntityReplicableBaseEvent<T> : MyEntityReplicableBase<T>, IMyProxyTarget
        where T : MyEntity, IMyEventProxy
    {
        public IMyEventProxy Target { get { return Instance; } }
    }
}
