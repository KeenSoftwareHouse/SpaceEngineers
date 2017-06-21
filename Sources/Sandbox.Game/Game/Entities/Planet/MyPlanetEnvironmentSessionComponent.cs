using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Planet
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 500)]
    public class MyPlanetEnvironmentSessionComponent : MySessionComponentBase
    {
        public override Type[] Dependencies
        {
            get { return new[] { typeof(MyCubeGrids) }; }
        }

        #region Update and Debug Draw

        private const int TIME_TO_UPDATE = 10;
        private const int UPDATES_TO_LAZY_UPDATE = 10;

        private int m_updateInterval;
        private int m_lazyUpdateInterval;

        public override void UpdateBeforeSimulation()
        {
            if (!EnableUpdate) return;

            m_updateInterval++;
            if (m_updateInterval > TIME_TO_UPDATE)
            {
                m_updateInterval = 0;
                m_lazyUpdateInterval++;
            }
            else return;

            bool doLazy = false;
            if (m_lazyUpdateInterval > UPDATES_TO_LAZY_UPDATE)
            {
                doLazy = true;
                m_lazyUpdateInterval = 0;
            }

            UpdatePlanetEnvironments(doLazy);

            // Update for items to disable
            if (!m_itemDisableJobRunning && m_cubeBlocksPending.Count() > 0)
            {
                Parallel.Start(GatherEnvItemsInBoxes, DisableGatheredItems);
                m_itemDisableJobRunning = true;
            }

        }

        public static bool EnableUpdate = true;
        public static bool DebugDrawSectors = false;
        public static bool DebugDrawDynamicObjectClusters = false;
        public static bool DebugDrawEnvironmentProviders = false;
        public static bool DebugDrawActiveSectorItems = false;
        public static bool DebugDrawActiveSectorProvider = false;
        public static bool DebugDrawProxies = false;
        public static bool DebugDrawCollisionCheckers = false;

        public static float DebugDrawDistance = 150;

        public override void Draw()
        {
            if (DebugDrawEnvironmentProviders)
                foreach (var provider in m_environmentProviders)
                {
                    provider.DebugDraw();
                }

            var closest = MyGamePruningStructure.GetClosestPlanet(MySector.MainCamera.Position);

            if (DebugDrawSectors && closest != null)
                ActiveSector = closest.Components.Get<MyPlanetEnvironmentComponent>().GetSectorForPosition(MySector.MainCamera.Position);
        }

        #endregion

        public override void LoadData()
        {
            base.LoadData();

            MyCubeGrids.BlockBuilt += MyCubeGridsOnBlockBuilt;
            MyEntities.OnEntityAdd += CheckCubeGridCreated;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            MyCubeGrids.BlockBuilt -= MyCubeGridsOnBlockBuilt;
            MyEntities.OnEntityAdd -= CheckCubeGridCreated;
        }

        #region Planet Environment

        private readonly HashSet<IMyEnvironmentDataProvider> m_environmentProviders = new HashSet<IMyEnvironmentDataProvider>();

        private readonly HashSet<MyPlanetEnvironmentComponent> m_planetEnvironments = new HashSet<MyPlanetEnvironmentComponent>();

        public static MyEnvironmentSector ActiveSector;

        public void RegisterPlanetEnvironment(MyPlanetEnvironmentComponent env)
        {
            m_planetEnvironments.Add(env);

            foreach (var provider in env.Providers)
                m_environmentProviders.Add(provider);
        }

        public void UnregisterPlanetEnvironment(MyPlanetEnvironmentComponent env)
        {
            m_planetEnvironments.Remove(env);

            foreach (var provider in env.Providers)
                m_environmentProviders.Remove(provider);
        }

        private void UpdatePlanetEnvironments(bool doLazy)
        {
            foreach (var component in m_planetEnvironments)
            {
                component.Update(doLazy);
            }
        }

        #endregion

        #region Cleared Areas

        private const int NewEnvReleaseVersion = 01133002;

        public override void BeforeStart()
        {
            if (MySession.Static.AppVersionFromSave < NewEnvReleaseVersion)
            {
                foreach (var environment in m_planetEnvironments)
                {
                    environment.InitClearAreasManagement();
                }
            }
        }

        #endregion

        #region Disabling items that overlap grids

        private MyListDictionary<MyCubeGrid, BoundingBoxD> m_cubeBlocksToWork = new MyListDictionary<MyCubeGrid, BoundingBoxD>();
        private volatile MyListDictionary<MyCubeGrid, BoundingBoxD> m_cubeBlocksPending = new MyListDictionary<MyCubeGrid, BoundingBoxD>();

        private volatile bool m_itemDisableJobRunning = false;

        private List<MyVoxelBase> m_tmpVoxelList = new List<MyVoxelBase>();
        private List<MyEntity> m_tmpEntityList = new List<MyEntity>();

        private MyListDictionary<MyEnvironmentSector, int> m_itemsToDisable = new MyListDictionary<MyEnvironmentSector, int>();

        private void CheckCubeGridCreated(MyEntity myEntity)
        {
            if (!MySession.Static.Ready) 
                return;

            // Handle new cube grids as well.
            var grid = myEntity as MyCubeGrid;
            if (grid != null && grid.IsStatic)
            {
                m_cubeBlocksPending.Add(grid, grid.PositionComp.WorldAABB);
            }
        }

        private void MyCubeGridsOnBlockBuilt(MyCubeGrid myCubeGrid, MySlimBlock mySlimBlock)
        {
            if (mySlimBlock == null || !myCubeGrid.IsStatic) 
                return;

            // Avoid multiple queues for compound block additions.
            var slimBlock = myCubeGrid.GetCubeBlock(mySlimBlock.Min);
            if (slimBlock != null)
            {
                var compound = slimBlock.FatBlock as MyCompoundCubeBlock;
                if (compound != null && mySlimBlock.FatBlock != compound)
                    return;
            }

            BoundingBoxD blockAabb;
            mySlimBlock.GetWorldBoundingBox(out blockAabb, true);
            m_cubeBlocksPending.Add(myCubeGrid, blockAabb);

            //Debug.Print("CubeGrid {0}: Block added at {1}.", myCubeGrid, mySlimBlock.Position);
        }

        private void GatherEnvItemsInBoxes()
        {
            var work = Interlocked.Exchange(ref m_cubeBlocksPending, m_cubeBlocksToWork);
            m_cubeBlocksToWork = work;

            int itemsVisited = 0;
            int blocksVisited = 0;

            ProfilerShort.Begin("PlanetEnvironment::GatherItemsInSlimBlocks()");
            foreach (var grid in work.Values)
            {
                for (int blockIndex = 0; blockIndex < grid.Count; ++blockIndex)
                {
                    BoundingBoxD blockAabb = grid[blockIndex];

                    MyGamePruningStructure.GetAllVoxelMapsInBox(ref blockAabb, m_tmpVoxelList);
                    blocksVisited++;

                    for (int i = 0; i < m_tmpVoxelList.Count; ++i)
                    {
                        var p = m_tmpVoxelList[i] as MyPlanet;

                        if (p == null) continue;

                        p.Hierarchy.QueryAABB(ref blockAabb, m_tmpEntityList);

                        for (int j = 0; j < m_tmpEntityList.Count; ++j)
                        {
                            var sector = m_tmpEntityList[j] as MyEnvironmentSector;

                            if (sector == null) 
                                return;

                            var bb = blockAabb;
                            sector.GetItemsInAabb(ref bb, m_itemsToDisable.GetOrAddList(sector));
                            if (sector.DataView != null && sector.DataView.Items != null)
                                itemsVisited += sector.DataView.Items.Count;
                        }

                        m_tmpEntityList.Clear();
                    }

                    m_tmpVoxelList.Clear();
                }
            }
            ProfilerShort.End();

            //Debug.Print("Processed {0} blocks with {1} items", blocksVisited, itemsVisited);

            work.Clear();
        }


        public void DisableGatheredItems()
        {
            ProfilerShort.Begin("PlanetEnvironment::DisableItemsOverlappingBlocks()");
            foreach (var sector in m_itemsToDisable)
            {
                for (int i = 0; i < sector.Value.Count; ++i)
                {
                    sector.Key.EnableItem(sector.Value[i], false);
                }
            }

            m_itemsToDisable.Clear();

            m_itemDisableJobRunning = false;
            ProfilerShort.End();
        }


        #endregion
    }
}
