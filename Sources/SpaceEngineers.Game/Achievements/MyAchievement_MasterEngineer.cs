using Sandbox.Engine.Networking;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Master Engineer
     * 
     * 150+ hours.
     */
    public class MyAchievement_MasterEngineer : MySteamAchievementBase
    {
        public const string StatNameTag = "MasterEngineer_MinutesPlayed";
        public override string AchievementTag { get { return "MyAchievement_MasterEngineer"; } }
        public override bool NeedsUpdate { get { return true; } }

        private int m_totalMinutesPlayed;
        private int m_lastLoggedMinute;

        public override void Init()
        {
            base.Init();

            if(IsAchieved) return;

            m_totalMinutesPlayed = MySteam.API.GetStatInt(StatNameTag);
        }

        public override void SessionLoad()
        {
            m_lastLoggedMinute = 0;
        }

        public override void SessionUpdate()
        {
            var minutesElapsed = (int)MySession.Static.ElapsedPlayTime.TotalMinutes;
            if (m_lastLoggedMinute < minutesElapsed)
            {
                // Update every minute
                m_totalMinutesPlayed++;
                m_lastLoggedMinute = minutesElapsed;
                MySteam.API.SetStat(StatNameTag, m_totalMinutesPlayed);

                // Final condition 150+h
                if (m_totalMinutesPlayed > 9000)
                {
                    NotifyAchieved();
                }
            }
        }
    }
}
