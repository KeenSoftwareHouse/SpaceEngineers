using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Algorithms;
using VRage.Collections;
using VRage.Utils;
using VRage.Trace;
using VRageMath;
using VRage.Generics;
using VRage.Profiler;
using VRage.Voxels;
using VRageRender.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyVoxelNavigationMesh : MyNavigationMesh
    {
        private static bool DO_CONSISTENCY_CHECKS = false;

        private MyVoxelBase m_voxelMap;
        private static MyVoxelBase m_staticVoxelMap;
        private Vector3 m_cellSize;

        // Cells that are fully processed and present in the mesh
        private MyVector3ISet m_processedCells;
        //private MyVector3ISet m_removedCells;
        private HashSet<ulong> m_cellsOnWayCoords;    // cells that are on some way 
        List<Vector3I> m_cellsOnWay;
        List<MyHighLevelPrimitive> m_primitivesOnPath;

        // Binary heap of cells to be added to the mesh
        private MyBinaryHeap<float, CellToAddHeapItem> m_toAdd;
        private List<CellToAddHeapItem> m_heapItemList;
        private class CellToAddHeapItem : HeapItem<float>
        {
            public Vector3I Position;
        }
        private MyVector3ISet m_markedForAddition;
        private static MyDynamicObjectPool<CellToAddHeapItem> m_heapItemAllocator = new MyDynamicObjectPool<CellToAddHeapItem>(128);

        private static MyVector3ISet m_tmpCellSet = new MyVector3ISet();
        private static List<MyCubeGrid> m_tmpGridList = new List<MyCubeGrid>();
        private static List<MyGridPathfinding.CubeId> m_tmpLinkCandidates = new List<MyGridPathfinding.CubeId>();
        private static Dictionary<MyGridPathfinding.CubeId, List<MyNavigationPrimitive>> m_tmpCubeLinkCandidates = new Dictionary<MyGridPathfinding.CubeId, List<MyNavigationPrimitive>>();
        private static MyDynamicObjectPool<List<MyNavigationPrimitive>> m_primitiveListPool = new MyDynamicObjectPool<List<MyNavigationPrimitive>>(8);

        private LinkedList<Vector3I> m_cellsToChange;
        private MyVector3ISet m_cellsToChangeSet;

        private static MyUnionFind m_vertexMapping = new MyUnionFind();
        private static List<int> m_tmpIntList = new List<int>();

        private MyVoxelConnectionHelper m_connectionHelper;
        private MyNavmeshCoordinator m_navmeshCoordinator;

        private MyHighLevelGroup m_higherLevel;
        private MyVoxelHighLevelHelper m_higherLevelHelper;
        private static HashSet<Vector3I> m_adjacentCells = new HashSet<Vector3I>();
        private static Dictionary<Vector3I, BoundingBoxD> m_adjacentBBoxes = new Dictionary<Vector3I, BoundingBoxD>();
        private static Vector3D m_halfMeterOffset = new Vector3D(MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF);
        private static BoundingBoxD m_cellBB = new BoundingBoxD();
        private static Vector3D m_bbMinOffset = new Vector3D(-0.125);
        private Vector3I m_maxCellCoord;                        // maximal cell coordinate that is valid on current map

        private const float ExploredRemovingDistance = 200;      // distance from some player in which are explored cell and higg level primitives removed as unused - it have to be higher than RemovingDistance
        private const float ProcessedRemovingDistance = 50;     // distance from some player in which are processed cells removed as unused
        private const float AddRemoveKoef = 0.5f;               // this koef have to be less than 1 - otherwise cells will be added and than again soon removed
        private const float MaxAddToProcessingDistance = ProcessedRemovingDistance * AddRemoveKoef;     // maximal distance from some player in which is some cell added to processing
        private float LimitAddingWeight = GetWeight(MaxAddToProcessingDistance);                        // weight in distance that is too far from players
        private const float CellsOnWayAdvance = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES;          // cells that are on a way of unit are computed as they are closer - they have higher priority of computing

        public static float PresentEntityWeight = 100;          // some big number - we wont that navigation under units is computed quickly
        public static float RecountCellWeight = 10;
        public static float JustAddedAdjacentCellWeight = 0.02f;
        public static float TooFarWeight = -100;                // don't count such cells

        #region Debug draw
        Vector3 m_debugPos1;
        Vector3 m_debugPos2;
        Vector3 m_debugPos3;
        Vector3 m_debugPos4;
        private struct DebugDrawEdge
        {
            public Vector3 V1;
            public Vector3 V2;

            public DebugDrawEdge(Vector3 v1, Vector3 v2)
            {
                V1 = v1;
                V2 = v2;
            }
        }
        Dictionary<ulong, List<DebugDrawEdge>> m_debugCellEdges;
        #endregion

        public const int NAVMESH_LOD = 0;

        // Dirty hack for the OBJ exporter
        public static MyVoxelBase VoxelMap
        { get { return m_staticVoxelMap; } }
  

        public Vector3D VoxelMapReferencePosition
        {
            get
            {
                return m_voxelMap.PositionLeftBottomCorner;
            }
        }

        public Vector3D VoxelMapWorldPosition
        {
            get
            {
                return m_voxelMap.PositionComp.GetPosition();
            }
        }

        public MyVoxelNavigationMesh(MyVoxelBase voxelMap, MyNavmeshCoordinator coordinator, Func<long> timestampFunction)
            : base(coordinator.Links, 16, timestampFunction)
        {
            m_voxelMap = voxelMap;
            m_staticVoxelMap = m_voxelMap;
            m_cellSize = m_voxelMap.SizeInMetres / m_voxelMap.Storage.Geometry.CellsCount * (1 << NAVMESH_LOD);

            m_processedCells = new MyVector3ISet();
            m_cellsOnWayCoords = new HashSet<ulong>();
            m_cellsOnWay = new List<Vector3I>();
            m_primitivesOnPath = new List<MyHighLevelPrimitive>(128);

            //m_removedCells = new MyVector3ISet();
            m_toAdd = new MyBinaryHeap<float, CellToAddHeapItem>(128);
            m_heapItemList = new List<CellToAddHeapItem>();
            m_markedForAddition = new MyVector3ISet();

            m_cellsToChange = new LinkedList<Vector3I>();
            m_cellsToChangeSet = new MyVector3ISet();

            m_connectionHelper = new MyVoxelConnectionHelper();
            m_navmeshCoordinator = coordinator;
            m_higherLevel = new MyHighLevelGroup(this, coordinator.HighLevelLinks, timestampFunction);
            m_higherLevelHelper = new MyVoxelHighLevelHelper(this);

            m_debugCellEdges = new Dictionary<ulong, List<DebugDrawEdge>>();

            voxelMap.Storage.RangeChanged += OnStorageChanged;

            m_maxCellCoord = m_voxelMap.Size / MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS - Vector3I.One;
        }

        public override string ToString()
        {
            return "Voxel NavMesh: " + m_voxelMap.StorageName;
        }

        private void OnStorageChanged(Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
        {
            if (!changedData.HasFlag(MyStorageDataTypeFlags.Content))
            {
                return;
            }

            InvalidateRange(minVoxelChanged, maxVoxelChanged);
        }

        public void InvalidateRange(Vector3I minVoxelChanged, Vector3I maxVoxelChanged)
        {
            minVoxelChanged -= MyPrecalcComponent.InvalidatedRangeInflate;
            maxVoxelChanged += MyPrecalcComponent.InvalidatedRangeInflate;

            m_voxelMap.Storage.ClampVoxelCoord(ref minVoxelChanged);
            m_voxelMap.Storage.ClampVoxelCoord(ref maxVoxelChanged);

            Vector3I minCell, maxCell;
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref minVoxelChanged, out minCell);
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref maxVoxelChanged, out maxCell);

            Vector3I currentCell = minCell;
            for (var it = new Vector3I_RangeIterator(ref minCell, ref maxCell); it.IsValid(); it.GetNext(out currentCell))
            {
                if (m_processedCells.Contains(ref(currentCell)))
                {
                    if (!m_cellsToChangeSet.Contains(ref currentCell))
                    {
                        m_cellsToChange.AddLast(currentCell);
                        m_cellsToChangeSet.Add(currentCell);
                    }
                }
                else
                {
                    MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, currentCell);
                    m_higherLevelHelper.TryClearCell(coord.PackId64());
                }
            }
        }

        public void MarkBoxForAddition(BoundingBoxD box)
        {
            ProfilerShort.Begin("VoxelNavMesh.MarkBoxForAddition");
            Vector3I pos, end;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(m_voxelMap.PositionLeftBottomCorner, ref box.Min, out pos);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(m_voxelMap.PositionLeftBottomCorner, ref box.Max, out end);

            m_voxelMap.Storage.ClampVoxelCoord(ref pos);
            m_voxelMap.Storage.ClampVoxelCoord(ref end);

            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref pos, out pos);
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref end, out end);

            Vector3 center = pos + end;
            center = center * 0.5f;

            pos /= 1 << NAVMESH_LOD;
            end /= 1 << NAVMESH_LOD;

            for (var it = new Vector3I_RangeIterator(ref pos, ref end); it.IsValid(); it.GetNext(out pos))
            {
                float rectDistance = Vector3.RectangularDistance(pos, center);
                if (rectDistance > 1)
                    continue;

                MarkCellForAddition(pos, PresentEntityWeight);
            }
            ProfilerShort.End();
        }

        private static float GetWeight(float rectDistance)
        {
            // returns number in interval (0,1)
            if (rectDistance < 0)
                return 1;
            return 1.0f / (1.0f + rectDistance); 
        }

        private bool IsCellPosValid(ref Vector3I cellPos)
        {
            if (cellPos.X > m_maxCellCoord.X || cellPos.Y > m_maxCellCoord.Y || cellPos.Z > m_maxCellCoord.Z )
                return false;
            MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, cellPos);
            return coord.IsCoord64Valid();
        }

        private void MarkCellForAddition(Vector3I cellPos, float weight)
        {
            if (m_processedCells.Contains(ref cellPos) || m_markedForAddition.Contains(ref cellPos))
                return;

            // check if is cellPos valid
            if (!IsCellPosValid(ref cellPos))
                return;

            if (!m_toAdd.Full)
            {
                MarkCellForAdditionInternal(ref cellPos, weight);
            }
            else
            {
                float min = m_toAdd.Min().HeapKey;
                if (weight > min)
                {
                    RemoveMinMarkedForAddition();
                    MarkCellForAdditionInternal(ref cellPos, weight);
                }
            }
        }

        private void MarkCellForAdditionInternal(ref Vector3I cellPos, float weight)
        {
            CellToAddHeapItem item = m_heapItemAllocator.Allocate();
            item.Position = cellPos;
            m_toAdd.Insert(item, weight);
            m_markedForAddition.Add(cellPos);
        }

        private void RemoveMinMarkedForAddition()
        {
            var heapItem = m_toAdd.RemoveMin();
            m_heapItemAllocator.Deallocate(heapItem);
            m_markedForAddition.Remove(heapItem.Position);
        }

        public bool RefreshOneChangedCell()
        {
            bool changed = false;
            while (!changed)
            {
                if (m_cellsToChange.Count == 0)
                {
                    return changed;
                }

                var firstCell = m_cellsToChange.First;
                Vector3I cell = firstCell.Value;
                m_cellsToChange.RemoveFirst();
                m_cellsToChangeSet.Remove(ref cell);

                if (m_processedCells.Contains(ref cell))
                {
                    RemoveCell(cell);
                    MarkCellForAddition(cell, RecountCellWeight);
                    changed = true;
                }
                else
                {
                    MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, cell);
                    m_higherLevelHelper.TryClearCell(coord.PackId64());
                }
            }

            CheckMeshConsistency();
            return changed;
        }

        public bool AddOneMarkedCell(List<Vector3D> importantPositions)
        {
            bool added = false;

            // adding of cells on way to prepared cells
            foreach (Vector3I cPos in m_cellsOnWay)
            {
                Vector3I cellPos = cPos;
                if (m_processedCells.Contains(ref cellPos) || m_markedForAddition.Contains(ref cellPos))
                    continue;

                float weight = CalculateCellWeight(importantPositions, cellPos);
                MarkCellForAddition(cellPos, weight);
            }

            while (!added)
            {
                if (m_toAdd.Count == 0)
                {
                    return added;
                }

                m_toAdd.QueryAll(m_heapItemList);
                float maxWeight = float.NegativeInfinity;
                CellToAddHeapItem maxItem = null;
                foreach (var item in m_heapItemList)
                {
                    float newWeight = CalculateCellWeight(importantPositions, item.Position);
                    //if (newWeight < 0.05)
                    //{
                    //    m_toAdd.Remove(item);
                    //    m_heapItemAllocator.Deallocate(item);
                    //    m_markedForAddition.Remove(item.Position);
                    //}
                    //else
                    {
                        if (newWeight > maxWeight)
                        {
                            maxWeight = newWeight;
                            maxItem = item;
                        }
                        m_toAdd.Modify(item, newWeight);
                    }
                }
                m_heapItemList.Clear();

                if (maxItem == null || maxWeight < LimitAddingWeight) return added;

                m_toAdd.Remove(maxItem);
                Vector3I cell = maxItem.Position;
                m_heapItemAllocator.Deallocate(maxItem);

                m_markedForAddition.Remove(cell);

                m_adjacentCells.Clear();
                if (AddCell(cell, ref m_adjacentCells))
                {
                    // adding of adjacent cells into cells marked for addition
                    foreach (Vector3I cellPos in m_adjacentCells)
                    {
                        float weight = CalculateCellWeight(importantPositions, cellPos);
                        MarkCellForAddition(cellPos, weight);
                    }

                    added = true;
                    break;
                }
            }

            m_higherLevelHelper.CheckConsistency();
            CheckOuterEdgeConsistency();

            return added;
        }

        private float CalculateCellWeight(List<Vector3D> importantPositions, Vector3I cellPos)
        {
            Vector3D worldCellCenterPos;
            Vector3I geometryCellPos = cellPos;
            MyVoxelCoordSystems.GeometryCellCenterCoordToWorldPos(m_voxelMap.PositionLeftBottomCorner, ref geometryCellPos, out worldCellCenterPos);
            // finding of distance from the nearest important object
            float dist = float.PositiveInfinity;
            foreach (Vector3D vec in importantPositions)
            {
                float d = Vector3.RectangularDistance(vec, worldCellCenterPos);
                if (d < dist)
                    dist = d;
            }
            if (m_cellsOnWayCoords.Contains(MyCellCoord.PackId64Static(0,cellPos)) )
                // cells on paths have higher priority for computing - they are computed as they are closer to persons
                dist -= CellsOnWayAdvance;

            return GetWeight(dist);
        }

        [Conditional("DEBUG")]
        private void AddDebugOuterEdge(ushort a, ushort b, List<DebugDrawEdge> debugEdgesList, Vector3D aTformed, Vector3D bTformed)
        {
            if (!m_connectionHelper.IsInnerEdge(a, b)) debugEdgesList.Add(new DebugDrawEdge(aTformed, bTformed));
        }

        private bool AddCell(Vector3I cellPos, ref HashSet<Vector3I> adjacentCellPos)
        {
            if (MyFakes.LOG_NAVMESH_GENERATION) MyCestmirPathfindingShorts.Pathfinding.VoxelPathfinding.DebugLog.LogCellAddition(this, cellPos);

            MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, cellPos);
            Debug.Assert(IsCellPosValid(ref cellPos));

            var voxelStart = cellPos * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
            var voxelEnd = voxelStart + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS + 1;

            var generatedMesh = MyPrecalcComponent.IsoMesher.Precalc(m_voxelMap.Storage, NAVMESH_LOD, voxelStart, voxelEnd, false, false);

            if (generatedMesh == null)
            {
                m_processedCells.Add(ref cellPos);
                m_higherLevelHelper.AddExplored(ref cellPos);
                return false;
            }

            ulong packedCoord = coord.PackId64();

