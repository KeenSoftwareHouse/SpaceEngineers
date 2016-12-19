using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using MyGridIntersection = Sandbox.Engine.Utils.MyGridIntersection;
using VRage.Game.Components;
using Sandbox.Engine.Utils;
using VRage.Profiler;
using VRage.Voxels;

namespace Sandbox.Engine.Voxels
{
    public partial class MyVoxelGeometry
    {
        private static List<Vector3I> m_sweepResultCache = new List<Vector3I>();
        private static List<int> m_overlapElementCache = new List<int>();

        private MyStorageBase m_storage;

        private Vector3I m_cellsCount;
        private readonly Dictionary<UInt64, CellData> m_cellsByCoordinate = new Dictionary<UInt64, CellData>();
        private readonly Dictionary<UInt64, MyIsoMesh> m_coordinateToMesh = new Dictionary<UInt64, MyIsoMesh>();
        private readonly FastResourceLock m_lock = new FastResourceLock();

        /// <summary>
        /// Lookup saying whether cell has no geometry (bit 1) or either has
        /// geometry or was invalidated (bit 0).
        /// </summary>
        private readonly LRUCache<ulong, ulong> m_isEmptyCache = new LRUCache<ulong, ulong>(128);

        public Vector3I CellsCount
        {
            get { return m_cellsCount; }
        }

        public MyVoxelGeometry()
        {
            MyPrecalcComponent.AssertUpdateThread();
        }

        public void Init(MyStorageBase storage)
        {
            MyPrecalcComponent.AssertUpdateThread();

            m_storage = storage;
            m_storage.RangeChanged += storage_RangeChanged;
            var size = m_storage.Size;
            m_cellsCount.X = size.X >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            m_cellsCount.Y = size.Y >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            m_cellsCount.Z = size.Z >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
        }

        public bool Intersects(ref BoundingSphereD localSphere)
        {
            MyPrecalcComponent.AssertUpdateThread();

            //  Get min and max cell coordinate where boundingBox can fit
            BoundingBoxD sphereBoundingBox = BoundingBoxD.CreateInvalid();
            sphereBoundingBox.Include(ref localSphere);
            Vector3I cellCoordMin, cellCoordMax;
            {
                Vector3D minD = sphereBoundingBox.Min;
                Vector3D maxD = sphereBoundingBox.Max;
                MyVoxelCoordSystems.LocalPositionToGeometryCellCoord(ref minD, out cellCoordMin);
                MyVoxelCoordSystems.LocalPositionToGeometryCellCoord(ref maxD, out cellCoordMax);
            }

            //  Fix min and max cell coordinates so they don't overlap the voxelmap
            ClampCellCoord(ref cellCoordMin);
            ClampCellCoord(ref cellCoordMax);

            MyCellCoord cell = new MyCellCoord();
            for (cell.CoordInLod.X = cellCoordMin.X; cell.CoordInLod.X <= cellCoordMax.X; cell.CoordInLod.X++)
            {
                for (cell.CoordInLod.Y = cellCoordMin.Y; cell.CoordInLod.Y <= cellCoordMax.Y; cell.CoordInLod.Y++)
                {
                    for (cell.CoordInLod.Z = cellCoordMin.Z; cell.CoordInLod.Z <= cellCoordMax.Z; cell.CoordInLod.Z++)
                    {
                        //  If no overlap between bounding box of data cell and the sphere
                        BoundingBox cellBoundingBox;
                        MyVoxelCoordSystems.GeometryCellCoordToLocalAABB(ref cell.CoordInLod, out cellBoundingBox);
                        if (cellBoundingBox.Intersects(ref localSphere) == false)
                            continue;

                        //  Get cell from cache. If not there, precalc it and store in the cache.
                        //  If null is returned, we know that cell doesn't contain any triangleVertexes so we don't need to do intersections.
                        CellData cachedData = GetCell(ref cell);

                        if (cachedData == null) continue;

                        for (int i = 0; i < cachedData.VoxelTrianglesCount; i++)
                        {
                            MyVoxelTriangle voxelTriangle = cachedData.VoxelTriangles[i];

                            MyTriangle_Vertices triangle;
                            cachedData.GetUnpackedPosition(voxelTriangle.VertexIndex0, out triangle.Vertex0);
                            cachedData.GetUnpackedPosition(voxelTriangle.VertexIndex1, out triangle.Vertex1);
                            cachedData.GetUnpackedPosition(voxelTriangle.VertexIndex2, out triangle.Vertex2);

                            BoundingBox localTriangleAABB = BoundingBox.CreateInvalid();
                            localTriangleAABB.Include(ref triangle.Vertex0);
                            localTriangleAABB.Include(ref triangle.Vertex1);
                            localTriangleAABB.Include(ref triangle.Vertex2);

                            //  First test intersection of triangle's bounding box with line's bounding box. And only if they overlap or intersect, do further intersection tests.
                            if (localTriangleAABB.Intersects(ref localSphere))
                            {
                                PlaneD trianglePlane = new PlaneD(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2);

                                if (MyUtils.GetSphereTriangleIntersection(ref localSphere, ref trianglePlane, ref triangle) != null)
                                {
                                    //  If intersection found - we are finished. We don't need to look for more.
                                    return true;
                                }
                            }
                        }

                    }
                }
            }

            return false;
        }

