using Sandbox.Game.Entities.Character;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement : Smile And Wave
     * 
     * Wave to an enemy that is also waving back within 5 meters looking at you in Survival.
     */
    public class MyAchievement_SmileAndWave : MySteamAchievementBase
    {
        private const string WAVE_ANIMATION_NAME = "RightHand/Wave";
        private MyStringId m_waveAnimationId;
        private MyCharacter m_localCharacter;

        public override string AchievementTag { get { return "MyAchievement_SmileAndWave"; } }
        public override bool NeedsUpdate { get { return m_localCharacter == null; } }

        public override void SessionBeforeStart()
        {
            // Do not initialize in creative mode
            if(MySession.Static.CreativeMode) return;

            m_waveAnimationId = MyStringId.GetOrCompute("wave");
            // To reset the local character to null state
            m_localCharacter = null;
        }

        public override void SessionUpdate()
        {
            // Lazy init
            m_localCharacter = MySession.Static.LocalCharacter;
            if (m_localCharacter != null)
            {

                // Animations triggered event registration
                MySession.Static.LocalCharacter.AnimationController.ActionTriggered += AnimationControllerOnActionTriggered;
            }
        }

        private void AnimationControllerOnActionTriggered(MyStringId animationAction)
        {
            if (animationAction != m_waveAnimationId)
                return;

            var localPlayerPosition = MySession.Static.LocalCharacter.PositionComp.GetPosition();
            var localPlayerFaction = MySession.Static.Factions.GetPlayerFaction(MySession.Static.LocalPlayerId);
            var localPlayerFactionId = localPlayerFaction == null ? 0 : localPlayerFaction.FactionId;
            foreach (var onlinePlayer in MySession.Static.Players.GetOnlinePlayers())
            {
                // null check for dedicated server lazy character init
                if(onlinePlayer.Character == null || onlinePlayer.Character == MySession.Static.LocalCharacter) continue;

                var position = onlinePlayer.Character.PositionComp.GetPosition();
                double distanceSqrt; 
                Vector3D.DistanceSquared(ref position, ref localPlayerPosition, out distanceSqrt);
                // Distance of 5m
                if (distanceSqrt < 25)
                {
                    // 0. Player is an enemy
                    var otherPlayerFaction = MySession.Static.Factions.GetPlayerFaction(onlinePlayer.Identity.IdentityId);
                    var otherPlayersFactionId = otherPlayerFaction == null ? 0 : otherPlayerFaction.FactionId;
                    if (MySession.Static.Factions.AreFactionsEnemies(localPlayerFactionId, otherPlayersFactionId))
                    {
                        // 1. Playing Wave animation?
                        if (IsPlayerWaving(onlinePlayer.Character))
                        {
                            // 2. Looking at each other
                            if (PlayersLookingFaceToFace(MySession.Static.LocalCharacter, onlinePlayer.Character))
                            {
                                NotifyAchieved();
                                MySession.Static.LocalCharacter.AnimationController.ActionTriggered -= AnimationControllerOnActionTriggered;
                                return;
                            }
                        }
                    }
                }
            }
        }

        private bool PlayersLookingFaceToFace(MyCharacter firstCharacter, MyCharacter secondCharacter)
        {
            // Angle between head direction vectors between 120 and 240 angles.
            var firstHeadVector = firstCharacter.GetHeadMatrix(false).Forward;
            var secondHeadVector = secondCharacter.GetHeadMatrix(false).Forward;
            double dot; Vector3D.Dot(ref firstHeadVector, ref secondHeadVector, out dot);
            return dot < -0.5;
        }

        private bool IsPlayerWaving(MyCharacter character)
        {
            var controller = character.AnimationController.Controller;

            for (var index = 0; index < controller.GetLayerCount(); index++)
            {
                var layer = controller.GetLayerByIndex(index);
                if (layer.CurrentNode != null && layer.CurrentNode.Name != null && layer.CurrentNode.Name == WAVE_ANIMATION_NAME)
                {
                    return true;
                } 
            }

            return false;
        }
    }
}
