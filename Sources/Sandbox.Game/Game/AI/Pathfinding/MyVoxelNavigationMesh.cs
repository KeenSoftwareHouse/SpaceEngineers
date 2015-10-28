﻿using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Collections;
using VRage.Utils;
using VRage.Voxels;
using VRage.Trace;
using VRageMath;
using VRage.Generics;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyVoxelNavigationMesh : MyNavigationMesh
    {
        private MyVoxelBase m_voxelMap;
        private Vector3 m_cellSize;

        // Cells that are fully processed and present in the mesh
        private MyVector3ISet m_processedCells;

        // Cells that are in the binary heap of cells to be added
        private MyVector3ISet m_markedForAddition;

        // Binary heap of cells to be added to the mesh
        private MyBinaryStructHeap<float, Vector3I> m_toAdd;

        private static MyVector3ISet m_tmpCellSet = new MyVector3ISet();
        private static List<MyCubeGrid> m_tmpGridList = new List<MyCubeGrid>();
        private static List<MyGridPathfinding.CubeId> m_tmpLinkCandidates = new List<MyGridPathfinding.CubeId>();
        private static Dictionary<MyGridPathfinding.CubeId, List<MyNavigationPrimitive>> m_tmpCubeLinkCandidates = new Dictionary<MyGridPathfinding.CubeId, List<MyNavigationPrimitive>>();
        private static MyDynamicObjectPool<List<MyNavigationPrimitive>> m_primitiveListPool = new MyDynamicObjectPool<List<MyNavigationPrimitive>>(8);

        private static MyUnionFind m_vertexMapping = new MyUnionFind();
        private static List<int> m_tmpIntList = new List<int>();

        private MyVoxelConnectionHelper m_connectionHelper;
        private MyNavmeshCoordinator m_navmeshCoordinator;

        private MyHighLevelGroup m_higherLevel;
        private MyVoxelHighLevelHelper m_higherLevelHelper;

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
            m_cellSize = m_voxelMap.SizeInMetres / m_voxelMap.Storage.Geometry.CellsCount * (1 << NAVMESH_LOD);

            m_processedCells = new MyVector3ISet();
            m_markedForAddition = new MyVector3ISet();
            m_toAdd = new MyBinaryStructHeap<float, Vector3I>(128);

            m_connectionHelper = new MyVoxelConnectionHelper();
            m_navmeshCoordinator = coordinator;
            m_higherLevel = new MyHighLevelGroup(this, coordinator.HighLevelLinks, timestampFunction);
            m_higherLevelHelper = new MyVoxelHighLevelHelper(this);

            m_debugCellEdges = new Dictionary<ulong, List<DebugDrawEdge>>();

            voxelMap.Storage.RangeChanged += OnStorageChanged;
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
            for (var it = new Vector3I.RangeIterator(ref minCell, ref maxCell); it.IsValid(); it.GetNext(out currentCell))
            {
                if (m_processedCells.Contains(ref currentCell))
                {
                    RemoveCell(currentCell);
                }

                MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, currentCell);
                m_higherLevelHelper.TryClearCell(coord.PackId64());
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

            for (var it = new Vector3I.RangeIterator(ref pos, ref end); it.IsValid(); it.GetNext(out pos))
            {
                if (!m_processedCells.Contains(ref pos) && !m_markedForAddition.Contains(ref pos))
                {
                    float weight = 1.0f / (0.01f + Vector3.RectangularDistance(pos, center));

                    if (!m_toAdd.Full)
                    {
                        m_toAdd.Insert(pos, weight);
                        m_markedForAddition.Add(ref pos);
                    }
                    else
                    {
                        float min = m_toAdd.MinKey();
                        if (weight > min)
                        {
                            Vector3I posRemoved = m_toAdd.RemoveMin();
                            m_markedForAddition.Remove(ref posRemoved);

                            m_toAdd.Insert(pos, weight);
                            m_markedForAddition.Add(ref pos);
                        }
                    }
                }
            }
            ProfilerShort.End();
        }

        public bool AddOneMarkedCell()
        {
            bool added = false;

            while (!added)
            {
                if (m_toAdd.Count == 0)
                {
                    return added;
                }

                Vector3I cell = m_toAdd.RemoveMax();
                m_markedForAddition.Remove(ref cell);

                if (AddCell(cell))
                {
                    added = true;
                    break;
                }
            }

            return added;
        }

        private bool AddCell(Vector3I cellPos)
        {
            MyCellCoord coord = new MyCellCoord(NAVMESH_LOD, cellPos);

            var generatedMesh = MyPrecalcComponent.IsoMesher.Precalc(new MyIsoMesherArgs()
            {
                Storage = m_voxelMap.Storage,
                GeometryCell = coord,
            });

            if (generatedMesh == null)
            {
                m_processedCells.Add(ref cellPos);
                m_higherLevelHelper.AddExplored(ref cellPos);
                return false;
            }

            ulong packedCoord = coord.PackId64();

            List<DebugDrawEdge> debugEdgesList = new List<DebugDrawEdge>();
            m_debugCellEdges[packedCoord] = debugEdgesList;

            MyVoxelPathfinding.CellId cellId = new MyVoxelPathfinding.CellId() { VoxelMap = m_voxelMap, Pos = cellPos };

            MyTrace.Send(TraceWindow.Ai, "Adding cell " + cellPos);

            m_connectionHelper.ClearCell();
            m_vertexMapping.Init(generatedMesh.VerticesCount);

            // Prepare list of possibly intersecting cube grids for voxel-grid navmesh intersection testing
            Vector3D bbMin = m_voxelMap.PositionLeftBottomCorner + (m_cellSize * (new Vector3D(-0.125) + cellPos));
            Vector3D bbMax = m_voxelMap.PositionLeftBottomCorner + (m_cellSize * (Vector3D.One + cellPos));
            BoundingBoxD cellBB = new BoundingBoxD(bbMin, bbMax);
            m_tmpGridList.Clear();
            m_navmeshCoordinator.PrepareVoxelTriangleTests(cellBB, m_tmpGridList);

            Vector3D voxelMapCenter = m_voxelMap.PositionComp.GetPosition();
            Vector3 centerDisplacement = voxelMapCenter - m_voxelMap.PositionLeftBottomCorner;

            // This is needed for correct edge classification - to tell, whether the edges are inner or outer edges of the cell
            ProfilerShort.Begin("Triangle preprocessing");
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
            ProfilerShort.End();

            ProfilerShort.Begin("Free face sorting");
            // Ensure that the faces have increasing index numbers
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

                if (MyPerGameSettings.NavmeshPresumesDownwardGravity)
                {
                    Vector3 normal = (cPos - aPos).Cross(bPos - aPos);
                    normal.Normalize();
                    if (normal.Dot(ref Vector3.Up) <= Math.Cos(MathHelper.ToRadians(54.0f))) continue;
                }

                Vector3D aTformed = aPos + voxelMapCenter;
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

                if (!m_connectionHelper.IsInnerEdge(a, b)) debugEdgesList.Add(new DebugDrawEdge(aTformed, bTformed));
                if (!m_connectionHelper.IsInnerEdge(b, c)) debugEdgesList.Add(new DebugDrawEdge(bTformed, cTformed));
                if (!m_connectionHelper.IsInnerEdge(c, a)) debugEdgesList.Add(new DebugDrawEdge(cTformed, aTformed));

                int edgeAB = m_connectionHelper.TryGetAndRemoveEdgeIndex(b, a, ref bPos, ref aPos);
                int edgeBC = m_connectionHelper.TryGetAndRemoveEdgeIndex(c, b, ref cPos, ref bPos);
                int edgeCA = m_connectionHelper.TryGetAndRemoveEdgeIndex(a, c, ref aPos, ref cPos);
                int formerAB = edgeAB;
                int formerBC = edgeBC;
                int formerCA = edgeCA;

                ProfilerShort.Begin("AddTriangle");
                var tri = AddTriangle(ref aPos, ref bPos, ref cPos, ref edgeAB, ref edgeBC, ref edgeCA);
                ProfilerShort.End();

                CheckMeshConsistency();

                m_higherLevelHelper.AddTriangle(tri.Index);

                if (formerAB == -1) m_connectionHelper.AddEdgeIndex(a, b, ref aPos, ref bPos, edgeAB);
                if (formerBC == -1) m_connectionHelper.AddEdgeIndex(b, c, ref bPos, ref cPos, edgeBC);
                if (formerCA == -1) m_connectionHelper.AddEdgeIndex(c, a, ref cPos, ref aPos, edgeCA);

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

            return true;
        }

        private bool RemoveCell(Vector3I cell)
        {
            if (!MyFakes.REMOVE_VOXEL_NAVMESH_CELLS) return true;

            Debug.Assert(m_processedCells.Contains(cell), "Removing a non-existent cell from the navmesh!");
            if (!m_processedCells.Contains(cell)) return false;

            MyTrace.Send(TraceWindow.Ai, "Removing cell " + cell);

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
                }
                m_higherLevelHelper.ClearCachedCell(packedCoord);
            }
            ProfilerShort.End();

            Debug.Assert(m_processedCells.Contains(ref cell));
            m_processedCells.Remove(ref cell);

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
                    if (Vector3D.RectangularDistance(worldPosition, position) < 75.0f)
                    {
                        remove = false;
                        break;
                    }
                }

                if (remove && !m_markedForAddition.Contains(cell))
                {
                    if (RemoveCell(cell))
                    {
                        removed = true;
                        break;
                    }
                }
            }

            m_tmpCellSet.Clear();

            ProfilerShort.End();

            return removed;
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
                    VRageRender.MyRenderProxy.DebugDrawAABB(bb, Color.Orange, 1.0f, 1.0f, false);
                    //VRageRender.MyRenderProxy.DebugDrawText3D(bb.Center, cell.ToString(), Color.Orange, 0.5f, false);
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
        }
    }
}
