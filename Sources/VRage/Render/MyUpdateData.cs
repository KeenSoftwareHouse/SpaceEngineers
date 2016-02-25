using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRageRender
{
    // TODO: OP! Add message count logging (to make sure we clean everything up properly)
    class MyUpdateData
    {
        MyConcurrentPool<MyUpdateFrame> m_frameDataPool;
        MyConcurrentQueue<MyUpdateFrame> m_updateDataQueue;

        public MyUpdateFrame CurrentUpdateFrame { get; private set; }

        public MyUpdateData()
        {
            m_frameDataPool = new MyConcurrentPool<MyUpdateFrame>(5, true);
            m_updateDataQueue = new MyConcurrentQueue<MyUpdateFrame>(5);

            CurrentUpdateFrame = m_frameDataPool.Get();
        }

        /// <summary>
        /// Commits current frame as atomic operation and prepares new frame
        /// </summary>
        public void CommitUpdateFrame()
        {
            VRage.Library.Utils.MyTimeSpan lastUpdateTimestamp = CurrentUpdateFrame.UpdateTimestamp;

            CurrentUpdateFrame.Processed = false;
            m_updateDataQueue.Enqueue(CurrentUpdateFrame);
            CurrentUpdateFrame = m_frameDataPool.Get();

            CurrentUpdateFrame.UpdateTimestamp = lastUpdateTimestamp;
        }

        /// <summary>
        /// Gets next frame for rendering, can return null in case there's nothing for rendering (no update frame submitted).
        /// When isPreFrame is true, don't handle draw messages, just process update messages and call method again.
        /// Pre frame must release messages and must be returned.
        /// Final frame is kept unmodified in queue, in case of slower update, so we can interpolate and draw frame again.
        /// </summary>
        public MyUpdateFrame GetRenderFrame(out bool isPreFrame)
        {
            if (m_updateDataQueue.Count > 1)
            {
                isPreFrame = true;
                return m_updateDataQueue.Dequeue();
            }

            isPreFrame = false;

            MyUpdateFrame frame;
            return m_updateDataQueue.TryPeek(out frame) ? frame : null;
        }

        /// <summary>
        /// PreFrame must be empty in this place
        /// </summary>
        public void ReturnPreFrame(MyUpdateFrame frame)
        {
            Debug.Assert(frame.RenderInput.Count == 0);
            m_frameDataPool.Return(frame);
        }
    }
}
