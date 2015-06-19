using System.Collections.Generic;
using VRage.Voxels;
using VRage.Native;
using VRageMath;
using VRageRender;
using System.Diagnostics;

namespace Sandbox.Engine.Voxels
{
    public struct MyIsoMesherArgs
    {
        public IMyStorage Storage;
        public MyCellCoord GeometryCell;
    }

    public sealed class MyIsoMesh : IMyIsoMesherOutputBuffer
    {
        // mk:TODO Make indices 1 int to prevent copying during HkGeometry creation if possible. Unfortunately, Havok winding order is different than ours. Maybe swap winding order in-place?
        // mk:TODO Also, it might be possible to pass in material id to HkGeometry to determine what is character walking on. Reading materials is very slow if they are procedural though.

        public readonly List<Vector3> Positions = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<byte> Materials = new List<byte>();
        public readonly List<Vector3I> Cells = new List<Vector3I>();

        public readonly List<MyVoxelTriangle> Triangles = new List<MyVoxelTriangle>();

        public int VerticesCount
        {
            get { return Positions.Count; }
        }

        public int TrianglesCount
        {
            get { return Triangles.Count; }
        }

        public Vector3 PositionScale;
        public Vector3D PositionOffset;

        void IMyIsoMesherOutputBuffer.Reserve(int vertexCount, int triangleCount)
        {
            if (Positions.Capacity < vertexCount)
            {
                Positions.Capacity = vertexCount;
                Normals.Capacity = vertexCount;
                Materials.Capacity = vertexCount;
                Cells.Capacity = vertexCount;
            }

            if (Triangles.Capacity < triangleCount)
                Triangles.Capacity = triangleCount;
        }

        void IMyIsoMesherOutputBuffer.WriteTriangle(int v0, int v1, int v2)
        {
            Triangles.Add(new MyVoxelTriangle()
            {
                VertexIndex0 = (short)v0,
                VertexIndex1 = (short)v1,
                VertexIndex2 = (short)v2,
            });
        }

        void IMyIsoMesherOutputBuffer.WriteVertex(ref Vector3I cell, ref Vector3 position, ref Vector3 normal, byte material)
        {
            Debug.Assert(position.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
            Positions.Add(position);
            Normals.Add(normal);
            Materials.Add(material);
            Cells.Add(cell);
        }

        public void Clear()
        {
            Positions.Clear();
            Normals.Clear();
            Materials.Clear();
            Cells.Clear();
            Triangles.Clear();
        }

        internal void GetUnpackedPosition(int idx, out Vector3 position)
        {
            position = Positions[idx] * PositionScale + PositionOffset;
        }

        internal void GetUnpackedVertex(int idx, out MyVoxelVertex vertex)
        {
            vertex.Position = Positions[idx] * PositionScale + PositionOffset;
            vertex.Normal = Normals[idx];
            vertex.Material = Materials[idx];
            vertex.Ambient = 0f;

            vertex.PositionMorph = vertex.Position;
            vertex.NormalMorph = vertex.Normal;
            vertex.MaterialMorph = vertex.Material;
        }

        public static bool IsEmpty(MyIsoMesh self)
        {
            return self == null || self.Triangles.Count == 0;
        }
    }

    public interface IMyIsoMesher
    {
        int InvalidatedRangeInflate { get; }

        MyIsoMesh Precalc(MyIsoMesherArgs args);
        MyIsoMesh Precalc(IMyStorage storage, int lod, Vector3I lodVoxelMin, Vector3I lodVoxelMax, bool generateMaterials);
    }
}
