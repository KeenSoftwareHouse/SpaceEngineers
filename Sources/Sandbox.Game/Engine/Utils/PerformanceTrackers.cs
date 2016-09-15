using System.Diagnostics;
using ParallelTasks;
using VRage;
using VRage.Collections;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    public class MyDebugHitCounter
    {
        public struct Sample
        {
            public uint Count;
            public uint Cycle;

            public float Value { get { return (float)Count / Cycle; } }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        private FastResourceLock m_lock = new FastResourceLock();

        public readonly MyQueue<Sample> History;

        private Sample current;

        private uint m_sampleCycle;
        private uint m_HistoryLength;

        public MyDebugHitCounter(uint cycleSize = 100000)
        {
            m_sampleCycle = cycleSize;
            m_HistoryLength = 10;

            History = new MyQueue<Sample>((int)m_HistoryLength);
        }

        public float CurrentHitRatio
        {
            get { return current.Value; }
        }

        public float LastCycleHitRatio
        {
            get
            {
                if (History.Count > 1)
                    return (float)History[1].Value;
                else
                    return 0f;
            }
        }

        [Conditional(VRage.Profiler.MyRenderProfiler.PerformanceProfilingSymbol)]
        public void Hit()
        {
            current.Count++;
            Miss();
        }

        [Conditional(VRage.Profiler.MyRenderProfiler.PerformanceProfilingSymbol)]
        public void Miss()
        {
            current.Cycle++;
            if (current.Cycle == m_sampleCycle)
            {
                Cycle();
            }
        }

        public void Cycle()
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                if (History.Count >= m_HistoryLength) History.Dequeue();
                History.Enqueue(current);
                current = new Sample();
            }
        }

        public float ValueAndCycle()
        {
            Cycle();
            return LastCycleHitRatio;

        }

        /* Cycle only if actual work was performed. */
        public void CycleWork()
        {
            if (current.Count > 0) Cycle();
        }
    }

    public class MyDebugWorkTracker<T> where T : new()
    {
        private SpinLockRef m_lock = new SpinLockRef();

        public readonly MyQueue<T> History;

        public T Current;

        private uint m_historyLength;

        public MyDebugWorkTracker(uint historyLength = 10)
        {
            m_historyLength = historyLength;

            History = new MyQueue<T>((int)m_historyLength);
        }

        [Conditional(VRage.Profiler.MyRenderProfiler.PerformanceProfilingSymbol)]
        public void Wrap()
        {
            using (m_lock.Acquire())
            {
                if (History.Count >= m_historyLength) History.Dequeue();
                History.Enqueue(Current);
                Current = new T();
            }
        }
    }

    public static class MyDebugWorkTrackerExtensions
    {
        [Conditional(VRage.Profiler.MyRenderProfiler.PerformanceProfilingSymbol)]
        public static void Hit(this MyDebugWorkTracker<int> self)
        {
            self.Current++;
        }

        public static int Min(this MyDebugWorkTracker<int> self)
        {
            int min = int.MaxValue;

            int len = self.History.Count;
            for (int i = 0; i < len; ++i)
            {
                var ithvalue = self.History[i];
                if (min > ithvalue)
                {
                    min = ithvalue;
                }
            }

            return min;
        }

        public static int Max(this MyDebugWorkTracker<int> self)
        {
            int max = int.MinValue;

            int len = self.History.Count;
            for (int i = 0; i < len; ++i)
            {
                var ithvalue = self.History[i];
                if (max < ithvalue)
                {
                    max = ithvalue;
                }
            }

            return max;
        }
        
        public static int Average(this MyDebugWorkTracker<int> self)
        {
            long average = 0;

            int len = self.History.Count;
            for (int i = 0; i < len; ++i)
            {
                average += self.History[i];
            }

            return (int) (average / len);
        }

        /**
         * Returns last/min/avg/max out of the history
         */
        public static Vector4I Stats(this MyDebugWorkTracker<int> self)
        {
            if (self.History.Count == 0)
                return new Vector4I(0,0,0,0);

            long average = 0;
            int min = int.MaxValue;
            int max = int.MinValue;

            Vector4I stats;

            int len = self.History.Count;
            for (int i = 0; i < len; ++i)
            {
                var ithvalue = self.History[i];
                if (max < ithvalue)
                    max = ithvalue;
                if (min > ithvalue)
                    min = ithvalue;
                average += ithvalue;
            }

            stats.X = self.History[len-1];
            stats.Y = min;
            stats.Z = (int) (average/len);
            stats.W = max;

            return stats;
        }
    }
}