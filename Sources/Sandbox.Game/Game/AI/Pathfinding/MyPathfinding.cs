using ParallelTasks;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyPathfinding : MyPathFindingSystem<MyNavigationPrimitive>, IMyPathfinding
    {
        private MyVoxelPathfinding m_voxelPathfinding;
        private MyGridPathfinding m_gridPathfinding;
        private MyNavmeshCoordinator m_navmeshCoordinator;
        private MyDynamicObstacles m_obstacles;

        public MyGridPathfinding GridPathfinding { get { return m_gridPathfinding; } }
        public MyVoxelPathfinding VoxelPathfinding { get { return m_voxelPathfinding; } }
        public MyNavmeshCoordinator Coordinator { get { return m_navmeshCoordinator; } }
        public MyDynamicObstacles Obstacles { get { return m_obstacles; } }

        // Just a debug draw thing
        public long LastHighLevelTimestamp { get; set; }

        public readonly Func<long> NextTimestampFunction;
        private long GenerateNextTimestamp()
        {
            CalculateNextTimestamp();
            return GetCurrentTimestamp();
        }

        public MyPathfinding()
        {
            NextTimestampFunction = GenerateNextTimestamp;

            m_obstacles = new MyDynamicObstacles();
            m_navmeshCoordinator = new MyNavmeshCoordinator(m_obstacles);
            m_gridPathfinding = new MyGridPathfinding(m_navmeshCoordinator);
            m_voxelPathfinding = new MyVoxelPathfinding(m_navmeshCoordinator);

            MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;
        }

        public void Update()
        {
            if (MyPerGameSettings.EnablePathfinding)
            {
                m_obstacles.Update();

                MyPathfindingStopwatch.CheckStopMeasuring();
                MyPathfindingStopwatch.Start();
                m_gridPathfinding.Update();

                m_voxelPathfinding.Update();
                //ParallelTasks.Parallel.Start(m_voxelPathfinding);
                MyPathfindingStopwatch.Stop();
            }
        }

        public IMyPathfindingLog GetPathfindingLog()
        {
            return m_voxelPathfinding.DebugLog;
        }

        public void UnloadData()
        {
            MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;

            m_voxelPathfinding.UnloadData();

            m_gridPathfinding = null;
            m_voxelPathfinding = null;
            m_navmeshCoordinator = null;
            m_obstacles.Clear();
            m_obstacles = null;
        }

        private void MyEntities_OnEntityAdd(MyEntity newEntity)
        {
            m_obstacles.TryCreateObstacle(newEntity);

            var grid = newEntity as MyCubeGrid;
            if (grid != null)
            {
                m_gridPathfinding.GridAdded(grid);
            }
        }

        public IMyPath FindPathGlobal(Vector3D begin, IMyDestinationShape end, MyEntity entity = null)
        {
            Debug.Assert(MyPerGameSettings.EnablePathfinding, "Pathfinding is not enabled!");
            if (!MyPerGameSettings.EnablePathfinding)
            {
                return null;
            }

            ProfilerShort.Begin("MyPathfinding.FindPathGlobal");
            MyPathfindingStopwatch.Start();

            // CH: TODO: Use pooling
            MySmartPath newPath = new MySmartPath(this);
            MySmartGoal newGoal = new MySmartGoal(end, entity);
            newPath.Init(begin, newGoal);

            MyPathfindingStopwatch.Stop();
            ProfilerShort.End();
            return newPath;
        }

        private MyNavigationPrimitive m_reachEndPrimitive;
        private float m_reachPredicateDistance;

        private bool ReachablePredicate(MyNavigationPrimitive primitive)
        {
            return (m_reachEndPrimitive.WorldPosition - primitive.WorldPosition).LengthSquared() <= m_reachPredicateDistance * m_reachPredicateDistance;
        }

        // MW:TODO optimize or change
        public bool ReachableUnderThreshold(Vector3D begin, IMyDestinationShape end, float thresholdDistance)
        {
            m_reachPredicateDistance = thresholdDistance;
            var beginPrimitive = FindClosestPrimitive(begin, false);
            var endPrimitive = FindClosestPrimitive(end.GetDestination(), false);

            if (beginPrimitive == null || endPrimitive == null)
                return false;

            var beginHL = beginPrimitive.GetHighLevelPrimitive();
            var endHL = endPrimitive.GetHighLevelPrimitive();

            ProfilerShort.Begin("HL");
            MySmartGoal goal = new MySmartGoal(end);
            var path = goal.FindHighLevelPath(this, beginHL);
            ProfilerShort.End();
            if (path == null)
                return false;

            m_reachEndPrimitive = endPrimitive;
            ProfilerShort.Begin("Prepare for travesal");
            PrepareTraversal(beginPrimitive, null, ReachablePredicate);
            ProfilerShort.End();
            ProfilerShort.Begin("checking for vertices");
            try
            {
                foreach (var vertex in this)
                {
                    if (vertex.Equals(m_reachEndPrimitive))
                        return true;
                }
            }
            finally
            {
                ProfilerShort.End();
            }

            return false;
        }

        public MyPath<MyNavigationPrimitive> FindPathLowlevel(Vector3D begin, Vector3D end)
        {
            MyPath<MyNavigationPrimitive> path = null;

            Debug.Assert(MyPerGameSettings.EnablePathfinding, "Pathfinding is not enabled!");
            if (!MyPerGameSettings.EnablePathfinding)
            {
                return path;
            }

            ProfilerShort.Begin("MyPathfinding.FindPathLowlevel");
            var startPrim = FindClosestPrimitive(begin, highLevel: false);
            var endPrim = FindClosestPrimitive(end, highLevel: false);
            if (startPrim != null && endPrim != null)
            {
                path = FindPath(startPrim, endPrim);
            }
            ProfilerShort.End();

            return path;
        }

        public MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, MyEntity entity = null)
        {
            double closestDistSq = double.PositiveInfinity;
            MyNavigationPrimitive closestPrimitive = null;

            MyNavigationPrimitive closest = null;

            MyVoxelMap voxelMap = entity as MyVoxelMap;
            MyCubeGrid cubeGrid = entity as MyCubeGrid;

            if (voxelMap != null)
            {
                closestPrimitive = VoxelPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq, voxelMap);
            }
            else if (cubeGrid != null)
            {
                closestPrimitive = GridPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq, cubeGrid);
            }
            else
            {
                closest = VoxelPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq);
                if (closest != null) closestPrimitive = closest;
                closest = GridPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq);
                if (closest != null) closestPrimitive = closest;
            }

            return closestPrimitive;
        }

        public void DebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW == false) return;

            m_gridPathfinding.DebugDraw();
            m_voxelPathfinding.DebugDraw();

            if (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES != MyWEMDebugDrawMode.NONE)
            {
                m_navmeshCoordinator.Links.DebugDraw(Color.Khaki);
            }
            if (MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY)
            {
                m_navmeshCoordinator.HighLevelLinks.DebugDraw(Color.LightGreen);
            }

            m_navmeshCoordinator.DebugDraw();
            m_obstacles.DebugDraw();
        }
    }
}
