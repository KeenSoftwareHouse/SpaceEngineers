using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Utils;

namespace Sandbox.Game.Replication
{
    class MyInventoryBaseReplicable : MyExternalReplicableEvent<MyInventoryBase>
    {
        public MyInventoryBase Inventory { get { return Instance; } }

        HashSet<ulong> m_clientList;
        Action<MyEntity> m_destroyEntity;

        public MyInventoryBaseReplicable()
        {
            m_destroyEntity = (entity) => RaiseDestroyed();
            m_clientList = new HashSet<ulong>();
        }

        protected override void OnHook()
        {
            base.OnHook();
            if (Inventory != null)
            {
                ((MyEntity)Inventory.Entity).OnClose += m_destroyEntity;
                Inventory.BeforeRemovedFromContainer += (component) => OnRemovedFromContainer();
            }
        }

        public override IMyReplicable GetParent()
        {
            Debug.Assert(Inventory.Entity != Inventory, "Inventory owner is inventory!");
            Debug.Assert(!((MyEntity)Inventory.Entity).Closed, "Sending inventory of closed entity");
            if (Inventory.Entity is MyCharacter)
            {
                return MyExternalReplicable.FindByObject(Inventory.Entity);
            }

            if (Inventory.Entity is MyCubeBlock)
            {
                return MyExternalReplicable.FindByObject((Inventory.Entity as MyCubeBlock).CubeGrid);
            }

            return null;
        }

        public override float GetPriority(MyClientInfo client,bool cached)
        {
            if (m_clientList.Contains(client.EndpointId.Value))
            {
                return 1.0f;
            }

            if (Inventory.Entity is MyCharacter)
            {
                MyPlayer player = MyPlayer.GetPlayerFromCharacter(Inventory.Entity as MyCharacter);
                if (player != null && player.Id.SteamId == client.EndpointId.Value)
                {
                    m_clientList.Add(client.EndpointId.Value);
                    return 1.0f;
                }
            }

            return 0.0f;
        }

        public override bool OnSave(BitStream stream)
        {
            long ownerId = Inventory.Entity.EntityId;
            VRage.Serialization.MySerializer.Write(stream, ref ownerId);

            MyStringHash inventoryId = Inventory.InventoryId;
            VRage.Serialization.MySerializer.Write(stream, ref inventoryId);

            return true;
        }

        protected override void OnLoad(BitStream stream, Action<MyInventoryBase> loadingDoneHandler)
        {
            long entityId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out entityId);

            MyStringHash inventoryId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out inventoryId);

            MyEntities.CallAsync(() => LoadAsync(entityId, inventoryId, loadingDoneHandler));
        }

        protected override void OnLoadBegin(BitStream stream, Action<MyInventoryBase> loadingDoneHandler)
        {
            OnLoad(stream, loadingDoneHandler);
        }

        private void LoadAsync(long entityId, MyStringHash inventoryId, Action<MyInventoryBase> loadingDoneHandler)
        {
            MyEntity entity;
            MyInventoryBase inventory = null;
            MyInventoryBase inventory2 = null;
            if (MyEntities.TryGetEntityById(entityId, out entity) && entity.Components.TryGet<MyInventoryBase>(out inventory))
            {
                if (inventory is MyInventoryAggregate)
                    inventory2 = (inventory as MyInventoryAggregate).GetInventory(inventoryId);
            }

            // TODO: In Medieval, inventory is sometimes null (on client) when one of two clients die
            loadingDoneHandler(inventory2 ?? inventory); 
        }

        public override void OnDestroy()
        {
            if (Inventory != null && Inventory.Entity != null)
            {
                ((MyEntity)Inventory.Entity).OnClose -= m_destroyEntity;
            }
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
        }

        void OnRemovedFromContainer()
        {
            if (Inventory != null && Inventory.Entity != null)
            {
                ((MyEntity)Inventory.Entity).OnClose -= m_destroyEntity;
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
