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

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncInventory
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
            MyInventory inv = owner.GetInventory(msg.InventoryIndex);
            var item = inv.GetItemByID(msg.itemId);
            if (!item.HasValue) return;
            inv.RemoveItems(msg.itemId, msg.Amount, spawn: msg.Spawn);
        }

        static void OnRemoveItemsSuccess(ref RemoveItemsMsg msg, MyNetworkClient sender)
        {
            if (!MyEntities.EntityExists(msg.OwnerEntityId)) return;
            IMyInventoryOwner owner = MyEntities.GetEntityById(msg.OwnerEntityId) as IMyInventoryOwner;
            MyInventory inv = owner.GetInventory(msg.InventoryIndex);
            inv.RemoveItemsInternal(msg.itemId, msg.Amount);
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
            MyInventory inv = owner.GetInventory(msg.InventoryIndex);
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
    }
}
