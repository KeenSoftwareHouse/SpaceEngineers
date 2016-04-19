using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;

namespace Sandbox.Game
{
    partial class MyInventory : IMyInventory, VRage.Game.ModAPI.IMyInventory
    {
        bool IMyInventory.IsFull
        {
            get { return IsFull;}
        }

        VRageMath.Vector3 IMyInventory.Size
        {
            get { return VRageMath.Vector3.MaxValue; }
        }

        bool VRage.Game.ModAPI.IMyInventory.Empty()
        {
            return Empty();
        }

        void VRage.Game.ModAPI.IMyInventory.Clear(bool sync)
        {
            Clear(sync);
        }

        bool IMyInventory.IsItemAt(int position)
        {
            return IsItemAt(position);
        }

        bool IMyInventory.CanItemsBeAdded(VRage.MyFixedPoint amount, SerializableDefinitionId contentId)
        {
            return CanItemsBeAdded(amount, contentId);
        }

        bool IMyInventory.ContainItems(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject ob)
        {
            return ContainItems(amount, ob);
        }

        void VRage.Game.ModAPI.IMyInventory.AddItems(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index)
        {
            AddItems(amount, objectBuilder, null, index);
        }

        VRage.MyFixedPoint IMyInventory.GetItemAmount(SerializableDefinitionId contentId, MyItemFlags flags)
        {
            return GetItemAmount(contentId,flags);
        }

        void VRage.Game.ModAPI.IMyInventory.RemoveItemsOfType(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, bool spawn)
        {
            RemoveItemsOfType(amount, objectBuilder, spawn);
        }

        void VRage.Game.ModAPI.IMyInventory.RemoveItemsOfType(VRage.MyFixedPoint amount, SerializableDefinitionId contentId, MyItemFlags flags, bool spawn)
        {
            RemoveItemsOfType(amount, contentId, flags, spawn);
        }

        void VRage.Game.ModAPI.IMyInventory.RemoveItemsAt(int itemIndex, VRage.MyFixedPoint? amount, bool sendEvent, bool spawn)
        {
            RemoveItemsAt(itemIndex, amount, sendEvent, spawn);
        }

        void VRage.Game.ModAPI.IMyInventory.RemoveItems(uint itemId, VRage.MyFixedPoint? amount, bool sendEvent, bool spawn)
        {
            RemoveItems(itemId, amount, sendEvent, spawn);
        }

        bool IMyInventory.TransferItemTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex, bool? stackIfPossible, VRage.MyFixedPoint? amount)
        {
            return TransferItemsTo(dst, sourceItemIndex, targetItemIndex, amount,true);
        }

        private bool TransferItemsTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex, VRage.MyFixedPoint? amount,bool useConveyor)
        {
            MyInventory dstInventory = dst as MyInventory;
            if (dstInventory != null)
            {
                if (sourceItemIndex < 0 || sourceItemIndex >= this.m_items.Count || (useConveyor == true && IsConnected(dstInventory) == false))
                {
                    return false;
                }
                Transfer(this as MyInventory, dstInventory, this.GetItems()[sourceItemIndex].ItemId, targetItemIndex.HasValue ? targetItemIndex.Value : -1, amount);
                return true;
            }
            return false;
        }

        List<IMyConveyorEndpoint> reachableVertices = new List<IMyConveyorEndpoint>();

        private bool IsConnected(MyInventory dstInventory)
        {
            var srcConveyor = (this.Owner as IMyConveyorEndpointBlock);
            if (srcConveyor != null)
            {
                reachableVertices.Clear();
                MyGridConveyorSystem.FindReachable(srcConveyor.ConveyorEndpoint, reachableVertices, (vertex) => vertex.CubeBlock != null);
                foreach (var vertex in reachableVertices)
                {
                    if (dstInventory.Owner == vertex.CubeBlock)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool IMyInventory.TransferItemFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex, bool? stackIfPossible, VRage.MyFixedPoint? amount)
        {
            return TransferItemsFrom(sourceInventory, sourceItemIndex, targetItemIndex, stackIfPossible, amount,true);
        }

        private bool TransferItemsFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex, bool? stackIfPossible, VRage.MyFixedPoint? amount,bool useConveyors)
        {
            MyInventory srcInventory = sourceInventory as MyInventory;
            if (srcInventory != null && (useConveyors == false || IsConnected(srcInventory) == true))
            {
                TransferItemFrom(srcInventory, sourceItemIndex, targetItemIndex, stackIfPossible, amount);
                return true;
            }
            return false;
        }

        VRage.MyFixedPoint IMyInventory.CurrentMass
        {
            get { return CurrentMass; }
        }

        VRage.MyFixedPoint IMyInventory.MaxVolume
        {
            get { return MaxVolume; }
        }

        VRage.MyFixedPoint IMyInventory.CurrentVolume
        {
            get { return CurrentVolume; }
        }

        IMyInventoryOwner IMyInventory.Owner
        {
            get { return Owner as IMyInventoryOwner; }
        }

        List<IMyInventoryItem> IMyInventory.GetItems()
        {
            return m_items.OfType<IMyInventoryItem>().ToList(); ;
        }

        IMyInventoryItem IMyInventory.GetItemByID(uint id)
        {
            MyPhysicalInventoryItem? item = GetItemByID(id);
            if (item != null)
            {
                return item.Value;
            }
            return null;
        }

        IMyInventoryItem IMyInventory.FindItem(SerializableDefinitionId contentId)
        {
            MyPhysicalInventoryItem? item = FindItem(contentId);

            if (item != null)
            {
                return item.Value;
            }
            return null;
        }

        bool VRage.Game.ModAPI.IMyInventory.TransferItemTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection = false)
        {
           return TransferItemsTo(dst, sourceItemIndex, targetItemIndex, amount, checkConnection);
        }
        bool VRage.Game.ModAPI.IMyInventory.TransferItemFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection = false)
        {
           return TransferItemsFrom(sourceInventory, sourceItemIndex, targetItemIndex, stackIfPossible, amount, checkConnection);
        }

        bool IMyInventory.IsConnectedTo(IMyInventory dst)
        {
            MyInventory dstInventory = dst as MyInventory;
            if (dstInventory != null)
            {
                return IsConnected(dstInventory);            
            }
            return false;
        }
    }
}
