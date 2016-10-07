using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyGridHighLevelHelper
    {
        private MyGridNavigationMesh m_mesh;
        private Vector3I m_cellSize;
        private ulong m_packedCoord;
        private int m_currentComponentRel;
        private List<List<int>> m_currentCellConnections;

        private MyVector3ISet m_changedCells;
        private MyVector3ISet m_changedCubes;

        private Dictionary<Vector3I, List<int>> m_triangleRegistry; // Belongs to the grid navigation mesh
        private MyNavmeshComponents m_components;

        // Stores triangles of all components for us to be able to set their component index. "null" values separate components (to avoid having to allocate more lists)
        private List<MyNavigationTriangle> m_tmpComponentTriangles = new List<MyNavigationTriangle>();
        private List<int> m_tmpNeighbors = new List<int>();

        private static HashSet<int> m_tmpCellTriangles = new HashSet<int>();
        private static MyGridHighLevelHelper m_currentHelper = null;

        private static readonly Vector3I CELL_COORD_SHIFT = new Vector3I(1 << 19);

        private Predicate<MyNavigationPrimitive> m_processTrianglePredicate = ProcessTriangleForHierarchyStatic;

        public bool IsDirty
        {
            get
            {
                return !m_changedCells.Empty;
            }
        }

        public MyGridHighLevelHelper(MyGridNavigationMesh mesh, Dictionary<Vector3I, List<int>> triangleRegistry, Vector3I cellSize)
        {
            m_mesh = mesh;
            m_cellSize = cellSize;
            m_packedCoord = 0;
            m_currentCellConnections = new List<List<int>>();

            m_changedCells = new MyVector3ISet();
            m_changedCubes = new MyVector3ISet();

            m_triangleRegistry = triangleRegistry;
            m_components = new MyNavmeshComponents();
        }

        // Actually, this function marks even cubes around the block to make sure that any changes caused in their triangles
        // will be reflected in the navigation mesh.
        public void MarkBlockChanged(MySlimBlock block)
        {
            Vector3I min = block.Min - Vector3I.One;
            Vector3I max = block.Max + Vector3I.One;

            Vector3I pos = min;
            for (var it = new Vector3I_RangeIterator(ref block.Min, ref block.Max); it.IsValid(); it.GetNext(out pos))
            {
                m_changedCubes.Add(pos);
            }

            Vector3I minCell = CubeToCell(ref min);
            Vector3I maxCell = CubeToCell(ref max);

            pos = minCell;
            for (var it = new Vector3I_RangeIterator(ref minCell, ref maxCell); it.IsValid(); it.GetNext(out pos))
            {
                m_changedCells.Add(pos);
            }
        }

        public void ProcessChangedCellComponents()
        {
            ProfilerShort.Begin("ProcessChangedCellComponents");

            m_currentHelper = this;

            Vector3I min, max, pos;
            List<int> triangles = null;
            foreach (var cell in m_changedCells)
            {
                min = CellToLowestCube(cell);
                max = min + m_cellSize - Vector3I.One;

                // Save a hashset of all the triangles in the current cell
                pos = min;
                for (var it = new Vector3I_RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out pos))
                {
                    if (!m_triangleRegistry.TryGetValue(pos, out triangles)) continue;

                    foreach (var triIndex in triangles)
                    {
                        m_tmpCellTriangles.Add(triIndex);
                    }
                }

                if (m_tmpCellTriangles.Count == 0) continue;

                MyCellCoord cellCoord = new MyCellCoord(0, cell);
                ulong packedCell = cellCoord.PackId64();
                m_components.OpenCell(packedCell);

                long timeBegin = m_mesh.GetCurrentTimestamp() + 1;
                long timeEnd = timeBegin;
                m_currentComponentRel = 0;

                m_tmpComponentTriangles.Clear();
                foreach (var triIndex in m_tmpCellTriangles)
                {
                    // Skip already visited triangles
                    var triangle = m_mesh.GetTriangle(triIndex);
                    if (m_currentComponentRel != 0 && m_mesh.VisitedBetween(triangle, timeBegin, timeEnd)) continue;

                    m_components.OpenComponent();

                    // Make sure we have place in m_currentCellConnections
                    if (m_currentComponentRel >= m_currentCellConnections.Count)
                    {
                        m_currentCellConnections.Add(new List<int>());
                    }

                    // Find connected component from an unvisited triangle and mark its connections
                    m_components.AddComponentTriangle(triangle, triangle.Center);
                    triangle.ComponentIndex = m_currentComponentRel;
                    m_tmpComponentTriangles.Add(triangle);
                    m_mesh.PrepareTraversal(triangle, null, m_processTrianglePredicate);
                    m_mesh.PerformTraversal();
                    m_tmpComponentTriangles.Add(null);

                    m_components.CloseComponent();

                    timeEnd = m_mesh.GetCurrentTimestamp();
                    if (m_currentComponentRel == 0)
                    {
                        timeBegin = timeEnd;
                    }
                    m_currentComponentRel++;
                }

                m_tmpCellTriangles.Clear();

                MyNavmeshComponents.ClosedCellInfo cellInfo = new MyNavmeshComponents.ClosedCellInfo();
                m_components.CloseAndCacheCell(ref cellInfo);

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
                        m_mesh.HighLevelGroup.AddPrimitive(cellInfo.StartingIndex + i, m_components.GetComponentCenter(i));
                    }
                }

                // Update existing component primitives
                if (!cellInfo.NewCell && cellInfo.ComponentNum == cellInfo.OldComponentNum)
                {
                    for (int i = 0; i < cellInfo.ComponentNum; ++i)
                    {
                        var primitive = m_mesh.HighLevelGroup.GetPrimitive(cellInfo.StartingIndex + i);
                        primitive.UpdatePosition(m_components.GetComponentCenter(i));
                    }
                }

                // Connect new components with the others in the neighboring cells
                for (int i = 0; i < cellInfo.ComponentNum; ++i)
                {
                    int compIndex = cellInfo.StartingIndex + i;

                    var primitive = m_mesh.HighLevelGroup.GetPrimitive(compIndex);
                    primitive.GetNeighbours(m_tmpNeighbors);

                    // Connect to disconnected components
                    foreach (var connection in m_currentCellConnections[i])
                    {
                        if (!m_tmpNeighbors.Remove(connection))
                        {
                            m_mesh.HighLevelGroup.ConnectPrimitives(compIndex, connection);
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

                // Set all the components as expanded
                for (int i = 0; i < cellInfo.ComponentNum; ++i)
                {
                    componentIndex = cellInfo.StartingIndex + i;
                    var component = m_mesh.HighLevelGroup.GetPrimitive(componentIndex);
                    if (component != null)
                    {
                        component.IsExpanded = true;
                    }
                }
            }

            m_changedCells.Clear();

            m_currentHelper = null;

            ProfilerShort.End();
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

            if (m_tmpCellTriangles.Contains(triangle.Index))
            {
                m_components.AddComponentTriangle(triangle, triangle.Center);
                m_tmpComponentTriangles.Add(triangle);
                return true;
            }
            else
            {
                ulong cellIndex;
                // This test succeeds only if the triangle belongs to an unchanged component or to a component that was changed,
                // but processed in a different cell already
                if (m_components.TryGetComponentCell(triangle.ComponentIndex, out cellIndex))
                {
                    // Save connections to other components. There won't be so many, so we can keep them in a list instead of a HashSet
                    if (!m_currentCellConnections[m_currentComponentRel].Contains(triangle.ComponentIndex))
                    {
                        m_currentCellConnections[m_currentComponentRel].Add(triangle.ComponentIndex);
                    }
                }
            }
            return false;
        }

        public MyHighLevelPrimitive GetHighLevelNavigationPrimitive(MyNavigationTriangle triangle)
        {
            Debug.Assert(triangle != null, "Navigation triangle was null!");
            if (triangle == null) return null;

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

        private void TryClearCell(ulong packedCoord)
        {
            MyNavmeshComponents.CellInfo cellInfo;
            if (!m_components.TryGetCell(packedCoord, out cellInfo))
            {
                return;
            }

            /*for (int i = 0; i < cellInfo.ComponentNum; ++i)
            {
                int componentIndex = cellInfo.StartingIndex + i;
                m_mesh.HighLevelGroup.RemovePrimitive(componentIndex);
            }*/

            m_components.ClearCell(packedCoord, ref cellInfo);
        }

        private Vector3I CubeToCell(ref Vector3I cube)
        {
            // We have 20 bits per coord for the cell coordinate. If we want signed coords, we have to convert to unsigned by
            // adding 2^19. The range will then be <0, 2^20-1> after shift and <-2^19, 2^19-1> before shift. Anything else will
            // be reported as overflow by MyCellCoord
            Vector3D cubeD = cube;
            cubeD = cubeD / m_cellSize;
            Vector3I retval;

            Vector3I.Floor(ref cubeD, out retval);

            retval += CELL_COORD_SHIFT;

            return retval;
        }

        private Vector3I CellToLowestCube(Vector3I cell)
        {
            return (cell - CELL_COORD_SHIFT) * m_cellSize;
        }
    }
}