        public bool Intersect(ref Line localLine, out VRage.Game.Models.MyIntersectionResultLineTriangle result, IntersectionFlags flags)
        {
            MyPrecalcComponent.AssertUpdateThread();

            ProfilerShort.Begin("VoxelMap.LineIntersection AABB sweep");
            m_sweepResultCache.Clear();
            MyGridIntersection.Calculate(
                m_sweepResultCache,
                (int)MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES,
                localLine.From,
                localLine.To,
                new Vector3I(0, 0, 0),
                m_cellsCount - 1
            );
            ProfilerShort.End();

            ProfilerShort.Begin("VoxelMap.LineIntersection test AABBs");
            float? minDistanceUntilNow = null;
            BoundingBox cellBoundingBox;
            MyCellCoord cell = new MyCellCoord();
            VRage.Game.Models.MyIntersectionResultLineTriangle? tmpResult = null;
            for (int index = 0; index < m_sweepResultCache.Count; index++)
            {
                cell.CoordInLod = m_sweepResultCache[index];

                MyVoxelCoordSystems.GeometryCellCoordToLocalAABB(ref cell.CoordInLod, out cellBoundingBox);

                float? distanceToBoundingBox = MyUtils.GetLineBoundingBoxIntersection(ref localLine, ref cellBoundingBox);

                // Sweep results are sorted; when we get far enough, make an early exit
                const float earlyOutDistance = 1.948557f * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES;  // = sqrt(3) * 9/8 * cell_side
                if (minDistanceUntilNow != null && distanceToBoundingBox != null && minDistanceUntilNow + earlyOutDistance < distanceToBoundingBox.Value)
                {
                    break;
                }

                //  Get cell from cache. If not there, precalc it and store in the cache.
                //  If null is returned, we know that cell doesn't contain any triangleVertexes so we don't need to do intersections.
                CellData cachedDataCell = GetCell(ref cell);

                if (cachedDataCell == null || cachedDataCell.VoxelTrianglesCount == 0) continue;

                GetCellLineIntersectionOctree(ref tmpResult, ref localLine, ref minDistanceUntilNow, cachedDataCell, flags);
            }

            ProfilerShort.End();

            result = tmpResult ?? default(VRage.Game.Models.MyIntersectionResultLineTriangle);
            return tmpResult.HasValue;
        }

        private bool TryGetCell(MyCellCoord cell, out bool isEmpty, out CellData nonEmptyCell)
        {
            MyPrecalcComponent.AssertUpdateThread();
            using (m_lock.AcquireSharedUsing())
            {
                if (IsEmpty(ref cell))
                {
                    isEmpty = true;
                    nonEmptyCell = null;
                    return true;
                }

                UInt64 key = cell.PackId64();
                if (m_cellsByCoordinate.TryGetValue(key, out nonEmptyCell))
                {
                    isEmpty = false;
                    return true;
                }

                isEmpty = default(bool);
                nonEmptyCell = default(CellData);
                return false;
            }
        }

