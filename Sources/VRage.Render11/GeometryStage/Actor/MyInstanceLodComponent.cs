using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using VRageMath.PackedVector;

namespace VRageRender
{
    /// <summary>
    /// Used to render LOD0 instances
    /// </summary>
    class MyInstanceLodComponent : MyActorComponent
    {
        struct MyInstanceLodId
        {
            public InstancingId Id;
            public int InstanceIndex;

            public override int GetHashCode()
            {
                return Id.Index * 397 ^ InstanceIndex;
            }
        }

        class MyLodTransitionData
        {
            public float Time;
            public float Delta;

            public bool IsProxyToInstance;
        }

        class MySingleInstance
        {
            public int CurrentLod;
            public MySingleInstanceLod[] Lods;

            public MyCullProxy CullProxy;
            public int BtreeProxy;
            public bool IsDirty;
        }

        class MySingleInstanceLod
        {
            public MyRenderableProxy[] RenderableProxies;
            public UInt64[] SortingKeys;

            public MyRenderableProxy[] RenderableProxiesForLodTransition;
            public UInt64[] SortingKeysForLodTransition;
        }

        private HashSet<InstancingId> m_dirtyInstances = new HashSet<InstancingId>();

        private Dictionary<MyInstanceLodId, MySingleInstance> m_instances;

        private Dictionary<MyInstanceLodId, MyLodTransitionData> m_activeTransitions = new Dictionary<MyInstanceLodId, MyLodTransitionData>();
        private HashSet<MyInstanceLodId> m_completedTransitions = new HashSet<MyInstanceLodId>();

        internal override void Construct()
        {
            base.Construct();

            Type = MyActorComponentEnum.InstanceLod;

            m_instances = new Dictionary<MyInstanceLodId, MySingleInstance>();
        }

        internal override void Assign(MyActor owner)
        {
            base.Assign(owner);
        }

        internal void OnFrameUpdate()
        {
            UpdateLodState();

            SetProxies();

            foreach (var id in m_dirtyInstances)
            {
                MyInstancing.RebuildGeneric(id);
            }
            m_dirtyInstances.Clear();
        }

