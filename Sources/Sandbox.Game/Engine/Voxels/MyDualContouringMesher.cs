using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using System.Collections.Generic;
using VRage;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using VRage.Native;
using Sandbox.Game;
using System.Diagnostics;

namespace Sandbox.Engine.Voxels
{
    class MyDualContouringMesher : IMyIsoMesher
    {
        const int SIZE_IN_VOXELS = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
        const float POSITION_SCALE = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS + 1f;
        /// <summary>
        /// Constant that ensures contoured voxel positions are in range 0 to 1 inclusive.
        /// Such positions can be encoded as normalized unsigned integer values.
        /// </summary>
        const float CONTOURED_VOXEL_SIZE = 1f / POSITION_SCALE;

        private MyStorageDataCache m_cache = new MyStorageDataCache();
        private MyIsoMesh m_buffer = new MyIsoMesh();

        const int AFFECTED_RANGE_OFFSET = -1;
        const int AFFECTED_RANGE_SIZE_CHANGE = 5;

        public int AffectedRangeOffset
        {
            get { return AFFECTED_RANGE_OFFSET; }
        }

        public int AffectedRangeSizeChange
        {
            get { return AFFECTED_RANGE_SIZE_CHANGE; }
        }

        public int InvalidatedRangeInflate
        {
            get { return AFFECTED_RANGE_SIZE_CHANGE + AFFECTED_RANGE_OFFSET; }
        }

        public MyIsoMesh Precalc(MyIsoMesherArgs args)
        {
            var voxelStart = args.GeometryCell.CoordInLod * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
            var voxelEnd = voxelStart + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS - 1
                + 1 // overlap to neighbor so geometry is stitched together within same LOD
                + 1; // for eg. 9 vertices in row we need 9 + 1 samples (voxels)

            return Precalc(args.Storage, args.GeometryCell.Lod, voxelStart, voxelEnd, true);
        }

        public MyIsoMesh Precalc(IMyStorage storage, int lod, Vector3I voxelStart, Vector3I voxelEnd, bool generateMaterials)
        {
            // change range so normal can be computed at edges (expand by 1 in all directions)
            voxelStart -= 1;
            voxelEnd += 1;
            m_cache.Resize(voxelStart, voxelEnd);
            storage.ReadRange(m_cache, MyStorageDataTypeFlags.Content, lod, ref voxelStart, ref voxelEnd);
            if (!m_cache.ContainsIsoSurface())
            {
                return null;
            }

            if (generateMaterials)
            {
                storage.ReadRange(m_cache, MyStorageDataTypeFlags.Material, lod, ref voxelStart, ref voxelEnd);
            }
            else
            {
                m_cache.ClearMaterials(0);
            }
            var voxelSize = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lod);

            ProfilerShort.Begin("Dual Contouring");
            unsafe
            {
                fixed (byte* voxels = m_cache.Data)
                {
                    var size3d = m_cache.Size3D;
                    Debug.Assert(size3d.X == size3d.Y && size3d.Y == size3d.Z);
                    IsoMesher.Calculate(size3d.X, (VoxelData*)voxels, m_buffer);
                }
            }
            ProfilerShort.End();

            if (m_buffer.VerticesCount == 0 && m_buffer.Triangles.Count == 0)
            {
                return null;
            }

            ProfilerShort.Begin("Geometry post-processing");
            {
                var vertexCellOffset = voxelStart - AffectedRangeOffset;
                var positions = m_buffer.Positions.GetInternalArray();
                var vertexCells = m_buffer.Cells.GetInternalArray();
                for (int i = 0; i < m_buffer.VerticesCount; i++)
                {
                    Debug.Assert(positions[i].IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                    vertexCells[i] += vertexCellOffset;
                }

                double numCellsHalf = 0.5 * (m_cache.Size3D.X - 3);
                m_buffer.PositionOffset = ((Vector3D)vertexCellOffset + numCellsHalf) * (double)voxelSize;
                m_buffer.PositionScale = new Vector3((float)(numCellsHalf * voxelSize));
            }
            ProfilerShort.End();

            // Replace filled mesh with new one.
            // This way prevents allocation of meshes which then end up empty.
            var buffer = m_buffer;
            m_buffer = new MyIsoMesh();
            return buffer;
        }

    }
}