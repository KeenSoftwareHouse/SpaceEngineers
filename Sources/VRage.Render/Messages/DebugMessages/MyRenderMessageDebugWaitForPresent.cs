using System.Threading;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugWaitForPresent : MyRenderMessageBase
    {
        public EventWaitHandle WaitHandle;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugWaitForPresent; } }
    }
}
