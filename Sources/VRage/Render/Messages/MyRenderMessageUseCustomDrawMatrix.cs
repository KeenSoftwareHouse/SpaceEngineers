using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUseCustomDrawMatrix : IMyRenderMessage
    {
        public uint ID;
        public MatrixD DrawMatrix;
        public bool Enable;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UseCustomDrawMatrix; } }
    }
}
