using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
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

          //  MyCubeGrids.BlockBuilt += MyCubeGridsOnBlockBuilt;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

          //  MyCubeGrids.BlockBuilt -= MyCubeGridsOnBlockBuilt;
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

        private List<MyVoxelBase> m_tmpVoxelList = new List<MyVoxelBase>();
        private List<MyEntity> m_tmpEntityList = new List<MyEntity>();

        private void MyCubeGridsOnBlockBuilt(MyCubeGrid myCubeGrid, MySlimBlock mySlimBlock)
        {
            if (mySlimBlock == null) return;

            var aabb = myCubeGrid.PositionComp.WorldAABB;

            MyGamePruningStructure.GetAllVoxelMapsInBox(ref aabb, m_tmpVoxelList);

            BoundingBoxD blockAabb;
            mySlimBlock.GetWorldBoundingBox(out blockAabb, true);

            for (int i = 0; i < m_tmpVoxelList.Count; ++i)
            {
                var p = m_tmpVoxelList[i] as MyPlanet;

                if (p == null) continue;

                p.Hierarchy.QueryAABB(ref aabb, m_tmpEntityList);

                for (int j = 0; j < m_tmpEntityList.Count; ++j)
                {
                    var sector = m_tmpEntityList[j] as MyEnvironmentSector;

                    var bb = blockAabb;
                    sector.DisableItemsInAabb(ref bb);
                }
            }
        }

        #endregion
    }
}
