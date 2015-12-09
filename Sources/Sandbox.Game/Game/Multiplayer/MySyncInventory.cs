using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using Sandbox.Game.GUI;
using SteamSDK;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using Sandbox.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Inventory;
using VRage.Components;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncInventory
    { 
        [ProtoContract]
        [MessageId(2475, P2PMessageEnum.Reliable)]
        struct TransferInventoryMsg
        {
            [ProtoMember]
            public long SourceEntityID;

            [ProtoMember]
            public long DestinationEntityID;

            [ProtoMember]
            public MyStringHash InventoryId;

            [ProtoMember]
            public bool RemoveEntityOnEmpty;

            [ProtoMember]
            public bool ClearSourceInventories; 
        }
        
        static MySyncInventory()
        {
            MySyncLayer.RegisterMessage<TransferInventoryMsg>(OnTransferInventoryMsg, MyMessagePermissions.FromServer);
        }
  
        public static void SendTransferInventoryMsg(long sourceEntityID, long destinationEntityID, MyInventory inventory, bool clearSourceInventories = false)
        {
            var msg = new TransferInventoryMsg();
            msg.SourceEntityID = sourceEntityID;
            msg.DestinationEntityID = destinationEntityID;
            msg.InventoryId = MyStringHash.GetOrCompute(inventory.InventoryId.ToString());
            msg.RemoveEntityOnEmpty = inventory.RemoveEntityOnEmpty;
            msg.ClearSourceInventories = clearSourceInventories;
            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }       

        static void OnTransferInventoryMsg(ref TransferInventoryMsg msg, MyNetworkClient sender)
        {
            MyEntity source = MyEntities.GetEntityById(msg.SourceEntityID);
            MyEntity destination = MyEntities.GetEntityById(msg.DestinationEntityID);
            Debug.Assert(source != null && destination != null, "Entities weren't found!");
            if (source != null && destination != null)
            {
                var inventory = source.Components.Get<MyInventoryBase>();
                var inventoryAggregate = inventory as MyInventoryAggregate;

                var destinationAggregate = destination.Components.Get<MyInventoryBase>() as MyInventoryAggregate;

                if (inventoryAggregate != null)
                {
                    inventory = inventoryAggregate.GetInventory(msg.InventoryId);
                    inventoryAggregate.ChildList.RemoveComponent(inventory);
                }
                else
                {
                    inventory.Container.Remove<MyInventoryBase>();
                }

                Debug.Assert(inventoryAggregate == null || (inventoryAggregate != null && inventoryAggregate.GetInventory(inventory.InventoryId) == null), "Source's entity inventory aggregate still contains inventory!");
                Debug.Assert(inventoryAggregate != null || (inventoryAggregate == null && !source.Components.Has<MyInventoryBase>()), "Inventory wasn't removed from it's source entity");

                if (source is MyCharacter)
                {
                    (source as MyCharacter).Inventory = null;
                }

                Debug.Assert(inventory.InventoryId.ToString() == msg.InventoryId.ToString(), "Inventory wasn't found!");

                if (destinationAggregate != null)
                {
                    destinationAggregate.ChildList.AddComponent(inventory);
                }
                else
                {
                    destination.Components.Add<MyInventoryBase>(inventory);
                }
               
                inventory.RemoveEntityOnEmpty = msg.RemoveEntityOnEmpty;    
           
                // TODO (OM): Since we still have IMyInventoryOwner we need to keep the below, but remove it, when IMyInventoryOwner is no longer needed
                if (inventory is MyInventory)
                {
                    (inventory as MyInventory).RemoveOwner();
                }

                Debug.Assert(destinationAggregate == null || (destinationAggregate.GetInventory(inventory.InventoryId) != null), "The destination aggregate doesn't contain inserted inventory!");

                // Check whether the destination entity has the detector component
                MyUseObjectsComponent useObjectComponent = null;
                if (!destination.Components.Has<MyUseObjectsComponentBase>())
                {
                    useObjectComponent = new MyUseObjectsComponent();
                    destination.Components.Add<MyUseObjectsComponentBase>(useObjectComponent);
                }
                else
                {
                    useObjectComponent = destination.Components.Get<MyUseObjectsComponentBase>() as MyUseObjectsComponent;                   
                }
                Debug.Assert(useObjectComponent != null, "Detector is missing on the entity!");
                if (useObjectComponent != null && useObjectComponent.GetDetectors("inventory").Count == 0)
                {
                    var useObjectMat = Matrix.CreateScale(destination.PositionComp.LocalAABB.Size) * Matrix.CreateTranslation(destination.PositionComp.LocalAABB.Center);
                    useObjectComponent.AddDetector("inventory", useObjectMat);
                    useObjectComponent.RecreatePhysics();
                }

                if (msg.ClearSourceInventories)
                {
                    source.Components.Remove<MyInventoryBase>();
                }
            }
        }
        
    }
}
