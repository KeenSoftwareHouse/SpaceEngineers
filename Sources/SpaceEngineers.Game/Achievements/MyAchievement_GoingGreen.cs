using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Going Green
     * 
     * Build 25 Solar Panels while playing in Survival Mode.
     */
    public class MyAchievement_GoingGreen : MySteamAchievementBase
    {
        // Number of already built solar panels
        private int m_solarPanelsBuilt = 0;
        // Name identifier of the solar panel build counter in steam systems
        private const string SolarPanelsBuiltStatName = "GoingGreen_SolarPanelsBuilt";
        
        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievement_GoingGreen"; } }
        public override bool NeedsUpdate { get { return false; } }

        public override void Init()
        {
            // Automaticaly sets up the achivement state and all the good stuff
            base.Init();

            // No need to register if already achieved
            if(IsAchieved) return;

            // Load amount of existing blocks
            m_solarPanelsBuilt = MySteam.API.GetStatInt(SolarPanelsBuiltStatName);
            // Register on block built event
            MyCubeGrids.BlockBuilt += MyCubeGridsOnBlockBuilt;
        }

        private void MyCubeGridsOnBlockBuilt(MyCubeGrid myCubeGrid, MySlimBlock mySlimBlock)
        {
            if (MySession.Static == null || mySlimBlock == null || mySlimBlock.FatBlock == null) return;

            // Survival mode condition
            if(MySession.Static.CreativeMode) return;

            // Solar panel condition
            if (mySlimBlock.FatBlock is MySolarPanel)
            {
                m_solarPanelsBuilt++;
                // Update the counter value in the steam proxy
                MySteam.API.SetStat(SolarPanelsBuiltStatName, m_solarPanelsBuilt);
                if (m_solarPanelsBuilt >= 25)
                {
                    // Final condition
                    NotifyAchieved();
                    MyCubeGrids.BlockBuilt -= MyCubeGridsOnBlockBuilt;
                }
            }
        }
    }
}
