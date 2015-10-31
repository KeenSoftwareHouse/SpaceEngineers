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

            return Precalc(args.Storage, args.GeometryCell.Lod, voxelStart, voxelEnd, true,true);
        }

        public MyIsoMesh Precalc(IMyStorage storage, int lod, Vector3I voxelStart, Vector3I voxelEnd, bool generateMaterials, bool useAmbient)
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

            var center = (storage.Size / 2) * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            var voxelSize = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lod);
            var vertexCellOffset = voxelStart - AffectedRangeOffset;
            double numCellsHalf = 0.5 * (m_cache.Size3D.X - 3);
            var posOffset = ((Vector3D)vertexCellOffset + numCellsHalf) * (double)voxelSize;

            if (generateMaterials)
            {
                m_cache.ClearMaterials(0);
            }

            IsoMesher mesher = new IsoMesher();
            ProfilerShort.Begin("Dual Contouring");
            unsafe
            {
                fixed (byte* voxels = m_cache.Data)
                {
                    var size3d = m_cache.Size3D;
                    Debug.Assert(size3d.X == size3d.Y && size3d.Y == size3d.Z);
                    mesher.Calculate(size3d.X, (VoxelData*)voxels, m_buffer, useAmbient, posOffset - center);
                }
            }
            ProfilerShort.End();

            if (generateMaterials)
            {
                using (MyVoxelMaterialRequestHelper.StartContouring())
                {
                    storage.ReadRange(m_cache, MyStorageDataTypeFlags.Material, lod, ref voxelStart, ref voxelEnd);
                    bool hasOcclusionHint = false;
                    FixCacheMaterial(voxelStart, voxelEnd);
                    unsafe
                    {
                        fixed (byte* voxels = m_cache.Data)
                        {
                            var size3d = m_cache.Size3D;
                            Debug.Assert(size3d.X == size3d.Y && size3d.Y == size3d.Z);
                            mesher.CalculateMaterials(size3d.X, (VoxelData*)voxels, hasOcclusionHint, -1);
                        }
                    }
                }
            }
            else
                m_cache.ClearMaterials(0);

            mesher.Finish(m_buffer);

            if (m_buffer.VerticesCount == 0 && m_buffer.Triangles.Count == 0)
            {
                return null;
            }

            ProfilerShort.Begin("Geometry post-processing");
            {
                var positions = m_buffer.Positions.GetInternalArray();
                var vertexCells = m_buffer.Cells.GetInternalArray();
                for (int i = 0; i < m_buffer.VerticesCount; i++)
                {
                    Debug.Assert(positions[i].IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                    vertexCells[i] += vertexCellOffset;
                    Debug.Assert(vertexCells[i].IsInsideInclusive(voxelStart + 1, voxelEnd - 1));
                }

                m_buffer.PositionOffset = posOffset;
                m_buffer.PositionScale = new Vector3((float)(numCellsHalf * voxelSize));
                m_buffer.CellStart = voxelStart + 1;
                m_buffer.CellEnd = voxelEnd - 1;
            }
            ProfilerShort.End();

            // Replace filled mesh with new one.
            // This way prevents allocation of meshes which then end up empty.
            var buffer = m_buffer;
            m_buffer = new MyIsoMesh();
            return buffer;
        }

        private void FixCacheMaterial(Vector3I voxelStart, Vector3I voxelEnd)
        {
            var mcount = Sandbox.Definitions.MyDefinitionManager.Static.VoxelMaterialCount;
            voxelEnd = Vector3I.Min(voxelEnd - voxelStart, m_cache.Size3D);
            voxelStart = Vector3I.Zero;
            var it = new Vector3I.RangeIterator(ref voxelStart, ref voxelEnd);
            var pos = it.Current;
            for(;it.IsValid();it.GetNext(out pos))
            {
                var lin = m_cache.ComputeLinear(ref pos);
                if (m_cache.Material(lin) >= mcount)
                    m_cache.Material(lin, 0);
            }
        }

    }
}