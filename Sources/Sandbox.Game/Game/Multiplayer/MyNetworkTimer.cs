using Sandbox.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    /// <summary>
    /// Returns continuous server time including network lag
    /// For server it's real time, and speed won't ever change
    /// </summary>
    class MyNetworkTimer
    {
        static readonly int AverageWindowSampleCount = 20;
        static readonly int SmoothWindowSampleCount = 10;

        Stopwatch m_timer = Stopwatch.StartNew();
        TimeSpan m_serverTimeOffset;

        /// <summary>
        /// Deltas of server times, localTime - serverTime
        /// </summary>
        Queue<TimeSpan> m_deltas = new Queue<TimeSpan>();

        // Counting of the frame time in ms
        TimeSpan m_lastTick = TimeSpan.Zero;
        int m_lastFrameMs = 0;
        double m_frameError = 0;

        public void GetDeltas(List<TimeSpan> toList)
        {
            toList.Clear();
            foreach (var x in m_deltas)
                toList.Add(x);
        }

        public double AverageDeltaMilliseconds
        {
            get
            {
                TimeSpan sum = TimeSpan.Zero;
                foreach (var x in m_deltas)
                    sum += x;
                return sum.TotalMilliseconds / m_deltas.Count;
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                return m_timer.Elapsed + m_serverTimeOffset;
            }
        }

        public int LastFrameTime
        {
            get
            {
                return m_lastFrameMs;
            }
        }

        public void InterpolateCorrection()
        {
            if (m_deltas.Count == 0)
                return;

            float smoothTime = 12;

            // Calculate correction and apply it
            TimeSpan correction = TimeSpan.FromMilliseconds(AverageDeltaMilliseconds / smoothTime);
            m_serverTimeOffset -= correction;
        }

        // Called only on clients
        public void AddSample(TimeSpan receiveTime, TimeSpan serverTime)
        {
            float windowSize = 15;

            // Best result:
            // Ticks + offset = servertime
            var delta = receiveTime - serverTime;
            if (m_deltas.Count == 0 || Math.Abs(delta.TotalSeconds) > 1.5f)
            {
                m_deltas.Clear();
                m_serverTimeOffset = serverTime - m_timer.Elapsed;
                for (int i = 0; i < windowSize; i++)
                {
                    m_deltas.Enqueue(TimeSpan.Zero);
                }
                //var newDelta = CurrentTicks - serverTime;
                return;
            }

            //return;
            // Add new delta sample
            m_deltas.Enqueue(delta);
            while (m_deltas.Count > windowSize)
                m_deltas.Dequeue();

            // TODO: incrementing it slowly over each frame would be better (now it's smoothen only in frames when time is received)
            InterpolateCorrection();
        }

        public void Tick()
        {
            TimeSpan dt = CurrentTime - m_lastTick;
            if (m_lastTick == TimeSpan.Zero)
            {
                // Initialization
                m_lastFrameMs = VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
                m_lastTick += dt;
            }
            else
            {
                double ms = dt.TotalMilliseconds + m_frameError;
                m_lastFrameMs = (int)Math.Floor(ms);
                m_frameError = ms - m_lastFrameMs;
            }
        }
    }
}
