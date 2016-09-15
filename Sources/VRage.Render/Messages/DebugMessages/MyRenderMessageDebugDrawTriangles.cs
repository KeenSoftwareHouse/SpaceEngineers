using System.Collections.Generic;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawTriangles : MyDebugRenderMessage, IDrawTrianglesMessage
    {
        public Color Color;
        public MatrixD WorldMatrix;
        public bool DepthRead;
        public bool Shaded;

        public List<int> Indices = new List<int>();
        public List<Vector3D> Vertices = new List<Vector3D>();

        public int VertexCount
        {
            get { return Vertices.Count; }
        }

        public void AddIndex(int index)
        {
            Indices.Add(index);
        }

        public void AddVertex(Vector3D position)
        {
            Vertices.Add(position);
        }

        public void AddTriangle(ref Vector3D v0, ref Vector3D v1, ref Vector3D v2)
        {
            int count = Vertices.Count;
            Indices.Add(count);
            Indices.Add(count + 1);
            Indices.Add(count + 2);
            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);
        }

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawTriangles; } }
    }
}
