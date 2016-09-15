using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyVoxelHighLevelHelper
    {
        public class Component : IMyHighLevelComponent
        {
            private int m_componentIndex;
            private Base6Directions.DirectionFlags m_exploredDirections;

            public Component(int index, Base6Directions.DirectionFlags exploredDirections)
            {
                m_componentIndex = index;
                m_exploredDirections = exploredDirections;
            }

            bool IMyHighLevelComponent.Contains(MyNavigationPrimitive primitive)
            {
                MyNavigationTriangle tri = primitive as MyNavigationTriangle;
                if (tri == null) return false;

                return tri.ComponentIndex == m_componentIndex;
            }

            bool IMyHighLevelComponent.FullyExplored
            {
                get
                {
                    return m_exploredDirections == Base6Directions.DirectionFlags.All;
                }
            }
        }

        public struct ConnectionInfo
        {
            public int ComponentIndex;
            public Base6Directions.Direction Direction;
        }

        public static readonly bool DO_CONSISTENCY_CHECKS = true;

        private MyVoxelNavigationMesh m_mesh;

        // Fields for currently processed cell/component:
        private bool m_cellOpen = false;
        MyIntervalList m_triangleList;
        private int m_currentComponentRel; // Index of the current component relative to m_currentComponent
        private int m_currentComponentMarker; // Fake index of component to mark visited triangles of current component
        private Vector3I m_currentCell;
        private ulong m_packedCoord;
        private List<List<ConnectionInfo>> m_currentCellConnections;
        private static MyVoxelHighLevelHelper m_currentHelper;

        private Dictionary<ulong, MyIntervalList> m_triangleLists; // Maps the cell coordinates onto a list of present triangle indices
        private MyVector3ISet m_exploredCells;
        private MyNavmeshComponents m_navmeshComponents;

        private Predicate<MyNavigationPrimitive> m_processTrianglePredicate = ProcessTriangleForHierarchyStatic;

        // Stores triangles of all components for us to be able to set their component index. "null" values separate components (to avoid having to allocate more lists)
        private List<MyNavigationTriangle> m_tmpComponentTriangles = new List<MyNavigationTriangle>();
        private List<int> m_tmpNeighbors = new List<int>();

        private static List<ulong> m_removedHLpackedCoord = new List<ulong>();


        public MyVoxelHighLevelHelper(MyVoxelNavigationMesh mesh)
        {
            m_mesh = mesh;
            m_triangleList = new MyIntervalList();

            m_triangleLists = new Dictionary<ulong, MyIntervalList>();
            m_exploredCells = new MyVector3ISet();
            m_navmeshComponents = new MyNavmeshComponents();

            m_currentCellConnections = new List<List<ConnectionInfo>>();
            for (int i = 0; i < 8; ++i)
            {
                m_currentCellConnections.Add(new List<ConnectionInfo>());
            }
        }

        /// <summary>
        /// Begins processing a voxel geometry cell
        /// </summary>
        public void OpenNewCell(MyCellCoord coord)
        {
            Debug.Assert(m_cellOpen == false, "Cannot open a new cell in MyVoxelHighLevelHelper while another one is open!");

            m_cellOpen = true;
            m_currentCell = coord.CoordInLod;
            m_packedCoord = coord.PackId64();
            m_triangleList.Clear();
        }

        public void AddTriangle(int triIndex)
        {
            m_triangleList.Add(triIndex);
        }

        /// <summary>
        /// Ends processing the currently open cell
        /// </summary>
        public void CloseCell()
        {
            Debug.Assert(m_cellOpen == true, "Cannot close cell in MyVoxelHighLevelHelper, because it's not open!");

            m_cellOpen = false;
            m_packedCoord = 0;
            m_triangleList.Clear();
        }

        public void ProcessCellComponents()
        {
            ProfilerShort.Begin("ProcessCellComponents");
            m_triangleLists.Add(m_packedCoord, m_triangleList.GetCopy());

            // Find the components by traversing the graph
            MyNavmeshComponents.ClosedCellInfo cellInfo = ConstructComponents();

            // Assign correct component indices to the new components and their triangles
            // Then, remove or create the respective high-level primitives, if the components changed since last time
            // Then, update connections, if the components changed since last time
            UpdateHighLevelPrimitives(ref cellInfo);

            // Mark explored directions in the navmesh component helper
            MarkExploredDirections(ref cellInfo);

            // Set all the components as expanded
            for (int i = 0; i < cellInfo.ComponentNum; ++i)
            {
                int componentIndex = cellInfo.StartingIndex + i;
                var component = m_mesh.HighLevelGroup.GetPrimitive(componentIndex);
                if (component != null) component.IsExpanded = true;
            }

            ProfilerShort.End();
        }

        private void MarkExploredDirections(ref MyNavmeshComponents.ClosedCellInfo cellInfo)
        {
            foreach (var direction in Base6Directions.EnumDirections)
            {
                var dirFlag = Base6Directions.GetDirectionFlag(direction);
                if (cellInfo.ExploredDirections.HasFlag(dirFlag))
                {
                    continue;
                }

                Vector3I dirVec = Base6Directions.GetIntVector(direction);

                MyCellCoord otherCoord = new MyCellCoord();
                otherCoord.Lod = MyVoxelNavigationMesh.NAVMESH_LOD;
                otherCoord.CoordInLod = m_currentCell + dirVec;
                if (otherCoord.CoordInLod.X == -1 || otherCoord.CoordInLod.Y == -1 || otherCoord.CoordInLod.Z == -1)
                {
                    continue;
                }

                ulong otherPackedCoord = otherCoord.PackId64();

                if (m_triangleLists.ContainsKey(otherPackedCoord))
                {
                    m_navmeshComponents.MarkExplored(otherPackedCoord, Base6Directions.GetFlippedDirection(direction));
                    cellInfo.ExploredDirections |= Base6Directions.GetDirectionFlag(direction);
                }
            }
            m_navmeshComponents.SetExplored(m_packedCoord, cellInfo.ExploredDirections);
        }

        private void UpdateHighLevelPrimitives(ref MyNavmeshComponents.ClosedCellInfo cellInfo)
        {
            ProfilerShort.Begin("UpdateHighLevelPrimitives");

            // Renumber triangles from the old indices to the newly assigned index from m_components
            int componentIndex = cellInfo.StartingIndex;
            foreach (var triangle in m_tmpComponentTriangles)
            {
                if (triangle == null)
                {
                    componentIndex++;
                    continue;
                }
                triangle.ComponentIndex = componentIndex;
            }
            m_tmpComponentTriangles.Clear();

            // Remove old component primitives
            if (!cellInfo.NewCell && cellInfo.ComponentNum != cellInfo.OldComponentNum)
            {
                for (int i = 0; i < cellInfo.OldComponentNum; ++i)
                {
                    m_mesh.HighLevelGroup.RemovePrimitive(cellInfo.OldStartingIndex + i);
                }
            }
            
            // Add new component primitives
            if (cellInfo.NewCell || cellInfo.ComponentNum != cellInfo.OldComponentNum)
            {
                for (int i = 0; i < cellInfo.ComponentNum; ++i)
                {
                    m_mesh.HighLevelGroup.AddPrimitive(cellInfo.StartingIndex + i, m_navmeshComponents.GetComponentCenter(i));
                }
            }

            // Update existing component primitives
            if (!cellInfo.NewCell && cellInfo.ComponentNum == cellInfo.OldComponentNum)
            {
                for (int i = 0; i < cellInfo.ComponentNum; ++i)
                {
                    var primitive = m_mesh.HighLevelGroup.GetPrimitive(cellInfo.StartingIndex + i);
                    primitive.UpdatePosition(m_navmeshComponents.GetComponentCenter(i));
                }
            }
            
            // Connect new components with the others in the neighboring cells
            for (int i = 0; i < cellInfo.ComponentNum; ++i)
            {
                int compIndex = cellInfo.StartingIndex + i;

                var primitive = m_mesh.HighLevelGroup.GetPrimitive(compIndex);
                primitive.GetNeighbours(m_tmpNeighbors);

                // Connect to disconnected components
                foreach (var connectionInfo in m_currentCellConnections[i])
                {
                    if (!m_tmpNeighbors.Remove(connectionInfo.ComponentIndex))
                    {
                        m_mesh.HighLevelGroup.ConnectPrimitives(compIndex, connectionInfo.ComponentIndex);
                    }
                }

                // Disconnect neighbors that should be no longer connected
                foreach (var neighbor in m_tmpNeighbors)
                {
                    // Only disconnect from the other cell if it is expanded and there was no connection found
                    var neighborPrimitive = m_mesh.HighLevelGroup.TryGetPrimitive(neighbor);
                    if (neighborPrimitive != null && neighborPrimitive.IsExpanded)
                    {
                        m_mesh.HighLevelGroup.DisconnectPrimitives(compIndex, neighbor);
                    }
                }

                m_tmpNeighbors.Clear();
                m_currentCellConnections[i].Clear();
            }

            ProfilerShort.End();
        }

        private MyNavmeshComponents.ClosedCellInfo ConstructComponents()
        {
            ProfilerShort.Begin("ConstructComponents");

            long timeBegin = m_mesh.GetCurrentTimestamp() + 1;
            long timeEnd = timeBegin;

            m_currentComponentRel = 0;
            m_navmeshComponents.OpenCell(m_packedCoord);

            m_tmpComponentTriangles.Clear();
            foreach (var triIndex in m_triangleList)
            {
                // The marker is used as a fake component index in triangles to mark visited triangles.
                // Negative numbers from -2 down are used to avoid collisions with existing component numbers (0, 1, 2, ...) or the special value -1
                m_currentComponentMarker = -2 - m_currentComponentRel;

                // Skip already visited triangles
                var triangle = m_mesh.GetTriangle(triIndex);
                if (m_mesh.VisitedBetween(triangle, timeBegin, timeEnd))
                {
                    continue;
                }

                m_navmeshComponents.OpenComponent();

                // Make sure we have place in m_currentCellConnections
                if (m_currentComponentRel >= m_currentCellConnections.Count)
                {
                    m_currentCellConnections.Add(new List<ConnectionInfo>());
                }

                // Find connected component from an unvisited triangle
                ProfilerShort.Begin("Graph traversal");
                m_currentHelper = this;

                m_navmeshComponents.AddComponentTriangle(triangle, triangle.Center);
                triangle.ComponentIndex = m_currentComponentMarker;
                m_tmpComponentTriangles.Add(triangle);

                m_mesh.PrepareTraversal(triangle, null, m_processTrianglePredicate);
                m_mesh.PerformTraversal();
                ProfilerShort.End();

                m_tmpComponentTriangles.Add(null); // Mark end of component in m_tmpComponentTriangles
                m_navmeshComponents.CloseComponent();

                timeEnd = m_mesh.GetCurrentTimestamp();
                m_currentComponentRel++;
            }

            MyNavmeshComponents.ClosedCellInfo cellInfo = new MyNavmeshComponents.ClosedCellInfo();
            m_navmeshComponents.CloseAndCacheCell(ref cellInfo);

            ProfilerShort.End();
            return cellInfo;
        }

        public MyIntervalList TryGetTriangleList(ulong packedCellCoord)
        {
            MyIntervalList retval = null;
            m_triangleLists.TryGetValue(packedCellCoord, out retval);
            return retval;
        }

        public void CollectComponents(ulong packedCoord, List<int> output)
        {
            MyNavmeshComponents.CellInfo cellInfo = new MyNavmeshComponents.CellInfo();
            if (m_navmeshComponents.TryGetCell(packedCoord, out cellInfo))
            {
                for (int i = 0; i < cellInfo.ComponentNum; ++i)
                {
                    output.Add(cellInfo.StartingIndex + i);
                }
            }
        }

        public IMyHighLevelComponent GetComponent(MyHighLevelPrimitive primitive)
        {
            ulong cellIndex;
            if (m_navmeshComponents.GetComponentCell(primitive.Index, out cellIndex))
            {
                Base6Directions.DirectionFlags exploredDirections;
                
                if (m_navmeshComponents.GetComponentInfo(primitive.Index, cellIndex, out exploredDirections))
                {
                    MyCellCoord coord = new MyCellCoord();
                    coord.SetUnpack(cellIndex);

                    // Look at present unexplored cells around this cell.
                    // Their direction can be marked as explored, because there was no geometry when they were being explored
                    foreach (var direction in Base6Directions.EnumDirections)
                    {
                        var directionFlag = Base6Directions.GetDirectionFlag(direction);
                        if (exploredDirections.HasFlag(directionFlag))
                        {
                            continue;
                        }

                        Vector3I neighbor = coord.CoordInLod + Base6Directions.GetIntVector(direction);
                        if (m_exploredCells.Contains(ref neighbor))
                        {
                            exploredDirections |= directionFlag;
                        }
                    }

                    return new Component(primitive.Index, exploredDirections);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void ClearCachedCell(ulong packedCoord)
        {
            Debug.Assert(m_triangleLists.ContainsKey(packedCoord));
            m_triangleLists.Remove(packedCoord);

            MyNavmeshComponents.CellInfo cellInfo;
            bool success = m_navmeshComponents.TryGetCell(packedCoord, out cellInfo);
            Debug.Assert(success, "Could not get the cell!");
            if (success == false) return;

            for (int i = 0; i < cellInfo.ComponentNum; ++i)
            {
                int componentIndex = cellInfo.StartingIndex + i;
                var component = m_mesh.HighLevelGroup.GetPrimitive(componentIndex);
                if (component != null)
                {
                    component.IsExpanded = false;
                }
            }
        }

        public void TryClearCell(ulong packedCoord)
        {
            if (m_triangleLists.ContainsKey(packedCoord))
            {
                ClearCachedCell(packedCoord);
            }

            RemoveExplored(packedCoord);

            MyNavmeshComponents.CellInfo cellInfo;
            if (!m_navmeshComponents.TryGetCell(packedCoord, out cellInfo))
            {
                return;
            }

            for (int i = 0; i < cellInfo.ComponentNum; ++i)
            {
                int componentIndex = cellInfo.StartingIndex + i;
                m_mesh.HighLevelGroup.RemovePrimitive(componentIndex);
            }

            foreach (var direction in Base6Directions.EnumDirections)
            {
                Base6Directions.DirectionFlags dirFlag = Base6Directions.GetDirectionFlag(direction);
                if (cellInfo.ExploredDirections.HasFlag(dirFlag))
                {
                    Vector3I dirVec = Base6Directions.GetIntVector(direction);

                    MyCellCoord otherCoord = new MyCellCoord();
                    otherCoord.SetUnpack(packedCoord);
                    Debug.Assert(otherCoord.Lod == MyVoxelNavigationMesh.NAVMESH_LOD);
                    otherCoord.CoordInLod = otherCoord.CoordInLod + dirVec;

                    MyNavmeshComponents.CellInfo otherCellInfo;

                    if (m_navmeshComponents.TryGetCell(otherCoord.PackId64(), out otherCellInfo))
                    {
                        Base6Directions.DirectionFlags flippedFlag = Base6Directions.GetDirectionFlag(Base6Directions.GetFlippedDirection(direction));
                        m_navmeshComponents.SetExplored(otherCoord.PackId64(), otherCellInfo.ExploredDirections & ~flippedFlag);
                    }
                    else
                    {
                        Debug.Assert(false, "Could not get the oposite explored cell!");
                    }
                }
            }

            m_navmeshComponents.ClearCell(packedCoord, ref cellInfo);
        }

        public MyHighLevelPrimitive GetHighLevelNavigationPrimitive(MyNavigationTriangle triangle)
        {
            Debug.Assert(triangle.Parent == this.m_mesh, "Finding cell of a navigation triangle in a wrong mesh!");
            if (triangle.Parent != this.m_mesh)
            {
                return null;
            }

            if (triangle.ComponentIndex != -1)
            {
                return m_mesh.HighLevelGroup.GetPrimitive(triangle.ComponentIndex);
            }
            else
            {
                return null;
            }
        }

        public void AddExplored(ref Vector3I cellPos)
        {
            m_exploredCells.Add(ref cellPos);
        }

        private void RemoveExplored(ulong packedCoord)
        {
            MyCellCoord coord = new MyCellCoord();
            coord.SetUnpack(packedCoord);

            m_exploredCells.Remove(ref coord.CoordInLod);
        }

        private static bool ProcessTriangleForHierarchyStatic(MyNavigationPrimitive primitive)
        {
            ProfilerShort.Begin("ProcessTriangleForHierarchy");
            var triangle = primitive as MyNavigationTriangle;
            bool retval = m_currentHelper.ProcessTriangleForHierarchy(triangle);
            ProfilerShort.End();

            return retval;
        }

        private bool ProcessTriangleForHierarchy(MyNavigationTriangle triangle)
        {
            // The triangle parent can be wrong when we have multiple navmeshes connected via external edges
            if (triangle.Parent != m_mesh)
            {
                return false;
            }

            // Previously unvisited triangle will be assigned the current relative component index
            if (triangle.ComponentIndex == -1)
            {
                m_navmeshComponents.AddComponentTriangle(triangle, triangle.Center);
                m_tmpComponentTriangles.Add(triangle);
                triangle.ComponentIndex = m_currentComponentMarker;
                return true;
            }
            else if (triangle.ComponentIndex == m_currentComponentMarker)
            {
                // We can safely ignore this triangle (it has already been processed);
                return true;
            }
            else
            {
                ulong cellIndex;
                if (m_navmeshComponents.GetComponentCell(triangle.ComponentIndex, out cellIndex))
                {
                    MyCellCoord cellCoord = new MyCellCoord();
                    cellCoord.SetUnpack(cellIndex);
                    Vector3I diff = cellCoord.CoordInLod - m_currentCell;
                    if (diff.RectangularLength() != 1)
                    {
                        // CH: TODO: Connection of components over cell edges or vertices. I currently silently ignore that...
                        return false;
                    }

                    ConnectionInfo connection = new ConnectionInfo();
                    connection.Direction = Base6Directions.GetDirection(diff);
                    connection.ComponentIndex = triangle.ComponentIndex;

                    // Save connections to other components. There won't be so many, so we can keep them in a list instead of a HashSet
                    if (!m_currentCellConnections[m_currentComponentRel].Contains(connection))
                    {
                        m_currentCellConnections[m_currentComponentRel].Add(connection);
                    }
                }
            }
            return false;
        }

        [Conditional("DEBUG")]
        public void CheckConsistency()
        {
            if (!DO_CONSISTENCY_CHECKS) return;

            MyCellCoord cellCoord = new MyCellCoord();
            foreach (var pair in m_triangleLists)
            {
                cellCoord.SetUnpack(pair.Key);
                Debug.Assert(m_exploredCells.Contains(ref cellCoord.CoordInLod), "Cell in triangle lists, but not explored!");
            }
        }

        public void DebugDraw()
        {
            if (MyFakes.DEBUG_DRAW_NAVMESH_EXPLORED_HL_CELLS)
            {
                foreach (var cell in m_exploredCells)
                {
                    BoundingBoxD cellAABB;
                    Vector3I cellCopy = cell;

                    MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(m_mesh.VoxelMapReferencePosition, ref cellCopy, out cellAABB);

                    VRageRender.MyRenderProxy.DebugDrawAABB(cellAABB, Color.Sienna, 1.0f, 1.0f, false);
                }
            }

            if (MyFakes.DEBUG_DRAW_NAVMESH_FRINGE_HL_CELLS)
            {
                foreach (var packedCoord in m_navmeshComponents.GetPresentCells())
                {
                    MyCellCoord coord = new MyCellCoord();
                    coord.SetUnpack(packedCoord);
                    Vector3I cellCoord = coord.CoordInLod;

                    if (m_exploredCells.Contains(ref cellCoord))
                    {
                        MyNavmeshComponents.CellInfo cellInfo = new MyNavmeshComponents.CellInfo();
                        if (m_navmeshComponents.TryGetCell(packedCoord, out cellInfo))
                        {
                            for (int i = 0; i < cellInfo.ComponentNum; ++i)
                            {
                                int componentIndex = cellInfo.StartingIndex + i;
                                var primitive = m_mesh.HighLevelGroup.GetPrimitive(componentIndex);
                                foreach (var direction in Base6Directions.EnumDirections)
                                {
                                    var dirFlag = Base6Directions.GetDirectionFlag(direction);
                                    if (cellInfo.ExploredDirections.HasFlag(dirFlag))
                                    {
                                        continue;
                                    }

                                    if (m_exploredCells.Contains(cellCoord + Base6Directions.GetIntVector(direction)))
                                    {
                                        continue;
                                    }

                                    Vector3 dirVec = Base6Directions.GetVector(direction);
                                    VRageRender.MyRenderProxy.DebugDrawLine3D(primitive.WorldPosition, primitive.WorldPosition + dirVec * 3.0f, Color.Red, Color.Red, false);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RemoveTooFarCells(List<Vector3D> importantPositions, float maxDistance, MyVector3ISet processedCells)
        {
            // remove too far high level info (if it isn't in processed cells)
            m_removedHLpackedCoord.Clear();
            foreach (var cell in m_exploredCells)
            {
                Vector3D worldCellCenterPos;
                Vector3I cellPos = cell;
                MyVoxelCoordSystems.GeometryCellCenterCoordToWorldPos(m_mesh.VoxelMapReferencePosition, ref cellPos, out worldCellCenterPos);

                // finding of distance from the nearest important object
                float dist = float.PositiveInfinity;
                foreach (Vector3D vec in importantPositions)
                {
                    float d = Vector3.RectangularDistance(vec, worldCellCenterPos);
                    if (d < dist)
                        dist = d;
                }

                if (dist > maxDistance && !processedCells.Contains(cellPos))
                {
                    MyCellCoord coord = new MyCellCoord(MyVoxelNavigationMesh.NAVMESH_LOD, cellPos);
                    m_removedHLpackedCoord.Add(coord.PackId64());
                }
            }
            foreach(ulong coord in m_removedHLpackedCoord)
            {
                TryClearCell(coord);
            }
        }

        public void GetCellsOfPrimitives(ref HashSet<ulong> cells, ref List<MyHighLevelPrimitive> primitives)
        {
            ulong cellIndex;
            foreach (MyHighLevelPrimitive primitive in primitives)
            {
                if (m_navmeshComponents.GetComponentCell(primitive.Index, out cellIndex))
                {
                    cells.Add(cellIndex);
                }
            }
        }
    }
}
