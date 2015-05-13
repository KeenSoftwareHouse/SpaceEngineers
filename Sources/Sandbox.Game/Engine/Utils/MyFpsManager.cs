using Sandbox.Game.Debugging;
using Sandbox.Game.World;

namespace Sandbox.Engine.Utils
{
    static class MyFpsManager
    {
        static long m_lastTime = 0;
        static int m_fpsCounter = 0;
        static int m_sessionTotalFrames = 0;
        static int m_maxSessionFPS = 0;
        static int m_minSessionFPS = int.MaxValue;

        static int m_lastFpsDrawn = 0;

        static long m_lastFrameTime = 0;
        static long m_lastFrameMin = long.MaxValue;
        static long m_lastFrameMax = long.MinValue;

        //  Returns FPS once per second. We can't display actual FPS at every frame, because it will be changing quickly and so unreadable.
        public static int GetFps()
        {
            return m_lastFpsDrawn;
        }

        public static int GetSessionTotalFrames()
        {
            return m_sessionTotalFrames;
        }

        public static int GetMaxSessionFPS()
        {
            return m_maxSessionFPS;
        }

        public static int GetMinSessionFPS()
        {
            return m_minSessionFPS;
        }

        /// <summary>
        /// Returns update + render time of last frame in ms
        /// </summary>
        /// <returns></returns>
        public static float FrameTime { get; private set; }

        /// <summary>
        /// Returns update + render time of last frame (Average in last second)
        /// </summary>
        /// <returns></returns>
        public static float FrameTimeAvg { get; private set; }
        public static float FrameTimeMin { get; private set; }
        public static float FrameTimeMax { get; private set; }

        public static void Update()
        {
            m_fpsCounter++;
            m_sessionTotalFrames++;

            if (MySession.Static == null)
            {
                m_sessionTotalFrames = 0;
                m_maxSessionFPS = 0;
                m_minSessionFPS = int.MaxValue;
            }

            long frameTicks = MyPerformanceCounter.ElapsedTicks - m_lastFrameTime;
            FrameTime = (float)MyPerformanceCounter.TicksToMs(frameTicks);
            m_lastFrameTime = MyPerformanceCounter.ElapsedTicks;

            if (frameTicks > m_lastFrameMax)
            {
                m_lastFrameMax = frameTicks;
            }

            if (frameTicks < m_lastFrameMin)
            {
                m_lastFrameMin = frameTicks;
            }

            long ticksFromLastDraw = MyPerformanceCounter.ElapsedTicks - m_lastTime;
            float msFromLastDraw = (float)MyPerformanceCounter.TicksToMs(ticksFromLastDraw);

            if (msFromLastDraw >= 1000)
            {
                FrameTimeMin = (float)MyPerformanceCounter.TicksToMs(m_lastFrameMin);
                FrameTimeMax = (float)MyPerformanceCounter.TicksToMs(m_lastFrameMax);
                FrameTimeAvg = msFromLastDraw / m_fpsCounter;
                m_lastFrameMin = long.MaxValue;
                m_lastFrameMax = long.MinValue;

                if (MySession.Static != null)
                {
                    m_minSessionFPS = System.Math.Min(m_minSessionFPS, m_fpsCounter);
                    m_maxSessionFPS = System.Math.Max(m_maxSessionFPS, m_fpsCounter);
                }

                m_lastTime = MyPerformanceCounter.ElapsedTicks;
                m_lastFpsDrawn = m_fpsCounter;
                m_fpsCounter = 0;
            }
        }
    }
}
