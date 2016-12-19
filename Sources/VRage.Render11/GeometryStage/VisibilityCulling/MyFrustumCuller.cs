using ParallelTasks;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Render11.Tools;
using VRageMath;

namespace VRageRender
{
    class MyFrustumCuller : MyVisibilityCuller
    {
        private readonly List<Task> m_tmpCullingTasks = new List<Task>();
        private readonly List<MyFrustumCullingWork> m_tmpAllocatedWork = new List<MyFrustumCullingWork>();
        private readonly List<int> m_tmpIndicesToRemove = new List<int>();

        private readonly List<HashSet<MyCullProxy_2>> m_shadowCascadeProxies2 = new List<HashSet<MyCullProxy_2>>();

        /// <summary>
        /// Goes through all renderables and adds the ones that are in the given frustums to the lists in frustumCullQuery
        /// </summary>
        protected override void DispatchCullQuery(MyCullQuery frustumCullQueries, MyDynamicAABBTreeD renderables)
        {
            ProfilerShort.Begin("DispatchFrustumCulling");
            for (int frustumQueryIndex = 1; frustumQueryIndex < frustumCullQueries.Size; ++frustumQueryIndex)
            {
                var cullWork = MyObjectPoolManager.Allocate<MyFrustumCullingWork>();
                m_tmpAllocatedWork.Add(cullWork);
                cullWork.Init(frustumCullQueries.FrustumCullQueries[frustumQueryIndex], renderables);
                m_tmpCullingTasks.Add(Parallel.Start(cullWork));
            }

            if (frustumCullQueries.Size > 0)
            {
                var cullWork = MyObjectPoolManager.Allocate<MyFrustumCullingWork>();
                m_tmpAllocatedWork.Add(cullWork);
                cullWork.Init(frustumCullQueries.FrustumCullQueries[0], renderables);
                cullWork.DoWork();
            }

            foreach (Task cullingTask in m_tmpCullingTasks)
            {
                cullingTask.Wait();
            }
            m_tmpCullingTasks.Clear();


            int i = 0;
            foreach (MyFrustumCullingWork cullWork in m_tmpAllocatedWork)
            {
                ProfilerShort.Begin(frustumCullQueries.FrustumCullQueries[i].Type.ToString());
                ProfilerShort.End(frustumCullQueries.FrustumCullQueries[i].List.Count + frustumCullQueries.FrustumCullQueries[i].List2.Count,
                    new VRage.Library.Utils.MyTimeSpan(cullWork.Elapsed));

                MyObjectPoolManager.Deallocate(cullWork);
                i++;
            }
            m_tmpAllocatedWork.Clear();

            ProfilerShort.End();
        }

