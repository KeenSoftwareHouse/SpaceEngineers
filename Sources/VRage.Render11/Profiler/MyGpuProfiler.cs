using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
using VRage.Collections;
using VRage.Library.Utils;

namespace VRageRender
{
    enum MyIssuedQueryEnum
    {
        BlockStart,
        BlockEnd
    }

    struct MyIssuedQuery
    {
        internal string m_tag;
        internal MyQuery m_query;
        internal MyIssuedQueryEnum m_info;

        internal MyIssuedQuery(MyQuery query, string tag, MyIssuedQueryEnum info)
        {
            m_tag = tag;
            m_query = query;
            m_info = info;
        }
    }

    class MyFrameProfilingContext
    {
        internal Queue<MyIssuedQuery> m_issued = new Queue<MyIssuedQuery>(128);
    }

#if !XB1 // XB1_NOPROFILER
    class MyFrameProfiling
    {
        internal MyQuery m_disjoint;
        internal Queue<MyIssuedQuery> m_issued = new Queue<MyIssuedQuery>(128);

        internal bool IsFinished()
        {
            return MyImmediateRC.RC.DeviceContext.IsDataAvailable(m_disjoint.m_query, AsynchronousFlags.DoNotFlush);
        }

        internal void Clear()
        {
            if (m_disjoint != null)
            {
                MyQueryFactory.RelaseDisjointQuery(m_disjoint);
                m_disjoint = null;
            }

            while (m_issued.Count > 0)
            {
                MyQueryFactory.RelaseTimestampQuery(m_issued.Dequeue().m_query);
            }
        }
    }

    class MyGpuProfiler
    {
        static Queue<MyFrameProfiling> m_pooledFrames = new Queue<MyFrameProfiling>(MyQueryFactory.MaxFramesLag);
        static Queue<MyFrameProfiling> m_frames = new Queue<MyFrameProfiling>(MyQueryFactory.MaxFramesLag);
        static MyFrameProfiling m_currentFrame = null;

        static MyGpuProfiler()
        {
            for (int i = 0; i < MyQueryFactory.MaxFramesLag; i++)
            {
                m_pooledFrames.Enqueue(new MyFrameProfiling());
            }
        }

        static void WaitForLastFrame()
        {
            if (m_frames.Count == 0)
            {
                return;
            }

            var front = m_frames.ElementAt(0);
            while (!MyImmediateRC.RC.DeviceContext.IsDataAvailable(front.m_disjoint.m_query))
            {
                Thread.Sleep(1);
            }
        }

        static void GatherFinishedFrames()
        {
            if (m_frames.Count == 0)
            {
                return;
            }

            bool ok = true;
            while (ok && m_frames.Count > 0)
            {
				//this will fail if all frames are finished.
				//ok = m_frames.ElementAt(0).IsFinished();
                ok = m_frames.Count == 0 ? false : m_frames.ElementAt(0).IsFinished();
                if (ok)
                {
                    var frame = m_frames.Dequeue();
                    GatherFrame(frame);
                    m_pooledFrames.Enqueue(frame);
                }
            }
        }

        static Stack<ulong> m_timestampStack = new Stack<ulong>();

        static void GatherFrame(MyFrameProfiling frame)
        {
            QueryDataTimestampDisjoint disjoint = MyImmediateRC.RC.DeviceContext.GetData<QueryDataTimestampDisjoint>(frame.m_disjoint.m_query, AsynchronousFlags.DoNotFlush);

#if UNSHARPER
            if (!disjoint.Disjoint.value)
#else
            if (!disjoint.Disjoint)
#endif
            {
                var freq = disjoint.Frequency;
                double invFreq = 1.0 / (double)freq;

                m_timestampStack.Clear();

                int stackDepth = 0;

                while (frame.m_issued.Count > 0)
                {
                    var q = frame.m_issued.Dequeue();

                    ulong timestamp;
                    MyImmediateRC.RC.DeviceContext.GetData<ulong>(q.m_query, AsynchronousFlags.DoNotFlush, out timestamp);

                    if (q.m_info == MyIssuedQueryEnum.BlockStart)
                    {
                        stackDepth++;
                        MyRender11.GetRenderProfiler().GPU_StartProfilingBlock(q.m_tag);
                        m_timestampStack.Push(timestamp);
                    }
                    else if (q.m_info == MyIssuedQueryEnum.BlockEnd)
                    {
                        stackDepth--;
                        var start = m_timestampStack.Pop();
                        var time = (timestamp - start) * invFreq;

                        // tick is 100 nanoseconds = 10^-7 second
                        MyRender11.GetRenderProfiler().GPU_EndProfilingBlock(0, MyTimeSpan.FromSeconds(time));
                    }

                    Debug.Assert(stackDepth >= 0);

                    MyQueryFactory.RelaseTimestampQuery(q.m_query);
                }

                Debug.Assert(stackDepth == 0);
            }

            frame.Clear();
        }

        internal static void IC_Enqueue(MyIssuedQuery q)
        {
			if (m_currentFrame == null)
				return;
            m_currentFrame.m_issued.Enqueue(q);
        }

        internal static void Join(MyFrameProfilingContext context)
        {
            while(context.m_issued.Count > 0)
            {
                IC_Enqueue(context.m_issued.Dequeue());
            }
        }

        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
        internal static void StartFrame()
        {
            if (m_pooledFrames.Count == 0)
            {
                WaitForLastFrame();
                GatherFinishedFrames();
            }

            m_currentFrame = m_pooledFrames.Dequeue();

            var disjoint = MyQueryFactory.CreateDisjointQuery();
            MyImmediateRC.RC.Begin(disjoint);
            m_currentFrame.m_disjoint = disjoint;

            IC_BeginBlock("Frame");
        }

        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
        internal static void EndFrame()
        {
            if (m_currentFrame == null)
            {
                return;
            }

            IC_EndBlock();
            MyImmediateRC.RC.End(m_currentFrame.m_disjoint);

            m_frames.Enqueue(m_currentFrame);
        }

        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
        internal static void IC_BeginBlock(string tag)
        {
            MyImmediateRC.RC.BeginProfilingBlock(tag);
        }

        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
        internal static void IC_BeginNextBlock(string tag)
        {
            MyImmediateRC.RC.EndProfilingBlock();
            MyImmediateRC.RC.BeginProfilingBlock(tag);
        }
        
        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
        internal static void IC_EndBlock()
        {
            MyImmediateRC.RC.EndProfilingBlock();
        }
    }
#else // XB1
    class MyGpuProfiler
    {
        internal static void IC_BeginBlock(string tag)
        {
        }

        internal static void IC_EndBlock()
        {
        }

        internal static void StartFrame()
        {
        }

        internal static void EndFrame()
        {
        }

        internal static void IC_Enqueue(MyIssuedQuery q)
        {
        }

        internal static void Join(MyFrameProfilingContext context)
        {
        }
    }
#endif // XB1
}
