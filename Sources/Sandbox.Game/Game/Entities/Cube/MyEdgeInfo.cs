using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    // Old size: 128 B (120 + packing)
    // New size: 32 B (22 + packing)
    public class MyEdgeInfo
    {
        public Vector4 LocalOrthoMatrix;
        private Color m_packedColor;
        public MyStringHash EdgeModel;

        // There is only one of 26 directions
        public Base27Directions.Direction PackedNormal0;
        public Base27Directions.Direction PackedNormal1;

        // This may be variant index or color index
        public Color Color
        {
            get
            {
                var result = m_packedColor;
                result.A = 0;
                return result;
            }
            set
            {
                byte a = m_packedColor.A;
                m_packedColor = value;
                m_packedColor.A = a;
            }
        }

        public MyCubeEdgeType EdgeType
        {
            get
            {
                return (MyCubeEdgeType)m_packedColor.A;
            }
            set
            {
                m_packedColor.A = (byte)value;
            }
        }

        public MyEdgeInfo()
        {
        }

        public MyEdgeInfo(ref Vector3 pos, ref Vector3I edgeDirection, ref Vector3 normal0, ref Vector3 normal1, ref Color color, MyStringHash edgeModel)
        {
            var info = MyCubeGridDefinitions.EdgeOrientations[edgeDirection];
            Debug.Assert(info.EdgeType != MyCubeEdgeType.Hidden, "Hidden edge types are now allowed");

            PackedNormal0 = Base27Directions.GetDirection(normal0);
            PackedNormal1 = Base27Directions.GetDirection(normal1);
            m_packedColor = color;
            EdgeType = info.EdgeType;
            LocalOrthoMatrix = Vector4.PackOrthoMatrix(pos, info.Orientation.Forward, info.Orientation.Up);
            EdgeModel = edgeModel;
        }
    }
}
