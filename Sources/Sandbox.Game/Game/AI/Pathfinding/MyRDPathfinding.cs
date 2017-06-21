using RecastDetour;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyRDPathfinding : IMyPathfinding
    {
        #region RequestedPath class
        /// <summary>
        /// Class for the debug drawing of the path
        /// </summary>
        class RequestedPath
        {
            public List<Vector3D> Path;
            public int LocalTicks;
        }
        #endregion

        #region Fields
        // 10 seconds - 500 ticks
        private const int DEBUG_PATH_MAX_TICKS = 150;

        private const int TILE_SIZE = 16;
        private const int TILE_HEIGHT = 70;
        private const int TILE_LINE_COUNT = 25;
        private readonly double MIN_NAVMESH_MANAGER_SQUARED_DISTANCE = Math.Pow(TILE_SIZE * ((TILE_LINE_COUNT*7/8)/2), 2);

        private Dictionary<MyPlanet, List<MyNavmeshManager>> m_planetManagers = new Dictionary<MyPlanet,List<MyNavmeshManager>>();
        private HashSet<MyCubeGrid> m_grids = new HashSet<MyCubeGrid>();
        private bool m_drawNavmesh = false;
        BoundingBoxD? m_debugInvalidateTileAABB;
        List<RequestedPath> m_debugDrawPaths = new List<RequestedPath>();
        #endregion

        #region Constructor
        public MyRDPathfinding()
        {
            MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;
            MyEntities.OnEntityRemove += MyEntities_OnEntityRemove;
        }
        #endregion

        #region Interface - IMyPathfinding Implementation
        public IMyPath FindPathGlobal(Vector3D begin, IMyDestinationShape end, MyEntity relativeEntity)
        {
            // TODO: relativeEntity NOT IMPLEMENTED

            // FOR DEBUG DRAW
            var path = new MyRDPath(this, begin, end);

            Vector3D targetPos;
            float targetRadius;
            VRage.ModAPI.IMyEntity relEnt;
            // If no next target, path is not found
            if (!path.GetNextTarget(begin, out targetPos, out targetRadius, out relEnt))
                path = null;

            return path;
        }

        public bool ReachableUnderThreshold(Vector3D begin, IMyDestinationShape end, float thresholdDistance)
        {
            // TODO: IMPLEMENT THIS!!! Eventually...
            return true;
        }

        /// <summary>
        /// Backwards compatibility
        /// </summary>
        public IMyPathfindingLog GetPathfindingLog()
        {
            return null;
        }

        public void Update()
        {
            foreach (var planetManagers in m_planetManagers)
            {
                for (int i = 0; i < planetManagers.Value.Count; i++)
                    if (!planetManagers.Value[i].Update())
                    {
                        planetManagers.Value.RemoveAt(i);
                        i--;
                    }
            }
        }

        public void UnloadData()
        {
            MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;

            foreach(var grid in m_grids)
            {
                grid.OnBlockAdded -= Grid_OnBlockAdded;
                grid.OnBlockRemoved -= Grid_OnBlockRemoved;
            }
            m_grids.Clear();

            foreach (var planetManagers in m_planetManagers)
                foreach (var manager in planetManagers.Value)
                    manager.UnloadData();
        }

        public void DebugDraw()
        {
            foreach (var planetManagers in m_planetManagers)
                foreach (var manager in planetManagers.Value)
                    manager.DebugDraw();

            if (m_debugInvalidateTileAABB.HasValue)
                VRageRender.MyRenderProxy.DebugDrawAABB(m_debugInvalidateTileAABB.Value, Color.Yellow, 0, 1, true, false);

            DebugDrawPaths();
        }
        #endregion

        #region Public Methods
        public static BoundingBoxD GetVoxelAreaAABB(MyVoxelBase storage, Vector3I minVoxelChanged, Vector3I maxVoxelChanged)
        {
            Vector3D min, max;
            MyVoxelCoordSystems.VoxelCoordToWorldPosition(storage.PositionLeftBottomCorner, ref minVoxelChanged, out min);
            MyVoxelCoordSystems.VoxelCoordToWorldPosition(storage.PositionLeftBottomCorner, ref maxVoxelChanged, out max);

            return new BoundingBoxD(min, max);
        }

        /// <summary>
        /// Returns the path between given positions.
        /// </summary>
        /// <param name="initialPosition"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        public List<Vector3D> GetPath(MyPlanet planet, Vector3D initialPosition, Vector3D targetPosition)
        {
            if (!m_planetManagers.ContainsKey(planet))
            {
                m_planetManagers[planet] = new List<MyNavmeshManager>();
                
                //TODO: can we optimize this??????????? Sectors instead of planet events, maybe..........
                planet.RangeChanged += VoxelChanged;
            }

            List<Vector3D> bestPath = GetBestPathFromManagers(planet, initialPosition, targetPosition);

            if (bestPath.Count > 0)
                m_debugDrawPaths.Add(new RequestedPath() { Path = bestPath, LocalTicks = 0 });

            return bestPath;
        }

        /// <summary>
        /// Adds to the tracked grids so the changes to it are followed
        /// </summary>
        /// <param name="cubeGrid"></param>
        /// <returns></returns>
        public bool AddToTrackedGrids(MyCubeGrid cubeGrid)
        {
            if (m_grids.Add(cubeGrid))
            {
                cubeGrid.OnBlockAdded += Grid_OnBlockAdded;
                cubeGrid.OnBlockRemoved += Grid_OnBlockRemoved;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Invalidates intersected tiles of navmesh managers, if they were generated
        /// </summary>
        /// <param name="areaBox"></param>
        public void InvalidateArea(BoundingBoxD areaBox)
        {
            var planet = GetPlanet(areaBox.Center);
            AreaChanged(planet, areaBox);
        }

        /// <summary>
        /// Enables or disables the drawing of the navmesh.
        /// </summary>
        /// <param name="drawNavmesh"></param>
        public void SetDrawNavmesh(bool drawNavmesh)
        {
            m_drawNavmesh = drawNavmesh;

            foreach (var planetManagers in m_planetManagers)
                foreach (var manager in planetManagers.Value)
                    manager.DrawNavmesh = m_drawNavmesh;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Returns the planet closest to the given position
        /// </summary>
        /// <param name="position">3D Point from where the search is centered</param>
        /// <returns>The closest planet</returns>
        private MyPlanet GetPlanet(Vector3D position)
        {
            //TODO: what value to put here??? Warning - Also defined in other places
            int voxelDistance = 500;
            BoundingBoxD box = new BoundingBoxD(position - voxelDistance * 0.5f, position + voxelDistance * 0.5f);
            return MyGamePruningStructure.GetClosestPlanet(ref box);
        }

        void MyEntities_OnEntityAdd(MyEntity obj)
        {
            // Check if it's a cube or rock or other stuff that matters
            var newEntity = obj as MyCubeGrid;
            if(newEntity != null)
            {
                // Get planet and invalidate the navmesh manager tile with an obb
                var planet = GetPlanet(newEntity.PositionComp.WorldAABB.Center);
                if (planet != null)
                {
                    List<MyNavmeshManager> managers;
                    if (m_planetManagers.TryGetValue(planet, out managers))
                    {
                        bool isInterestingGrid = false;
                        foreach (var manager in managers)
                            isInterestingGrid |= manager.InvalidateArea(newEntity.PositionComp.WorldAABB);

                        if (isInterestingGrid)
                            AddToTrackedGrids(newEntity);
                    }
                }
            }
            else
            {
                //newEntity = obj as ;
            }
        }

        void MyEntities_OnEntityRemove(MyEntity obj)
        {
            var newGrid = obj as MyCubeGrid;
            if (newGrid != null)
                if (m_grids.Remove(newGrid))
                {
                    newGrid.OnBlockAdded -= Grid_OnBlockAdded;
                    newGrid.OnBlockRemoved -= Grid_OnBlockRemoved;

                    var planet = GetPlanet(newGrid.PositionComp.WorldAABB.Center);
                    if (planet != null)
                    {
                        List<MyNavmeshManager> managers;
                        if (m_planetManagers.TryGetValue(planet, out managers))
                            foreach (var manager in managers)
                                manager.InvalidateArea(newGrid.PositionComp.WorldAABB);
                    }
                }
        }

        void Grid_OnBlockAdded(Entities.Cube.MySlimBlock slimBlock)
        {
            var planet = GetPlanet(slimBlock.WorldPosition);
            if (planet != null)
            {
                List<MyNavmeshManager> managers;
                if (m_planetManagers.TryGetValue(planet, out managers))
                {
                    var bb = slimBlock.WorldAABB;
                    foreach (var manager in managers)
                        manager.InvalidateArea(bb);
                }
            }
        }

        void Grid_OnBlockRemoved(Entities.Cube.MySlimBlock slimBlock)
        {
            var planet = GetPlanet(slimBlock.WorldPosition);
            if (planet != null)
            {
                List<MyNavmeshManager> managers;
                if (m_planetManagers.TryGetValue(planet, out managers))
                {
                    var bb = slimBlock.WorldAABB;
                    foreach (var manager in managers)
                        manager.InvalidateArea(bb);
                }
            }
        }

        void VoxelChanged(MyVoxelBase storage, Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
        {
            //TODO: Filter by MyStorageDataTypeFlags?

            var planet = storage as MyPlanet;
            if (planet != null)
            {
                var bb = GetVoxelAreaAABB(planet, minVoxelChanged, maxVoxelChanged);
                AreaChanged(planet, bb);
                //TODO: Only for Debug??
                m_debugInvalidateTileAABB = bb;
            }
        }

        /// <summary>
        /// Signals the navigation that the area changed and needs update.
        /// </summary>
        /// <param name="areaBox"></param>
        private void AreaChanged(MyPlanet planet, BoundingBoxD areaBox)
        {
            List<MyNavmeshManager> managers;
            if(m_planetManagers.TryGetValue(planet, out managers))
                foreach (var manager in managers)
                    manager.InvalidateArea(areaBox);
        }

        /// <summary>
        /// Returns the best path from managers according to both initial and target positions
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="initialPosition"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private List<Vector3D> GetBestPathFromManagers(MyPlanet planet, Vector3D initialPosition, Vector3D targetPosition)
        {
            bool noTilesToGenerated, pathContainsTarget;
            List<Vector3D> pathPoints;

            var managersContainingInitialPosition = m_planetManagers[planet].Where(m => m.ContainsPosition(initialPosition)).ToList();
            if (managersContainingInitialPosition.Count > 0)
            {
                //TODO: Check if a manager has both positions
                // In that case, if the returned path is final, check if the last position is reached
                foreach (var manager in managersContainingInitialPosition)
                    if (manager.ContainsPosition(targetPosition))
                    {
                        pathContainsTarget = manager.GetPathPoints(initialPosition, targetPosition, out pathPoints, out noTilesToGenerated);
                        //TODO: if there is path to target
                        if (pathContainsTarget || !noTilesToGenerated)
                            return pathPoints;
                    }

                // Choose the manager that is closer to the initial position
                MyNavmeshManager closestManager = null;
                double smallestDistance = double.MaxValue;
                foreach (var manager in managersContainingInitialPosition)
                {
                    double distanceToInitialPosition = (manager.Center - initialPosition).LengthSquared();
                    if (smallestDistance > distanceToInitialPosition)
                    {
                        smallestDistance = distanceToInitialPosition;
                        closestManager = manager;
                    }
                }

                pathContainsTarget = closestManager.GetPathPoints(initialPosition, targetPosition, out pathPoints, out noTilesToGenerated);
                //if (!finalPath || pathPoints.Count >= 2)
                //   return pathPoints;

                // It's the final path (all needed tiles generated)
                if (!pathContainsTarget && noTilesToGenerated && pathPoints.Count <= 2 && smallestDistance > MIN_NAVMESH_MANAGER_SQUARED_DISTANCE)
                {
                    var currentPositionDistanceToTarget = (initialPosition - targetPosition).LengthSquared();
                    var currentManagerDistanceToTarget = (closestManager.Center - targetPosition).LengthSquared();
                    // Generates new manager if the one available is far enough
                    if (currentManagerDistanceToTarget - currentPositionDistanceToTarget > MIN_NAVMESH_MANAGER_SQUARED_DISTANCE)
                    {
                        ProfilerShort.Begin("MyRDPathfinding.CreateManager2");
                        var manager = CreateManager(initialPosition);
                        manager.TilesToGenerate(initialPosition, targetPosition);
                        ProfilerShort.End();
                    }
                }

                return pathPoints;
            }
            else
            // No manager contains the initial position, bummer....
            {
                ProfilerShort.Begin("MyRDPathfinding.CreateManager1");
                var manager = CreateManager(initialPosition);
                manager.TilesToGenerate(initialPosition, targetPosition);
                ProfilerShort.End();

                return new List<Vector3D>();
            }
        }

        /// <summary>
        /// Creates a new manager centered in targetPosition and adds it to the list of managers
        /// </summary>
        /// <param name="initialPosition"></param>
        /// <param name="targetPosition"></param>
        private MyNavmeshManager CreateManager(Vector3D center, Vector3D? forwardDirection = null)
        {
            //TODO: Will this work badly with Artifical Gravity?
            if (!forwardDirection.HasValue)
            {
                Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
                forwardDirection = Vector3D.CalculatePerpendicularVector(gravityVector);
            }

            int tileSize = TILE_SIZE;
            int tileHeight = TILE_HEIGHT;
            int tileLineCount = TILE_LINE_COUNT;

            //TODO: this must receive the agent that is requesting a path so the options are setting appropriately
            var recastOptions = GetRecastOptions(null);
            //TODO: For now, it creates a manager with both points inside, if possible... Is this the best way?
            MyNavmeshManager manager = new MyNavmeshManager(this, center, forwardDirection.Value, tileSize, tileHeight, tileLineCount, recastOptions);
            //TODO: If a character has specific options for navmesh generation, there should be different manager lists for each character type
            manager.DrawNavmesh = m_drawNavmesh;

            m_planetManagers[manager.Planet].Add(manager);
            return manager;
        }

        /// <summary>
        /// Returns the Recast options to the navmesh is generating appropriately to this character
        /// </summary>
        /// <param name="character">The character </param>
        /// <returns>The options for navmesh creation</returns>
        private MyRecastOptions GetRecastOptions(MyCharacter character)
        {
            //TODO: Each character may have a different settings for pathfinding

            return new MyRecastOptions()
            {
                cellHeight = 0.2f,
                agentHeight = 1.5f,
                agentRadius = 0.5f,
                agentMaxClimb = 0.6f,
                agentMaxSlope = 60,
                regionMinSize = 1,
                regionMergeSize = 10,
                edgeMaxLen = 50,
                edgeMaxError = 3f,
                vertsPerPoly = 6,
                detailSampleDist = 6,
                detailSampleMaxError = 1,
                partitionType = 1
            };
        }

        private void DebugDrawSinglePath(List<Vector3D> path)
        {
            for (int i = 1; i < path.Count; i++)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(path[i], 0.5f, Color.Yellow, 0, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(path[i - 1], path[i], Color.Yellow, Color.Yellow, false);
            }
        }

        private void DebugDrawPaths()
        {
            var currentTime = DateTime.Now;

            // Update path debug draw list
            for (int i = 0; i < m_debugDrawPaths.Count; i++)
            {
                var pathCounter = m_debugDrawPaths[i];
                pathCounter.LocalTicks++;
                if (pathCounter.LocalTicks > DEBUG_PATH_MAX_TICKS)
                {
                    m_debugDrawPaths.RemoveAt(i);
                    i--;
                }
                else
                    DebugDrawSinglePath(pathCounter.Path);
            }
        }

        #endregion
    }
}
