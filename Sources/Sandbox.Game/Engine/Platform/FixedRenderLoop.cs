using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Sandbox.Engine.Utils;
using VRage;
using VRage.Utils;
using VRage.Utils;
using VRage;
using VRage.Library.Utils;

namespace Sandbox.Engine.Platform
{
    public class FixedRenderLoop : GenericRenderLoop
    {
        static readonly MyGameTimer m_gameTimer = new MyGameTimer();

        public override void Run(VoidAction tickCallback)
        {
            // 59.75 is sort of compensation
            long ticksPerFrame = (int)Math.Round(MyGameTimer.Frequency / 59.75f);
            long targetTicks = 0;

            MyLog.Default.WriteLine("Timer Frequency: " + MyGameTimer.Frequency);
            MyLog.Default.WriteLine("Ticks per frame: " + ticksPerFrame);

            ManualResetEventSlim waiter = new ManualResetEventSlim(false, 0);
            MyTimer.TimerEventHandler handler = new MyTimer.TimerEventHandler((a, b, c, d, e) =>
            {
                waiter.Set();
            });


            base.Run(delegate
            {
                using (Stats.Generic.Measure("WaitForUpdate"))
                {
                    var currentTicks = m_gameTimer.ElapsedTicks;

                    // Wait for correct frame start
                    targetTicks += ticksPerFrame;
                    if (currentTicks > targetTicks + ticksPerFrame * 5)
                    {
                        // We're more behind than 5 frames, don't try to catch up
                        targetTicks = currentTicks;
                    }
                    else
                    {
                        // For until correct tick comes
                        if (MyFakes.ENABLE_UPDATE_WAIT)
                        {
                            var remaining = MyTimeSpan.FromTicks(targetTicks - currentTicks);
                            int waitMs = (int)(remaining.Miliseconds - 0.1); // To handle up to 0.1ms inaccuracy of timer
                            if (waitMs > 0)
                            {
                                waiter.Reset();
                                MyTimer.StartOneShot(waitMs, handler);
                                waiter.Wait(17); // Never wait more than 17ms
                                //Debug.Assert(MyPerformanceCounter.ElapsedTicks < targetTicks);
                                //VRageRender.MyRenderStats.Write("WaitRemaining", (float)MyPerformanceCounter.TicksToMs(targetTicks - MyPerformanceCounter.ElapsedTicks), VRageRender.MyStatTypeEnum.MinMaxAvg, 300, 3);
                            }
                        }
                        while (m_gameTimer.ElapsedTicks < targetTicks) ;
                    }
                }

                //UpdateInternal();
                tickCallback();

                ProfilerShort.Commit();
            });
        }
    }
}
