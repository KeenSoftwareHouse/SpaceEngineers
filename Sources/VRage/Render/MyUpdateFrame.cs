using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Library.Utils;

namespace VRageRender
{
    /// <summary>
    /// Contains data produced by update frame, sent to render in thread-safe manner
    /// </summary>
    public class MyUpdateFrame
    {
        public bool Processed;
        public MyTimeSpan UpdateTimestamp;

        public readonly MyConcurrentSortableQueue<IMyRenderMessage> RenderInput = new MyConcurrentSortableQueue<IMyRenderMessage>(2048);
    }
}
