using Sandbox.Engine.Networking;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Game;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Death Wish
     * 
     * Armageddon Mode for 5 hours in Survival.
     */
    public class MyAchievement_DeathWish : MySteamAchievementBase
    {
        public const string StatNameTag = "DeathWish_MinutesPlayed";

        public override string AchievementTag { get { return "MyAchievement_DeathWish"; } }
        public override bool NeedsUpdate { get { return true; } }

        // disables or enables the progress of the achievement
        private bool m_conditionsMet = false;
        private int m_lastElapsedMinutes;
        private int m_totalMinutesPlayedInArmageddonSettings;

        public override void SessionLoad()
        {
            // Check if the armegeddon mode is enabled
            m_conditionsMet = MySession.Static.Settings.EnvironmentHostility == MyEnvironmentHostilityEnum.CATACLYSM_UNREAL 
                && !MySession.Static.CreativeMode;

            m_lastElapsedMinutes = 0;
        }

        public override void Init()
        {
            base.Init();

            if(IsAchieved) return;

            m_totalMinutesPlayedInArmageddonSettings = MySteam.API.GetStatInt(StatNameTag);
        }

        public override void SessionUpdate()
        {
            if (m_conditionsMet)
            {
                // Update one per minute basis
                var currentElapsedMinutes = (int)MySession.Static.ElapsedPlayTime.TotalMinutes;
                if (m_lastElapsedMinutes < currentElapsedMinutes)
                {
                    m_lastElapsedMinutes = currentElapsedMinutes;
                    m_totalMinutesPlayedInArmageddonSettings++;
                    MySteam.API.SetStat(StatNameTag, m_totalMinutesPlayedInArmageddonSettings);

                    // Final condition 5h
                    if (m_totalMinutesPlayedInArmageddonSettings > 300)
                    {
                        NotifyAchieved();
                    }
                }
            }
        }
    }
}
