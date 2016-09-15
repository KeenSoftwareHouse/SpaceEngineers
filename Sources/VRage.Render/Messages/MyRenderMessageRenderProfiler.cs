using VRage.Profiler;
using VRageRender.Profiler;

namespace VRageRender.Messages
{
    public class MyRenderMessageRenderProfiler : MyRenderMessageBase
    {
        public RenderProfilerCommand Command;
        public int Index;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RenderProfiler; } }
    }
}
