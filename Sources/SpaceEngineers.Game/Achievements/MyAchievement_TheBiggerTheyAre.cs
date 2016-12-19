using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: The Bigger They Are
     * 
     * Build more than 1,000,000 Kg of cubes.
     */
    class MyAchievement_TheBiggerTheyAre : MySteamAchievementBase
    {
        // Amount of mass you need to build (in kilograms)
        private const float BUILT_BLOCK_MASS_KG = 1000000;
        // Amount of mass you need to accumulate before achievement progress updated
        private const float UPDATE_STEP_KG = 10000;

        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievement_TheBiggerTheyAre"; } }
        public override bool NeedsUpdate { get { return false; } }

        // Number of already built mass
        private float m_massBuild = 0;
        private float m_massToNextUpload = 0;

        // Name identifier of the solar panel build counter in steam systems
        private const string MassBuiltStatName = "TheBiggerTheyAre_MassBuilt";

        public override void Init()
        {
            // Automaticaly sets up the achivement state and all the good stuff
            base.Init();

            // No need to register if already achieved
            if (IsAchieved) return;

            // Load amount of existing blocks
            m_massBuild = MySteam.API.GetStatInt(MassBuiltStatName);

            m_massToNextUpload = m_massBuild % UPDATE_STEP_KG;
            // Register on block built event
            MyCubeGrids.BlockBuilt += MyCubeGridsOnBlockBuilt;
        }

        private void MyCubeGridsOnBlockBuilt(MyCubeGrid myCubeGrid, MySlimBlock mySlimBlock)
        {
            // Survival mode condition
            if (MySession.Static.CreativeMode) return;

            m_massBuild += mySlimBlock.GetMass();
            m_massToNextUpload += mySlimBlock.GetMass();

            // Store the new mass built data
            MySteam.API.SetStat(MassBuiltStatName, m_massBuild);
            if (m_massBuild >= BUILT_BLOCK_MASS_KG)
            {
                // Final condition
                NotifyAchieved();
                MyCubeGrids.BlockBuilt -= MyCubeGridsOnBlockBuilt;
            }
            else if (m_massToNextUpload > UPDATE_STEP_KG) //every 10000kg of mass store current value on steam and update progress indicator
            {
                //Update achievement progress
                m_massToNextUpload %= UPDATE_STEP_KG;
                //MySteam.API.IndicateAchievementProgress(AchievementTag, (uint)m_massBuild, (uint)BUILT_BLOCK_MASS_KG);
            }
        }
    }
}
