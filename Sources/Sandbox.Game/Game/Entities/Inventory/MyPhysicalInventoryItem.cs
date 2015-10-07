using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System.Diagnostics;
using VRage;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game
{
    public partial struct MyPhysicalInventoryItem
    {
        public MyFixedPoint Amount;

        [DynamicObjectBuilder]
        public MyObjectBuilder_PhysicalObject Content;

        public uint ItemId;

        public MyPhysicalInventoryItem(MyFixedPoint amount, MyObjectBuilder_PhysicalObject content)
        {
            Debug.Assert(amount > 0, "Creating inventory item with zero amount!");
            ItemId = 0;
            Amount = amount;
            Content = content;
        }

        public MyPhysicalInventoryItem(MyObjectBuilder_InventoryItem item)
        {
            Debug.Assert(item.Amount > 0, "Creating inventory item with zero amount!");
            ItemId = 0;
            Amount = item.Amount;
            Content = item.PhysicalContent;
        }

        public MyObjectBuilder_InventoryItem GetObjectBuilder()
        {
            Debug.Assert(Amount > 0, "Getting object builder of inventory item with zero amount!");

            var itemObjectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
            itemObjectBuilder.Amount = Amount;
            itemObjectBuilder.PhysicalContent = Content;
            itemObjectBuilder.ItemId = ItemId;
            return itemObjectBuilder;
        }

        public override string ToString()
        {
            return string.Format("{0}x {1}", Amount, Content.GetId());
        }

        public MyEntity Spawn(MyFixedPoint amount, BoundingBoxD box, MyEntity owner = null)
        {
            MatrixD spawnMatrix = MatrixD.Identity;
            spawnMatrix.Translation = box.Center;
            var entity = Spawn(amount, spawnMatrix, owner);
            var size = entity.PositionComp.LocalVolume.Radius;
            var halfSize = box.Size / 2 - new Vector3(size);
            halfSize = Vector3.Max(halfSize, Vector3.Zero);
            box = new BoundingBoxD(box.Center - halfSize, box.Center + halfSize);
            var pos = MyUtils.GetRandomPosition(ref box);

            Vector3 forward = MyUtils.GetRandomVector3Normalized();
            Vector3 up = MyUtils.GetRandomVector3Normalized();
            while (forward == up)
                up = MyUtils.GetRandomVector3Normalized();

            Vector3 right = Vector3.Cross(forward, up);
            up = Vector3.Cross(right, forward);
            entity.WorldMatrix = MatrixD.CreateWorld(pos, forward, up);
            return entity;
        }

        public MyEntity Spawn(MyFixedPoint amount, MatrixD worldMatrix, MyEntity owner = null)
        {
            if (Content is MyObjectBuilder_BlockItem)
            {
                Debug.Assert(MyFixedPoint.IsIntegral(amount), "Spawning fractional number of grids!");

                var blockItem = Content as MyObjectBuilder_BlockItem;
                var builder = MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_CubeGrid)) as MyObjectBuilder_CubeGrid;
                builder.GridSizeEnum = MyCubeSize.Small;
                builder.IsStatic = false;
                builder.PersistentFlags |= MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled;
                builder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix);

                var block = MyObjectBuilderSerializer.CreateNewObject(blockItem.BlockDefId) as MyObjectBuilder_CubeBlock;
                builder.CubeBlocks.Add(block);

                MyCubeGrid firstGrid = null;
                for (int i = 0; i < amount; ++i)
                {
                    builder.EntityId = MyEntityIdentifier.AllocateId();
                    block.EntityId = MyEntityIdentifier.AllocateId();
                    MyCubeGrid newGrid = MyEntities.CreateFromObjectBuilder(builder) as MyCubeGrid;
                    firstGrid = firstGrid ?? newGrid;
                    MyEntities.Add(newGrid);
                    Sandbox.Game.Multiplayer.MySyncCreate.SendEntityCreated(builder);
                }
                return firstGrid;
            }
            else
            {
                MyPhysicalItemDefinition itemDefinition = null;
                MyDefinitionManager.Static.TryGetPhysicalItemDefinition(Content.GetObjectId(), out itemDefinition);
                if (itemDefinition != null)
                {
                    return MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(amount, Content), worldMatrix, owner != null ? owner.Physics : null);
                }
                return null;
            }
        }
    }
}
