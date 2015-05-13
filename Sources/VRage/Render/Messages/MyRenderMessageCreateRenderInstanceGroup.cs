using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateRenderBatch : IMyRenderMessage
    {
        public uint ID;
        public string DebugName;
        public MatrixD WorldMatrix;
        public RenderFlags Flags;

        public List<MyRenderBatchPart> RenderBatchParts = new List<MyRenderBatchPart>();

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateRenderBatch; } }
    }
}
