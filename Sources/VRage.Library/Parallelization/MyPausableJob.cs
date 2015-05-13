using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace VRage.Parallelization
{
    /// <summary>
    /// Allows to pause one thread at exact points
    /// </summary>
    public class MyPausableJob
    {
        volatile bool m_pause = false;
        AutoResetEvent m_pausedEvent = new AutoResetEvent(false);
        AutoResetEvent m_resumedEvent = new AutoResetEvent(false);

        public bool IsPaused { get { return m_pause; } }

        public void Pause()
        {
            Debug.Assert(!m_pause, "Already paused");
            m_pause = true;
            m_pausedEvent.WaitOne();
        }

        public void Resume()
        {
            Debug.Assert(m_pause, "Not paused");
            m_pause = false;
            m_resumedEvent.Set();
        }

        public void AllowPauseHere()
        {
            if (m_pause)
            {
                m_pausedEvent.Set();
                m_resumedEvent.WaitOne();
            }
        }
    }
}
