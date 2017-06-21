using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System.Collections.Generic;
using VRageMath;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Colorblind
     * 
     * Build and fly ship with more than 20 colors.
     */
    class MyAchievement_Colorblind : MySteamAchievementBase
    {
        // Number of unique color on one grid to achieve this achievment
        private const int NUMBER_OF_COLORS_TO_ACHIEV = 20;

        // List of used colors on current grid
        HashSet<Vector3> colorList;

        // Lazy update flag
        private bool m_isUpdating = true;

        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievment_ColorBlind"; } }
        public override bool NeedsUpdate { get { return m_isUpdating; } }

        public override void Init()
        {
            // Automaticaly sets up the achivement state and all the good stuff
            base.Init();

            // No need to register if already achieved
            if (IsAchieved) return;

            // Initialize color array
            colorList = new HashSet<Vector3>();

            m_isUpdating = true;

        }

        // Wait until LocalHumanPlayer is initialized
        public override void SessionUpdate()
        {
            base.SessionUpdate();

            if (m_isUpdating)
            {
                if (MySession.Static.LocalHumanPlayer != null)
                {
                    // Register on controlled entity changed event
                    MySession.Static.LocalHumanPlayer.Controller.ControlledEntityChanged += Controller_ControlledEntityChanged;
                    //Disable further updating
                    m_isUpdating = false;
                }
            }
        }

        private void Controller_ControlledEntityChanged(IMyControllableEntity oldEnt, IMyControllableEntity newEnt)
        {
            if (newEnt == null)
                return;

            if (MyCampaignManager.Static.IsCampaignRunning)
                return;

            var grid = newEnt.Entity.Parent as MyCubeGrid;
            if (grid == null) return;

            colorList.Clear();
            // Scan grid for colors
            foreach (var block in grid.GetBlocks())
            {
                // Filter blocks builded by player
                if (block.BuiltBy != MySession.Static.LocalHumanPlayer.Identity.IdentityId)
                    continue;
                // unique list of colors in grid
                colorList.Add(block.ColorMaskHSV);

                if (colorList.Count >= NUMBER_OF_COLORS_TO_ACHIEV)
                {
                    //Final condition
                    NotifyAchieved();
                    MySession.Static.LocalHumanPlayer.Controller.ControlledEntityChanged -= Controller_ControlledEntityChanged;
                    return;
                }
            }
        }



    }
}