        public bool TryGetMesh(MyCellCoord cell, out bool isEmpty, out MyIsoMesh nonEmptyMesh)
        {
            using (m_lock.AcquireSharedUsing())
            {
                if (IsEmpty(ref cell))
                {
                    isEmpty = true;
                    nonEmptyMesh = null;
                    return true;
                }

                UInt64 key = cell.PackId64();
                if (m_coordinateToMesh.TryGetValue(key, out nonEmptyMesh))
                {
                    isEmpty = false;
                    return true;
                }

                isEmpty = default(bool);
                nonEmptyMesh = default(MyIsoMesh);
                return false;
            }
        }

        public void SetMesh(MyCellCoord cell, MyIsoMesh mesh)
        {
            // Don't store anything but the most detailed lod (used in physics and raycasts).
            // This cache is mostly supposed to help physics and raycasts, not render.
            if (cell.Lod != 0)
                return;

            MyPrecalcComponent.AssertUpdateThread();
            using (m_lock.AcquireExclusiveUsing())
            {
                if (mesh != null)
                {
                    var key = cell.PackId64();
                    m_coordinateToMesh[key] = mesh;
                }
                else
                {
                    SetEmpty(ref cell, true);
                }
            }
        }

        /// <param name="minVoxelChanged">Inclusive min.</param>
        /// <param name="maxVoxelChanged">Inclusive max.</param>
        private void storage_RangeChanged(Vector3I minChanged, Vector3I maxChanged, MyStorageDataTypeFlags changedData)
        {
            MyPrecalcComponent.AssertUpdateThread();

            ProfilerShort.Begin("MyVoxelGeometry.storage_RangeChanged");

            minChanged -= MyPrecalcComponent.InvalidatedRangeInflate;
            maxChanged += MyPrecalcComponent.InvalidatedRangeInflate;
            m_storage.ClampVoxelCoord(ref minChanged);
            m_storage.ClampVoxelCoord(ref maxChanged);
            var minCellChanged = minChanged >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            var maxCellChanged = maxChanged >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            using (m_lock.AcquireExclusiveUsing())
            {
                if (minCellChanged == Vector3I.Zero && maxCellChanged == m_cellsCount - 1)
                {
                    m_cellsByCoordinate.Clear();
                    m_coordinateToMesh.Clear();
                    m_isEmptyCache.Reset();
                }
                else
                {
                    MyCellCoord cell = new MyCellCoord();
                    cell.CoordInLod = minCellChanged;
                    for (var it = new Vector3I_RangeIterator(ref minCellChanged, ref maxCellChanged); it.IsValid(); it.GetNext(out cell.CoordInLod))
                    {
                        var key = cell.PackId64();
                        m_cellsByCoordinate.Remove(key);
                        m_coordinateToMesh.Remove(key);
                        SetEmpty(ref cell, false);
                    }
                }
            }

            ProfilerShort.End();
        }

        private void GetCellLineIntersectionOctree(ref VRage.Game.Models.MyIntersectionResultLineTriangle? result, ref Line modelSpaceLine, ref float? minDistanceUntilNow, CellData cachedDataCell, IntersectionFlags flags)
        {
            m_overlapElementCache.Clear();
            if (cachedDataCell.Octree != null)
            {
                Vector3 packedStart, packedEnd;
                cachedDataCell.GetPackedPosition(ref modelSpaceLine.From, out packedStart);
                cachedDataCell.GetPackedPosition(ref modelSpaceLine.To, out packedEnd);
                var ray = new Ray(packedStart, packedEnd - packedStart);
                cachedDataCell.Octree.GetIntersectionWithLine(ref ray, m_overlapElementCache);
            }

            for (int j = 0; j < m_overlapElementCache.Count; j++)
            {
                var i = m_overlapElementCache[j];

                if (cachedDataCell.VoxelTriangles == null) //probably not calculated yet
                    continue;

                // this should never happen
                if (i >= cachedDataCell.VoxelTriangles.Length)
                {
                    Debug.Assert(i < cachedDataCell.VoxelTriangles.Length);
                    continue;
                }

                MyVoxelTriangle voxelTriangle = cachedDataCell.VoxelTriangles[i];

                MyTriangle_Vertices triangleVertices;
                cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex0, out triangleVertices.Vertex0);
                cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex1, out triangleVertices.Vertex1);
                cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex2, out triangleVertices.Vertex2);

