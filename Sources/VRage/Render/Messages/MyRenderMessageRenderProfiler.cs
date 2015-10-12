using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public enum RenderProfilerCommand
    {
        Enable,
        JumpToLevel,
        Pause,
        NextFrame,
        PreviousFrame,
        NextThread,
        PreviousThread,
        IncreaseLevel,
        DecreaseLevel,
        IncreaseLocalArea,
        DecreaseLocalArea,
        FindMaxChild,
        IncreaseRange,
        DecreaseRange,
        Reset,
    }

    public class MyRenderMessageRenderProfiler : IMyRenderMessage
    {
        public RenderProfilerCommand Command;
        public int Index;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.RenderProfiler; } }
    }
}
