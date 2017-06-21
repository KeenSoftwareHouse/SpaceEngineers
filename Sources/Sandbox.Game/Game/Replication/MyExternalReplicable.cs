using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Sandbox.Game.Replication
{
    public abstract class MyExternalReplicable : IMyReplicable
    {
        private static MyConcurrentDictionary<object, MyExternalReplicable> m_objectExternalReplicables = new MyConcurrentDictionary<object, MyExternalReplicable>();

        /// <summary>
        /// Raised when replicable is destroyed from inside, e.g. Entity is closed which causes replicable to be closed.
        /// </summary>
        public static event Action<MyExternalReplicable> Destroyed;

        public static MyExternalReplicable FindByObject(object obj)
        {
            return m_objectExternalReplicables.GetValueOrDefault(obj, null);
        }

        public virtual void Hook(object obj)
        {
            m_objectExternalReplicables[obj] = this;
        }

        public virtual void OnServerReplicate()
        {
        }

        public virtual bool IsReadyForReplication
        {
            get 
            {
                if (HasToBeChild)
                {
                    return GetParent() != null && GetParent().IsReadyForReplication;
                }

                return true; 
            }
        }

        public virtual Dictionary<IMyReplicable, Action> ReadyForReplicationAction
        {
            get 
            {
                var parent = GetParent();
                if (parent != null)
                    return parent.ReadyForReplicationAction;

                return null;
            }
        }

        protected virtual void RaiseDestroyed()
        {
            var handler = Destroyed;
            if (handler != null) handler(this);

            ReadyForReplicationAction.Remove(this);

            // Probably happens when replicable is not fully initialized, but it's being destroyed (world unload)
            var inst = GetInstance();
            if (inst != null) m_objectExternalReplicables.Remove(inst);
        }

        protected abstract object GetInstance();
        protected abstract void OnHook();

        public abstract bool HasToBeChild { get; }
        public abstract IMyReplicable GetParent();
        public abstract float GetPriority(MyClientInfo client,bool cached);
        public abstract bool OnSave(BitStream stream);
        public abstract void OnLoad(BitStream stream, Action<bool> loadingDoneHandler);
        public abstract void OnLoadBegin(BitStream stream, Action<bool> loadingDoneHandler);
        public abstract void OnDestroy();
        public abstract void GetStateGroups(List<IMyStateGroup> resultList);

        public abstract BoundingBoxD GetAABB();
        public Action<IMyReplicable> OnAABBChanged { get; set; }


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

        public virtual HashSet<IMyReplicable> GetDependencies()
        {
            return null;
        }
    }
}
