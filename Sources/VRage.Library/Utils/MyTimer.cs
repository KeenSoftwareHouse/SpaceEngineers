using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Library.Utils
{
    /// <summary>
    /// Hi-resolution wait timer, internally uses multimedia timer
    /// </summary>
    public sealed class MyTimer : IDisposable
    {
        private int m_interval;
        private Action m_callback;

        private int mTimerId;
		// P/Invoke declarations
		public delegate void TimerEventHandler(int id, int msg, IntPtr user, int dw1, int dw2);

        private TimerEventHandler mHandler; // Must be here to prevent GC collection of delegate

        public MyTimer(int intervalMS, Action callback)
        {
            m_interval = intervalMS;
            m_callback = callback;

            mHandler = new TimerEventHandler(OnTimer);
        }

        private void OnTimer(int id, int msg, IntPtr user, int dw1, int dw2)
        {
            m_callback();
        }

        /// <summary>
        /// Starts one shot periodic timer.
        /// Handler must be STORED somewhere to prevent GC collection until it's called!
        /// </summary>
        public static void StartOneShot(int intervalMS, TimerEventHandler handler)
        {
            timeSetEvent(intervalMS, 1, handler, IntPtr.Zero, TIME_ONESHOT);
        }

        public void Start()
        {
            Debug.Assert(mTimerId == 0, "Timer not disposed before starting again!");

            timeBeginPeriod(1);
            mTimerId = timeSetEvent(m_interval, 1, mHandler, IntPtr.Zero, TIME_PERIODIC);
        }

        public void Stop()
        {
            if (mTimerId != 0)
            {
                timeKillEvent(mTimerId);
                timeEndPeriod(1);
                mTimerId = 0;
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        ~MyTimer()
        {
            Debug.Fail("Timer not disposed!");
            Stop(); // Valid, releases unmanaged resources
        }


        private const int TIME_ONESHOT = 0;
        private const int TIME_PERIODIC = 1;
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimerEventHandler handler, IntPtr user, int eventType);
        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);
        [DllImport("winmm.dll")]
        private static extern int timeBeginPeriod(int msec);
        [DllImport("winmm.dll")]
        private static extern int timeEndPeriod(int msec);
    }
}
