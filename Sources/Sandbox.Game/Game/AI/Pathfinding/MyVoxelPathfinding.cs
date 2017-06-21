using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyVoxelPathfinding
    {
        public struct CellId: IEquatable<CellId>
        {
            public MyVoxelBase VoxelMap;
            public Vector3I Pos;

            public override bool Equals(object obj)
            {
                Debug.Assert(false, "Equals on struct does allocation!");
                if (ReferenceEquals(null, obj)) return false;
                if (obj.GetType() != typeof(CellId)) return false;

                return this.Equals((CellId)obj);
            }

            public override int GetHashCode()
            {
                return VoxelMap.GetHashCode() * 1610612741 + Pos.GetHashCode();
            }

            public bool Equals(CellId other)
            {
                return VoxelMap == other.VoxelMap && Pos == other.Pos;
            }
        }

        private int m_updateCtr;
        private const int UPDATE_PERIOD = 5;

        private Dictionary<MyVoxelBase, MyVoxelNavigationMesh> m_navigationMeshes;

        private List<Vector3D> m_tmpUpdatePositions;
        private List<MyVoxelBase> m_tmpVoxelMaps;
        private List<MyVoxelNavigationMesh> m_tmpNavmeshes;

        private MyNavmeshCoordinator m_coordinator;

        public MyVoxelPathfindingLog DebugLog;

        private static float MESH_DIST = 40.0f;

        public MyVoxelPathfinding(MyNavmeshCoordinator coordinator)
        {
            MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;

            m_navigationMeshes = new Dictionary<MyVoxelBase, MyVoxelNavigationMesh>();
            m_tmpUpdatePositions = new List<Vector3D>(8);
            m_tmpVoxelMaps = new List<MyVoxelBase>();
            m_tmpNavmeshes = new List<MyVoxelNavigationMesh>();
            m_coordinator = coordinator;
            coordinator.SetVoxelPathfinding(this);

            if (MyFakes.REPLAY_NAVMESH_GENERATION || MyFakes.LOG_NAVMESH_GENERATION)
            {
                DebugLog = new MyVoxelPathfindingLog("PathfindingLog.log");
            }
        }

        private void MyEntities_OnEntityAdd(MyEntity entity)
        {
            var voxelMap = entity as MyVoxelBase;
            if (voxelMap == null) return;
            if (MyPerGameSettings.Game == GameEnum.SE_GAME && !(voxelMap is MyPlanet)) return;

            m_navigationMeshes.Add(voxelMap, new MyVoxelNavigationMesh(voxelMap, m_coordinator, MyCestmirPathfindingShorts.Pathfinding.NextTimestampFunction));
            RegisterVoxelMapEvents(voxelMap);
        }

        private void RegisterVoxelMapEvents(MyVoxelBase voxelMap)
        {
            voxelMap.OnClose += voxelMap_OnClose;
        }

        private void voxelMap_OnClose(MyEntity entity)
        {
            var voxelMap = entity as MyVoxelBase;
            if (voxelMap == null) return;
            if (MyPerGameSettings.Game == GameEnum.SE_GAME && !(voxelMap is MyPlanet)) return;

            m_navigationMeshes.Remove(voxelMap);
        }

        public void UnloadData()
        {
            if (DebugLog != null)
            {
                DebugLog.Close();
                DebugLog = null;
            }
            MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;
        }

        public void Update()
        {
            ProfilerShort.Begin("MyVoxelPathfinding.Update");

            m_updateCtr++;
            int modulo = m_updateCtr % 6;

            if (modulo == 0 || modulo == 2 || modulo == 4)
            {
                if (MyFakes.DEBUG_ONE_VOXEL_PATHFINDING_STEP_SETTING)
                {
                    if (!MyFakes.DEBUG_ONE_VOXEL_PATHFINDING_STEP)
                        // voxel pathfinding step isn't allowed
                        return;
                    else
                        // disable next voxel pathfinding step - and do one
                        MyFakes.DEBUG_ONE_VOXEL_PATHFINDING_STEP = false;
                }
            }

            if (MyFakes.REPLAY_NAVMESH_GENERATION)
            {
                DebugLog.PerformOneOperation(MyFakes.REPLAY_NAVMESH_GENERATION_TRIGGER);
                MyFakes.REPLAY_NAVMESH_GENERATION_TRIGGER = false;
                ProfilerShort.End();
                return;
            }

            switch (modulo)
            {
                case 0:
                    GetUpdatePositions();
                    PerformCellMarking(m_tmpUpdatePositions);
                    PerformCellUpdates();
                    m_tmpUpdatePositions.Clear();
                    break;
                case 2:
                    GetUpdatePositions();
                    PerformCellMarking(m_tmpUpdatePositions);
                    PerformCellAdditions(m_tmpUpdatePositions);
                    m_tmpUpdatePositions.Clear();
                    break;
                case 4:
                    GetUpdatePositions();
                    PerformCellRemovals(m_tmpUpdatePositions);
                    RemoveFarHighLevelGroups(m_tmpUpdatePositions);
                    m_tmpUpdatePositions.Clear();
                    break;
            }
            ProfilerShort.End();
        }

        private void GetUpdatePositions()
        {
            m_tmpUpdatePositions.Clear();

            var players = Sync.Players.GetOnlinePlayers();
            foreach (var player in players)
            {
                var controlledEntity = player.Controller.ControlledEntity;
                if (controlledEntity == null) continue;

                m_tmpUpdatePositions.Add(controlledEntity.Entity.PositionComp.GetPosition());
            }
        }

        private void PerformCellRemovals(List<Vector3D> updatePositions)
        {
            ProfilerShort.Begin("Cell removals");
            ShuffleMeshes();
            foreach (var mesh in m_tmpNavmeshes)
            {
                if (mesh.RemoveOneUnusedCell(updatePositions))
                {
                    // Break after the first removed cell
                    break;
                }
            }
            m_tmpNavmeshes.Clear();
            ProfilerShort.End();
        }

        private void RemoveFarHighLevelGroups(List<Vector3D> updatePositions)
        {
            ProfilerShort.Begin("Far Cells removals");
            foreach (var mesh in m_navigationMeshes)
            {
                mesh.Value.RemoveFarHighLevelGroups(updatePositions);
            }

            ProfilerShort.End();
        }

        private void PerformCellAdditions(List<Vector3D> updatePositions)
        {
            ProfilerShort.Begin("Cell additions");
            MarkCellsOnPaths(); // it should be done before adding of cell - priority of adding is influenced by presence on path
            ShuffleMeshes();
            foreach (var mesh in m_tmpNavmeshes)
            {
                if (mesh.AddOneMarkedCell(updatePositions))
                {
                    // Break after the first added cell
                    break;
                }
            }
            m_tmpNavmeshes.Clear();
            ProfilerShort.End();
        }

        private void PerformCellUpdates()
        {
            ProfilerShort.Begin("Cell updates");
            ShuffleMeshes();
            foreach (var mesh in m_tmpNavmeshes)
            {
                if (mesh.RefreshOneChangedCell())
                {
                    break;
                }
            }
            m_tmpNavmeshes.Clear();
            ProfilerShort.End();
        }

        private void ShuffleMeshes()
        {
            m_tmpNavmeshes.Clear();
            foreach (var mesh in m_navigationMeshes)
            {
                m_tmpNavmeshes.Add(mesh.Value);
            }
            m_tmpNavmeshes.ShuffleList();
        }

        private void PerformCellMarking(List<Vector3D> updatePositions)
        {
            ProfilerShort.Begin("Cell marking");
            Vector3D offset = new Vector3D(1.0f);
            foreach (var pos in updatePositions)
            {
                BoundingBoxD box = new BoundingBoxD(pos - offset, pos + offset);

                ProfilerShort.Begin("GetVoxelMaps");
                m_tmpVoxelMaps.Clear();
                MyGamePruningStructure.GetAllVoxelMapsInBox(ref box, m_tmpVoxelMaps);
                ProfilerShort.End();

                foreach (var map in m_tmpVoxelMaps)
                {
                    MyVoxelNavigationMesh mesh = null;
                    m_navigationMeshes.TryGetValue(map, out mesh);
                    if (mesh == null) continue;

                    mesh.MarkBoxForAddition(box);
                }
            }
            m_tmpVoxelMaps.Clear();
            ProfilerShort.End();
        }

        private void MarkCellsOnPaths()
        {
            // walk through high level components that are observed - they have bigger priority - they are on the way
            foreach (var mesh in m_navigationMeshes)
            {
                mesh.Value.MarkCellsOnPaths();
            }
        }

        public void InvalidateBox(ref BoundingBoxD bbox)
        {
            foreach (var entry in m_navigationMeshes)
            {
                Vector3I min, max;
                if (!entry.Key.GetContainedVoxelCoords(ref bbox, out min, out max))
                {
                    continue;
                }

                entry.Value.InvalidateRange(min, max);
            }
        }

        public MyVoxelNavigationMesh GetVoxelMapNavmesh(MyVoxelBase map)
        {
            MyVoxelNavigationMesh retval = null;
            m_navigationMeshes.TryGetValue(map, out retval);
            return retval;
        }

        public MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistanceSq, MyVoxelBase voxelMap = null)
        {
            MyNavigationPrimitive retval = null;
            if (voxelMap != null)
            {
                MyVoxelNavigationMesh mesh = null;
                if (m_navigationMeshes.TryGetValue(voxelMap, out mesh))
                {
                    retval = mesh.FindClosestPrimitive(point, highLevel, ref closestDistanceSq);
                }
            }
            else
            {
                foreach (var entry in m_navigationMeshes)
                {
                    MyNavigationPrimitive closest = entry.Value.FindClosestPrimitive(point, highLevel, ref closestDistanceSq);
                    if (closest != null)
                        retval = closest;
                }
            }

            return retval;
        }

        [Conditional("DEBUG")]
        public void DebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW == false) return;

            if (DebugLog != null) DebugLog.DebugDraw();

            foreach (var entry in m_navigationMeshes)
            {
                Matrix drawMatrix = entry.Key.WorldMatrix;

                entry.Value.DebugDraw(ref drawMatrix);
            }
        }

        [Conditional("DEBUG")]
        public void RemoveTriangle(int index)
        {
            if (m_navigationMeshes.Count == 0) return;

            foreach (var mesh in m_navigationMeshes.Values)
            {
                mesh.RemoveTriangle(index);
            }
        }
    }
}

