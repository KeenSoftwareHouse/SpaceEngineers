using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageTakeScreenshot : IMyRenderMessage
    {
        public bool IgnoreSprites;
        public bool Debug;
        public VRageMath.Vector2 SizeMultiplier;
        public string PathToSave;
        public bool ShowNotification;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.TakeScreenshot; } }
    }
}
