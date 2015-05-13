using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderObject : IMyRenderMessage
    {
        public uint ID;
        public MatrixD WorldMatrix;
        public bool SortIntoCulling;
        public BoundingBoxD? AABB;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderObject; } }
    }
}
