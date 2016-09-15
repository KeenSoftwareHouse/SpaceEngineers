using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public interface IDrawTrianglesMessage
    {
        int VertexCount { get; }

        void AddIndex(int index);
        void AddVertex(Vector3D position);
        void AddTriangle(ref Vector3D v0, ref Vector3D v1, ref Vector3D v2);
    }

    public static class DrawTrianglesMessageExtensions
    {
        public static void AddTriangle(this IDrawTrianglesMessage msg, Vector3D v0, Vector3D v1, Vector3D v2)
        {
            msg.AddTriangle(ref v0, ref v1, ref v2);
        }
    }
}
