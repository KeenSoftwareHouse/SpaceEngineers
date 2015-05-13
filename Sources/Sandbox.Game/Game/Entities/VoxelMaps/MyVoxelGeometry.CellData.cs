using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.VoxelMaps.Voxels;
using Sandbox.Game.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRageMath;

namespace Sandbox.Game.Entities.VoxelMaps
{
    partial class MyVoxelGeometry
    {
        //  One cell of goemetry
        public class CellData
        {
            public static readonly Int64 INVALID_KEY = Int64.MaxValue;

            public Int64 Key;
            public int VoxelTrianglesCount;
            public int VoxelVerticesCount;
            public MyVoxelTriangle[] VoxelTriangles;
            private MyPackedVoxelVertex[] m_voxelVertices;
            public Vector3 PositionOffset;
            public float PositionScale;
            private MyOctree m_octree;

            private FastResourceLock m_syncRoot = new FastResourceLock();

            private HkBvCompressedMeshShape m_meshShape = (HkBvCompressedMeshShape)HkShape.Empty;

            public CellData()
            {
                Key = INVALID_KEY;
            }

            internal MyOctree Octree
            {
                get
                {
                    using (m_syncRoot.AcquireExclusiveUsing())
                    {
                        if (m_octree == null && VoxelTrianglesCount > 0)
                        {
                            m_octree = new MyOctree();
                            m_octree.Init(ref m_voxelVertices, ref VoxelVerticesCount, ref VoxelTriangles, ref VoxelTrianglesCount, out VoxelTriangles);
                        }
                        return m_octree;
                    }
                }
            }

            public void PrepareCache(
                MyVoxelVertex[] vertices, int vertexCount,
                MyVoxelTriangle[] triangles, int triangleCount,
                float positionScale, Vector3 positionOffset,
                bool createPhysicsShape)
            {
                using (m_syncRoot.AcquireExclusiveUsing())
                {
                    if (vertexCount == 0)
                    {
                        VoxelVerticesCount = 0;
                        VoxelTrianglesCount = 0;
                        m_octree = null;
                        m_voxelVertices = null;
                        PositionOffset = new Vector3(0f);
                        PositionScale = 0f;
                        return;
                    }
                    Debug.Assert(vertexCount <= Int16.MaxValue);

                    // copy voxel vertices
                    m_voxelVertices = new MyPackedVoxelVertex[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                        m_voxelVertices[i] = (MyPackedVoxelVertex)vertices[i];

                    Profiler.Begin("build octree");
                    if (m_octree == null)
                        m_octree = new MyOctree();
                    m_octree.Init(ref m_voxelVertices, ref vertexCount, ref triangles, ref triangleCount, out VoxelTriangles);
                    Profiler.End();

                    // set size only after the arrays are fully allocated
                    VoxelVerticesCount = vertexCount;
                    VoxelTrianglesCount = triangleCount;
                    PositionOffset = positionOffset;
                    PositionScale = positionScale;

                    if (createPhysicsShape)
                        CreateMeshShape();
                }
            }

            public HkBvCompressedMeshShape GetMeshShape()
            {
                CreateMeshShape();
                return m_meshShape;
            }

            public void SetShapeToCell(HkUniformGridShape shape, ref Vector3I cellCoord)
            {
                CreateMeshShape();
                shape.SetChild(cellCoord.X, cellCoord.Y, cellCoord.Z, m_meshShape);
            }

            private void CreateMeshShape()
            {
                Profiler.Begin("MyVoxelGeometry.CellData.CreateMeshShape");
                try
                {
                    if (!m_meshShape.Base.IsZero)
                        return;

                    List<int> indexList = new List<int>(VoxelTrianglesCount * 3);
                    List<Vector3> vertexList = new List<Vector3>(VoxelVerticesCount);

                    for (int i = 0; i < VoxelTrianglesCount; i++)
                    {
                        indexList.Add(VoxelTriangles[i].VertexIndex0);
                        indexList.Add(VoxelTriangles[i].VertexIndex2);
                        indexList.Add(VoxelTriangles[i].VertexIndex1);
                    }
                    Vector3 tmp;
                    for (int i = 0; i < VoxelVerticesCount; i++)
                    {
                        GetUnpackedPosition(ref m_voxelVertices[i], out tmp);
                        vertexList.Add(tmp);
                    }
                    using (var cellGeometry = new HkGeometry(vertexList, indexList))
                    {
                        var newShape = new HkBvCompressedMeshShape(cellGeometry, null, null, HkWeldingType.None);
                        Debug.Assert(newShape.Base.ReferenceCount == 1);
                        m_meshShape = newShape;
                    }
                }
                finally
                {
                    Profiler.End();
                }
            }

            //  This methods needs to be called after every cell cache released!! It releases vertex buffer. It's important, 
            //  because when this cell cache will be associated to a new cell, not same material vertex buffer will be used so unused needs to be disposed!!
            public void Reset()
            {
                using (m_syncRoot.AcquireExclusiveUsing())
                {
                    if (!m_meshShape.Base.IsZero)
                    {
                        MyPhysics.AssertThread();
                        Debug.Assert(m_meshShape.Base.ReferenceCount > 0);
                        m_meshShape.Base.RemoveReference();
                    }
                    m_meshShape = (HkBvCompressedMeshShape)HkShape.Empty;

                    VoxelTrianglesCount = 0;
                    VoxelTriangles = null;
                    m_octree = null;

                    VoxelVerticesCount = 0;
                    m_voxelVertices = null;
                    Key = INVALID_KEY;

                    m_octree = null;
                }
            }

            public void UnloadData()
            {
                if (!m_meshShape.Base.IsZero)
                {
                    MyPhysics.AssertThread();
                    Debug.Assert(m_meshShape.Base.ReferenceCount > 0);
                    m_meshShape.Base.RemoveReference();
                }
                m_meshShape = (HkBvCompressedMeshShape)HkShape.Empty;
            }

            public void GetUnpackedPosition(int voxelIndex, out Vector3 unpacked)
            {
                GetUnpackedPosition(ref m_voxelVertices[voxelIndex], out unpacked);
            }

            public void GetUnpackedPosition(ref MyPackedVoxelVertex packed, out Vector3 unpacked)
            {
                unpacked.X = packed.Position.X * MyPackedVoxelVertex.POSITION_UNPACK * PositionScale + PositionOffset.X;
                unpacked.Y = packed.Position.Y * MyPackedVoxelVertex.POSITION_UNPACK * PositionScale + PositionOffset.Y;
                unpacked.Z = packed.Position.Z * MyPackedVoxelVertex.POSITION_UNPACK * PositionScale + PositionOffset.Z;
            }

            public void GetPackedPosition(ref Vector3 unpacked, out Vector3 packed)
            {
                packed = (unpacked - PositionOffset) * (MyPackedVoxelVertex.POSITION_PACK / PositionScale);
            }

            public void GetUnpackedVertex(int index, out MyVoxelVertex vertex)
            {
                vertex = (MyVoxelVertex)m_voxelVertices[index];
                vertex.Position = vertex.Position * PositionScale + PositionOffset;
            }
        }

    }
}
