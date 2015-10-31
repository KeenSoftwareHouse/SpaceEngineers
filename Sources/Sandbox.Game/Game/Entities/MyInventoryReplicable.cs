using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replicables;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;


namespace Sandbox.Game.Entities
{
    class MyInventoryReplicable : MyExternalReplicable<MyInventory>, IMyProxyTarget
    {
        MyEntityInventoryStateGroup m_stateGroup;
        List<MyPhysicalInventoryItem> m_items = new List<MyPhysicalInventoryItem>();
        HashSet<ulong> m_clientList;

        public MyInventory Inventory { get { return Instance; } }
        IMyEventProxy IMyProxyTarget.Target { get { return Inventory; } }

        protected override void OnHook()
        {
            if (Inventory != null)
            {
                m_stateGroup = new MyEntityInventoryStateGroup(Inventory, Sync.IsServer);
                ((MyEntity)Inventory.Owner).OnClose += (entity) => RaiseDestroyed();
            }
        }

        public override IMyReplicable GetDependency()
        {
            Debug.Assert(Inventory.Owner != Inventory, "Inventory owner is inventory!");
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

        public override float GetPriority(MyClientStateBase client)
        {
            if (m_clientList == null)
            {
                m_clientList = new HashSet<ulong>();
            }

            if (m_clientList.Contains(client.EndpointId.Value))
            {
                return 1.0f;
            }

            if (Inventory.Owner is MyCharacter)
            {
                MyPlayer player = MyPlayer.GetPlayerFromCharacter(Inventory.Owner as MyCharacter);
                if (player != null && player.Id.SteamId == client.EndpointId.Value)
                {
                    m_clientList.Add(client.EndpointId.Value);
                    return 1.0f;
                }
            }
            float priority = Inventory.GetPriority(client);
            if (priority > 0.0f)
            {
                m_clientList.Add(client.EndpointId.Value);
            }
            return priority;
        }

        public override void OnSave(BitStream stream)
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

            List<MyPhysicalInventoryItem> items = Inventory.GetItems();
            int numItems = items.Count;
            VRage.Serialization.MySerializer.Write(stream, ref numItems);
            for (int i = 0; i < numItems; ++i)
            {
                MyPhysicalInventoryItem item = items[i];
                VRage.Serialization.MySerializer.Write(stream, ref item, MyObjectBuilderSerializer.Dynamic);
            }
        }

        protected override void OnLoad(BitStream stream, Action<MyInventory> loadingDoneHandler)
        {
            long entityId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out entityId);

            int inventoryId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out inventoryId);
            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);

            IMyInventoryOwner owner = entity as IMyInventoryOwner;

            MyInventory inventory = null;
            if (owner != null)
            {
                inventory = owner.GetInventory(inventoryId);

                m_items.Clear();
                int numItems;
                VRage.Serialization.MySerializer.CreateAndRead(stream, out numItems);
                for (int i = 0; i < numItems; ++i)
                {
                    MyPhysicalInventoryItem item;
                    VRage.Serialization.MySerializer.CreateAndRead(stream, out item, MyObjectBuilderSerializer.Dynamic);
                    m_items.Add(item);
                }

                inventory.SetItems(m_items);
            }
            Debug.Assert(inventory != null, "Dusan, we should fix this, try to find out what is that EntityId of owner on server: " + entityId);
            loadingDoneHandler(inventory);
        }

        public override void OnDestroy()
        {
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            if (m_stateGroup != null)
            {
                resultList.Add(m_stateGroup);
            }
        }

        public override string ToString()
        {
            string id = Inventory != null ? (Inventory.Owner != null ? Inventory.Owner.EntityId.ToString() : "<owner null>") : "<inventory null>";
            return String.Format("MyInventoryReplicable, Owner id: " + id);
        }
    }
}
