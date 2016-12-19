using System;
using System.Threading;

namespace VRage.Library.Utils
{
    public class WaitForTargetFrameRate
    {
        public long TickPerFrame
        {
            get
            {
                int ticks = (int)Math.Round(MyGameTimer.Frequency / m_targetFrequency);
                return (int)(ticks);
            }
        }

        private long m_targetTicks;

        public bool EnableMaxSpeed = false;  // disables waiting
        private const bool EnableUpdateWait = true;

        private readonly MyGameTimer m_timer;
        private readonly float m_targetFrequency;
        private readonly ManualResetEventSlim m_waiter = new ManualResetEventSlim(false, 0);
        private readonly MyTimer.TimerEventHandler m_handler;

        private int m_delta = 0;

        public WaitForTargetFrameRate(MyGameTimer timer, float targetFrequency = 59.75f)
        {
            m_timer = timer;
            m_targetFrequency = targetFrequency;
            m_handler = new MyTimer.TimerEventHandler((a, b, c, d, e) =>
            {
                m_waiter.Set();
            });
        }

        public void SetNextFrameDelayDelta(int delta)
        {
            m_delta = delta;
        }
        public void Wait()
        {
            m_timer.AddElapsed(MyTimeSpan.FromMilliseconds(-m_delta));

            var currentTicks = m_timer.ElapsedTicks;

            // Wait for correct frame start
            m_targetTicks += TickPerFrame;
            if ((currentTicks > m_targetTicks + TickPerFrame * 5) || EnableMaxSpeed)
            {
                // We're more behind than 5 frames, don't try to catch up
                // (or we just do not want to wait in EnableMaxSpeed mode)
                m_targetTicks = currentTicks;
            }
            else
            {
                // For until correct tick comes
                if (EnableUpdateWait)
                {
                    var remaining = MyTimeSpan.FromTicks(m_targetTicks - currentTicks);
                    int waitMs = (int)(remaining.Milliseconds - 0.1); // To handle up to 0.1ms inaccuracy of timer
                    if (waitMs > 0)
                    {
                        m_waiter.Reset();
                        MyTimer.StartOneShot(waitMs, m_handler);
                        m_waiter.Wait(17 + m_delta); // Never wait more than 17ms
                        //Debug.Assert(MyPerformanceCounter.ElapsedTicks < m_targetTicks);
                        //VRageRender.MyRenderStats.Write("WaitRemaining", (float)MyPerformanceCounter.TicksToMs(m_targetTicks - MyPerformanceCounter.ElapsedTicks), VRageRender.MyStatTypeEnum.MinMaxAvg, 300, 3);
                    }
                }

                // Sanity check to prevent freezing in the loop, wait at most 1 and 1/4 of frame
                if (m_targetTicks < (m_timer.ElapsedTicks + TickPerFrame + TickPerFrame / 4))
                {
                    while (m_timer.ElapsedTicks < m_targetTicks) { }  // Busy wait for the precise moment
                }
                else  // Something went terribly wrong, reset target ticks
                {
                    m_targetTicks = m_timer.ElapsedTicks;
                }
            }
            m_delta = 0;
        }
    }
}
