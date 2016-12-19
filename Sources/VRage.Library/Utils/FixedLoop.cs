using VRage.Library.Utils;
using VRage.Stats;

namespace Sandbox.Engine.Platform
{
    public class FixedLoop : GenericLoop
    {
        static readonly MyGameTimer m_gameTimer = new MyGameTimer();

        public long TickPerFrame { get { return m_waiter.TickPerFrame; } }
          
        public readonly MyStats StatGroup;
        public readonly string StatName;

        public bool EnableMaxSpeed
        {
            get { return m_waiter.EnableMaxSpeed;  }
            set { m_waiter.EnableMaxSpeed = value; }
        }

        private readonly WaitForTargetFrameRate m_waiter = new WaitForTargetFrameRate(m_gameTimer);

        public FixedLoop(MyStats statGroup = null, string statName = null)
        {
            StatGroup = statGroup ?? new MyStats();
            StatName = statName ?? "WaitForUpdate";
        }

        public void SetNextFrameDelayDelta(int delta)
        {
            m_waiter.SetNextFrameDelayDelta(delta);
        }

        public override void Run(VoidAction tickCallback)
        {
            base.Run(delegate
            {
                using (StatGroup.Measure(StatName))
                {
                    m_waiter.Wait();
                }

                //UpdateInternal();
                tickCallback();
            });
        }
    }
}
