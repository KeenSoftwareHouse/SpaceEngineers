using Sandbox.Engine.Networking;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Giant Leap For Humankind
     * 
     * Walk 1969 meters on a Moon.
     */
    class MyAchievement_GiantLeapForMankind : MySteamAchievementBase
    {

        private const double CHECK_INTERVAL_S = 3;
        private const int DISTANCE_TO_BE_WALKED = 1969;

        // Name identifier of walked distance on moon in steam systems
        private const string WalkedMoonStatName = "GiantLeapForMankind_WalkedMoon";

        // Walked distance
        private float m_metersWalkedOnMoon = 0;
        private float m_storedMetersWalkedOnMoon = 0;

        List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();

        // Check timer data
        private double m_lastCheckS = 0;


        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievement_GiantLeapForMankind"; } }
        public override bool NeedsUpdate { get { return !IsAchieved; } }

        public override void Init()
        {
            base.Init();
            if (IsAchieved) return;

            // Load walked distance
            m_metersWalkedOnMoon = MySteam.API.GetStatFloat(WalkedMoonStatName);
        }


        public override void SessionUpdate()
        {
            if (MySession.Static == null || MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.Physics == null) 
                return;

            var playTimeS = MySession.Static.ElapsedPlayTime.TotalSeconds;
            var elapsedTimeS = playTimeS - m_lastCheckS;
            // Check interval.
            if (elapsedTimeS < CHECK_INTERVAL_S)
                return;

            m_lastCheckS = MySession.Static.ElapsedPlayTime.TotalSeconds;

            double velocity = MySession.Static.LocalCharacter.Physics.LinearVelocity.Length();

            if (MyCharacter.IsWalkingState(MySession.Static.LocalCharacter.GetCurrentMovementState()) ||
                MyCharacter.IsRunningState(MySession.Static.LocalCharacter.GetCurrentMovementState()))
            {
                var characterPosition = MySession.Static.LocalCharacter.PositionComp.GetPosition();

                if (IsWalkingOnMoon(MySession.Static.LocalCharacter))
                {
                    m_metersWalkedOnMoon += (float)(elapsedTimeS * velocity);

                    // Store the new exploration data.
                    MySteam.API.SetStat(WalkedMoonStatName, m_metersWalkedOnMoon);
                    m_storedMetersWalkedOnMoon = m_metersWalkedOnMoon;

                    // Final condition.
                    if (m_metersWalkedOnMoon >= DISTANCE_TO_BE_WALKED)
                    {
                        // Done!
                        NotifyAchieved();
                    }
                }
            }          
        }


        private bool IsWalkingOnMoon(MyCharacter character)
        {
            float maxDistValue = MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE;
            var from = character.PositionComp.GetPosition() + character.PositionComp.WorldMatrix.Up * 0.5; //(needs some small distance from the bottom or the following call to HavokWorld.CastRay will find no hits)
            var to = from + character.PositionComp.WorldMatrix.Down * maxDistValue;

            MyPhysics.CastRay(from, to, m_hits, MyPhysics.CollisionLayers.CharacterCollisionLayer);

            // Skips invalid hits (null body, self character)
            int index = 0;
            while ((index < m_hits.Count) && ((m_hits[index].HkHitInfo.Body == null) || (m_hits[index].HkHitInfo.GetHitEntity() == character.Components)))
            {
                index++;
            }

            if (m_hits.Count == 0)
            {
                return false;
            }

            if (index < m_hits.Count)
            {
                // We must take only closest hit (others are hidden behind)
                var h = m_hits[index];
                var entity = h.HkHitInfo.GetHitEntity();

                var sqDist = Vector3D.DistanceSquared((Vector3D)h.Position, from);
                if (sqDist < maxDistValue * maxDistValue)
                {
                    var voxelBase = entity as MyVoxelBase;

                    if (voxelBase != null && voxelBase.Storage != null && voxelBase.Storage.DataProvider != null && voxelBase.Storage.DataProvider is Sandbox.Engine.Voxels.MyPlanetStorageProvider)
                    {
                        var planetProvider = voxelBase.Storage.DataProvider as Sandbox.Engine.Voxels.MyPlanetStorageProvider;
                        if (planetProvider.Generator != null && planetProvider.Generator.FolderName == "Moon")
                        {
                            m_hits.Clear();
                            return true;
                        }
                    }
                }
            }

            m_hits.Clear();
            return false;
        }

    }
}
