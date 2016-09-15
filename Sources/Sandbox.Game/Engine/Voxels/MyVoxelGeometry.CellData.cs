using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VRage;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    partial class MyVoxelGeometry
    {
        //  One cell of goemetry
        public class CellData
        {
            public int VoxelTrianglesCount;
            public int VoxelVerticesCount;
            public MyVoxelTriangle[] VoxelTriangles;

            // Vertex position normalization and scale+offset for cell might have to be added if we need massive scale of terrain.
            private Vector3 m_positionOffset, m_positionScale;
            private Vector3[] m_positions;
            private MyOctree m_octree;

            internal MyOctree Octree
            {
                get
                {
                    if (m_octree == null && VoxelTrianglesCount > 0)
                    {
                        m_octree = new MyOctree();
                        m_octree.Init(m_positions, VoxelVerticesCount, VoxelTriangles, VoxelTrianglesCount, out VoxelTriangles);
                    }
                    return m_octree;
                }
            }

            public void Init(
                Vector3 positionOffset, Vector3 positionScale,
                Vector3[] positions, int vertexCount,
                MyVoxelTriangle[] triangles, int triangleCount)
            {
                if (vertexCount == 0)
                {
                    VoxelVerticesCount = 0;
                    VoxelTrianglesCount = 0;
                    m_octree = null;
                    m_positions = null;
                    return;
                }
                Debug.Assert(vertexCount <= Int16.MaxValue);

                // copy voxel vertices
                m_positionOffset = positionOffset;
                m_positionScale = positionScale;
                m_positions = new Vector3[vertexCount];
                Array.Copy(positions, m_positions, vertexCount);

                ProfilerShort.Begin("build octree");
                if (m_octree == null)
                    m_octree = new MyOctree();
                m_octree.Init(m_positions, vertexCount, triangles, triangleCount, out VoxelTriangles);
                ProfilerShort.End();

                // set size only after the arrays are fully allocated
                VoxelVerticesCount = vertexCount;
                VoxelTrianglesCount = triangleCount;
            }

            public void GetUnpackedPosition(int index, out Vector3 unpacked)
            {
                unpacked = m_positions[index] * m_positionScale + m_positionOffset;
            }

            public void GetPackedPosition(ref Vector3 unpacked, out Vector3 packed)
            {
                packed = (unpacked - m_positionOffset) / m_positionScale;
            }
        }
    }
}
