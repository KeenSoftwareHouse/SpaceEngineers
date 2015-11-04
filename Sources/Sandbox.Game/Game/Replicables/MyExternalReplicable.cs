using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace Sandbox.Game.Replicables
{
    public abstract class MyExternalReplicable : IMyReplicable
    {
        private static Dictionary<object, MyExternalReplicable> m_objectExternalReplicables = new Dictionary<object, MyExternalReplicable>();

        /// <summary>
        /// Raised when replicable is destroyed from inside, e.g. Entity is closed which causes replicable to be close.
        /// </summary>
        public static event Action<MyExternalReplicable> Destroyed;

        public static MyExternalReplicable FindByObject(object obj)
        {
            return m_objectExternalReplicables.GetValueOrDefault(obj);
        }

        public virtual void Hook(object obj)
        {
            m_objectExternalReplicables.Add(obj, this);
        }

        protected virtual void RaiseDestroyed()
        {
            var handler = Destroyed;
            if (handler != null) handler(this);
            m_objectExternalReplicables.Remove(GetInstance());
        }

        protected abstract object GetInstance();
        protected abstract void OnHook();

        public abstract IMyReplicable GetDependency();
        public abstract float GetPriority(MyClientStateBase client);
        public abstract void OnSave(BitStream stream);
        public abstract void OnLoad(BitStream stream, Action loadingDoneHandler);
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
        [Event, Reliable, BroadcastExept]
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

    /// <summary>
    /// External replicable which is hooked to replicated object.
    /// On server instances are created by reacting to event like MyEntities.OnEntityCreated, subscribed by MyMultiplayerServerBase
    /// On clients instances are created by replication layer, which creates instance and calls OnLoad()
    /// </summary>
    /// <typeparam name="T">Type of the object to which is replicable hooked.</typeparam>
    public abstract class MyExternalReplicable<T> : MyExternalReplicable
    {
        public T Instance { get; private set; }

        protected abstract void OnLoad(BitStream stream, Action<T> loadingDoneHandler);

        protected sealed override object GetInstance()
        {
            return Instance;
        }

        protected sealed override void RaiseDestroyed()
        {
            base.RaiseDestroyed();
            Instance = default(T);
        }

        void OnLoadDone(T instance, Action loadingDoneHandler)
        {
            Debug.Assert(!Sync.IsServer, "OnLoadDone should be called only on client");
            Debug.Assert(Instance == null, "Already hooked, double call to Hook(object obj)?");
            Instance = instance;
            OnHook();
            loadingDoneHandler();
        }

        public sealed override void OnLoad(BitStream stream, Action loadingDoneHandler)
        {
            OnLoad(stream, (instance) => OnLoadDone(instance, loadingDoneHandler));
        }

        /// <summary>
        /// Called on server before adding object to replication layer.
        /// Should be manually called on client from OnLoad().
        /// </summary>
        public sealed override void Hook(object obj)
        {
            Debug.Assert(Sync.IsServer, "Hook should be called only on server (from MyMultiplayerServerBase)");
            Debug.Assert(Instance == null, "Already hooked, double call to Hook(object obj)?");
            Instance = (T)obj;
            base.Hook(obj);
            OnHook();
        }
    }
}
