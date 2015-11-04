using ParallelTasks;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRageRender
{
    class MyRenderingDispatcher
    {
        static List<MyRenderingWork> m_workList = new List<MyRenderingWork>();

        static MyRenderCullResult[] m_renderList = new MyRenderCullResult[50000];
        static Dictionary<MyRenderableProxy, int> m_indirection = new Dictionary<MyRenderableProxy, int>();
        static int m_renderElementsNum;

        static int NumberOfSetBits(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        internal static void ConsumeWork(MyRenderingWork work, Queue<CommandList> accumulator)
        {
            foreach(var pass in work.Passes)
            {
                pass.Join();
            }

            foreach(var rc in work.Contexts)
            {
                accumulator.Enqueue(rc.GrabCommandList());
                rc.Join();
                MyRenderContextPool.FreeRC(rc);
            }
        }

        internal static int GetRenderingThreadsNum()
        {
            int jobsNum = 1;
            if (MyRender11.Settings.EnableParallelRendering)
            {
                jobsNum = Parallel.Scheduler.ThreadCount;

                if (MyRender11.Settings.RenderThreadAsWorker)
                {
                    jobsNum++;
                }
            }
            return jobsNum;
        }

        class MySortingKeysComparerer : IComparer<int>
        {
            internal List<ulong> Values;

            public int Compare(int x, int y)
            {
                return Values[x].CompareTo(Values[y]);
            }
        }

        internal static void ScheduleAndWait(Queue<CommandList> accumulator)
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("ScheduleTasksAndWait");

            if (MyRender11.MultithreadedRenderingEnabled)
            {
                int N = m_workList.Count;

                int parallelStart = MyRender11.Settings.RenderThreadAsWorker ? 1 : 0;

                List<Task> tasks = new List<Task>();
                for (int i = parallelStart; i < N; i++)
                {
                    tasks.Add(Parallel.Start(m_workList[i]));
                }

                if (MyRender11.Settings.RenderThreadAsWorker && m_workList.Count > 0)
                {
                    m_workList[0].DoWork();
                }
                
                foreach (var task in tasks)
                {
                    task.Wait();
                }
            }
            else
            {
                foreach (var w in m_workList)
                {
                    w.DoWork();
                }
            }

            foreach (var w in m_workList)
            {
                ConsumeWork(w, accumulator);
            }

            m_workList.Clear();

            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        [Obsolete("Replaced by Dispatch_LoopPassThenObject")]
        internal static void Dispatch_LoopObjectThenPass(List<MyRenderingPass> queues, MyCullQuery cullingResults, Queue<CommandList> accumulator)
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("PrepareWork");

            m_workList.Clear();
            m_indirection.Clear();
            accumulator.Clear();

            for (int i = 0; i < cullingResults.Size; i++)
            {
                var list = cullingResults.FrustumQueries[i].List;

                for (int j = 0; j < list.Count; j++)
                {
                    for (int k = 0; k < list[j].Proxies.Length; k++)
                    {
                        var proxy = list[j].Proxies[k];
                        var sortKey = list[j].SortingKeys[k];

                        int index;
                        if (!m_indirection.TryGetValue(proxy, out index))
                        {
                            index = m_indirection.Count;
                            m_indirection[proxy] = index;

                            m_renderList[index].SortKey = sortKey;
                            m_renderList[index].RenderProxy = proxy;
                            m_renderList[index].ProcessingMask = new BitVector32();
                        }

                        // merge results
                        m_renderList[index].ProcessingMask = new BitVector32(cullingResults.FrustumQueries[i].Bitmask | m_renderList[index].ProcessingMask.Data);
                    }
                }
            }

            m_renderElementsNum = m_indirection.Count;

            Array.Sort(m_renderList, 0, m_renderElementsNum, MyCullResultsComparer.Instance);

            int jobsNum = GetRenderingThreadsNum();

            if (MyRender11.Settings.AmortizeBatchWork)
            {
                MyRender11.GetRenderProfiler().StartProfilingBlock("WorkAmortization");
                // calc approximated sum of work
                int workSum = 0;
                for (int i = 0; i < m_renderElementsNum; i++)
                {
                    for (int p = 0; p < queues.Count; p++)
                    {
                        var union = (queues[p].ProcessingMask & m_renderList[i].ProcessingMask.Data);
                        workSum += NumberOfSetBits(union);
                    }
                }

                int batchWork = (workSum + jobsNum - 1) / jobsNum;


                int from = 0;
                int work = 0;
                for (int i = 0; i < m_renderElementsNum; i++)
                {
                    for (int p = 0; p < queues.Count; p++)
                    {
                        var union = (queues[p].ProcessingMask & m_renderList[i].ProcessingMask.Data);
                        work += NumberOfSetBits(union);
                    }

                    if (work > batchWork)
                    {
                        List<MyRenderingPass> contextCopy = new List<MyRenderingPass>();
                        foreach (var q in queues)
                        {
                            contextCopy.Add(q.ForkWithNewContext());
                        }
                        m_workList.Add(new MyRenderingWork_LoopObjectThenPass(m_renderList, from, i, contextCopy));

                        from = i;
                        work = 0;
                    }
                }
                if (work > 0)
                {
                    List<MyRenderingPass> contextCopy = new List<MyRenderingPass>();
                    foreach (var q in queues)
                    {
                        contextCopy.Add(q.ForkWithNewContext());
                    }
                    m_workList.Add(new MyRenderingWork_LoopObjectThenPass(m_renderList, from, m_renderElementsNum, contextCopy));
                }

                MyRender11.GetRenderProfiler().EndProfilingBlock();
            }
            else
            {
                int batchWork = (m_renderElementsNum + jobsNum - 1) / jobsNum;

                for (int i = 0; i < jobsNum; i++)
                {
                    List<MyRenderingPass> contextCopy = new List<MyRenderingPass>();
                    foreach (var q in queues)
                    {
                        contextCopy.Add(q.ForkWithNewContext());
                    }
                    m_workList.Add(new MyRenderingWork_LoopObjectThenPass(m_renderList, i * batchWork, Math.Min((i + 1) * batchWork, m_renderElementsNum), contextCopy));
                }

            }
            
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            ScheduleAndWait(accumulator);
        }

        internal static void Dispatch_LoopPassThenObject(List<MyRenderingPass> queues, MyCullQuery cullingResults, Queue<CommandList> accumulator)
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("PrepareWork");

            m_workList.Clear();
            accumulator.Clear();

            List<List<MyRenderCullResultFlat>> passElements = new List<List<MyRenderCullResultFlat>>();
            List<MyRenderableProxy_2[]> passElements2 = new List<MyRenderableProxy_2[]>();

            foreach (var q in queues)
            {
                if(!MyRender11.DeferredContextsEnabled)
                {
                    q.SetImmediate(true);
                }

                passElements.Add(new List<MyRenderCullResultFlat>());
                passElements2.Add(null);
            }

            //bool okAfterCull = false;

            for (int i = 0; i < cullingResults.Size; i++)
            {
                var affectedQueueIds = new List<int>();

                for (int j = 0; j < queues.Count; j++)
                {
                    if ((queues[j].ProcessingMask & cullingResults.FrustumQueries[i].Bitmask) > 0)
                    {
                        affectedQueueIds.Add(j);
                    }
                }

                // proxy 
                var list = cullingResults.FrustumQueries[i].List;
                for (int j = 0; j < list.Count; j++)
                {
                    for (int k = 0; k < list[j].Proxies.Length; k++)
                    {
                        var proxy = list[j].Proxies[k];
                        var sortKey = list[j].SortingKeys[k];

                        for (int l = 0; l < affectedQueueIds.Count; l++)
                        {
                            var item = new MyRenderCullResultFlat();
                            item.RenderProxy = proxy;
                            item.SortKey = sortKey;

                            passElements[affectedQueueIds[l]].Add(item);
                        }
                    }
                }

                // proxy 2
                var list2 = cullingResults.FrustumQueries[i].List2;
                
                // flatten and sort
                List<UInt64> flattenedKeys = new List<ulong>();
                List<int> indirection = new List<int>();
                List<Tuple<int, int>> location = new List<Tuple<int, int>>();

                int c = 0;
                for (int a = 0; a < list2.Count; a++ )
                {
                    for (int b = 0; b < list2[a].SortingKeys.Length; b++ )
                    {
                        flattenedKeys.Add(list2[a].SortingKeys[b]);
                        indirection.Add(c++);
                        location.Add(Tuple.Create(a, b));
                    }
                }

                MyRenderableProxy_2[] flattenedProxies = new MyRenderableProxy_2[c];

                var comparerer = new MySortingKeysComparerer();
                comparerer.Values = flattenedKeys;
                indirection.Sort(0, indirection.Count, comparerer);

                for (int e = 0; e < c; e++ )
                {
                    var l = location[indirection[e]];
                    flattenedProxies[e] = list2[l.Item1].Proxies[l.Item2];
                }
                

                for (int l = 0; l < affectedQueueIds.Count; l++)
                {
                    passElements2[affectedQueueIds[l]] = flattenedProxies;
                }
            }

            foreach (var list in passElements)
            {
                list.Sort(MyCullResultsComparer.Instance);
            }

            int jobsNum = GetRenderingThreadsNum();

            // always amortize this path
            MyRender11.GetRenderProfiler().StartProfilingBlock("WorkAmortization");

            //passElements.RemoveAll(x => x.Count == 0);

            int workSum = 0;
            foreach (var list in passElements)
            {
                workSum += list.Count;
            }

            int batchWork = (workSum + jobsNum - 1) / jobsNum;

            //var renderingWork = new MyRenderingWork_LoopPassObject();
            var subworks = new List<MyRenderingWorkItem>();

            int work = 0;
            for (int i = 0; i < passElements.Count; i++ )
            {
                var list = passElements[i];
                int passBegin = 0;

                subworks.Add(new MyRenderingWorkItem { Pass = queues[i].Fork(), List2 = passElements2[i] });

                while(passBegin < list.Count)
                {
                    var toTake = Math.Min(list.Count - passBegin, batchWork - work);

                    var workItem = new MyRenderingWorkItem();
                    if(toTake < list.Count)
                    {
                        workItem.Pass = queues[i].Fork();
                    }
                    else
                    {
                        workItem.Pass = queues[i];
                    }

                    workItem.Renderables = list;
                    workItem.Begin = passBegin;
                    workItem.End = passBegin + toTake;

                    //renderingWork.m_subworks.Add(workItem);
                    subworks.Add(workItem);

                    passBegin += toTake;
                    work += toTake;

                    if(work == batchWork)
                    {
                        if (MyRender11.DeferredContextsEnabled)
                        {
                            m_workList.Add(new MyRenderingWork_LoopPassThenObject(MyRenderContextPool.AcquireRC(), subworks));
                        }
                        else
                        {
                            m_workList.Add(new MyRenderingWork_LoopPassThenObject(subworks));
                        }

                        work = 0;
                        subworks = new List<MyRenderingWorkItem>();
                        ///renderingWork = new MyRenderingWork_LoopPassObject(, subworks);
                    }
                }                
            }
            if (subworks.Count > 0)
            {
                if (MyRender11.DeferredContextsEnabled)
                {
                    m_workList.Add(new MyRenderingWork_LoopPassThenObject(MyRenderContextPool.AcquireRC(), subworks));
                }
                else
                {
                    m_workList.Add(new MyRenderingWork_LoopPassThenObject(subworks));
                }
            }

            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().EndProfilingBlock();

            ScheduleAndWait(accumulator);
        }
    }
}