        protected override void ProcessCullQueryResults(MyCullQuery cullQuery)
        {
            ProfilerShort.Begin("Reset");

            foreach (MyFrustumCullQuery frustumQuery in cullQuery.FrustumCullQueries)
            {
                var cullProxies = frustumQuery.List;
                foreach (MyCullProxy cullProxy in cullProxies)
                {
                    cullProxy.Updated = false;
                    cullProxy.FirstFullyContainingCascadeIndex = uint.MaxValue;
                }
            }
            ProfilerShort.End();
            foreach (var frustumQuery in cullQuery.FrustumCullQueries)
            {
                ProfilerShort.Begin("Distance cull and update");

                var cullProxies = frustumQuery.List;
                bool isShadowFrustum = (frustumQuery.Type == MyFrustumEnum.ShadowCascade) || (frustumQuery.Type == MyFrustumEnum.ShadowProjection);
                for (int cullProxyIndex = 0; cullProxyIndex < cullProxies.Count; ++cullProxyIndex)
                {
                    MyCullProxy cullProxy = cullProxies[cullProxyIndex];
                    if (cullProxy == null || cullProxy.RenderableProxies == null || cullProxy.RenderableProxies.Length == 0 || cullProxy.RenderableProxies[0].Parent == null || !cullProxy.RenderableProxies[0].Parent.IsVisible)
                    {
                        m_tmpIndicesToRemove.Add(cullProxyIndex);
                        continue;
                    }

                    if (!cullProxy.Updated)
                    {
                        var renderableComponent = cullProxy.Parent;
                        if (renderableComponent != null)
                        {
                            renderableComponent.OnFrameUpdate();
                            if (renderableComponent.IsCulled)
                            {
                                m_tmpIndicesToRemove.Add(cullProxyIndex);
                                continue;
                            }
                            renderableComponent.UpdateInstanceLods();


                        // Proxies can get removed in UpdateInstanceLods
                        if (cullProxy.RenderableProxies == null)
                        {
                            m_tmpIndicesToRemove.Add(cullProxyIndex);
                            continue;
                        }
                        }

                        cullProxy.Updated = true;
                    }



                        foreach (MyRenderableProxy proxy in cullProxy.RenderableProxies)
                        {
                            bool shouldCastShadows = proxy.Flags.HasFlags(MyRenderableProxyFlags.CastShadows)
                                                     && (proxy.Flags.HasFlags(MyRenderableProxyFlags.DrawOutsideViewDistance) || frustumQuery.Index < 4);

                       
                            if (isShadowFrustum && !shouldCastShadows)
                            {
                                m_tmpIndicesToRemove.Add(cullProxyIndex);
                                break;
                            }
                            var worldMat = proxy.WorldMatrix;
                            worldMat.Translation -= MyRender11.Environment.Matrices.CameraPosition;
                            proxy.CommonObjectData.LocalMatrix = worldMat;
                        }
                    
                    }

                for (int removeIndex = m_tmpIndicesToRemove.Count - 1; removeIndex >= 0; --removeIndex)
                {
                    cullProxies.RemoveAtFast(m_tmpIndicesToRemove[removeIndex]);
                    frustumQuery.IsInsideList.RemoveAtFast(m_tmpIndicesToRemove[removeIndex]);
                }
                m_tmpIndicesToRemove.SetSize(0);

                ProfilerShort.BeginNextBlock("Culling by query type");
                if (frustumQuery.Type == MyFrustumEnum.ShadowCascade)
                {
                    while (m_shadowCascadeProxies2.Count < MyShadowCascades.Settings.NewData.CascadesCount)
                        m_shadowCascadeProxies2.Add(new HashSet<MyCullProxy_2>());

                    bool isHighCascade = frustumQuery.Index < 3;

                    // List 1
                    for (int cullProxyIndex = 0; cullProxyIndex < cullProxies.Count; ++cullProxyIndex)
                    {
                        var cullProxy = cullProxies[cullProxyIndex];

                        if ((isHighCascade && (cullProxy.FirstFullyContainingCascadeIndex < frustumQuery.Index - 1)) ||
                            (!isHighCascade && cullProxy.FirstFullyContainingCascadeIndex < frustumQuery.Index) ||
                            cullProxy.RenderableProxies == null)
                        {
                            cullProxies.RemoveAtFast(cullProxyIndex);
                            frustumQuery.IsInsideList.RemoveAtFast(cullProxyIndex);
                            --cullProxyIndex;
                            continue;
                        }
                        else
                        {
                            if (frustumQuery.IsInsideList[cullProxyIndex])
                            {
                                cullProxy.FirstFullyContainingCascadeIndex = (uint)frustumQuery.Index;
                            }
                        }
                    }

                    // List 2
                    var cullProxies2 = frustumQuery.List2;
                    m_shadowCascadeProxies2[frustumQuery.Index].Clear();
                    for (int cullProxyIndex = 0; cullProxyIndex < cullProxies2.Count; ++cullProxyIndex)
                    {
                        var cullProxy2 = cullProxies2[cullProxyIndex];
                        bool containedInHigherCascade = false;

                        // Cull items if they're fully contained in higher resolution cascades
                        for (int hashSetIndex = 0; hashSetIndex < frustumQuery.Index; ++hashSetIndex)
                        {
                            if (m_shadowCascadeProxies2[hashSetIndex].Contains(cullProxy2))
                            {
                                cullProxies2.RemoveAtFast(cullProxyIndex);
                                frustumQuery.IsInsideList2.RemoveAtFast(cullProxyIndex);
                                --cullProxyIndex;
                                containedInHigherCascade = true;
                                break;
                            }
                        }

                        if (!containedInHigherCascade && frustumQuery.IsInsideList2[cullProxyIndex])
                        {
                            m_shadowCascadeProxies2[frustumQuery.Index].Add(cullProxy2);
                        }
                    }
                }
                else if (frustumQuery.Type == MyFrustumEnum.ShadowProjection)
                {
                    for (int cullProxyIndex = 0; cullProxyIndex < cullProxies.Count; ++cullProxyIndex)
                    {
                        var cullProxy = frustumQuery.List[cullProxyIndex];
                        if (cullProxy.RenderableProxies == null)
                        {
                            cullProxies.RemoveAtFast(cullProxyIndex);
                            frustumQuery.IsInsideList.RemoveAtFast(cullProxyIndex);
                            --cullProxyIndex;
                            continue;
                        }
                    }
                }
                ProfilerShort.End();
                if (frustumQuery.Ignored != null)
                {
                    for (int cullProxyIndex = 0; cullProxyIndex < cullProxies.Count; cullProxyIndex++)
                    {
                        if ((cullProxies[cullProxyIndex].RenderableProxies == null || cullProxies[cullProxyIndex].RenderableProxies.Length == 0 || cullProxies[cullProxyIndex].RenderableProxies[0] == null) || (frustumQuery.Ignored.Contains(cullProxies[cullProxyIndex].RenderableProxies[0].Parent.Owner.ID)))
                        {
                            cullProxies.RemoveAtFast(cullProxyIndex);
                            frustumQuery.IsInsideList.RemoveAtFast(cullProxyIndex);
                            --cullProxyIndex;
                            continue;
                        }
                    }

                }
            }

            foreach (MyFrustumCullQuery frustumQuery in cullQuery.FrustumCullQueries)
            {
                switch(frustumQuery.Type)
                {
                    case MyFrustumEnum.MainFrustum:
                    {
                        MyStatsUpdater.Passes.GBufferObjects += frustumQuery.List.Count;
                        break;
                    }
                    case MyFrustumEnum.ShadowCascade:
                    {
                        MyStatsUpdater.CSMObjects[frustumQuery.Index] += frustumQuery.List.Count;
                        break;
                    }
                    case MyFrustumEnum.ShadowProjection:
                    {
                        MyStatsUpdater.Passes.ShadowProjectionObjects += frustumQuery.List.Count;
                        break;
                    }
                }
            }

        }
    }
}
