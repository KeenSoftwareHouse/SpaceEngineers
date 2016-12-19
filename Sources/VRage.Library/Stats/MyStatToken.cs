using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Utils;

namespace VRage.Stats
{
    public struct MyStatToken : IDisposable
    {
        private readonly MyGameTimer m_timer;
        private readonly MyTimeSpan m_startTime;
        private readonly MyStat m_stat;

        internal MyStatToken(MyGameTimer timer, MyStat stat)
        {
            m_timer = timer;
            m_startTime = timer.Elapsed;
            m_stat = stat;
        }

        public void Dispose()
        {
            m_stat.Write((float)(m_timer.Elapsed - m_startTime).Milliseconds);
        }
    }
}
