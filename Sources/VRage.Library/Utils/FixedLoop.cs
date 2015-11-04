using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
using VRage.Library.Utils;
using VRage.Stats;

namespace Sandbox.Engine.Platform
{
    public class FixedLoop : GenericLoop
    {
        const bool EnableUpdateWait = true;

        static readonly MyGameTimer m_gameTimer = new MyGameTimer();

        // 59.75 is sort of compensation
        public readonly long TickPerFrame = (int)Math.Round(MyGameTimer.Frequency / 59.75f);
        public readonly MyStats StatGroup;
        public readonly string StatName;

        public FixedLoop(MyStats statGroup = null, string statName = null)
        {
            StatGroup = statGroup ?? new MyStats();
            StatName = statName ?? "WaitForUpdate";
        }

        public override void Run(VoidAction tickCallback)
        {
            long targetTicks = 0;
            
            ManualResetEventSlim waiter = new ManualResetEventSlim(false, 0);
            MyTimer.TimerEventHandler handler = new MyTimer.TimerEventHandler((a, b, c, d, e) =>
            {
                waiter.Set();
            });

            base.Run(delegate
            {
                using (StatGroup.Measure(StatName))
                {
                    var currentTicks = m_gameTimer.ElapsedTicks;

                    // Wait for correct frame start
                    targetTicks += TickPerFrame;
                    if (currentTicks > targetTicks + TickPerFrame * 5)
                    {
                        // We're more behind than 5 frames, don't try to catch up
                        targetTicks = currentTicks;
                    }
                    else
                    {
                        // For until correct tick comes
                        if (EnableUpdateWait)
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
            });
        }
    }
}
