using System.Diagnostics;
using VRage.ObjectBuilders;

namespace VRage.Game.Entity
{
    public partial struct MyPhysicalInventoryItem
    {
        public MyFixedPoint Amount;
        public float Scale;

        [DynamicObjectBuilder]
        public MyObjectBuilder_PhysicalObject Content;

        public uint ItemId;

        public MyPhysicalInventoryItem(MyFixedPoint amount, MyObjectBuilder_PhysicalObject content, float scale = 1)
        {
            Debug.Assert(amount > 0, "Creating inventory item with zero amount!");
            ItemId = 0;
            Amount = amount;
            Scale = scale;
            Content = content;
        }

        public MyPhysicalInventoryItem(MyObjectBuilder_InventoryItem item)
        {
            Debug.Assert(item.Amount > 0, "Creating inventory item with zero amount!");
            ItemId = 0;
            Amount = item.Amount;
            Scale = item.Scale;
            Content = item.PhysicalContent.Clone() as MyObjectBuilder_PhysicalObject;
        }

        public MyObjectBuilder_InventoryItem GetObjectBuilder()
        {
            Debug.Assert(Amount > 0, "Getting object builder of inventory item with zero amount!");

            var itemObjectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
            itemObjectBuilder.Amount = Amount;
            itemObjectBuilder.Scale = Scale;
            itemObjectBuilder.PhysicalContent = Content;
            itemObjectBuilder.ItemId = ItemId;
            return itemObjectBuilder;
        }

        public override string ToString()
        {
            return string.Format("{0}x {1}", Amount, Content.GetId());
        }
    }
}
