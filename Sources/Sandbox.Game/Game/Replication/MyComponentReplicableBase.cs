using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Serialization;

namespace Sandbox.Game.Replication
{
    public abstract class MyComponentReplicableBase<T> : MyExternalReplicableEvent<T>
        where T: MyEntityComponentBase, IMyEventProxy
    {
        public T Component { get { return Instance; } }
        Action<MyEntity> m_raiseDestroyedHandler;

        public MyComponentReplicableBase()
        {
            m_raiseDestroyedHandler = (entity) => RaiseDestroyed();
        }

        protected override void OnHook()
        {
            base.OnHook();
            if (Component != null)
            {
                ((MyEntity)Component.Entity).OnClose += m_raiseDestroyedHandler;
                Component.BeforeRemovedFromContainer += (component) => OnComponentRemovedFromContainer();
            }
        }

        private void OnComponentRemovedFromContainer()
        {
            if (Component != null && Component.Entity != null)
            {
                ((MyEntity)Component.Entity).OnClose -= m_raiseDestroyedHandler;
                RaiseDestroyed();
            }
        }

        public override bool HasToBeChild
        {
            get
            {
                return true;
            }
        }

        public override float GetPriority(MyClientInfo client, bool cached)
        {
            System.Diagnostics.Debug.Fail("Cannot call GetPriority on children");
            return 0;
        }

        protected override void OnLoad(BitStream stream, Action<T> loadingDoneHandler)
        {
            long entityId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out entityId);

            MyEntities.CallAsync(() => LoadAsync(entityId, loadingDoneHandler));
        }

        protected override void OnLoadBegin(BitStream stream, Action<T> loadingDoneHandler)
        {
            // This will not be called when the component does not have a streamed replicable
            throw new NotImplementedException();
        }

        private void LoadAsync(long entityId, Action<T> loadingDoneHandler)
        {
            var savedUnderType = MyComponentTypeFactory.GetComponentType(typeof(T));
            MyEntity entity = null;
            MyComponentBase component = null;
            if (!MyEntities.TryGetEntityById(entityId, out entity) ||
                !entity.Components.TryGet(savedUnderType, out component))
            {
                Debug.Fail("MyEntityComponentReplicableBase - trying to init component on an entity, but either entity or component wasn't found!");
            }

            loadingDoneHandler(component as T);
        }

        public override bool OnSave(BitStream stream)
        {
            long entityId = Component.Entity.EntityId;
            MySerializer.Write(stream, ref entityId);
            return true;
        }

        public override void OnDestroy()
        {
            if (Component != null && Component.Entity != null)
            {
                ((MyEntity)Component.Entity).OnClose -= m_raiseDestroyedHandler;
            }
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList) { }

        public override VRageMath.BoundingBoxD GetAABB()
        {
            System.Diagnostics.Debug.Fail("GetAABB can be called only on root replicables");
            return VRageMath.BoundingBoxD.CreateInvalid();
        }
    }
}
