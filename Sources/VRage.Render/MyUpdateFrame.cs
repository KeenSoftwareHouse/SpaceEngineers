using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParallelTasks;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRageRender.Messages;

namespace VRageRender
{
    /// <summary>
    /// Contains data produced by update frame, sent to render in thread-safe manner
    /// </summary>
    public class MyUpdateFrame
    {
        public bool Processed;
        public MyTimeSpan UpdateTimestamp;

        public readonly List<MyRenderMessageBase> RenderInput = new List<MyRenderMessageBase>(2048);

        private readonly SpinLockRef m_lock = new SpinLockRef();

        public void Enqueue(MyRenderMessageBase message)
        {
            using (m_lock.Acquire())
                RenderInput.Add(message);
        }
    }
}
