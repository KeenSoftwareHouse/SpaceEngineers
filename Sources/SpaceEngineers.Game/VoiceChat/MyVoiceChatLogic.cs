using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.VoiceChat;
using Sandbox.Game.World;
using VRage.Library.Utils;
using VRage.Data.Audio;
using VRageMath;
using Sandbox;
using Sandbox.Game.GameSystems;

namespace SpaceEngineers.Game.VoiceChat
{
    public class MyVoiceChatLogic : IMyVoiceChatLogic
    {
        private const float VOICE_DISTANCE = 40;
        private const float VOICE_DISTANCE_SQ = VOICE_DISTANCE * VOICE_DISTANCE;

        public bool ShouldSendVoice(MyPlayer player)
        {
            return MyAntennaSystem.Static.CheckConnection(MySession.Static.LocalHumanPlayer.Identity, player.Identity);

            //var character = player.Character;
            //return Vector3D.DistanceSquared(character.PositionComp.GetPosition(), MySession.Static.LocalCharacter.PositionComp.GetPosition()) <= VOICE_DISTANCE_SQ;
        }

        public bool ShouldPlayVoice(MyPlayer player, MyTimeSpan timestamp, out MySoundDimensions dimension, out float maxDistance)
        {
            MyTimeSpan now = MySandboxGame.Static.UpdateTime;
            double startPlaybackMs = VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 30 * 1000;
            if ((now - timestamp).Milliseconds > startPlaybackMs)
            {
                dimension = MySoundDimensions.D3;
                maxDistance = float.MaxValue;
                return true;
            }
            else
            {
                dimension = MySoundDimensions.D2;
                maxDistance = 0;
                return false;
            }
        }
    }
}
