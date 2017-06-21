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
        long m_elapsedTicks;
        double m_multiplier = 1;

        /// <summary>
        /// Number of ticks per seconds
        /// </summary>
        public static readonly long Frequency = Stopwatch.Frequency;

        public double Multiplier
        {
            get { return m_multiplier; }
            set { m_elapsedTicks = ElapsedTicks; m_startTicks = Stopwatch.GetTimestamp(); m_multiplier = value; }
        }

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
                return m_elapsedTicks + (long)(m_multiplier * (Stopwatch.GetTimestamp() - m_startTicks));
            }
        }

        public MyTimeSpan Elapsed
        {
            get
            {
                return new MyTimeSpan(ElapsedTicks);
            }
        }

        public void AddElapsed(MyTimeSpan timespan)
        {
            m_startTicks -= timespan.Ticks;
        }
        public MyGameTimer()
        {
            m_startTicks = Stopwatch.GetTimestamp();
        }
    }
}
