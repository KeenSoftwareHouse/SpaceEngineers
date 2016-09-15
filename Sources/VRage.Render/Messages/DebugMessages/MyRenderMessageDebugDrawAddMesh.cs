using System.Collections.Generic;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawMesh : MyRenderMessageBase
    {
        public uint ID;
        public Color Color;
        public MatrixD WorldMatrix;
        public bool DepthRead;
        public bool Shaded;

        public struct FormatPositionColor
        {
            public Vector3 Position;
            public Color Color;
        }

        public List<FormatPositionColor> Vertices = new List<FormatPositionColor>();

        public int VertexCount
        {
            get { return Vertices.Count; }
        }


        public void AddTriangle(ref Vector3D v0, Color c0, ref Vector3D v1, Color c1, ref Vector3D v2, Color c2)
        {
            int count = Vertices.Count;
            Vertices.Add(new FormatPositionColor(){
                    Position = v0,
                    Color = c0
                });
            Vertices.Add(new FormatPositionColor(){
                    Position = v1,
                    Color = c1
                });
            Vertices.Add(new FormatPositionColor(){
                    Position = v2,
                    Color = c2
                });
        }

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawMesh; } }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
    }
}
