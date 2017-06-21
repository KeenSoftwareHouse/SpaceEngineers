using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: The Harder They Fall
     * 
     * Destroy more than 1,000,000 Kg of cubes.
     */
    class MyAchievement_TheHarderTheyFall : MySteamAchievementBase
    {
        // Amount of mass you need to destroy (in kilograms)
        private const float DESTROY_BLOCK_MASS_KG = 1000000;
        // Amount of mass you need to accumulate before achievement progress updated
        private const float UPDATE_STEP_KG = 10000;

        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievement_TheHarderTheyFall"; } }
        public override bool NeedsUpdate { get { return false; } }

        // Number of already built mass
        private float m_massDestroyed = 0;
        private float m_massToNextUpload = 0;
        

        // Name identifier of the solar panel build counter in steam systems
        private const string MassDestroyedStatName = "TheHarderTheyFall_MassDestroyed";

        public override void Init()
        {
            // Automaticaly sets up the achivement state and all the good stuff
            base.Init();

            // No need to register if already achieved
            if (IsAchieved) return;

            // Load amount of existing blocks
            m_massDestroyed = MySteam.API.GetStatInt(MassDestroyedStatName);

            m_massToNextUpload = m_massDestroyed % UPDATE_STEP_KG;
            // Register on block destroy event
            MyCubeGrids.BlockDestroyed += MyCubeGridsOnBlockDestroyed;
        }

        private void MyCubeGridsOnBlockDestroyed(MyCubeGrid myCubeGrid, MySlimBlock mySlimBlock)
        {
            // Survival mode condition
            if (MySession.Static.CreativeMode) return;

            m_massDestroyed += mySlimBlock.GetMass();
            m_massToNextUpload += mySlimBlock.GetMass();

            // Store the new mass built data
            MySteam.API.SetStat(MassDestroyedStatName, m_massDestroyed);
            if (m_massDestroyed >= DESTROY_BLOCK_MASS_KG)
            {
                // Final condition
                NotifyAchieved();
                MyCubeGrids.BlockBuilt -= MyCubeGridsOnBlockDestroyed;
            }
            else if (m_massToNextUpload > UPDATE_STEP_KG) //every 10000kg of mass update progress indicator
            {
                //Update achievement progress
                m_massToNextUpload %= UPDATE_STEP_KG;
                //MySteam.API.IndicateAchievementProgress(AchievementTag, (uint)m_massDestroyed, (uint)DESTROY_BLOCK_MASS_KG);
            }
        }
    }
}
