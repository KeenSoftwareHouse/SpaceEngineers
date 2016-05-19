using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Debris;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.GameSystems.Conveyors
{
    [MyEntityType(typeof(MyObjectBuilder_ConveyorPacket))]
    public class MyConveyorPacket : MyEntity
    {
        public MyPhysicalInventoryItem Item;
        public int LinePosition;

        // Used for position interpolation
        private float m_segmentLength;
        private Base6Directions.Direction m_segmentDirection;

        public void Init(MyObjectBuilder_ConveyorPacket builder, MyEntity parent)
        {
            Item = new MyPhysicalInventoryItem(builder.Item);
            LinePosition = builder.LinePosition;

            var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(Item.Content);

            var ore = Item.Content as MyObjectBuilder_Ore;

            string model = physicalItem.Model;
            float scale = 1.0f;
            if (ore != null)
            {
                foreach (var mat in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
                {
                    if (mat.MinedOre == ore.SubtypeName)
                    {
                        model = MyDebris.GetRandomDebrisVoxel();
                        scale = (float)Math.Pow((float)Item.Amount * physicalItem.Volume / MyDebris.VoxelDebrisModelVolume, 0.333f);
                        break;
                    }
                }
            }

            if (scale < 0.05f)
                scale = 0.05f;
            else if (scale > 1.0f)
                scale = 1.0f;

            bool entityIdAllocationSuspended = MyEntityIdentifier.AllocationSuspended;
            MyEntityIdentifier.AllocationSuspended = false;
            Init(null, model, parent, null, null);
            MyEntityIdentifier.AllocationSuspended = entityIdAllocationSuspended;
            PositionComp.Scale = scale;

            // Packets are serialized by conveyor lines
            Save = false;
        }

        public void SetSegmentLength(float length)
        {
            m_segmentLength = length;
        }

        public void SetLocalPosition(Vector3I sectionStart, int sectionStartPosition, float cubeSize, Base6Directions.Direction forward, Base6Directions.Direction offset)
        {
            int segmentPosition = LinePosition - sectionStartPosition;

            Matrix localMatrix = PositionComp.LocalMatrix;
            Vector3 offsetVector = PositionComp.LocalMatrix.GetDirectionVector(forward) * segmentPosition + PositionComp.LocalMatrix.GetDirectionVector(offset) * 0.10f;
            localMatrix.Translation = (sectionStart + offsetVector / PositionComp.Scale.Value) * cubeSize;
            PositionComp.LocalMatrix = localMatrix;

            m_segmentDirection = forward;
        }

        public void MoveRelative(float linePositionFraction)
        {
            base.PrepareForDraw();

            Matrix localMatrix = PositionComp.LocalMatrix;
            localMatrix.Translation += PositionComp.LocalMatrix.GetDirectionVector(m_segmentDirection) * m_segmentLength * linePositionFraction / PositionComp.Scale.Value;
            PositionComp.LocalMatrix = localMatrix;
        }
    }
}
