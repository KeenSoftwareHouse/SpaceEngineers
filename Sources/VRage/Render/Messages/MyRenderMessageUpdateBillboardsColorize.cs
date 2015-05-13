using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
   public class MyRenderMessageUpdateBillboardsColorize : IMyRenderMessage
    {
        public bool Enable;
        public Color Color;
        public float Distance;
        public Vector3 Normal;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateBillboardsColorize; } }
    }
}
