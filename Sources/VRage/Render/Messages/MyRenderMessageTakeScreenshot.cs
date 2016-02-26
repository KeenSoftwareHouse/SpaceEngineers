using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageTakeScreenshot : MyRenderMessageBase
    {
        public bool IgnoreSprites;
        public bool Debug;
        public VRageMath.Vector2 SizeMultiplier;
        public string PathToSave;
        public bool ShowNotification;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.TakeScreenshot; } }
    }
}