                Vector3 calculatedTriangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangleVertices);
                if (!calculatedTriangleNormal.IsValid())
                    continue;
                //We dont want backside intersections
                if (((int)(flags & IntersectionFlags.FLIPPED_TRIANGLES) == 0) &&
                    Vector3.Dot(modelSpaceLine.Direction, calculatedTriangleNormal) > 0)
                    continue;

                // AABB intersection test removed, AABB is tested inside BVH
                float? distance = MyUtils.GetLineTriangleIntersection(ref modelSpaceLine, ref triangleVertices);

                //  If intersection occured and if distance to intersection is closer to origin than any previous intersection
                if ((distance != null) && ((result == null) || (distance.Value < result.Value.Distance)))
                {
                    minDistanceUntilNow = distance.Value;
                    result = new VRage.Game.Models.MyIntersectionResultLineTriangle(i, ref triangleVertices, ref calculatedTriangleNormal, distance.Value);
                }
            }
        }

        private void ClampCellCoord(ref Vector3I cellCoord)
        {
            var dataCellsCountMinusOne = m_cellsCount - 1;
            Vector3I.Clamp(ref cellCoord, ref Vector3I.Zero, ref dataCellsCountMinusOne, out cellCoord);
        }

        internal CellData GetCell(ref MyCellCoord cell)
        {
            MyPrecalcComponent.AssertUpdateThread();

            bool isEmpty;
            CellData data;
            if (TryGetCell(cell, out isEmpty, out data))
            {
                return data;
            }

            MyIsoMesh mesh;
            if (!TryGetMesh(cell, out isEmpty, out mesh))
            {
                ProfilerShort.Begin("Cell precalc");
                if (true)
                {
                    var min = cell.CoordInLod << MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
                    var max = min + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
                    // overlap to neighbor; introduces extra data but it makes logic for raycasts simpler (no need to check neighbor cells)
                    min -= 1;
                    max += 2;
                    mesh = MyPrecalcComponent.IsoMesher.Precalc(m_storage, 0, min, max, false, MyFakes.ENABLE_VOXEL_COMPUTED_OCCLUSION, true);
                }
                else
                {
                    mesh = MyPrecalcComponent.IsoMesher.Precalc(new MyIsoMesherArgs()
                    {
                        Storage = m_storage,
                        GeometryCell = cell,
                    });
                }
                ProfilerShort.End();
            }

            if (mesh != null)
            {
                data = new CellData();
                data.Init(
                    mesh.PositionOffset, mesh.PositionScale,
                    mesh.Positions.GetInternalArray(), mesh.VerticesCount,
                    mesh.Triangles.GetInternalArray(), mesh.TrianglesCount);
            }

            if (cell.Lod == 0)
            {
                using (m_lock.AcquireExclusiveUsing())
                {
                    if (data != null)
                    {
                        var key = cell.PackId64();
                        m_cellsByCoordinate[key] = data;
                    }
                    else
                    {
                        SetEmpty(ref cell, true);
                    }
                }
            }

            return data;
        }

        private void ComputeIsEmptyLookup(MyCellCoord cell, out ulong outCacheKey, out int outBit)
        {
            var offset = cell.CoordInLod % 4;
            cell.CoordInLod >>= 2;
            Debug.Assert(offset.IsInsideInclusive(Vector3I.Zero, new Vector3I(3)));
            outCacheKey = cell.PackId64();
            outBit = offset.X + 4 * (offset.Y + 4 * offset.Z);
            Debug.Assert((uint)outBit < 64u);
        }

        private bool IsEmpty(ref MyCellCoord cell)
        {
            ulong cacheKey;
            int bitIdx;
            ComputeIsEmptyLookup(cell, out cacheKey, out bitIdx);
            var cacheLine = m_isEmptyCache.Read(cacheKey);
            return (cacheLine & ((ulong)1 << bitIdx)) != 0;
        }

        private void SetEmpty(ref MyCellCoord cell, bool value)
        {
            UInt64 cacheKey;
            int bitIdx;
            ComputeIsEmptyLookup(cell, out cacheKey, out bitIdx);
            var cacheLine = m_isEmptyCache.Read(cacheKey);
            if (value)
                cacheLine |= ((ulong)1) << bitIdx;
            else
                cacheLine &= ~(((ulong)1) << bitIdx);
            m_isEmptyCache.Write(cacheKey, cacheLine);

            Debug.Assert(IsEmpty(ref cell) == value);
        }
    }
}