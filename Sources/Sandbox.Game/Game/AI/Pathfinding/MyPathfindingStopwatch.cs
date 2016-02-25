using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using VRage.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    static class MyPathfindingStopwatch
    {
        static Stopwatch s_stopWatch = null;
        static Stopwatch s_gloabalStopwatch = null;
        static MyLog s_log = new MyLog();
        const int StopTimeMs = 10000;
        static int s_levelOfStarting = 0; // counter of stating calling

        static MyPathfindingStopwatch()
        {
            s_stopWatch = new Stopwatch();
            s_gloabalStopwatch = new Stopwatch();
            s_log = new MyLog();
        }

        [Conditional("DEBUG")]
        public static void StartMeasuring()
        {
            s_stopWatch.Reset();            // measured time of pure pathfinding
            s_gloabalStopwatch.Reset();     // measured gametime
            s_gloabalStopwatch.Start();
        }
        [Conditional("DEBUG")]
        public static void CheckStopMeasuring()
        {
            if (s_gloabalStopwatch.IsRunning && s_gloabalStopwatch.ElapsedMilliseconds > StopTimeMs)
            {
                StopMeasuring();
            }
        }

        [Conditional("DEBUG")]
        public static void StopMeasuring()  // is called some time after StartMeasuring - like 1 minute or so
        {
            // logging of measured time
            s_gloabalStopwatch.Stop();
            string message = String.Format("pathfinding elapsed time: {0} ms / in {1} ms", s_stopWatch.ElapsedMilliseconds, StopTimeMs);
            s_log.WriteLineAndConsole(message);
        }

        [Conditional("DEBUG")]
        public static void Start()
        {
            if (!s_stopWatch.IsRunning)
            {
                s_stopWatch.Start();
                s_levelOfStarting = 1;
            }
            else
                s_levelOfStarting++;
        }
        [Conditional("DEBUG")]
        public static void Stop()
        {
            if (s_stopWatch.IsRunning)
            {
                s_levelOfStarting--;
                if (s_levelOfStarting == 0)
                    s_stopWatch.Stop();
            }
        }
        [Conditional("DEBUG")]
        public static void Reset()
        {
            s_stopWatch.Reset();
        }
    }
}
