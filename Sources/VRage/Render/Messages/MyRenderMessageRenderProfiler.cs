using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public enum RenderProfilerCommand
    {
        /// <summary>
        /// Only enables the profiler, used by the developer debug window
        /// </summary>
        Enable,

        /// <summary>
        /// Toggles profiler enabled/disabled state, doesn't reset profiler level
        /// </summary>
        ToggleEnabled,

        JumpToLevel,

        /// <summary>
        /// Jumps all the way to the root element
        /// </summary>
        JumpToRoot,

        Pause,
        
        NextFrame,
        PreviousFrame,

        /// <summary>
        /// Disables the current selection again
        /// </summary>
        DisableFrameSelection,

        NextThread,
        PreviousThread,
        IncreaseLevel,
        DecreaseLevel,
        IncreaseLocalArea,
        DecreaseLocalArea,
        IncreaseRange,
        DecreaseRange,
        Reset,
        SetLevel,

        /// <summary>
        /// Changes the profiler's sorting order, see ProfilerSortingOptions for the possible sorting options
        /// </summary>
        ChangeSortingOrder,

        /// <summary>
        /// Copies the current path to clipboard
        /// </summary>
        CopyPathToClipboard,

        /// <summary>
        /// Tries to navigate to the path in the clipboard
        /// </summary>
        TryGoToPathInClipboard,
    }

    /// <summary>
    /// Profiler sorting order types
    /// </summary>
    public enum RenderProfilerSortingOrder
    {
        /// <summary>
        /// Order in which the elements are logged
        /// </summary>
        Id = 0,

        /// <summary>
        /// Milliseconds spent in the previous frame, sorted from slowest to fastest
        /// </summary>
        MillisecondsLastFrame,

        /// <summary>
        /// Milliseconds spent on average, sorted from slowest to fastest
        /// </summary>
        MillisecondsAverage,

        /// <summary>
        /// Total number of sorting types
        /// </summary>
        NumSortingTypes,
    }

    public class MyRenderMessageRenderProfiler : MyRenderMessageBase
    {
        public RenderProfilerCommand Command;
        public int Index;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RenderProfiler; } }
    }
}
