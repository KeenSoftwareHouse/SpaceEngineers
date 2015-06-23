using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyVoxelPathfinding
    {
        public struct CellId
        {
            public MyVoxelBase VoxelMap;
            public Vector3I Pos;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (obj.GetType() != typeof(CellId)) return false;

                CellId other = (CellId)obj;
                return VoxelMap == other.VoxelMap && Pos == other.Pos;
            }

            public override int GetHashCode()
            {
                return VoxelMap.GetHashCode() * 1610612741 + Pos.GetHashCode();
            }
        }

        private int m_updateCtr;
        private const int UPDATE_PERIOD = 5;

        private Dictionary<MyVoxelBase, MyVoxelNavigationMesh> m_navigationMeshes;

        private List<Vector3D> m_tmpUpdatePositions;
        private List<MyVoxelBase> m_tmpVoxelMaps;
        private List<MyVoxelNavigationMesh> m_tmpNavmeshes;

        private MyNavmeshCoordinator m_coordinator;

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
        }

        private void MyEntities_OnEntityAdd(MyEntity entity)
        {
            var voxelMap = entity as MyVoxelMap;
            if (voxelMap == null) return;

            m_navigationMeshes.Add(voxelMap, new MyVoxelNavigationMesh(voxelMap, m_coordinator, MyAIComponent.Static.Pathfinding.NextTimestampFunction));
            RegisterVoxelMapEvents(voxelMap);
        }

        private void RegisterVoxelMapEvents(MyVoxelMap voxelMap)
        {
            voxelMap.OnClose += voxelMap_OnClose;
        }

        private void voxelMap_OnClose(MyEntity entity)
        {
            var voxelMap = entity as MyVoxelMap;
            if (voxelMap == null) return;

            m_navigationMeshes.Remove(voxelMap);
        }

        public void UnloadData()
        {
            MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;
        }

        public void Update()
        {
            ProfilerShort.Begin("MyVoxelPathfinding.Update");

            if (++m_updateCtr >= UPDATE_PERIOD)
            {
                m_tmpUpdatePositions.Clear();
                m_updateCtr = 0;

                var players = Sync.Players.GetOnlinePlayers();
                foreach (var player in players)
                {
                    var controlledEntity = player.Controller.ControlledEntity;
                    if (controlledEntity == null) continue;

                    m_tmpUpdatePositions.Add(controlledEntity.Entity.PositionComp.GetPosition());
                }

                m_tmpNavmeshes.Clear();

                ProfilerShort.Begin("Cell marking");
                Vector3D offset = new Vector3D(20.0f);
                foreach (var pos in m_tmpUpdatePositions)
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
                        Debug.Assert(mesh != null, "Navigation mesh for a voxel map is not generated!");
                        if (mesh == null) continue;

                        mesh.MarkBoxForAddition(box);

                        if (!m_tmpNavmeshes.Contains(mesh))
                            m_tmpNavmeshes.Add(mesh);
                    }
                }
                m_tmpVoxelMaps.Clear();
                ProfilerShort.End();

                m_tmpNavmeshes.Clear();

                ProfilerShort.Begin("Cell additions");
                foreach (var mesh in m_navigationMeshes)
                {
                    m_tmpNavmeshes.Add(mesh.Value);
                }
                m_tmpNavmeshes.ShuffleList();

                foreach (var mesh in m_tmpNavmeshes)
                {
                    if (mesh.AddOneMarkedCell())
                    {
                        // Break after the first added cell
                        break;
                    }
                }
                ProfilerShort.End();

                ProfilerShort.Begin("Cell removals");
                foreach (var mesh in m_tmpNavmeshes)
                {
                    if (mesh.RemoveOneUnusedCell(m_tmpUpdatePositions))
                    {
                        // Break after the first removed cell
                        break;
                    }
                }
                ProfilerShort.End();

                m_tmpNavmeshes.Clear();
                m_tmpUpdatePositions.Clear();
            }

            ProfilerShort.End();
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

        public MyVoxelNavigationMesh GetVoxelMapNavmesh(MyVoxelMap map)
        {
            MyVoxelNavigationMesh retval = null;
            m_navigationMeshes.TryGetValue(map, out retval);
            return retval;
        }

        public MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistanceSq, MyVoxelMap voxelMap = null)
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

