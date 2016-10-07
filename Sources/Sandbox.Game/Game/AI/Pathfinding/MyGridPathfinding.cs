using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyGridPathfinding
    {
        public struct CubeId
        {
            public MyCubeGrid Grid;
            public Vector3I Coords;

            public override bool Equals(object obj)
            {
                if (obj is CubeId)
                {
                    CubeId other = (CubeId)obj;
                    return other.Grid == this.Grid && other.Coords == this.Coords;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return Grid.GetHashCode() * 1610612741 + Coords.GetHashCode();
            }
        }

        private Dictionary<MyCubeGrid, MyGridNavigationMesh> m_navigationMeshes;

        private MyNavmeshCoordinator m_coordinator;
        private bool m_highLevelNavigationDirty;

        public MyGridPathfinding(MyNavmeshCoordinator coordinator)
        {
            m_navigationMeshes = new Dictionary<MyCubeGrid, MyGridNavigationMesh>();
            m_coordinator = coordinator;
            m_coordinator.SetGridPathfinding(this);

            m_highLevelNavigationDirty = false;
        }

        public void GridAdded(MyCubeGrid grid)
        {
            // CH: TODO: Don't add all grids immediately. E.g. copy-paste preview grids don't need to be added

            if (!GridCanHaveNavmesh(grid)) return;

            m_navigationMeshes.Add(grid, new MyGridNavigationMesh(grid, m_coordinator, 32, MyCestmirPathfindingShorts.Pathfinding.NextTimestampFunction));
            RegisterGridEvents(grid);
        }

        private void RegisterGridEvents(MyCubeGrid grid)
        {
            grid.OnClose += grid_OnClose;
        }

        public static bool GridCanHaveNavmesh(MyCubeGrid grid)
        {
            // CH: TODO: Disabling grid navmeshes in SE for now
            return MyPerGameSettings.Game == GameEnum.ME_GAME && grid.GridSizeEnum == MyCubeSize.Large;
        }

        void grid_OnClose(MyEntity entity)
        {
            var grid = entity as MyCubeGrid;
            if (grid == null) return;

            if (!GridCanHaveNavmesh(grid)) return;

            m_coordinator.RemoveGridNavmeshLinks(grid);
            m_navigationMeshes.Remove(grid);
        }

        public void Update()
        {
            if (m_highLevelNavigationDirty)
            {
                ProfilerShort.Begin("MyGridPathfinding.Update");

                foreach (var entry in m_navigationMeshes)
                {
                    MyGridNavigationMesh navMesh = entry.Value;
                    if (!navMesh.HighLevelDirty) continue;

                    navMesh.UpdateHighLevel();
                }

                m_highLevelNavigationDirty = false;

                ProfilerShort.End();
            }
        }

        public List<Vector4D> FindPathGlobal(MyCubeGrid startGrid, MyCubeGrid endGrid, ref Vector3D start, ref Vector3D end)
        {
            Debug.Assert(startGrid == endGrid, "Pathfinding between different grids not implemented yet!");
            if (startGrid != endGrid) return null;

            Vector3D tformedStart = Vector3D.Transform(start, startGrid.PositionComp.WorldMatrixInvScaled);
            Vector3D tformedEnd = Vector3D.Transform(end, endGrid.PositionComp.WorldMatrixInvScaled);

            MyGridNavigationMesh mesh = null;
            if (m_navigationMeshes.TryGetValue(startGrid, out mesh))
            {
                return mesh.FindPath(tformedStart, tformedEnd);
            }

            return null;
        }

        public MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistSq, MyCubeGrid grid = null)
        {
            if (highLevel == true) return null;

            MyNavigationPrimitive retval = null;
            if (grid != null)
            {
                MyGridNavigationMesh mesh = null;
                if (m_navigationMeshes.TryGetValue(grid, out mesh))
                {
                    retval = mesh.FindClosestPrimitive(point, highLevel, ref closestDistSq);
                }
            }
            else
            {
                foreach (var entry in m_navigationMeshes)
                {
                    MyNavigationPrimitive closest = entry.Value.FindClosestPrimitive(point, highLevel, ref closestDistSq);
                    if (closest != null)
                        retval = closest;
                }
            }

            return retval;
        }

        public void GetCubeTriangles(CubeId cubeId, List<MyNavigationTriangle> trianglesOut)
        {
            MyGridNavigationMesh gridMesh = null;
            Debug.Assert(m_navigationMeshes.TryGetValue(cubeId.Grid, out gridMesh), "Navigation mesh missing for a grid");
            if (gridMesh == null) return;

            gridMesh.GetCubeTriangles(cubeId.Coords, trianglesOut);
        }

        public MyGridNavigationMesh GetNavmesh(MyCubeGrid grid)
        {
            MyGridNavigationMesh retval = null;
            bool success = m_navigationMeshes.TryGetValue(grid, out retval);
            Debug.Assert(success, "Could not find the grid navigation mesh!");

            return retval;
        }

        public void MarkHighLevelDirty()
        {
            m_highLevelNavigationDirty = true;
        }

        [Conditional("DEBUG")]
        public void DebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW == false || MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES == MyWEMDebugDrawMode.NONE) return;

            foreach (var entry in m_navigationMeshes)
            {
                Matrix m = entry.Key.WorldMatrix;
                Matrix.Rescale(ref m, 2.5f);
                entry.Value.DebugDraw(ref m);
            }

            /*MyCubeBlockDefinition def = null;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "LargeBlockArmorBlock"), out def);
            if (def != null)
            {
                def.NavigationInfo.Mesh.DebugDraw();
            }*/
        }

        [Conditional("DEBUG")]
        public void RemoveTriangle(int index)
        {
            if (m_navigationMeshes.Count == 0) return;

            foreach (var mesh in m_navigationMeshes.Values)
            {
                mesh.RemoveFace(index);
            }
        }
    }
}
