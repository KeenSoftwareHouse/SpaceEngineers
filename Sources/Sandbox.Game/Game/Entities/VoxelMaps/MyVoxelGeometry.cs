using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.VoxelMaps.Voxels;
using Sandbox.Game.Voxels;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.VoxelMaps
{
    public partial class MyVoxelGeometry
    {
        private static List<Vector3I> m_sweepResultCache = new List<Vector3I>();
        private static List<int> m_overlapElementCache = new List<int>();

        private MyVoxelMap m_voxelMap;
        private Vector3I m_cellsCount;
        private Dictionary<Int64, CellData> m_cellsByCoordinate = new Dictionary<long, CellData>();

        internal Vector3I CellsCount
        {
            get { return m_cellsCount; }
        }

        internal MyVoxelGeometry() { }

        internal void Init(MyVoxelMap voxelMap)
        {
            m_voxelMap = voxelMap;
            var size = m_voxelMap.Size;
            m_cellsCount.X = size.X >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            m_cellsCount.Y = size.Y >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            m_cellsCount.Z = size.Z >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
        }

        internal void Clear()
        {
            foreach (var entry in m_cellsByCoordinate)
            {
                entry.Value.Reset();
                entry.Value.UnloadData();
                CellPool.Deallocate(entry.Value);
            }
            m_cellsByCoordinate.Clear();
        }

        internal bool Intersects(ref BoundingSphere sphere)
        {
            //  Get min and max cell coordinate where boundingBox can fit
            BoundingBox sphereBoundingBox = BoundingBox.CreateInvalid();
            sphereBoundingBox.Include(ref sphere);
            Vector3I cellCoordMin = GetCellCoordinateFromMeters(ref sphereBoundingBox.Min);
            Vector3I cellCoordMax = GetCellCoordinateFromMeters(ref sphereBoundingBox.Max);

            //  Fix min and max cell coordinates so they don't overlap the voxelmap
            FixDataCellCoord(ref cellCoordMin);
            FixDataCellCoord(ref cellCoordMax);

            Vector3I cellCoord;
            for (cellCoord.X = cellCoordMin.X; cellCoord.X <= cellCoordMax.X; cellCoord.X++)
            {
                for (cellCoord.Y = cellCoordMin.Y; cellCoord.Y <= cellCoordMax.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = cellCoordMin.Z; cellCoord.Z <= cellCoordMax.Z; cellCoord.Z++)
                    {
                        //  If no overlap between bounding box of data cell and the sphere
                        BoundingBox cellBoundingBox;
                        GetDataCellBoundingBox(ref cellCoord, out cellBoundingBox);
                        if (cellBoundingBox.Intersects(ref sphere) == false)
                            continue;

                        //  Get cell from cache. If not there, precalc it and store in the cache.
                        //  If null is returned, we know that cell doesn't contain any triangleVertexes so we don't need to do intersections.
                        CellData cachedDataCell = GetCell(MyLodTypeEnum.LOD0, ref cellCoord);

                        if (cachedDataCell == null) continue;

                        for (int i = 0; i < cachedDataCell.VoxelTrianglesCount; i++)
                        {
                            MyVoxelTriangle voxelTriangle = cachedDataCell.VoxelTriangles[i];

                            MyTriangle_Vertexes triangle;
                            cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex0, out triangle.Vertex0);
                            cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex1, out triangle.Vertex1);
                            cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex2, out triangle.Vertex2);
                            triangle.Vertex0 += m_voxelMap.PositionLeftBottomCorner;
                            triangle.Vertex1 += m_voxelMap.PositionLeftBottomCorner;
                            triangle.Vertex2 += m_voxelMap.PositionLeftBottomCorner;

                            BoundingBox voxelTriangleBoundingBox = BoundingBox.CreateInvalid();
                            voxelTriangleBoundingBox.Include(ref triangle.Vertex0);
                            voxelTriangleBoundingBox.Include(ref triangle.Vertex1);
                            voxelTriangleBoundingBox.Include(ref triangle.Vertex2);

                            //  First test intersection of triangle's bounding box with line's bounding box. And only if they overlap or intersect, do further intersection tests.
                            if (voxelTriangleBoundingBox.Intersects(ref sphere))
                            {
                                MyPlane trianglePlane = new MyPlane(ref triangle);

                                if (MyUtils.GetSphereTriangleIntersection(ref sphere, ref trianglePlane, ref triangle) != null)
                                {
                                    //  If intersection found - we are finished. We don't need to look for more.
                                    Profiler.End();
                                    return true;
                                }
                            }
                        }

                    }
                }
            }

            return false;
        }

        internal bool Intersect(ref Line worldLine, out MyIntersectionResultLineTriangleEx? result, IntersectionFlags flags)
        {
            Line localLine = new Line(worldLine.From - m_voxelMap.PositionLeftBottomCorner,
                                      worldLine.To - m_voxelMap.PositionLeftBottomCorner, true);

            Profiler.Begin("VoxelMap.LineIntersection AABB sweep");
            m_sweepResultCache.Clear();
            MyGridIntersection.Calculate(
                m_sweepResultCache,
                (int)MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES,
                localLine.From,
                localLine.To,
                new Vector3I(0, 0, 0),
                m_cellsCount - 1
            );
            Profiler.End();

            Profiler.Begin("VoxelMap.LineIntersection test AABBs");
            float? minDistanceUntilNow = null;
            BoundingBox cellBoundingBox;
            Vector3I cellCoord;
            MyIntersectionResultLineTriangle? tmpResult = null;
            for (int index = 0; index < m_sweepResultCache.Count; index++)
            {
                var coord = m_sweepResultCache[index];
                cellCoord = coord;

                GetDataCellBoundingBox(ref cellCoord, out cellBoundingBox);

                float? distanceToBoundingBox = MyUtils.GetLineBoundingBoxIntersection(ref worldLine, ref cellBoundingBox);

                // Sweep results are sorted; when we get far enough, make an early exit
                const float earlyOutDistance = 1.948557f * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES;  // = sqrt(3) * 9/8 * cell_side
                if (minDistanceUntilNow != null && distanceToBoundingBox != null && minDistanceUntilNow + earlyOutDistance < distanceToBoundingBox.Value)
                {
                    break;
                }

                //  Get cell from cache. If not there, precalc it and store in the cache.
                //  If null is returned, we know that cell doesn't contain any triangleVertexes so we don't need to do intersections.
                CellData cachedDataCell = GetCell(MyLodTypeEnum.LOD0, ref cellCoord);

                if (cachedDataCell == null || cachedDataCell.VoxelTrianglesCount == 0) continue;

                GetCellLineIntersectionOctree(ref tmpResult, ref localLine, ref minDistanceUntilNow, cachedDataCell, flags);
            }

            Profiler.End();

            if (tmpResult.HasValue)
            {
                result = new MyIntersectionResultLineTriangleEx(tmpResult.Value, m_voxelMap, ref worldLine);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private void GetCellLineIntersectionOctree(ref MyIntersectionResultLineTriangle? result, ref Line modelSpaceLine, ref float? minDistanceUntilNow, CellData cachedDataCell, IntersectionFlags flags)
        {
            m_overlapElementCache.Clear();
            if (cachedDataCell.Octree != null)
            {
                Vector3 packedStart;
                cachedDataCell.GetPackedPosition(ref modelSpaceLine.From, out packedStart);
                var ray = new Ray(packedStart, modelSpaceLine.Direction);
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

                MyTriangle_Vertexes triangleVertices;
                cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex0, out triangleVertices.Vertex0);
                cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex1, out triangleVertices.Vertex1);
                cachedDataCell.GetUnpackedPosition(voxelTriangle.VertexIndex2, out triangleVertices.Vertex2);

                Vector3 calculatedTriangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangleVertices);

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
                    result = new MyIntersectionResultLineTriangle(ref triangleVertices, ref calculatedTriangleNormal, distance.Value);
                }
            }
        }

        private Vector3I GetCellCoordinateFromMeters(ref Vector3 worldPosition)
        {
            var voxelCoord = m_voxelMap.GetVoxelCoordinateFromMeters(worldPosition);
            voxelCoord >>= MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            return voxelCoord;
        }

        //  If data cell coord0 (in data cell units, not voxels or metres) is outside of the voxelmap, we fix its coordinate so
        //  it lie in the voxelmap.
        private void FixDataCellCoord(ref Vector3I cellCoord)
        {
            var dataCellsCountMinusOne = m_cellsCount - 1;
            Vector3I.Clamp(ref cellCoord, ref Vector3I.Zero, ref dataCellsCountMinusOne, out cellCoord);
        }

        //  Calculates bounding box of a specified data cell. Coordinates are in world/absolute space.
        private void GetDataCellBoundingBox(ref Vector3I cellCoord, out BoundingBox outBoundingBox)
        {
            Vector3 dataCellMin = GetDataCellPositionAbsolute(ref cellCoord);
            outBoundingBox = new BoundingBox(dataCellMin, dataCellMin + MyVoxelConstants.GEOMETRY_CELL_SIZE_VECTOR_IN_METRES);
        }

        private Vector3 GetDataCellPositionAbsolute(ref Vector3I cellCoord)
        {
            Vector3I voxelCoord = cellCoord << MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            return m_voxelMap.GetVoxelPositionAbsolute(ref voxelCoord);
        }

        internal CellData GetCell(MyLodTypeEnum lod, ref Vector3I cellCoord)
        {
            Int64 key = MySession.Static.VoxelMaps.GetCellHashCode(m_voxelMap.VoxelMapId, ref cellCoord, lod);

            CellData cachedCell;
            if (!m_cellsByCoordinate.TryGetValue(key, out cachedCell))
            {
                if (CellAffectsTriangles(m_voxelMap.Storage, lod, ref cellCoord))
                {
                    cachedCell = CellPool.AllocateOrCreate();
                    m_cellsByCoordinate.Add(key, cachedCell);

                    Profiler.Begin("Cell precalc");
                    MyVoxelPrecalc.PrecalcImmediatelly(
                        new MyVoxelPrecalcTaskItem(
                            lod,
                            m_voxelMap,
                            cachedCell,
                            new Vector3I(
                                cellCoord.X * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS,
                                cellCoord.Y * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS,
                                cellCoord.Z * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS)));
                    Profiler.End();
                }
            }

            return cachedCell;
        }

        internal CellData GetCellLater(MyLodTypeEnum lod, ref Vector3I cellCoord)
        {
            Int64 key = MySession.Static.VoxelMaps.GetCellHashCode(m_voxelMap.VoxelMapId, ref cellCoord, lod);
            CellData cachedCell;
            if (!m_cellsByCoordinate.TryGetValue(key, out cachedCell))
            {
                if (CellAffectsTriangles(m_voxelMap.Storage, lod, ref cellCoord))
                {
                    cachedCell = CellPool.AllocateOrCreate();
                    m_cellsByCoordinate.Add(key, cachedCell);

                    MyVoxelPrecalc.AddToQueue(MyLodTypeEnum.LOD0, m_voxelMap, cachedCell,
                        cellCoord.X * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS,
                        cellCoord.Y * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS,
                        cellCoord.Z * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS);
                }
            }

            return cachedCell;
        }

        /// <param name="minVoxelChanged">Inclusive min.</param>
        /// <param name="maxVoxelChanged">Inclusive max.</param>
        internal void InvalidateRange(Vector3I minChanged, Vector3I maxChanged)
        {
            minChanged -= MyVoxelPrecalc.InvalidatedRangeInflate;
            maxChanged += MyVoxelPrecalc.InvalidatedRangeInflate;
            m_voxelMap.FixVoxelCoord(ref minChanged);
            m_voxelMap.FixVoxelCoord(ref maxChanged);
            var minCellChanged = minChanged >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            var maxCellChanged = maxChanged >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            Vector3I cellCoord = minCellChanged;
            for (var it = new Vector3I.RangeIterator(ref minCellChanged, ref maxCellChanged); it.IsValid(); it.GetNext(out cellCoord))
            {
                RemoveCell(ref cellCoord);
            }
        }

        private void RemoveCell(ref Vector3I cellCoord)
        {
            Int64 key = MySession.Static.VoxelMaps.GetCellHashCode(m_voxelMap.VoxelMapId, ref cellCoord, MyLodTypeEnum.LOD0);
            CellData cell;
            m_cellsByCoordinate.TryGetValue(key, out cell);
            if (cell != null)
            {
                cell.Reset();
                cell.UnloadData();
                m_cellsByCoordinate.Remove(key);
                CellPool.Deallocate(cell);
            }
        }

        private static bool CellAffectsTriangles(IMyStorage storage, MyLodTypeEnum lod, ref Vector3I cellCoord)
        {
            Profiler.Begin("CellAffectsTriangles");
            try
            {
                //  Fix max cell coordinates so they don't fall from voxelmap
                var rangeMin = cellCoord << MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
                rangeMin += MyVoxelPrecalc.AffectedRangeOffset;
                var rangeMax = Vector3I.Min(rangeMin + (MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS + MyVoxelPrecalc.AffectedRangeSizeChange - 1), storage.Size - 1);
                var type = storage.GetRangeType(GetLodIndex(lod), ref rangeMin, ref rangeMax);
                return type == MyVoxelRangeType.MIXED;
            }
            finally
            {
                Profiler.End();
            }
        }

        public static int GetLodIndex(MyLodTypeEnum lod)
        {
            Debug.Assert(lod == MyLodTypeEnum.LOD0 || lod == MyLodTypeEnum.LOD1);
            return (lod == MyLodTypeEnum.LOD0) ? 0 : MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
        }

        internal static void ComputeCellCoord(ref Vector3I voxelCoord, out Vector3I cellCoord)
        {
            cellCoord = voxelCoord >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
        }

    }
}
