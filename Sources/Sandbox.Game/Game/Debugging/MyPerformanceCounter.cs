
using System.Text;
using Sandbox.AppCode.Game.Sessions;
using Sandbox.Game.World;
using System.Linq;
using System;
using Sandbox.Engine.Utils;
using VRage.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.Gui;
using VRage;
using VRageMath;

//  This class is used for measurements like drawn triangles, number of textures loaded, etc.
//  IMPORTANT: Use this class only for profiling / debuging. Don't use it for real game code.

namespace Sandbox.Game.Debugging
{
    static class MyPerformanceCounter
    {
        struct Timer
        {
            public static readonly Timer Empty = new Timer() { Runtime = 0, StartTime = long.MaxValue };

            public long StartTime;
            public long Runtime;
            //public string StackTrace;

            public bool IsRunning { get { return StartTime != long.MaxValue; } }
        }

        static Stopwatch m_timer = new Stopwatch();

        static MyPerformanceCounter()
        {
            m_timer.Start();
        }

        public static double TicksToMs(long ticks)
        {
            return ticks / (double)Stopwatch.Frequency * 1000.0;
        }

        public static long ElapsedTicks
        {
            get
            {
                return m_timer.ElapsedTicks;
            }
        }

    }

}
