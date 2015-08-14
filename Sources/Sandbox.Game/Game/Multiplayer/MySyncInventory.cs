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
        [MessageId(2467, P2PMessageEnum.Reliable)]
        struct TransferItemsMsg :IEntityMessage
        {
            public long OwnerEntityId;
            public long GetEntityId() { return OwnerEntityId; }
            public byte InventoryIndex;
            public uint itemId;
            public MyFixedPoint Amount;

            public long DestOwnerEntityId;
            public byte DestInventoryIndex;
            public int DestItemIndex;

            public BoolBlit Spawn;
        }

        [MessageId(2468, P2PMessageEnum.Reliable)]
        struct RemoveItemsMsg : IEntityMessage
        {
            public long OwnerEntityId;
            public long GetEntityId() { return OwnerEntityId; }
            public byte InventoryIndex;
            public uint itemId;
            public MyFixedPoint Amount;
            public BoolBlit Spawn;
        }

        [ProtoContract]
        [MessageId(2469, P2PMessageEnum.Reliable)]
        struct AddItemsMsg : IEntityMessage
        {
            [ProtoMember]
            public long OwnerEntityId;
            public long GetEntityId() { return OwnerEntityId; }

            [ProtoMember]
            public byte InventoryIndex;
            [ProtoMember]
            public int itemIdx;
            [ProtoMember]
            public MyFixedPoint Amount;
            [ProtoMember]
            public MyObjectBuilder_PhysicalObject Item;
        }

        [ProtoContract]
        [MessageId(2470, P2PMessageEnum.Reliable)]
        struct TakeFloatingObjectMsg : IEntityMessage
        {
            [ProtoMember]
            public long OwnerEntityId;
            public long GetEntityId() { return OwnerEntityId; }

            [ProtoMember]
            public byte InventoryIndex;
            [ProtoMember]
            public long FloatingObjectId;
            [ProtoMember]
            public MyFixedPoint Amount;
        }

        [MessageId(2471, P2PMessageEnum.Reliable)]
        struct OperationFailedMsg : IEntityMessage
        {
            public long OwnerEntityId;
            public long GetEntityId() { return OwnerEntityId; }
        }

        [MessageId(2472, P2PMessageEnum.Reliable)]
        struct UpdateOxygenLevelMsg : IEntityMessage
        {
            public long OwnerEntityId;
            public long GetEntityId() { return OwnerEntityId; }

            public uint ItemId;
            public byte InventoryIndex;
            public float OxygenLevel;
        }

		[ProtoContract]
		[MessageId(2473, P2PMessageEnum.Reliable)]
		struct ConsumeItemMsg : IEntityMessage
		{
			[ProtoMember]
			public long OwnerEntityId;
			public long GetEntityId() { return OwnerEntityId; }

			[ProtoMember]
			public byte InventoryId;

			[ProtoMember]
			public SerializableDefinitionId ItemId;

			[ProtoMember]
			public MyFixedPoint Amount;

			[ProtoMember]
			public long ConsumerEntityId;
		}

        [MessageId(2474, P2PMessageEnum.Reliable)]
        struct TransferItemsBaseMsg : IEntityMessage
        {
            public long SourceContainerId;              //TODO(OM): this is currently MyEntityId as this is the container for Inventory components
            public MyStringHash SourceInventoryId;      // this is computed inventory hash from string id
            public long SourceItemId;                   // this is IMyInventoryItem.ItemId

            public long DestinationContainerId;
            public MyStringHash DestinationInventoryId;
            
            public MyFixedPoint Amount;

            public long GetEntityId() { return SourceContainerId; }
        }

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
        }
        
        static MySyncInventory()
        {
            MySyncLayer.RegisterMessage<TransferItemsMsg>(OnTransferItemsRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RemoveItemsMsg>(OnRemoveItemsRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RemoveItemsMsg>(OnRemoveItemsSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<AddItemsMsg>(OnAddItemsRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<AddItemsMsg>(OnAddItemsSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<TakeFloatingObjectMsg>(OnTakeFloatingObjectRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<OperationFailedMsg>(OnInventoryOperationFail, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);

            MySyncLayer.RegisterMessage<UpdateOxygenLevelMsg>(OnUpdateOxygenLevel, MyMessagePermissions.FromServer);
			MySyncLayer.RegisterMessage<ConsumeItemMsg>(OnConsumeItem, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<TransferItemsBaseMsg>(OnTransferItemsBaseMsg, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<TransferInventoryMsg>(OnTransferInventoryMsg, MyMessagePermissions.FromServer);
        }

        public static void SendTransferInventoryMsg(long sourceEntityID, long destinationEntityID, MyInventory inventory)
        {
            var msg = new TransferInventoryMsg();
            msg.SourceEntityID = sourceEntityID;
            msg.DestinationEntityID = destinationEntityID;
            msg.InventoryId = MyStringHash.GetOrCompute(inventory.InventoryId.ToString());
            msg.RemoveEntityOnEmpty = inventory.RemoveEntityOnEmpty;
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
            }
        }
        
        static void OnInventoryOperationFail(ref OperationFailedMsg msg, MyNetworkClient sender)
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudDeleteBlock);
        }

        #region TakeFloatingObject

        public void TakeFloatingObjectRequest(MyInventory inv, MyFloatingObject obj)
        {
            var msg = new TakeFloatingObjectMsg();
            msg.OwnerEntityId = inv.Owner.EntityId;
            msg.InventoryIndex = inv.InventoryIdx;
            msg.FloatingObjectId = obj.EntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnTakeFloatingObjectRequest(ref TakeFloatingObjectMsg msg, MyNetworkClient sender)
        {
            MyEntity owner;
            if (!MyEntities.TryGetEntityById(msg.OwnerEntityId, out owner) || !(owner is IMyInventoryOwner))
                return;
            var inv = (owner as IMyInventoryOwner).GetInventory(msg.InventoryIndex);
            MyFloatingObject floatingObject;
            if (!MyEntities.TryGetEntityById<MyFloatingObject>(msg.FloatingObjectId, out floatingObject) || floatingObject.MarkedForClose)
                return;
            inv.TakeFloatingObject(floatingObject);
        }

        private static bool TakeFloatingObjectPrepare(long ownerEntityId, long floatingObjectId, byte inventoryIndex, out MyFloatingObject obj, out MyFixedPoint amount)
        {
            obj = null;
            amount = 0;

            if (!MyEntities.EntityExists(ownerEntityId) || !MyEntities.EntityExists(floatingObjectId))
                return false;
            obj = MyEntities.GetEntityById(floatingObjectId) as MyFloatingObject;
            if (obj.MarkedForClose)
                return false;
            var owner = MyEntities.GetEntityById(ownerEntityId) as IMyInventoryOwner;
            var inv = owner.GetInventory(inventoryIndex);
            return ComputeFloatingObjectAmount(obj, ref amount, inv);
        }

        private static bool ComputeFloatingObjectAmount(MyFloatingObject obj, ref MyFixedPoint amount, MyInventory inv)
        {
            amount = obj.Item.Amount;
            if (!MySession.Static.CreativeMode)
                amount = MyFixedPoint.Min(amount, inv.ComputeAmountThatFits(obj.Item.Content.GetId()));
            if (amount <= 0) // does not fit into inventory
                return false;
            return true;
        }

        #endregion

        #region RemoveItems

        public void SendRemoveItemsAnnounce(MyInventory inv, MyFixedPoint amount, uint itemId)
        {
            Debug.Assert(inv.Owner != null, "Inventory must have owner to be able to remove items synchronously!");
            Debug.Assert(Sync.IsServer);
            var msg = new RemoveItemsMsg();
            msg.OwnerEntityId = inv.Owner.EntityId;
            msg.InventoryIndex = inv.InventoryIdx;
            msg.itemId = itemId;
            msg.Amount = amount;
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        public void SendRemoveItemsRequest(MyInventory inv, MyFixedPoint amount, uint itemId, bool spawn = false)
        {
            Debug.Assert(inv.Owner != null, "Inventory must have owner to be able to remove items synchronously!");
            var msg = new RemoveItemsMsg();
            msg.OwnerEntityId = inv.Owner.EntityId;
            msg.InventoryIndex = inv.InventoryIdx;
            msg.itemId = itemId;
            msg.Amount = amount;
            msg.Spawn = spawn;
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnRemoveItemsRequest(ref RemoveItemsMsg msg, MyNetworkClient sender)
        {
            if (!MyEntities.EntityExists(msg.OwnerEntityId)) return;

            IMyInventoryOwner owner = MyEntities.GetEntityById(msg.OwnerEntityId) as IMyInventoryOwner;
            MyInventory inv = null;
            if (owner != null)
            {
                inv = owner.GetInventory(msg.InventoryIndex);
            }
            else
            {
                // NOTE: this should be the default code after we get rid of the inventory owner and should be searched by it's id
                MyEntity entity = MyEntities.GetEntityById(msg.OwnerEntityId);
                MyInventoryBase baseInventory;
                if (entity.Components.TryGet<MyInventoryBase>(out baseInventory))
                {
                    inv = baseInventory as MyInventory;
                }
            }
            var item = inv.GetItemByID(msg.itemId);
            if (!item.HasValue) return;
            inv.RemoveItems(msg.itemId, msg.Amount, spawn: msg.Spawn);
        }

        static void OnRemoveItemsSuccess(ref RemoveItemsMsg msg, MyNetworkClient sender)
        {
            if (!MyEntities.EntityExists(msg.OwnerEntityId)) return;
            IMyInventoryOwner owner = MyEntities.GetEntityById(msg.OwnerEntityId) as IMyInventoryOwner;
            MyInventory inv = null;
            if (owner != null)
            {
               inv = owner.GetInventory(msg.InventoryIndex);
            }
            else
            {
                // NOTE: this should be the default code after we get rid of the inventory owner and should be searched by it's id
                MyEntity entity = MyEntities.GetEntityById(msg.OwnerEntityId);
                MyInventoryBase baseInventory;
                if (entity.Components.TryGet<MyInventoryBase>(out baseInventory))
                {
                    inv = baseInventory as MyInventory;
                }
            }

            if (inv != null)
            {
                inv.RemoveItemsInternal(msg.itemId, msg.Amount);
            }
            else
            {
                Debug.Fail("Inventory was not found!");
            }
        }

        #endregion

        #region AddItems

        public void SendAddItemsAnnounce(MyInventory inv, MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index = -1)
        {
            Debug.Assert(inv.Owner != null, "Inventory must have owner to be able to add items synchronously!");
            var msg = new AddItemsMsg();
            msg.OwnerEntityId = inv.Owner.EntityId;
            msg.InventoryIndex = inv.InventoryIdx;
            msg.itemIdx = index;
            msg.Item = objectBuilder;
            msg.Amount = amount;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        public void SendAddItemsRequest(MyInventory inv, int index, MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder)
        {
            Debug.Assert(inv.Owner != null, "Inventory must have owner to be able to add items synchronously!");
            var msg = new AddItemsMsg();
            msg.OwnerEntityId = inv.Owner.EntityId;
            msg.InventoryIndex = inv.InventoryIdx;
            msg.itemIdx = index;
            msg.Item = objectBuilder;
            msg.Amount = amount;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnAddItemsRequest(ref AddItemsMsg msg, MyNetworkClient sender)
        {
            if (!MyEntities.EntityExists(msg.OwnerEntityId))
                return;
            IMyInventoryOwner owner = MyEntities.GetEntityById(msg.OwnerEntityId) as IMyInventoryOwner;
            owner.GetInventory(msg.InventoryIndex).AddItems(msg.Amount, msg.Item, msg.itemIdx);
        }

        static void OnAddItemsSuccess(ref AddItemsMsg msg, MyNetworkClient sender)
        {
            if (!MyEntities.EntityExists(msg.OwnerEntityId)) return;
            AddItemsInternal(msg);
        }

        private static void AddItemsInternal(AddItemsMsg msg)
        {
            IMyInventoryOwner owner = MyEntities.GetEntityById(msg.OwnerEntityId) as IMyInventoryOwner;
            MyInventory inv = owner.GetInventory(msg.InventoryIndex);
            inv.AddItemsInternal(msg.Amount, msg.Item, msg.itemIdx);
        }

        #endregion

        #region TransferItems

        public void TransferItems(MyInventory source, MyFixedPoint amount, uint itemId, MyInventory destination, int destinationIndex, bool spawn = false)
        {
            if (destination == null)
            {
                source.RemoveItems(itemId, amount, true, spawn);
                return;
            }

            var msg = PrepareTransferItemMessage(source, amount, itemId, destination, destinationIndex, spawn);

            if (Sync.IsServer)
            {
                TransferItemsInternal(source, destination, itemId, spawn, destinationIndex, amount);
            }
            else
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static TransferItemsMsg PrepareTransferItemMessage(MyInventory source, MyFixedPoint amount, uint itemId, MyInventory destination, int destinationIndex, bool spawn)
        {
            var msg = new TransferItemsMsg();
            msg.OwnerEntityId = source.Owner.EntityId;
            for (byte i = 0; i < source.Owner.InventoryCount; i++)
            {
                if (source.Owner.GetInventory(i).Equals(source))
                {
                    msg.InventoryIndex = i;
                    break;
                }
            }
            msg.itemId = itemId;
            msg.Amount = amount;
            msg.Spawn = spawn;

            if (source.Equals(destination))
            {
                msg.DestOwnerEntityId = msg.OwnerEntityId;
                msg.DestInventoryIndex = msg.InventoryIndex;
            }
            else
            {
                msg.DestOwnerEntityId = destination.Owner.EntityId;
                for (byte i = 0; i < destination.Owner.InventoryCount; i++)
                {
                    if (destination.Owner.GetInventory(i).Equals(destination))
                    {
                        msg.DestInventoryIndex = i;
                        break;
                    }
                }
            }
            msg.DestItemIndex = destinationIndex;
            return msg;
        }

        static void OnTransferItemsRequest(ref TransferItemsMsg msg, MyNetworkClient sender)
        {
            if (!MyEntities.EntityExists(msg.OwnerEntityId) || !MyEntities.EntityExists(msg.DestOwnerEntityId)) return;

            IMyInventoryOwner srcOwner = MyEntities.GetEntityById(msg.OwnerEntityId) as IMyInventoryOwner;
            IMyInventoryOwner destOwner = MyEntities.GetEntityById(msg.DestOwnerEntityId) as IMyInventoryOwner;
            MyInventory src = srcOwner.GetInventory(msg.InventoryIndex);
            MyInventory dst = destOwner.GetInventory(msg.DestInventoryIndex);

            TransferItemsInternal(src, dst, msg.itemId, msg.Spawn, msg.DestItemIndex, msg.Amount);
        }

        private static void TransferItemsInternal(MyInventory src, MyInventory dst, uint itemId, bool spawn, int destItemIndex, MyFixedPoint amount)
        {
            Debug.Assert(Sync.IsServer);
            MyFixedPoint remove = amount;

            var srcItem = src.GetItemByID(itemId);
            if (!srcItem.HasValue) return;

            FixTransferAmount(src, dst, srcItem, spawn, ref remove, ref amount);

            if (amount != 0)
            {
                if (dst.AddItems(amount, srcItem.Value.Content, destItemIndex))
                {
                    if (remove != 0)
                        src.RemoveItems(itemId, remove);
                }
            }
        }

        private static void FixTransferAmount(MyInventory src, MyInventory dst, MyPhysicalInventoryItem? srcItem, bool spawn, ref MyFixedPoint remove, ref MyFixedPoint add)
        {
            Debug.Assert(Sync.IsServer);
            if (srcItem.Value.Amount < remove)
            {
                remove = srcItem.Value.Amount;
                add = remove;
            }

            if (!MySession.Static.CreativeMode && !src.Equals(dst))
            {
                MyFixedPoint space = dst.ComputeAmountThatFits(srcItem.Value.Content.GetId());
                if (space < remove)
                {
                    if (spawn)
                    {
                        MyEntity e = (dst.Owner as MyEntity);
                        Matrix m = e.WorldMatrix;
                        MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(remove - space, srcItem.Value.Content), e.PositionComp.GetPosition() + m.Forward + m.Up, m.Forward, m.Up, e.Physics);
                    }
                    else
                    {
                        remove = space;
                    }
                    add = space;
                }
            }
        }
        #endregion
        
        private static void OnUpdateOxygenLevel(ref UpdateOxygenLevelMsg msg, MyNetworkClient sender)
        {
            if (!MyEntities.EntityExists(msg.OwnerEntityId)) return;

            IMyInventoryOwner owner = MyEntities.GetEntityById(msg.OwnerEntityId) as IMyInventoryOwner;
            MyInventory inv = null;
            if (owner != null)
            {
                inv = owner.GetInventory(msg.InventoryIndex);
            }
            else
            {
                // NOTE: this should be the default code after we get rid of the inventory owner and should be searched by it's id
                MyEntity entity = MyEntities.GetEntityById(msg.OwnerEntityId);
                MyInventoryBase baseInventory;
                if (entity.Components.TryGet<MyInventoryBase>(out baseInventory))
                {
                    inv = baseInventory as MyInventory;
                }
            }
            
            var item = inv.GetItemByID(msg.ItemId);
            if (!item.HasValue) return;

            var oxygenContainer = item.Value.Content as MyObjectBuilder_OxygenContainerObject;
            if (oxygenContainer != null)
            {
                oxygenContainer.OxygenLevel = msg.OxygenLevel;
                inv.UpdateOxygenAmount();
            }
        }

        public void UpdateOxygenLevel(MyInventory inv, float level, uint itemId)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new UpdateOxygenLevelMsg();
            msg.OwnerEntityId = inv.Owner.EntityId;
            msg.InventoryIndex = inv.InventoryIdx;
            msg.ItemId = itemId;
            msg.OxygenLevel = level;

            Sync.Layer.SendMessageToAll(ref msg);
        }

		#region Consume inventory item

		private static void OnConsumeItem(ref ConsumeItemMsg msg, MyNetworkClient sender)
		{
			if (!MyEntities.EntityExists(msg.OwnerEntityId) || (msg.ConsumerEntityId != 0 && !MyEntities.EntityExists(msg.ConsumerEntityId))) return;

			var entity = MyEntities.GetEntityById(msg.OwnerEntityId);
			if (entity == null)
				return;

			var inventoryOwner = entity as IMyInventoryOwner;
			if (inventoryOwner == null)
				return;

			var inventory = inventoryOwner.GetInventory(msg.InventoryId);
			if (inventory == null)
				return;

			if (Sync.IsServer)
			{
				var existingAmount = inventory.GetItemAmount(msg.ItemId);
				if (existingAmount < msg.Amount)
					msg.Amount = existingAmount;
			}

			if (msg.ConsumerEntityId != 0)
			{
				entity = MyEntities.GetEntityById(msg.ConsumerEntityId);
				if (entity == null)
					return;
			}

			if(entity.Components != null)
			{
				var statComp = entity.Components.Get<MyEntityStatComponent>() as MyCharacterStatComponent;
				if(statComp != null)
				{
					var definition = MyDefinitionManager.Static.GetDefinition(msg.ItemId) as MyConsumableItemDefinition;
					statComp.Consume(msg.Amount, definition);
					var character = entity as MyCharacter;
					if (character != null)
						character.StartSecondarySound(definition.EatingSound, true);
				}
			}

			if (Sync.IsServer)
			{
				inventory.RemoveItemsOfType(msg.Amount, msg.ItemId);
				Sync.Layer.SendMessageToAll(ref msg);
			}
		}

		public static void ConsumeItem(MyInventory inventory, IMyInventoryItem item, MyFixedPoint amount, long consumerEntityId = 0)
		{
			if(inventory == null || inventory.Owner == null)
				return;

			var msg = new ConsumeItemMsg()
			{
				OwnerEntityId = inventory.Owner.EntityId,
				InventoryId = inventory.InventoryIdx,
				ItemId = item.GetDefinitionId(),
				Amount = amount,
				ConsumerEntityId = consumerEntityId,
			};

			Sync.Layer.SendMessageToServer(ref msg);
		}
		#endregion


        #region MyInventoryBase Sync

        internal static void SendTransferItemsMessage(MyInventoryBase sourceInventory, MyInventoryBase destinationInventory, IMyInventoryItem item, MyFixedPoint amount)
        {
            if (sourceInventory == null || destinationInventory == null || item == null)
            {
                Debug.Fail("Invalid parameters!");
                return;
            }
            Debug.Assert(amount > 0, "Amount is <= 0! Sending meaningless message! Don't call this if transferring amount is 0");
            var msg = new TransferItemsBaseMsg();
            msg.Amount = amount;
            msg.SourceContainerId = sourceInventory.Container.Entity.EntityId;
            msg.DestinationContainerId = destinationInventory.Container.Entity.EntityId;
            msg.SourceItemId = item.ItemId;
            msg.SourceInventoryId = MyStringHash.GetOrCompute(sourceInventory.InventoryId.ToString());
            msg.DestinationInventoryId = MyStringHash.GetOrCompute(destinationInventory.InventoryId.ToString());
            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnTransferItemsBaseMsg(ref TransferItemsBaseMsg msg, MyNetworkClient sender)
        {
            MyEntity sourceContainer = MyEntities.GetEntityById(msg.SourceContainerId);
            MyEntity destinationContainer = MyEntities.GetEntityById(msg.DestinationContainerId);
            if (sourceContainer == null || destinationContainer == null)
            {
                Debug.Fail("Containers/Entities weren't found!");
                return;
            }

            // CH: TODO: This breaks the object design, but so far we wouldn't be able to move items between other inventories than MyInventory anyway
            MyInventory sourceInventory = sourceContainer.GetInventory(msg.SourceInventoryId) as MyInventory;
            MyInventoryBase destinationInventory = destinationContainer.GetInventory(msg.DestinationInventoryId);
            if (sourceInventory == null || destinationInventory == null)
            {
                Debug.Fail("Inventories weren't found!");
                return;
            }

            var items = sourceInventory.GetItems();
            foreach (var item in items)
            {
                if (item.ItemId == msg.SourceItemId)
                {
                    MyInventoryBase.TransferItems(sourceInventory, destinationInventory, item, msg.Amount);
                    return;
                }
            }            
        }

        #endregion
    }
}