#if DEBUG
            List<DebugDrawEdge> debugEdgesList = new List<DebugDrawEdge>();
            m_debugCellEdges[packedCoord] = debugEdgesList;
#endif

            MyVoxelPathfinding.CellId cellId = new MyVoxelPathfinding.CellId() { VoxelMap = m_voxelMap, Pos = cellPos };

            MyTrace.Send(TraceWindow.Ai, "Adding cell " + cellPos);

            m_connectionHelper.ClearCell();
            m_vertexMapping.Resize(generatedMesh.VerticesCount);

            // Prepare list of possibly intersecting cube grids for voxel-grid navmesh intersection testing
            Vector3D bbMin = m_voxelMap.PositionLeftBottomCorner + (m_cellSize * (m_bbMinOffset + cellPos));
            Vector3D bbMax = m_voxelMap.PositionLeftBottomCorner + (m_cellSize * (Vector3D.One + cellPos));
            m_cellBB.Min = bbMin;
            m_cellBB.Max = bbMax;
            m_tmpGridList.Clear();
            m_navmeshCoordinator.PrepareVoxelTriangleTests(m_cellBB, m_tmpGridList);

            Vector3D voxelMapCenter = m_voxelMap.PositionComp.GetPosition();
            Vector3 centerDisplacement = voxelMapCenter - m_voxelMap.PositionLeftBottomCorner;

            float eps = 0.5f;   // "little bit" constant - see under

            Vector3I minCell = cellPos - Vector3I.One;
            Vector3I maxCell = cellPos + Vector3I.One;

            Vector3I adjacentCell = minCell;
            m_adjacentBBoxes.Clear();
            for (var it = new Vector3I_RangeIterator(ref minCell, ref maxCell); it.IsValid(); it.GetNext(out adjacentCell))
            {
                if (adjacentCell.Equals(cellPos))
                    continue;
                Vector3I vec = adjacentCell - cellPos;
                BoundingBoxD bboxAdjacentCell;
                MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(m_voxelMap.PositionLeftBottomCorner, ref adjacentCell, out bboxAdjacentCell);
                bboxAdjacentCell.Translate(vec * -eps);    // moving of bbox little bit towards current (added) cell
                bboxAdjacentCell.Translate(m_halfMeterOffset);// triangles are moved half meter from border of cells
                m_adjacentBBoxes[adjacentCell] = bboxAdjacentCell;
            }

            // This is needed for correct edge classification - to tell, whether the edges are inner or outer edges of the cell
            // Also, vertices are unified here if they lie close to each other
            ProfilerShort.Begin("Triangle preprocessing");
            PreprocessTriangles(generatedMesh, centerDisplacement);
            ProfilerShort.End();

            // Ensure that the faces have increasing index numbers
            ProfilerShort.Begin("Free face sorting");
            Mesh.SortFreeFaces();
            ProfilerShort.End();

            m_higherLevelHelper.OpenNewCell(coord);

            ProfilerShort.Begin("Adding triangles");
            for (int i = 0; i < generatedMesh.TrianglesCount; i++)
            {
                ushort a = generatedMesh.Triangles[i].VertexIndex0;
                ushort b = generatedMesh.Triangles[i].VertexIndex1;
                ushort c = generatedMesh.Triangles[i].VertexIndex2;
                ushort setA = (ushort)m_vertexMapping.Find(a);
                ushort setB = (ushort)m_vertexMapping.Find(b);
                ushort setC = (ushort)m_vertexMapping.Find(c);

                if (setA == setB || setB == setC || setA == setC) continue;

                Vector3 aPos, bPos, cPos;
                Vector3 vert;
                generatedMesh.GetUnpackedPosition(a, out vert);
                aPos = vert - centerDisplacement;
                generatedMesh.GetUnpackedPosition(b, out vert);
                bPos = vert - centerDisplacement;
                generatedMesh.GetUnpackedPosition(c, out vert);
                cPos = vert - centerDisplacement;

                // Discard too steep triangles
                Vector3D aTformed = aPos + voxelMapCenter;
                Vector3 gravityUp = Sandbox.Game.GameSystems.MyGravityProviderSystem.CalculateNaturalGravityInPoint(aTformed);
                if (Vector3.IsZero(gravityUp)) gravityUp = Vector3.Up;
                else gravityUp = -Vector3.Normalize(gravityUp);
                Vector3 normal = (cPos - aPos).Cross(bPos - aPos);
                normal.Normalize();
                if (normal.Dot(gravityUp) <= Math.Cos(MathHelper.ToRadians(54.0f))) continue;

                Vector3D bTformed = bPos + voxelMapCenter;
                Vector3D cTformed = cPos + voxelMapCenter;

                bool intersecting = false;
                m_tmpLinkCandidates.Clear();
                m_navmeshCoordinator.TestVoxelNavmeshTriangle(ref aTformed, ref bTformed, ref cTformed, m_tmpGridList, m_tmpLinkCandidates, out intersecting);
                if (intersecting)
                {
                    m_tmpLinkCandidates.Clear();
                    continue;
                }

#if DEBUG
                if (!m_connectionHelper.IsInnerEdge(a, b)) debugEdgesList.Add(new DebugDrawEdge(aTformed, bTformed));
                if (!m_connectionHelper.IsInnerEdge(b, c)) debugEdgesList.Add(new DebugDrawEdge(bTformed, cTformed));
                if (!m_connectionHelper.IsInnerEdge(c, a)) debugEdgesList.Add(new DebugDrawEdge(cTformed, aTformed));
#endif

                if (!m_connectionHelper.IsInnerEdge(a, b))
                {
                    foreach (KeyValuePair<Vector3I, BoundingBoxD> pair in m_adjacentBBoxes)
                    {
                        if (pair.Value.Contains(aTformed) == ContainmentType.Contains || pair.Value.Contains(bTformed) == ContainmentType.Contains)
                            adjacentCellPos.Add(pair.Key);
                    }
                }
                if (!m_connectionHelper.IsInnerEdge(b, c))
                {
                    foreach (KeyValuePair<Vector3I, BoundingBoxD> pair in m_adjacentBBoxes)
                    {
                        if (pair.Value.Contains(bTformed) == ContainmentType.Contains || pair.Value.Contains(cTformed) == ContainmentType.Contains)
                            adjacentCellPos.Add(pair.Key);
                    }
                }
                if (!m_connectionHelper.IsInnerEdge(c, a))
                {
                    foreach (KeyValuePair<Vector3I, BoundingBoxD> pair in m_adjacentBBoxes)
                    {
                        if (pair.Value.Contains(cTformed) == ContainmentType.Contains || pair.Value.Contains(aTformed) == ContainmentType.Contains)
                            adjacentCellPos.Add(pair.Key);
                    }
                }

                int edgeAB = m_connectionHelper.TryGetAndRemoveEdgeIndex(b, a, ref bPos, ref aPos);
                int edgeBC = m_connectionHelper.TryGetAndRemoveEdgeIndex(c, b, ref cPos, ref bPos);
                int edgeCA = m_connectionHelper.TryGetAndRemoveEdgeIndex(a, c, ref aPos, ref cPos);
                int formerAB = edgeAB;
                int formerBC = edgeBC;
                int formerCA = edgeCA;

                ProfilerShort.Begin("AddTriangle");
                var tri = AddTriangle(ref aPos, ref bPos, ref cPos, ref edgeAB, ref edgeBC, ref edgeCA);
                ProfilerShort.End();

                ProfilerShort.Begin("Fix outer edges");
                // Iterate over the triangle's vertices and fix possible outer edges (because the triangle vertices could have moved)
                {
                    var v = Mesh.GetFace(tri.Index).GetVertexEnumerator();

                    while (v.MoveNext())
                    {
                        int vertInd = v.CurrentIndex;
                        Vector3 currentPosition = Mesh.GetVertexPosition(vertInd);
                        var edges = Mesh.GetVertexEdges(vertInd);
                        while (edges.MoveNext())
                        {
                            var edge = edges.Current;
                            if (edge.LeftFace == MyWingedEdgeMesh.INVALID_INDEX)
                            {
                                if (vertInd == edge.Vertex1)
                                {
                                    m_connectionHelper.FixOuterEdge(edges.CurrentIndex, true, currentPosition);
                                }
                                else
                                {
                                    m_connectionHelper.FixOuterEdge(edges.CurrentIndex, false, currentPosition);
                                }
                            }
                            else if (edge.RightFace == MyWingedEdgeMesh.INVALID_INDEX)
                            {
                                if (vertInd == edge.Vertex1)
                                {
                                    m_connectionHelper.FixOuterEdge(edges.CurrentIndex, false, currentPosition);
                                }
                                else
                                {
                                    m_connectionHelper.FixOuterEdge(edges.CurrentIndex, true, currentPosition);
                                }
                            }
                        }
                    }

                    //Mesh.PrepareFreeVertexHashset();
                }
                ProfilerShort.End();

                ProfilerShort.Begin("Updating vertices");
                // We have to get the triangle vertices again for the connection helper (they could have moved)
                Vector3 realA, realB, realC;
                {
                    var eabEntry = Mesh.GetEdge(edgeAB);
                    int aIndex = eabEntry.GetFacePredVertex(tri.Index);
                    int bIndex = eabEntry.GetFaceSuccVertex(tri.Index);
                    var ebcEntry = Mesh.GetEdge(eabEntry.GetNextFaceEdge(tri.Index));
                    int cIndex = ebcEntry.GetFaceSuccVertex(tri.Index);
                    realA = Mesh.GetVertexPosition(aIndex);
                    realB = Mesh.GetVertexPosition(bIndex);
                    realC = Mesh.GetVertexPosition(cIndex);
                }
                ProfilerShort.End();

                CheckMeshConsistency();

                m_higherLevelHelper.AddTriangle(tri.Index);

                if (formerAB == -1) m_connectionHelper.AddEdgeIndex(a, b, ref realA, ref realB, edgeAB);
                if (formerBC == -1) m_connectionHelper.AddEdgeIndex(b, c, ref realB, ref realC, edgeBC);
                if (formerCA == -1) m_connectionHelper.AddEdgeIndex(c, a, ref realC, ref realA, edgeCA);

                // TODO: Instead of this, just add the tri into a list of tris that want to connect with the link candidates
                //m_navmeshCoordinator.TryAddVoxelNavmeshLinks(tri, cellId, m_tmpLinkCandidates);
                foreach (var candidate in m_tmpLinkCandidates)
                {
                    List<MyNavigationPrimitive> primitives = null;
                    if (!m_tmpCubeLinkCandidates.TryGetValue(candidate, out primitives))
                    {
                        primitives = m_primitiveListPool.Allocate();
                        m_tmpCubeLinkCandidates.Add(candidate, primitives);
                    }

                    primitives.Add(tri);
                }
                m_tmpLinkCandidates.Clear();
            }
            ProfilerShort.End();

            m_tmpGridList.Clear();
            m_connectionHelper.ClearCell();
            m_vertexMapping.Clear();

            Debug.Assert(!m_processedCells.Contains(ref cellPos));
            m_processedCells.Add(ref cellPos);
            m_higherLevelHelper.AddExplored(ref cellPos);

            // Find connected components in the current cell's subgraph of the navigation mesh
            m_higherLevelHelper.ProcessCellComponents();
            m_higherLevelHelper.CloseCell();

            // Create navmesh links using the navmesh coordinator, taking into consideration the high level components
            m_navmeshCoordinator.TryAddVoxelNavmeshLinks2(cellId, m_tmpCubeLinkCandidates);
            m_navmeshCoordinator.UpdateVoxelNavmeshCellHighLevelLinks(cellId);

            foreach (var candidate in m_tmpCubeLinkCandidates)
            {
                candidate.Value.Clear();
                m_primitiveListPool.Deallocate(candidate.Value);
            }
            m_tmpCubeLinkCandidates.Clear();
            CheckOuterEdgeConsistency();

            return true;
        }

        private void PreprocessTriangles(MyIsoMesh generatedMesh, Vector3 centerDisplacement)
        {
            for (int i = 0; i < generatedMesh.TrianglesCount; i++)
            {
                ushort a = generatedMesh.Triangles[i].VertexIndex0;
                ushort b = generatedMesh.Triangles[i].VertexIndex1;
                ushort c = generatedMesh.Triangles[i].VertexIndex2;

                Vector3 aPos, bPos, cPos;
                Vector3 vert;

                generatedMesh.GetUnpackedPosition(a, out vert);
                aPos = vert - centerDisplacement;
                generatedMesh.GetUnpackedPosition(b, out vert);
                bPos = vert - centerDisplacement;
                generatedMesh.GetUnpackedPosition(c, out vert);
                cPos = vert - centerDisplacement;

                bool invalidTriangle = false;
                if ((bPos - aPos).LengthSquared() <= MyVoxelConnectionHelper.OUTER_EDGE_EPSILON_SQ)
                {
                    m_vertexMapping.Union(a, b);
                    invalidTriangle = true;
                }
                if ((cPos - aPos).LengthSquared() <= MyVoxelConnectionHelper.OUTER_EDGE_EPSILON_SQ)
                {
                    m_vertexMapping.Union(a, c);
                    invalidTriangle = true;
                }
                if ((cPos - bPos).LengthSquared() <= MyVoxelConnectionHelper.OUTER_EDGE_EPSILON_SQ)
                {
                    m_vertexMapping.Union(b, c);
                    invalidTriangle = true;
                }

                if (invalidTriangle) continue;

                m_connectionHelper.PreprocessInnerEdge(a, b);
                m_connectionHelper.PreprocessInnerEdge(b, c);
                m_connectionHelper.PreprocessInnerEdge(c, a);
            }
        }

        private bool RemoveCell(Vector3I cell)
        {
            if (!MyFakes.REMOVE_VOXEL_NAVMESH_CELLS) return true;

            Debug.Assert(m_processedCells.Contains(cell), "Removing a non-existent cell from the navmesh!");
            if (!m_processedCells.Contains(cell)) return false;

            MyTrace.Send(TraceWindow.Ai, "Removing cell " + cell);
            if (MyFakes.LOG_NAVMESH_GENERATION) MyCestmirPathfindingShorts.Pathfinding.VoxelPathfinding.DebugLog.LogCellRemoval(this, cell);

            ProfilerShort.Begin("Removing navmesh links");
            MyVoxelPathfinding.CellId cellId = new MyVoxelPathfinding.CellId() { VoxelMap = m_voxelMap, Pos = cell };
            m_navmeshCoordinator.RemoveVoxelNavmeshLinks(cellId);
            ProfilerShort.End();

            ProfilerShort.Begin("Removing triangles");
            MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, cell);
            ulong packedCoord = coord.PackId64();
            MyIntervalList triangleList = m_higherLevelHelper.TryGetTriangleList(packedCoord);
            if (triangleList != null)
            {
                foreach (var triangleIndex in triangleList)
                {
                    RemoveTerrainTriangle(GetTriangle(triangleIndex));
                    CheckMeshConsistency();
                }
                m_higherLevelHelper.ClearCachedCell(packedCoord);
            }
            ProfilerShort.End();

            Debug.Assert(m_processedCells.Contains(ref cell));
            m_processedCells.Remove(ref cell);
            CheckOuterEdgeConsistency();

            return triangleList != null;
        }

        private void RemoveTerrainTriangle(MyNavigationTriangle tri)
        {
            var vertices = tri.GetVertexEnumerator();

            vertices.MoveNext();
            Vector3 aPos = vertices.Current;
            vertices.MoveNext();
            Vector3 bPos = vertices.Current;
            vertices.MoveNext();
            Vector3 cPos = vertices.Current;

            int edgeAB = tri.GetEdgeIndex(0);
            int edgeBC = tri.GetEdgeIndex(1);
            int edgeCA = tri.GetEdgeIndex(2);

            int tmp;

            ProfilerShort.Begin("Handling outer edges");
            tmp = edgeAB;
            if (!m_connectionHelper.TryRemoveOuterEdge(ref aPos, ref bPos, ref tmp))
            {
                var edge = Mesh.GetEdge(edgeAB);
                if (edge.OtherFace(tri.Index) != -1)
                    m_connectionHelper.AddOuterEdgeIndex(ref bPos, ref aPos, edgeAB);
            }
            tmp = edgeBC;
            if (!m_connectionHelper.TryRemoveOuterEdge(ref bPos, ref cPos, ref tmp))
            {
                var edge = Mesh.GetEdge(edgeBC);
                if (edge.OtherFace(tri.Index) != -1)
                    m_connectionHelper.AddOuterEdgeIndex(ref cPos, ref bPos, edgeBC);
            }
            tmp = edgeCA;
            if (!m_connectionHelper.TryRemoveOuterEdge(ref cPos, ref aPos, ref tmp))
            {
                var edge = Mesh.GetEdge(edgeCA);
                if (edge.OtherFace(tri.Index) != -1)
                    m_connectionHelper.AddOuterEdgeIndex(ref aPos, ref cPos, edgeCA);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Removing the tri");
            RemoveTriangle(tri);
            ProfilerShort.End();
        }

        public void RemoveTriangle(int index)
        {
            var tri = GetTriangle(index);
            RemoveTerrainTriangle(tri);
        }

        public bool RemoveOneUnusedCell(List<Vector3D> importantPositions)
        {
            ProfilerShort.Begin("RemoveOneUnusedCell");

            m_tmpCellSet.Clear();
            m_tmpCellSet.Union(m_processedCells);

            bool removed = false;

            foreach (var cell in m_tmpCellSet)
            {
                Vector3I geometryCellCoord = cell * (1 << NAVMESH_LOD);
                Vector3D localPosition;
                Vector3D worldPosition;
                MyVoxelCoordSystems.GeometryCellCoordToLocalPosition(ref geometryCellCoord, out localPosition);
                localPosition += new Vector3D((1 << NAVMESH_LOD) * 0.5f);
                MyVoxelCoordSystems.LocalPositionToWorldPosition(m_voxelMap.PositionLeftBottomCorner, ref localPosition, out worldPosition);

                bool remove = true;

                foreach (var position in importantPositions)
                {
                    if (Vector3D.RectangularDistance(worldPosition, position) < ProcessedRemovingDistance)
                    {
                        remove = false;
                        break;
                    }
                }

                if (remove && !m_markedForAddition.Contains(cell))
                {
                    if (RemoveCell(cell))
                    {
                        Vector3I cellPos = cell;
                        // get right weight for this cell
                        float weight = CalculateCellWeight(importantPositions, cellPos);
                        MarkCellForAddition(cellPos, weight);
                        removed = true;
                        break;
                    }
                }
            }

            m_tmpCellSet.Clear();

            ProfilerShort.End();

            return removed;
        }

        public void RemoveFarHighLevelGroups(List<Vector3D> updatePositions)
        {
            // CH: TODO: Only remove when a certain memory limit is reached
            // remove too far high level info (if it isn't in processed cells)
            // we can't erase high level info for m_processedCells for it is necessary for deleting of triangle navigation net when cell is removed
            m_higherLevelHelper.RemoveTooFarCells(updatePositions, ExploredRemovingDistance, m_processedCells);
        }

        public void MarkCellsOnPaths()
        {
            m_primitivesOnPath.Clear();
            m_higherLevel.GetPrimitivesOnPath(ref m_primitivesOnPath);

            m_cellsOnWayCoords.Clear();
            m_higherLevelHelper.GetCellsOfPrimitives(ref m_cellsOnWayCoords, ref m_primitivesOnPath);

            m_cellsOnWay.Clear();
            foreach (ulong cellCoord in m_cellsOnWayCoords)
            {
                MyCellCoord coord = new MyCellCoord();
                coord.SetUnpack(cellCoord);
                Vector3I cell = coord.CoordInLod;
                m_cellsOnWay.Add(cell);
            }
        }

        [Conditional("DEBUG")]
        // Do not use in production code! Serves only for debugging
        public void AddCellDebug(Vector3I cellPos)
        {
            HashSet<Vector3I> adjacent = new HashSet<Vector3I>();
            AddCell(cellPos, ref adjacent);
        }

        [Conditional("DEBUG")]
        // Do not use in production code! Serves only for debugging
        public void RemoveCellDebug(Vector3I cellPos)
        {
            RemoveCell(cellPos);
        }

        public List<Vector4D> FindPathGlobal(Vector3D start, Vector3D end)
        {
            start = Vector3D.Transform(start, m_voxelMap.PositionComp.WorldMatrixNormalizedInv);
            end = Vector3D.Transform(end, m_voxelMap.PositionComp.WorldMatrixNormalizedInv);
            return FindPath(start, end);
        }

        /// <summary>
        /// All coords should be in the voxel local coordinates
        /// </summary>
        public List<Vector4D> FindPath(Vector3 start, Vector3 end)
        {
            float closestDistance = float.PositiveInfinity;
            MyNavigationTriangle startTri = GetClosestNavigationTriangle(ref start, ref closestDistance);
            if (startTri == null) return null;
            closestDistance = float.PositiveInfinity;
            MyNavigationTriangle endTri = GetClosestNavigationTriangle(ref end, ref closestDistance);
            if (endTri == null) return null;

            m_debugPos1 = Vector3.Transform(startTri.Position, m_voxelMap.PositionComp.WorldMatrix);
            m_debugPos2 = Vector3.Transform(endTri.Position, m_voxelMap.PositionComp.WorldMatrix);
            m_debugPos3 = Vector3.Transform(start, m_voxelMap.PositionComp.WorldMatrix);
            m_debugPos4 = Vector3.Transform(end, m_voxelMap.PositionComp.WorldMatrix);

            var path = FindRefinedPath(startTri, endTri, ref start, ref end);

            return path;
        }

        private static readonly Vector3I[] m_cornerOffsets =
        {
            new Vector3I(-1,-1,-1),
            new Vector3I( 0,-1,-1),
            new Vector3I(-1, 0,-1),
            new Vector3I( 0, 0,-1),
            new Vector3I(-1,-1, 0),
            new Vector3I( 0,-1, 0),
            new Vector3I(-1, 0, 0),
            new Vector3I( 0, 0, 0),
        };
        private MyNavigationTriangle GetClosestNavigationTriangle(ref Vector3 point, ref float closestDistanceSq)
        {
            // TODO: When point is completely away (according to BB), return null

            MyNavigationTriangle closestTriangle = null;

            // Convert from world matrix local coords to LeftBottomCorner-based coords
            Vector3 lbcPoint = point + (m_voxelMap.PositionComp.GetPosition() - m_voxelMap.PositionLeftBottomCorner);

            Vector3I closestCellCorner = Vector3I.Round(lbcPoint / m_cellSize);
            for (int i = 0; i < 8; ++i)
            {
                Vector3I cell = closestCellCorner + m_cornerOffsets[i];
                if (!m_processedCells.Contains(cell)) continue;

                MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, cell);
                ulong packedCoord = coord.PackId64();
                MyIntervalList triList = m_higherLevelHelper.TryGetTriangleList(packedCoord);
                if (triList == null) continue;

                foreach (var triIndex in triList)
                {
                    MyNavigationTriangle tri = GetTriangle(triIndex);

                    // TODO: Use triangle centers so far
                    float distSq = Vector3.DistanceSquared(tri.Center, point);
                    if (distSq < closestDistanceSq)
                    {
                        closestDistanceSq = distSq;
                        closestTriangle = tri;
                    }
                }
            }

            return closestTriangle;
        }

        private MyHighLevelPrimitive GetClosestHighLevelPrimitive(ref Vector3 point, ref float closestDistanceSq)
        {
            MyHighLevelPrimitive retval = null;

            // Convert from world matrix local coords to LeftBottomCorner-based coords
            Vector3 lbcPoint = point + (m_voxelMap.PositionComp.GetPosition() - m_voxelMap.PositionLeftBottomCorner);

            m_tmpIntList.Clear();

            // Collect components from the eight closest cells
            Vector3I closestCellCorner = Vector3I.Round(lbcPoint / m_cellSize);
            for (int i = 0; i < 8; ++i)
            {
                Vector3I cell = closestCellCorner + m_cornerOffsets[i];

                MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, cell);
                ulong packedCoord = coord.PackId64();

                m_higherLevelHelper.CollectComponents(packedCoord, m_tmpIntList);
            }

            foreach (int componentIndex in m_tmpIntList)
            {
                var hlPrimitive = m_higherLevel.GetPrimitive(componentIndex);
                Debug.Assert(hlPrimitive != null, "Couldnt' find a high-level primitive for the index given by higher level helper!");
                if (hlPrimitive == null) continue;

                float distSq = Vector3.DistanceSquared(hlPrimitive.Position, point);
                if (distSq < closestDistanceSq)
                {
                    closestDistanceSq = distSq;
                    retval = hlPrimitive;
                }
            }

            m_tmpIntList.Clear();

            return retval;
        }

        public override MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistanceSq)
        {
            MatrixD invMat = m_voxelMap.PositionComp.WorldMatrixNormalizedInv;
            Vector3 localPos = Vector3D.Transform(point, invMat);
            float closestDistSq = (float)closestDistanceSq;

            MyNavigationPrimitive closestPrimitive = null;
            if (highLevel)
            {
                closestPrimitive = GetClosestHighLevelPrimitive(ref localPos, ref closestDistSq);
            }
            else
            {
                closestPrimitive = GetClosestNavigationTriangle(ref localPos, ref closestDistSq);
            }

            if (closestPrimitive != null)
            {
                closestDistanceSq = closestDistSq;
            }

            return closestPrimitive;
        }

        public override MatrixD GetWorldMatrix()
        {
            return m_voxelMap.WorldMatrix;
        }

        public override Vector3 GlobalToLocal(Vector3D globalPos)
        {
            return Vector3D.Transform(globalPos, m_voxelMap.PositionComp.WorldMatrixNormalizedInv);
        }

        public override Vector3D LocalToGlobal(Vector3 localPos)
        {
            return Vector3D.Transform(localPos, m_voxelMap.WorldMatrix);
        }

        public override MyHighLevelGroup HighLevelGroup
        {
            get { return m_higherLevel; }
        }

        public override MyHighLevelPrimitive GetHighLevelPrimitive(MyNavigationPrimitive myNavigationTriangle)
        {
            return m_higherLevelHelper.GetHighLevelNavigationPrimitive(myNavigationTriangle as MyNavigationTriangle);
        }

        public override IMyHighLevelComponent GetComponent(MyHighLevelPrimitive highLevelPrimitive)
        {
            return m_higherLevelHelper.GetComponent(highLevelPrimitive);
        }

        [Conditional("DEBUG")]
        private void CheckOuterEdgeConsistency()
        {
            if (!DO_CONSISTENCY_CHECKS) return;
            Mesh.PrepareFreeEdgeHashset();

            var outerEdgeList = new List<MyTuple<MyVoxelConnectionHelper.OuterEdgePoint, Vector3>>();
            m_connectionHelper.CollectOuterEdges(outerEdgeList);
            foreach (var tuple in outerEdgeList)
            {
                // The edge must exist in the mesh
                int edgeIndex = tuple.Item1.EdgeIndex;
                Mesh.CheckEdgeIndexValidQuick(edgeIndex);
                var edge = Mesh.GetEdge(edgeIndex);

                // The edge must be on the mesh edge
                Debug.Assert(edge.LeftFace == -1 || edge.RightFace == -1, "Edge is not on the edge of the mesh, yet it is in outer edge list!");

                // The edge vertex must correspond the correct vertex in the mesh
                if (tuple.Item1.FirstPoint)
                {
                    int vertex = edge.GetFaceSuccVertex(-1);
                    Debug.Assert(tuple.Item2 == Mesh.GetVertexPosition(vertex), "Inconsistency in outer edge position!");
                }
                else
                {
                    int vertex = edge.GetFacePredVertex(-1);
                    Debug.Assert(tuple.Item2 == Mesh.GetVertexPosition(vertex), "Inconsistency in outer edge position!");
                }
            }
        }

        public override void DebugDraw(ref Matrix drawMatrix)
        {
            base.DebugDraw(ref drawMatrix);

            if (MyFakes.DEBUG_DRAW_NAVMESH_PROCESSED_VOXEL_CELLS)
            {
                Vector3 tformedCellSize = Vector3.TransformNormal(m_cellSize, drawMatrix);
                Vector3 origin = Vector3.Transform(m_voxelMap.PositionLeftBottomCorner - m_voxelMap.PositionComp.GetPosition(), drawMatrix);

                BoundingBoxD bb;
                foreach (var cell in m_processedCells)
                {
                    bb.Min = origin + tformedCellSize * (new Vector3D(0.0625) + cell);
                    bb.Max = bb.Min + tformedCellSize;
                    bb.Inflate(-0.2f);
                    VRageRender.MyRenderProxy.DebugDrawAABB(bb, Color.Orange, 1.0f, 1.0f, false);
                    VRageRender.MyRenderProxy.DebugDrawText3D(bb.Center, cell.ToString(), Color.Orange, 0.5f, false);
                }
                //foreach (var cell in m_removedCells)
                //{
                //    bb.Min = origin + tformedCellSize * (new Vector3D(0.0625) + cell);
                //    bb.Max = bb.Min + tformedCellSize;
                //    bb.Inflate(-0.6f);
                //    VRageRender.MyRenderProxy.DebugDrawAABB(bb, Color.Olive, 1.0f, 1.0f, false);
                //}
            }

            if (MyFakes.DEBUG_DRAW_NAVMESH_CELLS_ON_PATHS)
            {
                Vector3 tformedCellSize = Vector3.TransformNormal(m_cellSize, drawMatrix);
                Vector3 origin = Vector3.Transform(m_voxelMap.PositionLeftBottomCorner - m_voxelMap.PositionComp.GetPosition(), drawMatrix);

                BoundingBoxD bb;
                MyCellCoord coord = new MyCellCoord();
                foreach (ulong cellCoord in m_cellsOnWayCoords)
                {
                    coord.SetUnpack(cellCoord);
                    Vector3I cell = coord.CoordInLod;

                    bb.Min = origin + tformedCellSize * (new Vector3D(0.0625) + cell);
                    bb.Max = bb.Min + tformedCellSize;
                    bb.Inflate(-0.3f);
                    VRageRender.MyRenderProxy.DebugDrawAABB(bb, Color.Green, 1.0f, 1.0f, false);
                }
            }

            if (MyFakes.DEBUG_DRAW_NAVMESH_PREPARED_VOXEL_CELLS)
            {
                Vector3 tformedCellSize = Vector3.TransformNormal(m_cellSize, drawMatrix);
                Vector3 origin = Vector3.Transform(m_voxelMap.PositionLeftBottomCorner - m_voxelMap.PositionComp.GetPosition(), drawMatrix);

                BoundingBoxD bb;
                // find maximum
                float max = float.NegativeInfinity;
                Vector3I maxCell = Vector3I.Zero;
                for (int i = 0; i < m_toAdd.Count; ++i)
                {
                    var item = m_toAdd.GetItem(i);
                    float weight = item.HeapKey;
                    if ( weight > max )
                    {
                        max = weight;
                        maxCell = item.Position;
                    }
                }

                for (int i = 0; i < m_toAdd.Count; ++i)
                {
                    var item = m_toAdd.GetItem(i);
                    
                    float weight = item.HeapKey;
                    Vector3I cell = item.Position;

                    bb.Min = origin + tformedCellSize * (new Vector3D(0.0625) + cell);
                    bb.Max = bb.Min + tformedCellSize;
                    bb.Inflate(-0.1f);
                    Color col = Color.Aqua;
                    if (cell.Equals(maxCell))
                        col = Color.Red;
                    VRageRender.MyRenderProxy.DebugDrawAABB(bb, col, 1, 1.0f, false);
                    //string str = String.Format("{0}[{1},{2},{3}]",weight.ToString("n2"),cell.X,cell.Y,cell.Z);
                    string str = String.Format("{0}", weight.ToString("n2"));
                    VRageRender.MyRenderProxy.DebugDrawText3D(bb.Center, str, col, 0.7f, false);
                }
            }


            VRageRender.MyRenderProxy.DebugDrawSphere(m_debugPos1, 0.2f, Color.Red, 1.0f, false);
            VRageRender.MyRenderProxy.DebugDrawSphere(m_debugPos2, 0.2f, Color.Green, 1.0f, false);
            VRageRender.MyRenderProxy.DebugDrawSphere(m_debugPos3, 0.1f, Color.Red, 1.0f, false);
            VRageRender.MyRenderProxy.DebugDrawSphere(m_debugPos4, 0.1f, Color.Green, 1.0f, false);

            if (MyFakes.DEBUG_DRAW_VOXEL_CONNECTION_HELPER)
            {
                m_connectionHelper.DebugDraw(ref drawMatrix, Mesh);
            }

            if (MyFakes.DEBUG_DRAW_NAVMESH_CELL_BORDERS)
            {
                foreach (var cell in m_debugCellEdges)
                {
                    foreach (var edge in cell.Value)
                    {
                        VRageRender.MyRenderProxy.DebugDrawLine3D(edge.V1, edge.V2, Color.Orange, Color.Orange, false);
                    }
                }
            }
            else
            {
                m_debugCellEdges.Clear();
            }

            if (MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY)
            {
                if (MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY_LITE)
                {
                    m_higherLevel.DebugDraw(lite: true);
                }
                else
                {
                    m_higherLevel.DebugDraw(lite: false);
                    m_higherLevelHelper.DebugDraw();
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES == MyWEMDebugDrawMode.LINES && !(this.m_voxelMap is MyVoxelPhysics))
            {
                int i = 0;
                MyWingedEdgeMesh.EdgeEnumerator e = Mesh.GetEdges();
                Vector3D offset = m_voxelMap.PositionComp.GetPosition();
                while (e.MoveNext())
                {
                    Vector3D v1 = Mesh.GetVertexPosition(e.Current.Vertex1) + offset;
                    Vector3D v2 = Mesh.GetVertexPosition(e.Current.Vertex2) + offset;
                    Vector3D s = (v1 + v2) * 0.5;

                    if (MyCestmirPathfindingShorts.Pathfinding.Obstacles.IsInObstacle(s))
                    {
                        VRageRender.MyRenderProxy.DebugDrawSphere(s, 0.05f, Color.Red, 1.0f, false);
                    }
                    i++;
                }
            }
        }
    }
}
