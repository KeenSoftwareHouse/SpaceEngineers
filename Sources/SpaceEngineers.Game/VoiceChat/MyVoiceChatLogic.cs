using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.VoiceChat;
using Sandbox.Game.World;
using VRage.Library.Utils;
using VRage.Data.Audio;

namespace SpaceEngineers.Game.VoiceChat
{
    public class MyVoiceChatLogic : IMyVoiceChatLogic
    {
        public bool ShouldSendVoice(MyPlayer player)
        {
            return false;
        }

        public bool ShouldPlayVoice(MyPlayer player, MyTimeSpan timestamp, out MySoundDimensions dimension, out float maxDistance)
        {
            dimension = MySoundDimensions.D2;
            maxDistance = 0;
            return false;
        }
    }
}
