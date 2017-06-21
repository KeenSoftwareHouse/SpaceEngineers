using System.Collections.Generic;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRageMath;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Lost In Space
     * 
     * Spend more than 1 hour out of sight of other players on a MP server.
     */
    public class MyAchievement_LostInSpace : MySteamAchievementBase
    {
        public const string StatNameTag = "LostInSpace_LostInSpaceStartedS";
        public const int CheckIntervalMs = 3000;

        public override string  AchievementTag  { get { return "MyAchievement_LostInSpace"; } }
        public override bool    NeedsUpdate     { get { return m_conditionsValid; } }

        private int m_startedS;
        private double m_lastTimeChecked;
        private bool m_conditionsValid;

        private readonly List<MyPhysics.HitInfo> m_hitInfoResults 
            = new List<MyPhysics.HitInfo>();

        public override void Init()
        {
            base.Init();

            if(IsAchieved) return;

            m_startedS = MySteam.API.GetStatInt(StatNameTag);
            m_lastTimeChecked = 0;
        }

        public override void SessionLoad()
        {
            m_conditionsValid = MyMultiplayer.Static != null;

            m_lastTimeChecked = 0;
        }

        public override void SessionUpdate()
        {
            if(!m_conditionsValid) return;

            var playTimeMs = MySession.Static.ElapsedGameTime.TotalMilliseconds;
            var playTimeS = (int)playTimeMs / 1000;
            var elapsedMs = playTimeMs - m_lastTimeChecked;
            // Dont not check every update
            if (elapsedMs > CheckIntervalMs)
            {
                m_lastTimeChecked = playTimeMs;

                // You cannot be alone on the server to get this achievement.
                if (MySession.Static.Players.GetOnlinePlayerCount() == 1)
                {
                    m_startedS = playTimeS;
                    MySteam.API.SetStat(StatNameTag, m_startedS);
                    return;
                }

                // Check the sight condition
                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (player != MySession.Static.LocalHumanPlayer && IsThePlayerInSight(player))
                    {
                        m_startedS = playTimeS;
                        MySteam.API.SetStat(StatNameTag, m_startedS);
                        break;
                    }
                }

                if (playTimeS - m_startedS > 3600)
                {
                    // One hour of hide and seek passed.
                    NotifyAchieved();
                }
            }
        }

        private bool IsThePlayerInSight(MyPlayer player)
        {
            if (player.Character == null) return false;
            if (MySession.Static.LocalCharacter == null) return false;

            var playerPosition = player.Character.PositionComp.GetPosition();
            var localPlayerPosition = MySession.Static.LocalCharacter.PositionComp.GetPosition();

            // Is just about 2k meters
            if (Vector3D.DistanceSquared(playerPosition, localPlayerPosition) > 4000000)
            {
                return false;
            }

            m_hitInfoResults.Clear();
            MyPhysics.CastRay(playerPosition, localPlayerPosition, m_hitInfoResults);

            foreach (var hitInfo in m_hitInfoResults)
            {
                if(hitInfo.HkHitInfo.GetHitEntity() is MyCharacter)
                    continue;

                return false;
            }

            return true;
        }
    }
}
