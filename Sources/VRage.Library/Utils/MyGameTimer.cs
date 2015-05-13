using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Library.Utils
{
    /// <summary>
    /// Global thread-safe timer.
    /// Time for update and time for draw must be copied at the beginning of update and draw.
    /// </summary>
    public class MyGameTimer
    {
        long m_startTicks;

        /// <summary>
        /// Number of ticks per seconds
        /// </summary>
        public static readonly long Frequency = Stopwatch.Frequency;

        /// <summary>
        /// This may not be accurate for large values - double accuracy
        /// </summary>
        public TimeSpan ElapsedTimeSpan
        {
            get
            {
                return Elapsed.TimeSpan;
            }
        }

        public long ElapsedTicks
        {
            get
            {
                return Stopwatch.GetTimestamp() - m_startTicks;
            }
        }

        public MyTimeSpan Elapsed
        {
            get
            {
                return new MyTimeSpan(Stopwatch.GetTimestamp() - m_startTicks);
            }
        }

        public MyGameTimer()
        {
            m_startTicks = Stopwatch.GetTimestamp();
        }
    }
}
