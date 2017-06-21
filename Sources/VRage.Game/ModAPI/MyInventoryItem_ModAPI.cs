using System.Diagnostics;
using VRage.ModAPI;
using VRage.Game.ModAPI;

namespace VRage.Game.Entity
{
    public partial struct MyPhysicalInventoryItem : IMyInventoryItem
    {
        VRage.MyFixedPoint ModAPI.Ingame.IMyInventoryItem.Amount
        {
            get
            {
                return Amount;
            }
            set
            {
                Amount = value;
            }
        }

        float ModAPI.Ingame.IMyInventoryItem.Scale
        {
            get
            {
                return Scale;
            }
            set
            {
                Scale = value;
            }
        }

        VRage.ObjectBuilders.MyObjectBuilder_Base ModAPI.Ingame.IMyInventoryItem.Content
        {
            get
            {
                return Content;
            }
            set
            {
                Debug.Assert(value is MyObjectBuilder_PhysicalObject, "Content is not physical object!");
                Content = value as MyObjectBuilder_PhysicalObject;
            }
        }

        uint ModAPI.Ingame.IMyInventoryItem.ItemId
        {
            get
            {
                return ItemId;
            }
            set
            {
                ItemId = value;
            }
        }
    }
}
