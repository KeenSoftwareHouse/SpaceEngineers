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
    public abstract class MyExternalReplicable : IMyReplicable
    {
        private static Dictionary<object, MyExternalReplicable> m_objectExternalReplicables = new Dictionary<object, MyExternalReplicable>();

        /// <summary>
        /// Raised when replicable is destroyed from inside, e.g. Entity is closed which causes replicable to be closed.
        /// </summary>
        public static event Action<MyExternalReplicable> Destroyed;

        public static MyExternalReplicable FindByObject(object obj)
        {
            return m_objectExternalReplicables.GetValueOrDefault(obj);
        }

        public virtual void Hook(object obj)
        {
            m_objectExternalReplicables[obj] = this;
        }

        public virtual void OnServerReplicate()
        {
        }

        protected virtual void RaiseDestroyed()
        {
            var handler = Destroyed;
            if (handler != null) handler(this);

            // Probably happens when replicable is not fully initialized, but it's being destroyed (world unload)
            var inst = GetInstance();
            if (inst != null) m_objectExternalReplicables.Remove(inst);
        }

        protected abstract object GetInstance();
        protected abstract void OnHook();

        public bool IsChild { get; protected set; }
        public abstract IMyReplicable GetDependency();
        public abstract float GetPriority(MyClientInfo client);
        public abstract bool OnSave(BitStream stream);
        public abstract void OnLoad(BitStream stream, Action<bool> loadingDoneHandler);
        public abstract void OnLoadBegin(BitStream stream, Action<bool> loadingDoneHandler);
        public abstract void OnDestroy();
        public abstract void GetStateGroups(List<IMyStateGroup> resultList);

        /// <summary>
        /// Hack to allow interop with old event system without complete rewrite.
        /// </summary>
        [Event, Reliable, Server]
        public void RpcToServer_Implementation(BitReaderWriter reader)
        {
            Sync.Layer.ProcessRpc(reader);
        }

        /// <summary>
        /// Hack to allow interop with old event system without complete rewrite.
        /// </summary>
        [Event, Reliable, Broadcast]
        public void RpcToAll_Implementation(BitReaderWriter reader)
        {
            Sync.Layer.ProcessRpc(reader);
        }

        /// <summary>
        /// Hack to allow interop with old event system without complete rewrite.
        /// </summary>
        [Event, Reliable, BroadcastExcept]
        public void RpcToAllButOne_Implementation(BitReaderWriter reader)
        {
            Sync.Layer.ProcessRpc(reader);
        }

        /// <summary>
        /// Hack to allow interop with old event system without complete rewrite.
        /// </summary>
        [Event, Reliable, Client]
        public void RpcToClient_Implementation(BitReaderWriter reader)
        {
            Sync.Layer.ProcessRpc(reader);
        }
    }
}
