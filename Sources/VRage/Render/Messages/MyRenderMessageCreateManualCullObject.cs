using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateManualCullObject: IMyRenderMessage
    {
        public uint ID;
        public string DebugName;
        public MatrixD WorldMatrix;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateManualCullObject; } }
    }
}
