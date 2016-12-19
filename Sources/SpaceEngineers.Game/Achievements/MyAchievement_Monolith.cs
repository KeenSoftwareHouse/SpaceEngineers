using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Monolith
     * 
     * Get within 5 meter radius of the monolith. *Monoliths have to be in the world on loading time
     */
    public class MyAchievement_Monolith : MySteamAchievementBase
    {
        public override string AchievementTag { get { return "MyAchievement_Monolith"; } }

        public override bool NeedsUpdate
        {
            get
            {
                // Explicitly disabled
                if(!m_globalConditions) return false;
                // Enable and disable updates
                var elapsedS = (uint)MySession.Static.ElapsedPlayTime.TotalSeconds - m_lastTimeUpdatedS;
                return elapsedS > UPDATE_INTERVAL_S;
            }
        }

        private const uint UPDATE_INTERVAL_S = 3;

        private bool m_globalConditions;
        private uint m_lastTimeUpdatedS;
        private readonly List<MyCubeGrid> m_monolithGrids = new List<MyCubeGrid>(); 

        public override void SessionUpdate()
        {
            // Check for needed for dedicated server. The character is loaded later.
            if (MySession.Static.LocalCharacter == null) return;

            m_lastTimeUpdatedS = (uint)MySession.Static.ElapsedPlayTime.TotalSeconds;

            if (MySession.Static.LocalCharacter == null) return;

            // Check all existing monoliths
            var characterPosition = MySession.Static.LocalCharacter.PositionComp.GetPosition();
            foreach (var grid in m_monolithGrids)
            {
                var position = grid.PositionComp.WorldVolume.Center;
                var distance = Vector3D.DistanceSquared(characterPosition, position);
                if (distance < 400 + grid.PositionComp.WorldVolume.Radius)
                {
                    NotifyAchieved();
                    return;
                }
            }
        }

        public override void SessionBeforeStart()
        {
            m_globalConditions = !MySession.Static.CreativeMode;

            if(!m_globalConditions) return;

            m_lastTimeUpdatedS = 0;
            // Find and register monolits
            m_monolithGrids.Clear();
            foreach (var entity in MyEntities.GetEntities())
            {
                var cubeGrid = entity as MyCubeGrid;
                if (cubeGrid != null && cubeGrid.BlocksCount == 1)
                {
                    var monolith = cubeGrid.CubeBlocks.FirstElement();
                    if (monolith.BlockDefinition.Id.SubtypeId == MyStringHash.GetOrCompute("Monolith"))
                    {
                        m_monolithGrids.Add(cubeGrid);
                    }
                }
            }
        }
    }
}
