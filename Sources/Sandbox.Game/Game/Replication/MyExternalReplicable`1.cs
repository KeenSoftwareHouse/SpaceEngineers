using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Game.Entity;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// External replicable which is hooked to replicated object.
    /// On server instances are created by reacting to event like MyEntities.OnEntityCreated, subscribed by MyMultiplayerServerBase
    /// On clients instances are created by replication layer, which creates instance and calls OnLoad()
    /// </summary>
    /// <typeparam name="T">Type of the object to which is replicable hooked.</typeparam>
    public abstract class MyExternalReplicable<T> : MyExternalReplicable
    {
        public T Instance { get; private set; }
        public override bool IsReadyForReplication
        {
            get
            {
                MyEntity entity = Instance as MyEntity;
                var entityContainer = Instance as VRage.Game.Components.MyEntityComponentBase;
                if (entity != null)
                    return entity.IsReadyForReplication;
                else
                    if (entityContainer != null)
                        return ((MyEntity)entityContainer.Entity).IsReadyForReplication;
                    else
                        return base.IsReadyForReplication;
            }
        }

        public override Dictionary<IMyReplicable, Action> ReadyForReplicationAction
        {
            get
            {
                MyEntity entity = Instance as MyEntity;
                var entityContainer = Instance as VRage.Game.Components.MyEntityComponentBase;
                if (entity != null)
                    return entity.ReadyForReplicationAction;
                else
                    if (entityContainer != null)
                        return ((MyEntity)entityContainer.Entity).ReadyForReplicationAction;
                    else
                        return base.ReadyForReplicationAction;
            }
        }

        protected abstract void OnLoad(BitStream stream, Action<T> loadingDoneHandler);
        protected abstract void OnLoadBegin(BitStream stream, Action<T> loadingDoneHandler);
    
        protected sealed override object GetInstance()
        {
            return Instance;
        }

        protected sealed override void RaiseDestroyed()
        {
            base.RaiseDestroyed();
            Instance = default(T);
            Debug.Assert(!(this is IMyProxyTarget) || ((IMyProxyTarget)this).Target != null, "IMyProxyTarget.Target cannot depend on Instance, it must be stored and never cleared!");
        }

        void OnLoadDone(T instance, Action<bool> loadingDoneHandler)
        {
            Debug.Assert(!Sync.IsServer, "OnLoadDone should be called only on client");
            if (instance != null)
            {
                HookInternal(instance);
                Debug.Assert(!(this is IMyProxyTarget) || ((IMyProxyTarget)this).Target != null, "IMyProxyTarget.Target must be set in OnHook!");
                loadingDoneHandler(true);
            }
            else
            {
                loadingDoneHandler(false);
            }
        }

        public sealed override void OnLoad(BitStream stream, Action<bool> loadingDoneHandler)
        {
            OnLoad(stream, (instance) => OnLoadDone(instance, loadingDoneHandler));
        }

        public sealed override void OnLoadBegin(BitStream stream, Action<bool> loadingDoneHandler)
        {
            OnLoadBegin(stream, (instance) => OnLoadDone(instance, loadingDoneHandler));
        }

        /// <summary>
        /// Called on server before adding object to replication layer.
        /// </summary>
        public sealed override void Hook(object obj)
        {
            Debug.Assert(Sync.IsServer, "Hook should be called only on server (from MyMultiplayerServerBase)");
            HookInternal(obj);
        }

        private void HookInternal(object obj)
        {
            Debug.Assert(Instance == null, "Already hooked, double call to Hook(object obj)?");
            Instance = (T)obj;
            base.Hook(obj);
            OnHook();
        }
    }
}
