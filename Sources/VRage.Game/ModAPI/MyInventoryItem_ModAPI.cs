using System.Diagnostics;
using VRage.ModAPI;
using VRage.ModAPI.Ingame;

namespace VRage.Game.Entity
{
    public partial struct MyPhysicalInventoryItem : IMyInventoryItem
    {
        VRage.MyFixedPoint IMyInventoryItem.Amount
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

        float IMyInventoryItem.Scale
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

        VRage.ObjectBuilders.MyObjectBuilder_Base IMyInventoryItem.Content
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

        uint IMyInventoryItem.ItemId
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
