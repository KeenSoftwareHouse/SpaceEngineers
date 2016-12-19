using Sandbox.Game;
using Sandbox.Game.SessionComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: The Story Begins
     * 
     * Finish first campaign
     */
    class MyAchievement_ToTheStars : MySteamAchievementBase
    {

        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievement_ToTheStars"; } }
        public override bool NeedsUpdate { get { return false; } }

        public override void Init()
        {
            base.Init();

            if (IsAchieved) return;
            
            // Register event
            MyCampaignManager.Static.OnCampaignFinished += Static_OnCampaignFinished;
        }


        private void Static_OnCampaignFinished()
        {
            // Check if it is vanilla campaign
            if (MyCampaignManager.Static.ActiveCampaign.IsVanilla)
            {
                NotifyAchieved();
                MyCampaignManager.Static.OnCampaignFinished -= Static_OnCampaignFinished;
            }
        }
    }
}
