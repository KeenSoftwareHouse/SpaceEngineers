using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Replication
{
    class MyInventoryReplicable : MyExternalReplicableEvent<MyInventory>
    {
        StateGroups.MyPropertySyncStateGroup m_propertySync;
        StateGroups.MyEntityInventoryStateGroup m_stateGroup;
        MyExternalReplicable m_parent;

        public MyInventory Inventory { get { return Instance; } }
        Action<MyEntity> m_destroyEntity;

        MyEntity m_owner = null;

        public MyInventoryReplicable()
        {
            m_destroyEntity = (entity) => RaiseDestroyed();

        }

        protected override void OnHook()
        {
            base.OnHook();
            if (Inventory != null)
            {
                m_stateGroup = new StateGroups.MyEntityInventoryStateGroup(Inventory, Sync.IsServer, this);
                ((MyEntity)Inventory.Owner).OnClose += m_destroyEntity;
                Inventory.BeforeRemovedFromContainer += (component) => OnRemovedFromContainer();
                m_propertySync = new StateGroups.MyPropertySyncStateGroup(this, Instance.SyncType);
            }
        }

        public override IMyReplicable GetParent()
        {
            if (Inventory == null)
                return null;

            Debug.Assert(!((MyEntity)Inventory.Owner).Closed, "Sending inventory of closed entity");
         
            if (Inventory.Owner is MyCubeBlock)
            {
                return MyExternalReplicable.FindByObject((Inventory.Owner as MyCubeBlock).CubeGrid);
            }

            return MyExternalReplicable.FindByObject(Inventory.Owner);
        }

        public override float GetPriority(MyClientInfo client,bool cached)
        {
            MyEntity owner = Inventory.Owner.GetTopMostParent();

            if (owner != m_owner)
            {
                m_owner = owner;
                m_parent = MyExternalReplicable.FindByObject(owner);
            }

            if (m_parent != null && client.HasReplicable(m_parent))
            {
                return 1.0f;
            }

            return 0.0f;
        }

        public override bool OnSave(BitStream stream)
        {
            //int bitPos = stream.BitPosition;        
            long ownerId = Inventory.Owner.EntityId;
            VRage.Serialization.MySerializer.Write(stream, ref ownerId);

            int inventoryId = 0;

            for (int i = 0; i < Inventory.Owner.InventoryCount; ++i)
            {
                if (Inventory == Inventory.Owner.GetInventory(i))
                {
                    inventoryId = i;
                    break;
                }
            }

            VRage.Serialization.MySerializer.Write(stream, ref inventoryId);
            return true;
        }

        protected override void OnLoad(BitStream stream, Action<MyInventory> loadingDoneHandler)
        {
            long entityId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out entityId);

            int inventoryId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out inventoryId);

            MyEntities.CallAsync(() => LoadAsync(entityId, inventoryId, loadingDoneHandler));
        }

        protected override void OnLoadBegin(BitStream stream, Action<MyInventory> loadingDoneHandler)
        {
            OnLoad(stream, loadingDoneHandler);
        }

        private static void LoadAsync(long entityId, int inventoryId,  Action<MyInventory> loadingDoneHandler)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);

            MyInventory inventory = null;
            MyEntity owner = (entity != null && entity.HasInventory) ? entity : null;
            if (owner != null)
            {
                inventory = owner.GetInventory(inventoryId) as MyInventory;       
            }
          //  Debug.Assert(inventory != null, "Dusan, we should fix this, try to find out what is that EntityId of owner on server: " + entityId);
            loadingDoneHandler(inventory);
        }

        public override void OnDestroy()
        {
            if (Inventory != null && Inventory.Owner != null)
            {
                ((MyEntity)Inventory.Owner).OnClose -= m_destroyEntity;
            }
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            if (m_stateGroup != null)
            {
                resultList.Add(m_stateGroup);
            }
            resultList.Add(m_propertySync);
        }

        public override string ToString()
        {
            string id = Inventory != null ? (Inventory.Owner != null ? Inventory.Owner.EntityId.ToString() : "<owner null>") : "<inventory null>";
            return String.Format("MyInventoryReplicable, Owner id: " + id);
        }

        void OnRemovedFromContainer()
        {
            if (Inventory != null && Inventory.Owner != null)
            {
                ((MyEntity)Inventory.Owner).OnClose -= m_destroyEntity;
                RaiseDestroyed();
            }
        }

        public override bool HasToBeChild
        {
            get { return true; }
        }

        public override VRageMath.BoundingBoxD GetAABB()
        {
            System.Diagnostics.Debug.Fail("GetAABB can be called only on root replicables");
            return VRageMath.BoundingBoxD.CreateInvalid();
        }
    }
}
