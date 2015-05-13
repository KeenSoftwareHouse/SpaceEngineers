using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
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

        private MyVoxelNavigationMesh m_mesh;

        // Fields for currently processed cell/component:
        private bool m_cellOpen = false;
        MyIntervalList m_triangleList;
        private int m_currentComponent;
        private int m_currentComponentRel; // Index of the current component relative to m_currentComponent
        private Vector3I m_currentCell;
        private ulong m_packedCoord;
        private List<List<ConnectionInfo>> m_currentCellConnections;
        private static MyVoxelHighLevelHelper m_currentHelper;

        private Dictionary<ulong, MyIntervalList> m_triangleLists; // Maps the cell coordinates onto a list of present triangle indices
        private MyVector3ISet m_exploredCells;
        private MyNavmeshComponents m_navmeshComponents;

        private Predicate<MyNavigationPrimitive> m_processTrianglePredicate = ProcessTriangleForHierarchyStatic;

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

            long timeBegin = m_mesh.GetCurrentTimestamp() + 1;
            long timeEnd = timeBegin;

            m_currentComponentRel = 0;
            m_currentComponent = m_navmeshComponents.OpenCell(m_packedCoord);

            foreach (var triIndex in m_triangleList)
            {
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
                triangle.ComponentIndex = m_navmeshComponents.OpenComponentIndex;
                m_mesh.PrepareTraversal(triangle, null, m_processTrianglePredicate);

                var primitiveEnum = m_mesh.GetEnumerator();
                while (primitiveEnum.MoveNext());
                primitiveEnum.Dispose();
                ProfilerShort.End();

                m_navmeshComponents.CloseComponent();

                timeEnd = m_mesh.GetCurrentTimestamp();
                m_currentComponentRel++;
            }

            MyNavmeshComponents.ClosedCellInfo cellInfo = new MyNavmeshComponents.ClosedCellInfo();
            m_navmeshComponents.CloseAndCacheCell(ref cellInfo);

            // Add new component primitives 
            if (cellInfo.NewCell)
            {
                for (int i = 0; i < cellInfo.ComponentNum; ++i)
                {
                    m_mesh.HighLevelGroup.AddPrimitive(cellInfo.StartingIndex + i, m_navmeshComponents.GetComponentCenter(i));
                }
            }
            
            // Connect new components with the others in the neighboring cells
            for (int i = 0; i < cellInfo.ComponentNum; ++i)
            {
                foreach (var connectionInfo in m_currentCellConnections[i])
                {
                    if (!cellInfo.ExploredDirections.HasFlag(Base6Directions.GetDirectionFlag(connectionInfo.Direction)))
                    {
                        m_mesh.HighLevelGroup.ConnectPrimitives(cellInfo.StartingIndex + i, connectionInfo.ComponentIndex);
                    }
                }
                m_currentCellConnections[i].Clear();
            }

            // Mark explored directions in the navmesh component helper
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

            // Set all the components as expanded
            for (int i = 0; i < cellInfo.ComponentNum; ++i)
            {
                int componentIndex = cellInfo.StartingIndex + i;
                var component = m_mesh.HighLevelGroup.GetPrimitive(componentIndex);
                if (component != null)
                {
                    component.IsExpanded = true;
                }
            }

            ProfilerShort.End();
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

            if (triangle.ComponentIndex == -1)
            {
                m_navmeshComponents.AddComponentTriangle(triangle, triangle.Center);
                triangle.ComponentIndex = m_navmeshComponents.OpenComponentIndex;
                return true;
            }
            else if (triangle.ComponentIndex == m_navmeshComponents.OpenComponentIndex)
            {
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
    }
}
