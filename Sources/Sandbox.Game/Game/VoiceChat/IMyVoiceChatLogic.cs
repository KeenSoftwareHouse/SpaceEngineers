using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Data.Audio;
using VRage.Library.Utils;

namespace Sandbox.Game.VoiceChat
{
    public interface IMyVoiceChatLogic
    {
        bool ShouldSendVoice(MyPlayer player);
        bool ShouldPlayVoice(MyPlayer player, MyTimeSpan timestamp, out MySoundDimensions dimension, out float maxDistance);
    }
}
