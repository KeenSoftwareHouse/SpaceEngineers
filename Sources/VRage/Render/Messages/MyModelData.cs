using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage
{
    public struct MySectionInfo
    {
        public int IndexStart;
        public int TriCount;
        public string MaterialName;
    }

    public class MyModelData
    {
        public List<MySectionInfo> Sections = new List<MySectionInfo>();

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
