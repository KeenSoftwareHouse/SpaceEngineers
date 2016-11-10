using ParallelTasks;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Generics;
using VRage.Profiler;
using VRage.Import;
using VRageRender.Import;
using VRage.Render11.Common;

namespace VRageRender
{
    internal class MyRenderingDispatcher
    {
        private class MySortingKeysComparer : IComparer<int>
        {
            internal List<ulong> Values;

            public int Compare(int x, int y)
            {
                return Values[x].CompareTo(Values[y]);
            }
        }
        private readonly MySortingKeysComparer m_sortingKeysComparer = new MySortingKeysComparer();

        static readonly List<MyRenderingWork> m_workList = new List<MyRenderingWork>();

        private readonly List<ulong> m_resultSortedKeys = new List<ulong>();
        private readonly Dictionary<ulong, List<MyPassCullResult>> m_tmpSortListDictionary = new Dictionary<ulong, List<MyPassCullResult>>();
        private readonly MyDynamicObjectPool<List<MyRenderCullResultFlat>> m_resultListPool = new MyDynamicObjectPool<List<MyRenderCullResultFlat>>(8);
        private readonly MyDynamicObjectPool<List<MyPassCullResult>> m_sortListPool = new MyDynamicObjectPool<List<MyPassCullResult>>(8);

        private readonly List<Task> m_taskList = new List<Task>(8);

        private readonly List<List<MyRenderCullResultFlat>> m_passElements = new List<List<MyRenderCullResultFlat>>();
        private readonly List<MyRenderableProxy_2[]> m_passElements2 = new List<MyRenderableProxy_2[]>();
        private readonly List<int> m_affectedQueueIds = new List<int>();
        private readonly List<ulong> m_flattenedKeys = new List<ulong>();
        private readonly List<int> m_indirectionList = new List<int>();
        private readonly List<MyTuple<int, int>> m_location = new List<MyTuple<int, int>>();

        private readonly List<MyRenderingWorkItem> m_subworks = new List<MyRenderingWorkItem>();

