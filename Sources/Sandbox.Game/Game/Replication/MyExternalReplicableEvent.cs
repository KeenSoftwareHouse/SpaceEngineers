using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Same as MyExternalReplicableEvent, but with support for event proxy.
    /// </summary>
    public abstract class MyExternalReplicableEvent<T> : MyExternalReplicable<T>, IMyProxyTarget
        where T : IMyEventProxy
    {
        private IMyEventProxy m_proxy;

        IMyEventProxy IMyProxyTarget.Target { get { return m_proxy; } }

        protected override void OnHook()
        {
            m_proxy = Instance;
        }
    }
}