        void UpdateLodState()
        {
            ProfilerShort.Begin("MyInstancingComponent::UpdateLodState");
            foreach (var pair in m_activeTransitions)
            {
                ProfilerShort.Begin("SetAlphaForInstance");
                pair.Value.Time += pair.Value.Delta;

                SetAlphaForInstance(pair.Key.Id, pair.Key.InstanceIndex, pair.Value.Time);
                ProfilerShort.End();

                if (pair.Value.IsProxyToInstance)
                {
                    if (pair.Value.Time >= 1.0f)
                    {
                        ProfilerShort.Begin("PositiveTransition");

                        var instance = m_instances[pair.Key];

                        MyScene.RenderablesDBVH.RemoveProxy(instance.BtreeProxy);

                        foreach (var lod in instance.Lods)
                        {
                            foreach (var proxy in lod.RenderableProxies)
                            {
                                MyProxiesFactory.Remove(proxy);
                            }
                        }
                        MyProxiesFactory.Remove(instance.CullProxy);
                        m_instances.Remove(pair.Key);

                        m_completedTransitions.Add(pair.Key);

                        ProfilerShort.End();
                        continue;
                    }
                    else if (pair.Value.Time <= -1.0f)
                    {
                        ProfilerShort.Begin("NegativeTransition");
                        MyInstancing.Instancings.Data[pair.Key.Id.Index].SetVisibility(pair.Key.InstanceIndex, false);
                        m_dirtyInstances.Add(pair.Key.Id);

                        m_completedTransitions.Add(pair.Key);
                        pair.Value.Time = 0;
                        ProfilerShort.End();

                    }
                }
                else
                {
                    if (Math.Abs(pair.Value.Time) >= 1.0f)
                    {
                        var instance = m_instances[pair.Key];
                        instance.CurrentLod += Math.Sign(pair.Value.Delta);
                        pair.Value.Delta = -pair.Value.Delta;
                        
                        m_completedTransitions.Add(pair.Key);
                        pair.Value.Time = 0;
                        instance.IsDirty = true;
                    }
                }
                ProfilerShort.Begin("SetAlphaForProxies");
                SetAlphaForProxies(pair.Key, pair.Value);
                ProfilerShort.End();
            }

            foreach (var key in m_completedTransitions)
            {
                m_activeTransitions.Remove(key);
            }

            m_completedTransitions.Clear();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        float GetAlphaForTime(float transitionTime, bool isStartLod)
        {
            if (transitionTime == 0)
            {
                return 0;
            }
            if (Math.Abs(transitionTime) > 1.0f)
            {
                return 0;
            }
            return isStartLod ? Math.Abs(transitionTime) : (2.0f - Math.Abs(transitionTime));
        }

        void SetAlphaForProxies(MyInstanceLodId id, MyLodTransitionData data)
        {
            var instance = m_instances[id];
            if (data.IsProxyToInstance)
            {
                float value = GetAlphaForTime(data.Time, data.Delta > 0);
                float otherValue = GetAlphaForTime(data.Time, data.Delta < 0);

                var lod = instance.Lods[instance.CurrentLod];
                foreach (var proxy in lod.RenderableProxies)
                {
                    proxy.ObjectData.CustomAlpha = value;
                }
            }
            else
            {
                float valueForStartLod = GetAlphaForTime(data.Time, true);
                float valueForEndLod = GetAlphaForTime(data.Time, false);

                var startLod = instance.Lods[instance.CurrentLod];
                var endLod = instance.Lods[instance.CurrentLod + Math.Sign(data.Delta)];
                foreach (var proxy in startLod.RenderableProxies)
                {
                    proxy.ObjectData.CustomAlpha = valueForStartLod;
                }
                foreach (var proxy in endLod.RenderableProxies)
                {
                    proxy.ObjectData.CustomAlpha = valueForEndLod;
                }
            }
        }

        void SetAlphaForInstance(InstancingId id, int index, float transitionTime)
        {
            float value = GetAlphaForTime(transitionTime, transitionTime < 0.0f);

            var instancing = MyInstancing.Instancings.Data[id.Index].InstanceData;
            if (instancing != null)
            {
                var color = instancing[index].ColorMaskHSV.ToVector4();
                color.W = value;
                instancing[index].ColorMaskHSV = new HalfVector4(color);
                m_dirtyInstances.Add(id);
            }
        }

        internal void SetProxies()
        {
            foreach (var kv in m_instances)
            {
                var instance = kv.Value;
                if (instance.IsDirty)
                {
                    MyLodTransitionData transition;
                    if (m_activeTransitions.TryGetValue(kv.Key, out transition) && !transition.IsProxyToInstance)
                    {
                        int lodIndex = Math.Min(instance.CurrentLod, instance.CurrentLod + Math.Sign(transition.Delta));
                        instance.CullProxy.Proxies = instance.Lods[lodIndex].RenderableProxiesForLodTransition;
                        instance.CullProxy.SortingKeys = instance.Lods[lodIndex].SortingKeysForLodTransition;
                    }
                    else
                    {
                        instance.CullProxy.Proxies = instance.Lods[instance.CurrentLod].RenderableProxies;
                        instance.CullProxy.SortingKeys = instance.Lods[instance.CurrentLod].SortingKeys;
                    }
                }
            }
        }

        internal void AddInstanceLod(InstancingId id, int index, MyRenderableProxy[][] newProxies, ulong[][] newSortingKeys, BoundingBoxD aabb)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("AddInstanceLod");
            if (!SetDelta(id, index, -0.1f))
            {
                MyInstanceLodId key = new MyInstanceLodId { Id = id, InstanceIndex = index };

                MySingleInstance instance = new MySingleInstance();
                instance.Lods = new MySingleInstanceLod[newProxies.Length];

                for (int i = 0; i < newProxies.Length; i++)
                {
                    MySingleInstanceLod lod = new MySingleInstanceLod();
                    lod.RenderableProxies = newProxies[i];
                    lod.SortingKeys = newSortingKeys[i];

                    if (i < newProxies.Length - 1)
                    {
                        lod.RenderableProxiesForLodTransition = new MyRenderableProxy[newProxies[i].Length + newProxies[i + 1].Length];
                        for (int j = 0; j < newProxies[i].Length; j++)
                        {
                            lod.RenderableProxiesForLodTransition[j] = newProxies[i][j];
                        }
                        for (int j = 0; j < newProxies[i + 1].Length; j++)
                        {
                            lod.RenderableProxiesForLodTransition[j + newProxies[i].Length] = newProxies[i + 1][j];
                        }

                        lod.SortingKeysForLodTransition = new ulong[newSortingKeys[i].Length + newSortingKeys[i + 1].Length];
                        for (int j = 0; j < newSortingKeys[i].Length; j++)
                        {
                            lod.SortingKeysForLodTransition[j] = newSortingKeys[i][j];
                        }
                        for (int j = 0; j < newSortingKeys[i + 1].Length; j++)
                        {
                            lod.SortingKeysForLodTransition[j + newSortingKeys[i].Length] = newSortingKeys[i + 1][j];
                        }
                    }

                    instance.Lods[i] = lod;
                }
                
                instance.CurrentLod = 0;

                instance.CullProxy = MyProxiesFactory.CreateCullProxy();

                instance.BtreeProxy = MyScene.RenderablesDBVH.AddProxy(ref aabb, instance.CullProxy, 0);
                m_instances.Add(key, instance);

                instance.IsDirty = true;
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        internal void RemoveInstanceLod(InstancingId id, int index)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("RemoveInstanceLod");
            Debug.Assert(m_instances.ContainsKey(new MyInstanceLodId { Id = id, InstanceIndex = index }), "Cannot remove instance");

            MyInstancing.Instancings.Data[id.Index].SetVisibility(index, true);
            m_dirtyInstances.Add(id);
            SetDelta(id, index, 0.1f);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        /// <returns>Returns true if the item is in transition</returns>
        bool SetDelta(InstancingId id, int index, float delta)
        {
            var key = new MyInstanceLodId { Id = id, InstanceIndex = index };
            MyLodTransitionData data;
            if (m_activeTransitions.TryGetValue(key, out data))
            {
                data.Delta = delta;
                data.Time = 0;
                data.IsProxyToInstance = true;
                return true;
            }
            else
            {
                data = new MyLodTransitionData { Time = 0.0f, Delta = delta, IsProxyToInstance = true };
                m_activeTransitions.Add(key, data);
                return false;
            }
        }

        internal bool IsFar(InstancingId id, int index)
        {
            var key = new MyInstanceLodId { Id = id, InstanceIndex = index };
            MyLodTransitionData data;
            if (m_activeTransitions.TryGetValue(key, out data))
            {
                if (data.IsProxyToInstance)
                {
                    return data.Delta > 0.0f;
                }
            }
            return MyInstancing.Instancings.Data[id.Index].VisibilityMask[index];
        }

        internal void SetLod(InstancingId id, int index, int lod)
        {
            MySingleInstance instance;
            var key = new MyInstanceLodId { Id = id, InstanceIndex = index };
            if (m_instances.TryGetValue(key, out instance) && instance.CurrentLod != lod)
            {
                MyLodTransitionData transition;
                if (m_activeTransitions.TryGetValue(key, out transition))
                {
                    if (transition.IsProxyToInstance)
                    {
                        instance.CurrentLod = lod;
                        instance.IsDirty = true;
                    }
                }
                else
                {
                    transition = new MyLodTransitionData();
                    transition.Delta = instance.CurrentLod > lod ? -0.1f : 0.1f;
                    transition.Time = 0.0f;
                    transition.IsProxyToInstance = false;
                    m_activeTransitions.Add(key, transition);
                    instance.IsDirty = true;
                }
            }
        }

        internal override void Destruct()
        {
            base.Destruct();
            foreach (var instance in m_instances.Values)
            {
                MyScene.RenderablesDBVH.RemoveProxy(instance.BtreeProxy);
                foreach (var lod in instance.Lods)
                {
                    foreach (var proxy in lod.RenderableProxies)
                    {
                        MyProxiesFactory.Remove(proxy);
                    }
                }
            }
        }

        internal override void OnRemove(MyActor owner)
        {
            base.OnRemove(owner);
            this.Deallocate();
        }
    }
}
