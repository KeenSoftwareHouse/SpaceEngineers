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
        MyPropertySyncStateGroup m_propertySync;
        MyEntityInventoryStateGroup m_stateGroup;
        HashSet<ulong> m_clientList;

        public MyInventory Inventory { get { return Instance; } }
        Action<MyEntity> m_destroyEntity;

        public MyInventoryReplicable()
        {
            m_destroyEntity = (entity) => RaiseDestroyed();
        }

        protected override void OnHook()
        {
            base.OnHook();
            if (Inventory != null)
            {
                m_stateGroup = new MyEntityInventoryStateGroup(Inventory, Sync.IsServer);
                ((MyEntity)Inventory.Owner).OnClose += m_destroyEntity;
                Inventory.BeforeRemovedFromContainer += (component) => OnRemovedFromContainer();
                m_propertySync = new MyPropertySyncStateGroup(this, Instance.SyncType);
            }
        }

        public override IMyReplicable GetDependency()
        {
            Debug.Assert(!((MyEntity)Inventory.Owner).Closed, "Sending inventory of closed entity");
            if (Inventory.Owner is MyCharacter)
            {
                return MyExternalReplicable.FindByObject(Inventory.Owner);
            }

            if (Inventory.Owner is MyCubeBlock)
            {
                return MyExternalReplicable.FindByObject((Inventory.Owner as MyCubeBlock).CubeGrid);
            }

            return null;
        }

        public override float GetPriority(MyClientInfo client)
        {
            if (m_clientList == null)
            {
                m_clientList = new HashSet<ulong>();
            }

            if (m_clientList.Contains(client.EndpointId.Value))
            {
                return 1.0f;
            }

            MyEntity owner = Inventory.Owner.GetTopMostParent();
            var parent = MyExternalReplicable.FindByObject(owner);
            if (parent != null && client.HasReplicable(parent))
            {
                m_clientList.Add(client.EndpointId.Value);
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

    }
}