        internal void RecordCommandLists(MyCullQuery processedCullQuery, Queue<CommandList> outCommandLists)
        {
            ProfilerShort.Begin("PrepareWork");

            ProfilerShort.Begin("Init");
            Debug.Assert(m_workList.Count == 0, "Work list not cleared after use!");

            foreach (List<MyRenderCullResultFlat> cullResults in m_passElements)
            {
                cullResults.Clear();
                m_resultListPool.Deallocate(cullResults);
            }
            m_passElements.Clear();
            m_passElements2.Clear();

            for (int renderPassIndex = 0; renderPassIndex < processedCullQuery.Size; ++renderPassIndex )
            {
                if (!MyRender11.DeferredContextsEnabled)
                {
                    processedCullQuery.RenderingPasses[renderPassIndex].SetImmediate(true);
                }

                m_passElements.Add(m_resultListPool.Allocate());
                m_passElements2.Add(null);
            }

            ProfilerShort.BeginNextBlock("Flatten");
            for (int i = 0; i < processedCullQuery.Size; ++i)
            {
                m_affectedQueueIds.SetSize(0);
                var frustumQuery = processedCullQuery.FrustumCullQueries[i];

                for (int renderPassIndex = 0; renderPassIndex < processedCullQuery.Size; renderPassIndex++)
                {
                    if ((processedCullQuery.RenderingPasses[renderPassIndex].ProcessingMask & frustumQuery.Bitmask) > 0)
                    {
                        m_affectedQueueIds.Add(renderPassIndex);
                    }
                }

                var cullProxies = frustumQuery.List;
                var queryType = frustumQuery.Type;

                foreach (MyCullProxy cullProxy in cullProxies)
                {
                    var renderableProxies = cullProxy.RenderableProxies;
                    if (renderableProxies == null)
                        continue;

                    for (int proxyIndex = 0; proxyIndex < renderableProxies.Length; ++proxyIndex)
                    {
                        var flag = renderableProxies[proxyIndex].DrawSubmesh.Flags;
                        if (queryType == MyFrustumEnum.MainFrustum)
                        {
                            if((flag & MyDrawSubmesh.MySubmeshFlags.Gbuffer) != MyDrawSubmesh.MySubmeshFlags.Gbuffer)
                                continue;
                        }
                        else if (queryType == MyFrustumEnum.ShadowCascade || queryType == MyFrustumEnum.ShadowProjection)
                        {
                            if((flag & MyDrawSubmesh.MySubmeshFlags.Depth) != MyDrawSubmesh.MySubmeshFlags.Depth)
                                continue;
                        }

                        MyRenderableProxy renderableProxy = renderableProxies[proxyIndex];
                        ulong sortKey = cullProxy.SortingKeys[proxyIndex];

                        var item = new MyRenderCullResultFlat
                        {
                            SortKey = sortKey,
                            RenderProxy = renderableProxy,
                        };

                        if (renderableProxy.Material != MyMeshMaterialId.NULL && renderableProxy.Material.Info.Technique == MyMeshDrawTechnique.GLASS)
                        {
                            if (queryType == MyFrustumEnum.MainFrustum)
                                MyStaticGlassRenderer.Renderables.Add(item);

                            continue;
                        }

                        for (int queueIndex = 0; queueIndex < m_affectedQueueIds.Count; ++queueIndex)
                        {
                            var queueId = m_affectedQueueIds[queueIndex];
                            m_passElements[queueId].Add(item);
                        }
                    }
                }

                // proxy 2
                var list2 = frustumQuery.List2;
                
                // flatten and sort
                m_flattenedKeys.SetSize(0);
                m_indirectionList.SetSize(0);
                m_location.SetSize(0);

                int indirectionCounter = 0;
                for (int list2Index = 0; list2Index < list2.Count; ++list2Index )
                {
                    for (int sortKeyIndex = 0; sortKeyIndex < list2[list2Index].SortingKeys.Length; sortKeyIndex++ )
                    {
                        m_flattenedKeys.Add(list2[list2Index].SortingKeys[sortKeyIndex]);
                        m_indirectionList.Add(indirectionCounter++);
                        m_location.Add(MyTuple.Create(list2Index, sortKeyIndex));
                    }
                }

                MyRenderableProxy_2[] flattenedProxies = null;
                
                if(indirectionCounter > 0)
                    flattenedProxies = new MyRenderableProxy_2[indirectionCounter];

                m_sortingKeysComparer.Values = m_flattenedKeys;
                m_indirectionList.Sort(0, m_indirectionList.Count, m_sortingKeysComparer);

                if (flattenedProxies != null)
                {
                    for (int e = 0; e < indirectionCounter; e++)
                    {
                        var l = m_location[m_indirectionList[e]];
                        flattenedProxies[e] = list2[l.Item1].Proxies[l.Item2];
                    }
                }
                
                for (int l = 0; l < m_affectedQueueIds.Count; l++)
                {
                    m_passElements2[m_affectedQueueIds[l]] = flattenedProxies;
                }
            }

            ProfilerShort.BeginNextBlock("Sort");

            m_tmpSortListDictionary.Clear();
            m_resultSortedKeys.Clear();
            for (int i = 0; i < m_passElements.Count; i++)
            {
                var flatCullResults = m_passElements[i];

                foreach (MyRenderCullResultFlat element in flatCullResults)
                {
                    List<MyPassCullResult> sortList;
                    if (!m_tmpSortListDictionary.TryGetValue(element.SortKey, out sortList))
                    {
                        sortList = m_sortListPool.Allocate();
                        m_tmpSortListDictionary.Add(element.SortKey, sortList);
                        m_resultSortedKeys.Add(element.SortKey);
                    }

                    sortList.Add(new MyPassCullResult() { PassIndex = i, CullResult = element });
                }

                flatCullResults.Clear();
            }

            m_resultSortedKeys.Sort();
            foreach (ulong sortKey in m_resultSortedKeys)
            {
                List<MyPassCullResult> sortList = m_tmpSortListDictionary[sortKey];
                foreach (var result in sortList)
                    m_passElements[result.PassIndex].Add(result.CullResult);

                sortList.SetSize(0);
                m_sortListPool.Deallocate(sortList);
            }

            int jobsNum = GetRenderingThreadsNum();

            // always amortize this path
            ProfilerShort.BeginNextBlock("WorkAmortization");

            //passElements.RemoveAll(x => x.Count == 0);

            int workSum = 0;
            foreach (var list in m_passElements)
            {
                workSum += list.Count;
            }

            int batchWork = (workSum + jobsNum - 1) / jobsNum;

            Debug.Assert(m_subworks.Count == 0);

            int work = 0;
            for (int passElementIndex = 0; passElementIndex < m_passElements.Count; ++passElementIndex )
            {
                var flatCullResults = m_passElements[passElementIndex];
                if (flatCullResults.Count == 0)
                {
                    MyObjectPoolManager.Deallocate(processedCullQuery.RenderingPasses[passElementIndex]);
                    processedCullQuery.RenderingPasses[passElementIndex] = null;
                    if (m_passElements2[passElementIndex] == null || m_passElements2[passElementIndex].Length == 0)
                        continue;
                }

                if (processedCullQuery.RenderingPasses[passElementIndex] == null)
                    continue;

                int passBegin = 0;

                if(m_passElements2[passElementIndex] != null && m_passElements2[passElementIndex].Length > 0)
                    m_subworks.Add(new MyRenderingWorkItem
                    {
                        Pass = processedCullQuery.RenderingPasses[passElementIndex].Fork(),
                        List2 = m_passElements2[passElementIndex]
                    });

                while(passBegin < flatCullResults.Count)
                {
                    int toTake = Math.Min(flatCullResults.Count - passBegin, batchWork - work);

                    var workItem = new MyRenderingWorkItem
                    {
                        Renderables = flatCullResults,
                        Begin = passBegin,
                        End = passBegin + toTake
                    };

                    if (toTake < flatCullResults.Count && workItem.End != workItem.Renderables.Count)
                        workItem.Pass = processedCullQuery.RenderingPasses[passElementIndex].Fork();
                    else
                    {
                        workItem.Pass = processedCullQuery.RenderingPasses[passElementIndex];
                        processedCullQuery.RenderingPasses[passElementIndex] = null;    // Consume the pass so it doesn't get cleaned up later with the cull query, but instead with the work item
                    }

                    m_subworks.Add(workItem);

                    passBegin += toTake;
                    work += toTake;

                    Debug.Assert(work <= batchWork);
                    if (work != batchWork)
                        continue;

                    if (MyRender11.DeferredContextsEnabled)
                    {
                        var renderWork = MyObjectPoolManager.Allocate<MyRenderingWorkRecordCommands>();
                        renderWork.Init(MyManagers.DeferredRCs.AcquireRC(), m_subworks);
                        m_workList.Add(renderWork);
                    }
                    else
                    {
                        var renderWork = MyObjectPoolManager.Allocate<MyRenderingWorkRecordCommands>();
                        renderWork.Init(m_subworks);
                        m_workList.Add(renderWork);
                    }

                    work = 0;

                    m_subworks.Clear();
                }                
            }
            if (m_subworks.Count > 0)
            {
                if (MyRender11.DeferredContextsEnabled)
                {
                    var renderWork = MyObjectPoolManager.Allocate<MyRenderingWorkRecordCommands>();
                    renderWork.Init(MyManagers.DeferredRCs.AcquireRC(), m_subworks);
                    m_workList.Add(renderWork);
                }
                else
                {
                    var renderWork = MyObjectPoolManager.Allocate<MyRenderingWorkRecordCommands>();
                    renderWork.Init(m_subworks);
                    m_workList.Add(renderWork);
                }
                m_subworks.Clear();
            }

            ProfilerShort.End();

            ProfilerShort.End();

            DoRecordingWork(outCommandLists);

            foreach(var renderWork in m_workList)
            {
                foreach (var pass in renderWork.Passes)
                {
                    ProfilerShort.Begin(pass.DebugName);
                    ProfilerShort.End(0,
                        new VRage.Library.Utils.MyTimeSpan(pass.Elapsed));
                }
                MyObjectPoolManager.Deallocate(renderWork);
            }
            m_workList.Clear();
        }

