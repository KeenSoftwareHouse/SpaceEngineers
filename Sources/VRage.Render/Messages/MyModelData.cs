using System.Collections.Generic;
using VRageMath;

namespace VRageRender.Messages
{
    public struct MyRuntimeSectionInfo
    {
        public int IndexStart;
        public int TriCount;
        public string MaterialName;
    }

    public class MyModelData
    {
        public List<MyRuntimeSectionInfo> Sections = new List<MyRuntimeSectionInfo>();

        public BoundingBox AABB;

        public List<int> Indices = new List<int>();
        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector3> Tangents = new List<Vector3>();
        public List<Vector2> TexCoords = new List<Vector2>();

        public void Clear()
        {
            AABB = BoundingBox.CreateInvalid();
            Indices.Clear();
            Positions.Clear();
            Normals.Clear();
            Tangents.Clear();
            TexCoords.Clear();
            Sections.Clear();
        }
    }
}
