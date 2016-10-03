using Sandbox.Engine.Utils;
using System.Collections.Generic;
using VRageMath;
using VRage.Native;
using System.Diagnostics;
using VRage.Profiler;
using VRage.Voxels;

namespace Sandbox.Engine.Voxels
{
    class MyDualContouringMesher : IMyIsoMesher
    {
        private MyStorageData m_cache = new MyStorageData();
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

            return Precalc(args.Storage, args.GeometryCell.Lod, voxelStart, voxelEnd, true, MyFakes.ENABLE_VOXEL_COMPUTED_OCCLUSION);
        }

        public MyIsoMesh Precalc(IMyStorage storage, int lod, Vector3I voxelStart, Vector3I voxelEnd, bool generateMaterials, bool useAmbient, bool doNotCheck = false, bool adviseCache = false)
        {
            // change range so normal can be computed at edges (expand by 1 in all directions)
            voxelStart -= 1;
            voxelEnd += 1;

            if (storage == null) return null;

            using (storage.Pin())
            {
                if (storage.Closed) return null;    

                MyVoxelRequestFlags request = MyVoxelRequestFlags.ContentChecked; // | (doNotCheck ? MyVoxelRequestFlags.DoNotCheck : 0);
                if (adviseCache)
                    request |= MyVoxelRequestFlags.AdviseCache;
                //if (lod == 0 && generateMaterials) request |= MyVoxelRequestFlags.AdviseCache;

                bool readAmbient = false;

                if (generateMaterials && storage.DataProvider != null && storage.DataProvider.ProvidesAmbient)
                    readAmbient = true;

                m_cache.Resize(voxelStart, voxelEnd);
                if (readAmbient) m_cache.StoreOcclusion = true;

                storage.ReadRange(m_cache, MyStorageDataTypeFlags.Content, lod, ref voxelStart, ref voxelEnd, ref request);

                if (request.HasFlags(MyVoxelRequestFlags.EmptyContent) || request.HasFlags(MyVoxelRequestFlags.FullContent)
                    || (!request.HasFlags(MyVoxelRequestFlags.ContentChecked) && !m_cache.ContainsIsoSurface()))
                {
                    //if(generateMaterials && lod == 0) Debugger.Break();
                    //storage.DebugDrawChunk(voxelStart, voxelEnd);
                    return null;
                }

                var center = (storage.Size / 2) * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                var voxelSize = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lod);
                var vertexCellOffset = voxelStart - AffectedRangeOffset;
                double numCellsHalf = 0.5 * (m_cache.Size3D.X - 3);
                var posOffset = ((Vector3D)vertexCellOffset + numCellsHalf) * (double)voxelSize;

                if (generateMaterials)
                {
                    // 255 is the new black
                    m_cache.ClearMaterials(255);
                }

                if (readAmbient)
                    m_cache.Clear(MyStorageDataTypeEnum.Occlusion, 0);

                IsoMesher mesher = new IsoMesher();
                ProfilerShort.Begin("Dual Contouring");
                unsafe
                {
                    fixed (byte* content = m_cache[MyStorageDataTypeEnum.Content])
                    fixed (byte* material = m_cache[MyStorageDataTypeEnum.Material])
                    {
                        var size3d = m_cache.Size3D;
                        Debug.Assert(size3d.X == size3d.Y && size3d.Y == size3d.Z);
                        mesher.Calculate(size3d.X, content, material, m_buffer, useAmbient, posOffset - center);
                    }
                }
                
                if (generateMaterials)
                {
                    request = 0;

                    request |= MyVoxelRequestFlags.SurfaceMaterial;
                    request |= MyVoxelRequestFlags.ConsiderContent;

                    var req = readAmbient ? MyStorageDataTypeFlags.Material | MyStorageDataTypeFlags.Occlusion : MyStorageDataTypeFlags.Material;

                    storage.ReadRange(m_cache, req, lod, ref voxelStart, ref voxelEnd, ref request);

                    FixCacheMaterial(voxelStart, voxelEnd);
                    unsafe
                    {
                        fixed (byte* content = m_cache[MyStorageDataTypeEnum.Content])
                        fixed (byte* material = m_cache[MyStorageDataTypeEnum.Material])
                        {
                            int materialOverride = request.HasFlags(MyVoxelRequestFlags.OneMaterial) ? m_cache.Material(0) : -1;
                            var size3d = m_cache.Size3D;
                            Debug.Assert(size3d.X == size3d.Y && size3d.Y == size3d.Z);

                            if (readAmbient)
                                fixed (byte* ambient = m_cache[MyStorageDataTypeEnum.Occlusion])
                                    mesher.CalculateMaterials(size3d.X, content, material, ambient, materialOverride);
                            else
                                mesher.CalculateMaterials(size3d.X, content, material, null, materialOverride);
                        }

                    }
                }
                else
                    m_cache.ClearMaterials(0);

                mesher.Finish(m_buffer);
                ProfilerShort.End();

                if (m_buffer.VerticesCount == 0 || m_buffer.Triangles.Count == 0)
                {
                    return null;
                }

                ProfilerShort.Begin("Geometry post-processing");
                {
                    var positions = m_buffer.Positions.GetInternalArray();
                    var vertexCells = m_buffer.Cells.GetInternalArray();
                    var materials = m_buffer.Materials.GetInternalArray();
                    var ambients = m_buffer.Ambient.GetInternalArray();
                    for (int i = 0; i < m_buffer.VerticesCount; i++)
                    {
                        Debug.Assert(positions[i].IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                        vertexCells[i] += vertexCellOffset;
                        Debug.Assert(vertexCells[i].IsInsideInclusive(voxelStart + 1, voxelEnd - 1));
                        Debug.Assert(materials[i] != MyVoxelConstants.NULL_MATERIAL);
                        Debug.Assert(ambients[i] >= 0 && ambients[i] <= 1);
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
        }

        private void ComputeAndAssignOcclusion()
        {

        }

        //[Conditional("DEBUG")]
        private void FixCacheMaterial(Vector3I voxelStart, Vector3I voxelEnd)
        {
            var mcount = Sandbox.Definitions.MyDefinitionManager.Static.VoxelMaterialCount;
            voxelEnd = Vector3I.Min(voxelEnd - voxelStart, m_cache.Size3D);
            voxelStart = Vector3I.Zero;
            var it = new Vector3I_RangeIterator(ref voxelStart, ref voxelEnd);
            var pos = it.Current;
            for (; it.IsValid(); it.GetNext(out pos))
            {
                var lin = m_cache.ComputeLinear(ref pos);
                var mat = m_cache.Material(lin);

                if (mat >= mcount && mat != MyVoxelConstants.NULL_MATERIAL)
                {
                    //Debug.Fail(String.Format("VoxelData contains invalid materials (id: {0}).", m_cache.Material(lin)));
                    m_cache.Material(lin, MyVoxelConstants.NULL_MATERIAL);
                }
            }
        }

    }
}