        private void DoRecordingWork(Queue<CommandList> commandListQueue)
        {
            ProfilerShort.Begin("ScheduleTasksAndWait");

            if (MyRender11.MultithreadedRenderingEnabled)
            {
                int N = m_workList.Count;

                int parallelStart = MyRender11.Settings.RenderThreadAsWorker ? 1 : 0;

                for (int i = parallelStart; i < N; i++)
                {
                    m_taskList.Add(Parallel.Start(m_workList[i]));
                }

                if (MyRender11.Settings.RenderThreadAsWorker && m_workList.Count > 0)
                {
                    m_workList[0].DoWork();
                }

                foreach (Task task in m_taskList)
                {
                    task.Wait();
                }
                m_taskList.Clear();
            }
            else
            {
                foreach (MyRenderingWork renderWork in m_workList)
                {
                    renderWork.DoWork();
                }
            }

            foreach (MyRenderingWork renderWork in m_workList)
            {
                ConsumeWork(renderWork, commandListQueue);
            }

            ProfilerShort.End();
        }

        private void ConsumeWork(MyRenderingWork work, Queue<CommandList> accumulator)
        {
            foreach (var pass in work.Passes)
            {
                pass.Join();
            }

            foreach (var rc in work.RCs)
            {
                accumulator.Enqueue(MyRenderUtils.JoinAndGetCommandList(rc));
                MyManagers.DeferredRCs.FreeRC(rc);
            }
        }

        private int GetRenderingThreadsNum()
        {
            int jobsNum = 1;
            if (MyRender11.Settings.EnableParallelRendering)
            {
                jobsNum = Parallel.Scheduler.ThreadCount;

                if (MyRender11.Settings.RenderThreadAsWorker)
                {
                    ++jobsNum;
                }
            }
            return jobsNum;
        }

        private struct MyPassCullResult
        {
            public int PassIndex;
            public MyRenderCullResultFlat CullResult;
        }
    }
}
