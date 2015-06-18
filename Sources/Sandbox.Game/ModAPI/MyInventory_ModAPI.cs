using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Game
{
    partial class MyInventory : Sandbox.ModAPI.Interfaces.IMyInventory, Sandbox.ModAPI.IMyInventory
    {
        bool Sandbox.ModAPI.Interfaces.IMyInventory.IsFull
        {
            get { return IsFull;}
        }

        VRageMath.Vector3 Sandbox.ModAPI.Interfaces.IMyInventory.Size
        {
            get { return Size; }
        }

        bool Sandbox.ModAPI.IMyInventory.Empty()
        {
            return Empty();
        }

        void Sandbox.ModAPI.IMyInventory.Clear(bool sync)
        {
            Clear(sync);
        }

        bool Sandbox.ModAPI.Interfaces.IMyInventory.IsItemAt(int position)
        {
            return IsItemAt(position);
        }

        bool Sandbox.ModAPI.Interfaces.IMyInventory.CanItemsBeAdded(VRage.MyFixedPoint amount, SerializableDefinitionId contentId)
        {
            return CanItemsBeAdded(amount, contentId);
        }

        bool Sandbox.ModAPI.Interfaces.IMyInventory.ContainItems(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject ob)
        {
            return ContainItems(amount, ob);
        }

        void Sandbox.ModAPI.IMyInventory.AddItems(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index)
        {
            AddItems(amount, objectBuilder, index);
        }

        VRage.MyFixedPoint Sandbox.ModAPI.Interfaces.IMyInventory.GetItemAmount(SerializableDefinitionId contentId, MyItemFlags flags)
        {
            return GetItemAmount(contentId,flags);
        }

        void Sandbox.ModAPI.IMyInventory.RemoveItemsOfType(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, bool spawn)
        {
            RemoveItemsOfType(amount, objectBuilder, spawn);
        }

        void Sandbox.ModAPI.IMyInventory.RemoveItemsOfType(VRage.MyFixedPoint amount, SerializableDefinitionId contentId, MyItemFlags flags, bool spawn)
        {
            RemoveItemsOfType(amount, contentId, flags, spawn);
        }

        void Sandbox.ModAPI.IMyInventory.RemoveItemsAt(int itemIndex, VRage.MyFixedPoint? amount, bool sendEvent, bool spawn)
        {
            RemoveItemsAt(itemIndex, amount, sendEvent, spawn);
        }

        void Sandbox.ModAPI.IMyInventory.RemoveItems(uint itemId, VRage.MyFixedPoint? amount, bool sendEvent, bool spawn)
        {
            RemoveItems(itemId, amount, sendEvent, spawn);
        }

        bool Sandbox.ModAPI.Interfaces.IMyInventory.TransferItemTo(Sandbox.ModAPI.Interfaces.IMyInventory dst, int sourceItemIndex, int? targetItemIndex, bool? stackIfPossible, VRage.MyFixedPoint? amount)
        {
            return TransferItemsTo(dst, sourceItemIndex, targetItemIndex, amount,true);
        }

        private bool TransferItemsTo(Sandbox.ModAPI.Interfaces.IMyInventory dst, int sourceItemIndex, int? targetItemIndex, VRage.MyFixedPoint? amount,bool useConveyor)
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
                MyGridConveyorSystem.Pathfinding.FindReachable(srcConveyor.ConveyorEndpoint, reachableVertices, (vertex) => vertex.CubeBlock != null && vertex.CubeBlock is IMyInventoryOwner);
                foreach (var vertex in reachableVertices)
                {
                    if (dstInventory.Owner == vertex.CubeBlock as IMyInventoryOwner)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool Sandbox.ModAPI.Interfaces.IMyInventory.TransferItemFrom(Sandbox.ModAPI.Interfaces.IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex, bool? stackIfPossible, VRage.MyFixedPoint? amount)
        {
            return TransferItemsFrom(sourceInventory, sourceItemIndex, targetItemIndex, stackIfPossible, amount,true);
        }

        private bool TransferItemsFrom(Sandbox.ModAPI.Interfaces.IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex, bool? stackIfPossible, VRage.MyFixedPoint? amount,bool useConveyors)
        {
            MyInventory srcInventory = sourceInventory as MyInventory;
            if (srcInventory != null && (useConveyors == false || IsConnected(srcInventory) == true))
            {
                TransferItemFrom(srcInventory, sourceItemIndex, targetItemIndex, stackIfPossible, amount);
                return true;
            }
            return false;
        }

        VRage.MyFixedPoint Sandbox.ModAPI.Interfaces.IMyInventory.CurrentMass
        {
            get { return CurrentMass; }
        }

        VRage.MyFixedPoint Sandbox.ModAPI.Interfaces.IMyInventory.MaxVolume
        {
            get { return MaxVolume; }
        }

        VRage.MyFixedPoint Sandbox.ModAPI.Interfaces.IMyInventory.CurrentVolume
        {
            get { return CurrentVolume; }
        }


        ModAPI.Interfaces.IMyInventoryOwner Sandbox.ModAPI.Interfaces.IMyInventory.Owner
        {
            get { return Owner; }
        }

        List<Sandbox.ModAPI.Interfaces.IMyInventoryItem> Sandbox.ModAPI.Interfaces.IMyInventory.GetItems()
        {
            return m_items.OfType<Sandbox.ModAPI.Interfaces.IMyInventoryItem>().ToList(); ;
        }

        Sandbox.ModAPI.Interfaces.IMyInventoryItem Sandbox.ModAPI.Interfaces.IMyInventory.GetItemByID(uint id)
        {
            MyPhysicalInventoryItem? item = GetItemByID(id);
            if (item != null)
            {
                return item.Value;
            }
            return null;
        }

        Sandbox.ModAPI.Interfaces.IMyInventoryItem Sandbox.ModAPI.Interfaces.IMyInventory.FindItem(SerializableDefinitionId contentId)
        {
            MyPhysicalInventoryItem? item = FindItem(contentId);

            if (item != null)
            {
                return item.Value;
            }
            return null;
        }

        bool Sandbox.ModAPI.IMyInventory.TransferItemTo(Sandbox.ModAPI.Interfaces.IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection=false)
        {
           return TransferItemsTo(dst, sourceItemIndex, targetItemIndex, amount, checkConnection);
        }
        bool Sandbox.ModAPI.IMyInventory.TransferItemFrom(Sandbox.ModAPI.Interfaces.IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection=false)
        {
           return TransferItemsFrom(sourceInventory, sourceItemIndex, targetItemIndex, stackIfPossible, amount, checkConnection);
        }

        bool Sandbox.ModAPI.Interfaces.IMyInventory.IsConnectedTo(Sandbox.ModAPI.Interfaces.IMyInventory dst)
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
