using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Library;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Same as MyEntityReplicableBase, but with support for event proxy.
    /// </summary>
    public abstract class MyEntityReplicableBaseEvent<T> : MyEntityReplicableBase<T>, IMyProxyTarget
        where T : MyEntity, IMyEventProxy
    {
        private IMyEventProxy m_proxy;

        IMyEventProxy IMyProxyTarget.Target { get { return m_proxy; } }

        protected override void OnHook()
        {
            base.OnHook();
            RegisterAsserts(); // The code is not ready for this (tons of asserts raised)
            m_proxy = Instance;
        }

        [Conditional("DEBUG")]
        void RegisterAsserts()
        {
            if (!Sync.IsServer)
            {
                Instance.OnMarkForClose += OnMarkForCloseOnClient;
                Instance.OnClose += OnMarkForCloseOnClient;
            }
        }

        void OnMarkForCloseOnClient(MyEntity entity)
        {
            // MyEntity.Close() on client should not be called, except when REPLICABLE_DESTROY is received from server

            // When entity is being closed correctly on client:
            // Close() is called from OnDestroy() which is called by ReplicationClient after entity is removed from replication.
            // Therefore we shouldn't be able to get it's network ID.
            if (MyMultiplayer.Static == null)
            {
                return;
            }
            var obj = MyMultiplayer.Static.ReplicationLayer.GetProxyTarget(m_proxy);
            
            NetworkId netId;
            if (MySession.Static.Ready && obj != null && MyMultiplayer.Static.ReplicationLayer.TryGetNetworkIdByObject(obj, out netId))
            {
                Debug.Fail("Deleting entity, but network object is still there! Close called by client?" + MyEnvironment.NewLine + m_proxy.ToString());
            }
        }
    }
}
