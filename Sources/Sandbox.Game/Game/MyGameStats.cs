using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game
{
    // MW: So far thats everything. It will be used for server stats mostly.
    class MyGameStats
    {
        public static MyGameStats Static { get; private set; }

        private DateTime m_lastStatMeasurePerSecond;
        private long m_previousUpdateCount;

        public long UpdateCount
        {
            get;
            private set;
        }

        public long UpdatesPerSecond
        {
            get;
            private set;
        }

        static MyGameStats()
        {
            Static = new MyGameStats();
        }

        private MyGameStats()
        {
            m_previousUpdateCount = 0;
            UpdateCount = 0;
        }

        public void Update()
        {
            UpdateCount++;

            double totalSeconds = (DateTime.UtcNow - m_lastStatMeasurePerSecond).TotalSeconds;
            if (totalSeconds >= 1.0)
            {
                UpdatesPerSecond = (UpdateCount - m_previousUpdateCount);
                m_previousUpdateCount = UpdateCount;
                m_lastStatMeasurePerSecond = DateTime.UtcNow;
                if (MyFakes.ENABLE_NETGRAPH)
                {
                    MyHud.Netgraph.UpdatesPerSecond = UpdatesPerSecond;
                    MyHud.Netgraph.FramesPerSecond = MyFpsManager.GetFps();
                }
            }
        }
    }
}
