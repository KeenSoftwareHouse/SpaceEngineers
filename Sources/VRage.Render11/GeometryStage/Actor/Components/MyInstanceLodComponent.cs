using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Generics;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;

namespace VRageRender
{
    /// <summary>
    /// Used to render LOD0 instances
    /// </summary>
    class MyInstanceLodComponent : MyActorComponent
    {
        private struct MyInstanceLodId
        {
            internal InstancingId Id;
            internal int InstanceIndex;

            public override int GetHashCode()
            {
                return Id.Index * 397 ^ InstanceIndex;
            }

            #region Equals
            internal class MyInstanceLodIdComparerType : IEqualityComparer<MyInstanceLodId>
            {
                public bool Equals(MyInstanceLodId left, MyInstanceLodId right)
                {
                    return left.Id == right.Id && left.InstanceIndex == right.InstanceIndex;
                }

                public int GetHashCode(MyInstanceLodId instanceLodId)
                {
                    return instanceLodId.GetHashCode();
                }
            }
            internal static MyInstanceLodIdComparerType Comparer = new MyInstanceLodIdComparerType();
            #endregion

            private static StringBuilder m_stringHelper = new StringBuilder();
            public override string ToString()
            {
                m_stringHelper.Clear();
                return m_stringHelper.Append(Id.ToString()).Append(", ").AppendInt32(InstanceIndex).ToString();
            }
        }

        [PooledObject]
#if XB1
        private class MyLodTransitionData : IMyPooledObjectCleaner
#else // !XB1
        private class MyLodTransitionData
#endif // !XB1
        {
            internal float Time;
            internal float Delta;

            internal bool IsProxyToInstance;
            internal float StartDistanceSquared;
            internal float StartTime;

#if XB1
            public void ObjectCleaner()
            {
                Cleanup();
            }
#else // !XB1
            [PooledObjectCleaner]
            public static void Cleanup(MyLodTransitionData transitionData)
            {
                transitionData.Cleanup();
            }
#endif // !XB1

            internal void Cleanup()
            {
                Time = 0;
                Delta = 0;
                IsProxyToInstance = false;
                StartDistanceSquared = 0;
                StartTime = 0;
            }
        }

        private class MySingleInstance
        {
            internal int CurrentLod;
            internal MySingleInstanceLod[] Lods;

            internal Vector3D Position;

            internal MyCullProxy CullProxy;
            internal int BtreeProxy;
            internal bool IsDirty;

            private static readonly MyDynamicObjectPool<MySingleInstance> m_objectPool = new MyDynamicObjectPool<MySingleInstance>(16);

            internal static MySingleInstance Allocate()
            {
                return m_objectPool.Allocate();
            }

            internal static void Release(MySingleInstance instance)
            {
                instance.ReleaseLodProxies();
                m_objectPool.Deallocate(instance);
            }

            internal static void Deallocate(MySingleInstance instance)
            {
                instance.DeallocateLodProxies();
                m_objectPool.Deallocate(instance);
            }

            internal void ReleaseLodProxies()
            {
                if(Lods != null)
                {
                    foreach(var instanceLod in Lods)
                    {
                        instanceLod.ReleaseProxies();
                    }
                }
            }

            internal void DeallocateLodProxies()
            {
                if(Lods != null)
                {
                    foreach(var instanceLod in Lods)
                    {
                        instanceLod.DeallocateProxies();
                    }
                    Lods = null;
                }
            }
        }

        private class MySingleInstanceLod
        {
            internal MyRenderableProxy[] RenderableProxies;
            internal UInt64[] SortingKeys;

            internal MyRenderableProxy[] RenderableProxiesForLodTransition;
            internal UInt64[] SortingKeysForLodTransition;


            internal void ReleaseProxies()
            {
                if(RenderableProxies != null)
                {
                    for (int proxyIndex = 0; proxyIndex < RenderableProxies.Length; ++proxyIndex )
                    {
                        MyObjectPoolManager.Deallocate(RenderableProxies[proxyIndex]);
                        RenderableProxies[proxyIndex] = null;
                    }
                }
                RenderableProxiesForLodTransition = null;
            }

            internal void DeallocateProxies()
            {
                if (RenderableProxies != null)
                {
                    foreach(var renderableProxy in RenderableProxies)
                    {
                        MyObjectPoolManager.Deallocate(renderableProxy);
                    }
                    RenderableProxies = null;
                }
                RenderableProxiesForLodTransition = null;
            }
        }

        Vector3D m_lastCameraPosition;
        float m_lastCameraSpeed;

        private readonly HashSet<InstancingId> m_dirtyInstances = new HashSet<InstancingId>(InstancingId.Comparer);
        private readonly HashSet<MyInstanceLodId> m_instancesToRemove = new HashSet<MyInstanceLodId>(MyInstanceLodId.Comparer);

        private Dictionary<MyInstanceLodId, MySingleInstance> m_instances;

        private readonly Dictionary<MyInstanceLodId, MyLodTransitionData> m_activeTransitions = new Dictionary<MyInstanceLodId, MyLodTransitionData>(MyInstanceLodId.Comparer);
        private readonly HashSet<MyInstanceLodId> m_completedTransitions = new HashSet<MyInstanceLodId>(MyInstanceLodId.Comparer);

        internal override void Construct()
        {
            base.Construct();

            Type = MyActorComponentEnum.InstanceLod;

            m_instances = new Dictionary<MyInstanceLodId, MySingleInstance>(MyInstanceLodId.Comparer);
        }

        internal override void Destruct()
        {
            foreach (var instance in m_instances.Values)
            {
                RemoveAndDeallocateInstance(instance);
            }
            m_instances.Clear();

            base.Destruct();
        }

        internal override void Assign(MyActor owner)
        {
            base.Assign(owner);
        }

        internal void OnFrameUpdate()
        {
            UpdateLodState();

            SetProxies();

            ProfilerShort.Begin("Rebuild dirty");
            foreach (var id in m_dirtyInstances)
            {
                MyInstancing.RebuildGeneric(id);
            }
            m_dirtyInstances.Clear();
            ProfilerShort.End();

        }

        void UpdateLodState()
        {
            ProfilerShort.Begin("MyInstancingComponent::UpdateLodState");
            foreach (var pair in m_activeTransitions)
            {
                var lodTransitionData = pair.Value;
                ProfilerShort.Begin("SetAlphaForInstance");
                float delta = lodTransitionData.Delta;
                if (MyLodUtils.LOD_TRANSITION_DISTANCE)
                {
                    float distance = (float)(MyRender11.Environment.Matrices.CameraPosition - m_lastCameraPosition).Length();
                    if (distance < 0.01f)
                        distance = m_lastCameraSpeed;
                    delta = distance * 0.01f;// MyLodUtils.GetTransitionDelta(distance * 0.1f, pair.Value.Time, m_instances[pair.Key].CurrentLod);
                    delta = Math.Max(distance * 0.01f, 0.025f);
                    delta *= Math.Sign(lodTransitionData.Delta);
                    m_lastCameraPosition = MyRender11.Environment.Matrices.CameraPosition;
                    m_lastCameraSpeed = distance;
                }
                lodTransitionData.Time = lodTransitionData.Time + delta;
                SetAlphaForInstance(pair.Key.Id, pair.Key.InstanceIndex, lodTransitionData.Time);

                ProfilerShort.BeginNextBlock("Transitions");
                if (lodTransitionData.IsProxyToInstance)
                {
                    if (lodTransitionData.Time >= 1.0f)
                    {
                        ProfilerShort.Begin("PositiveTransition");

                        var instance = m_instances[pair.Key];

                        RemoveAndReleaseInstance(instance);
                        m_instances.Remove(pair.Key);

                        m_completedTransitions.Add(pair.Key);

                        ProfilerShort.End();
                        ProfilerShort.End();
                        continue;
                    }
                    else if (lodTransitionData.Time <= -1.0f)
                    {
                        ProfilerShort.Begin("NegativeTransition");
                        MyInstancing.Instancings.Data[pair.Key.Id.Index].SetVisibility(pair.Key.InstanceIndex, false);
                        m_dirtyInstances.Add(pair.Key.Id);

                        m_completedTransitions.Add(pair.Key);
                        lodTransitionData.Time = 0;
                        ProfilerShort.End();
                    }
                }
                else
                {
                    if (Math.Abs(lodTransitionData.Time) >= 1.0f)
                    {
                        var instance = m_instances[pair.Key];
                        instance.CurrentLod += Math.Sign(lodTransitionData.Delta);
                        lodTransitionData.Delta = -lodTransitionData.Delta;

                        m_completedTransitions.Add(pair.Key);
                        lodTransitionData.Time = 0;
                        instance.IsDirty = true;
                    }
                }
                ProfilerShort.End();

                SetAlphaForProxies(pair.Key, lodTransitionData);
            }

            foreach (var key in m_completedTransitions)
            {
                RemoveTransition(key);
            }

            m_completedTransitions.Clear();
            ProfilerShort.End();
        }

        float GetAlphaForTime(float transitionTime, bool isStartLod)
        {
            if (transitionTime == 0 || Math.Abs(transitionTime) >= 1.0f)
                return 0;

            // Value over 1 is interpreted by the shader to do dithering with an inversed mask
            // This is done so that when blending between two lod levels, one pixel will be from current lod
            // and the other from the next lod and there are no missing pixels.
            // Could not use negative because that currently means hologram rendering.
            // SL: Moved inversed dithering to values of 2 to 3 instead of 1 to 2 to make sure the values don't overlap
            return isStartLod ? Math.Abs(transitionTime) : (3.0f - Math.Abs(transitionTime));
        }

        void SetAlphaForProxies(MyInstanceLodId id, MyLodTransitionData data)
        {
            ProfilerShort.Begin("SetAlphaForProxies");
            var instance = m_instances[id];
            if (data.IsProxyToInstance)
            {
                float value = GetAlphaForTime(data.Time, data.Delta > 0);
                float otherValue = GetAlphaForTime(data.Time, data.Delta < 0);

                var lod = instance.Lods[instance.CurrentLod];
                foreach (var proxy in lod.RenderableProxies)
                {
                    proxy.CommonObjectData.CustomAlpha = value;
                }
            }
            else
            {
                float valueForStartLod = GetAlphaForTime(data.Time, true);
                float valueForEndLod = GetAlphaForTime(data.Time, false);

                var startLod = instance.Lods[instance.CurrentLod];
                var endLod = instance.Lods[instance.CurrentLod + Math.Sign(data.Delta)];
                foreach (var renderableProxy in startLod.RenderableProxies)
                {
                    renderableProxy.CommonObjectData.CustomAlpha = valueForStartLod;
                }
                foreach (var renderableProxy in endLod.RenderableProxies)
                {
                    renderableProxy.CommonObjectData.CustomAlpha = valueForEndLod;
                }
            }
            ProfilerShort.End();
        }

        void SetAlphaForInstance(InstancingId id, int index, float transitionTime)
        {
            float value = GetAlphaForTime(transitionTime, transitionTime < 0.0f);

            var instancing = MyInstancing.Instancings.Data[id.Index].InstanceData;
            if (instancing != null && instancing.Length > index)
            {
                var color = instancing[index].ColorMaskHSV.ToVector4();
                color.W = value;
                instancing[index].ColorMaskHSV = new HalfVector4(color);
                m_dirtyInstances.Add(id);
            }
        }

        void RemoveTransition(MyInstanceLodId instanceLodId)
        {
            MyLodTransitionData transitionData = null;
            if(m_activeTransitions.TryGetValue(instanceLodId, out transitionData))
            {
                MyObjectPoolManager.Deallocate(transitionData);
                m_activeTransitions.Remove(instanceLodId);
            }
        }

        internal void SetProxies()
        {
            ProfilerShort.Begin("SetProxies");
            foreach (var idAndInstance in m_instances)
            {
                var instance = idAndInstance.Value;
                if (instance.IsDirty)
                {
                    MyLodTransitionData transition;
                    if (m_activeTransitions.TryGetValue(idAndInstance.Key, out transition) && !transition.IsProxyToInstance)
                    {
                        int lodIndex = Math.Min(instance.CurrentLod, instance.CurrentLod + Math.Sign(transition.Delta));
                        instance.CullProxy.RenderableProxies = instance.Lods[lodIndex].RenderableProxiesForLodTransition;
                        instance.CullProxy.SortingKeys = instance.Lods[lodIndex].SortingKeysForLodTransition;
                    }
                    else
                    {
                        instance.CullProxy.RenderableProxies = instance.Lods[instance.CurrentLod].RenderableProxies;
                        instance.CullProxy.SortingKeys = instance.Lods[instance.CurrentLod].SortingKeys;
                    }
                    instance.IsDirty = false;
                }
            }
            ProfilerShort.End();
        }

        internal void AddInstanceLod(InstancingId id, int index, MyRenderableProxy[][] newProxies, ulong[][] newSortingKeys, BoundingBoxD aabb, Vector3D position)
        {
            ProfilerShort.Begin("AddInstanceLod");

            if (!SetDelta(id, index, -0.1f, (float)(MyRender11.Environment.Matrices.CameraPosition - position).LengthSquared()))
            {
                MyInstanceLodId key = new MyInstanceLodId { Id = id, InstanceIndex = index };

                MySingleInstance instance = MySingleInstance.Allocate();

                Array.Resize(ref instance.Lods, newProxies.Length);
                instance.Position = position;

                for (int lodIndex = 0; lodIndex < newProxies.Length; ++lodIndex)
                {
                    MyUtils.Init(ref instance.Lods[lodIndex]);
                    MySingleInstanceLod lod = instance.Lods[lodIndex];

                    if(lod.RenderableProxies != null && lod.RenderableProxies.Length == newProxies[lodIndex].Length)
                        Array.Copy(newProxies[lodIndex], lod.RenderableProxies, lod.RenderableProxies.Length);
                    else
                        lod.RenderableProxies = newProxies[lodIndex];

                    lod.SortingKeys = newSortingKeys[lodIndex];

                    if (lodIndex < newProxies.Length - 1)
                    {
                        int lodTransitionProxiesCount = newProxies[lodIndex].Length + newProxies[lodIndex + 1].Length;

                        Array.Resize(ref lod.RenderableProxiesForLodTransition, lodTransitionProxiesCount);
                        Array.Copy(newProxies[lodIndex], lod.RenderableProxiesForLodTransition, newProxies[lodIndex].Length);
                        Array.Copy(newProxies[lodIndex + 1], 0, lod.RenderableProxiesForLodTransition, newProxies[lodIndex].Length, newProxies[lodIndex + 1].Length);

                        int sortingKeysLength = newSortingKeys[lodIndex].Length + newSortingKeys[lodIndex + 1].Length;
                        Array.Resize(ref lod.SortingKeysForLodTransition, sortingKeysLength);
                        Array.Copy(newSortingKeys[lodIndex], lod.SortingKeysForLodTransition, newSortingKeys[lodIndex].Length);
                        Array.Copy(newSortingKeys[lodIndex + 1], 0, lod.SortingKeysForLodTransition, newSortingKeys[lodIndex].Length, newSortingKeys[lodIndex + 1].Length);
                    }
                }

                instance.CurrentLod = 0;

                instance.CullProxy = MyObjectPoolManager.Allocate<MyCullProxy>();

                instance.BtreeProxy = MyScene.DynamicRenderablesDBVH.AddProxy(ref aabb, instance.CullProxy, 0);
                m_instances.Add(key, instance);

                instance.IsDirty = true;
            }
            ProfilerShort.End();
        }

        internal void RemoveInstanceLod(InstancingId id, int index)
        {
            ProfilerShort.Begin("RemoveInstanceLod");
            if (!m_instances.ContainsKey(new MyInstanceLodId { Id = id, InstanceIndex = index }))
            {
                // Can happen when the instance was removed manually by the game
                ProfilerShort.End();
                return;
            }

            MyInstancing.Instancings.Data[id.Index].SetVisibility(index, true);
            m_dirtyInstances.Add(id);
            SetDelta(id, index, 0.1f, (float)(MyRender11.Environment.Matrices.CameraPosition - m_instances[new MyInstanceLodId { Id = id, InstanceIndex = index }].Position).LengthSquared());
            ProfilerShort.End();
        }

        /// <returns>Returns true if the item is in transition</returns>
        bool SetDelta(InstancingId id, int index, float delta, float startDistanceSquared)
        {
            var key = new MyInstanceLodId { Id = id, InstanceIndex = index };
            MyLodTransitionData data;
            if (m_activeTransitions.TryGetValue(key, out data))
            {
                data.Delta = delta;
                data.Time = 0;
                data.IsProxyToInstance = true;
                data.StartDistanceSquared = startDistanceSquared;
                return true;
            }
            else
            {
                data = MyObjectPoolManager.Allocate<MyLodTransitionData>();
                data.Time = 0.0f;
                data.Delta = delta;
                data.IsProxyToInstance = true;
                data.StartDistanceSquared = startDistanceSquared;
                m_activeTransitions.Add(key, data);
                return false;
            }
        }

        internal bool IsFar(InstancingId id, int index)
        {
            bool isFar = false;
            var key = new MyInstanceLodId { Id = id, InstanceIndex = index };
            MyLodTransitionData data;
            if (m_activeTransitions.TryGetValue(key, out data) && data.IsProxyToInstance)
                isFar = data.Delta > 0.0f;
            else
                isFar = MyInstancing.Instancings.Data[id.Index].VisibilityMask[index];

            return isFar;
        }

        internal void SetLod(InstancingId instancingId, int index, int lod)
        {
            MySingleInstance instance;
            var instanceLodId = new MyInstanceLodId { Id = instancingId, InstanceIndex = index };
            if (m_instances.TryGetValue(instanceLodId, out instance) && instance.CurrentLod != lod)
            {
                MyLodTransitionData transition;
                if (m_activeTransitions.TryGetValue(instanceLodId, out transition))
                {
                    if (transition.IsProxyToInstance)
                    {
                        instance.CurrentLod = lod;
                        instance.IsDirty = true;
                    }
                }
                else
                {
                    transition = MyObjectPoolManager.Allocate<MyLodTransitionData>();
                    transition.Delta = instance.CurrentLod > lod ? -0.1f : 0.1f;
                    transition.Time = 0.0f;
                    transition.IsProxyToInstance = false;
                    transition.StartDistanceSquared = (float)(MyRender11.Environment.Matrices.CameraPosition - instance.Position).LengthSquared();
                    m_activeTransitions.Add(instanceLodId, transition);
                    instance.IsDirty = true;
                }
            }
        }

        internal override void OnRemove(MyActor owner)
        {
            base.OnRemove(owner);
            this.Deallocate();
        }

        internal void ClearInstances(InstancingId id)
        {
            foreach (var instance in m_instances)
            {
                if (instance.Key.Id == id)
                {
                    m_instancesToRemove.Add(instance.Key);
                }
            }

            foreach (var instanceId in m_instancesToRemove)
            {
                RemoveAndReleaseInstance(m_instances[instanceId]);
                m_instances.Remove(instanceId);

                RemoveTransition(instanceId);
            }

            m_instancesToRemove.Clear();
        }

        private void RemoveInstance(MySingleInstance instance)
        {
            Debug.Assert(instance != null, "RemoveInstance called for null instance!");
            if(instance == null)
                return;

            Debug.Assert(instance.BtreeProxy != -1 && instance.CullProxy != null);
            if (instance.BtreeProxy != -1)
            {
                MyScene.DynamicRenderablesDBVH.RemoveProxy(instance.BtreeProxy);
                instance.BtreeProxy = -1;
            }

            if (instance.CullProxy != null)
            {
                MyObjectPoolManager.Deallocate(instance.CullProxy);
                instance.CullProxy = null;
            }
        }

        private void RemoveAndReleaseInstance(MySingleInstance instance)
        {
            RemoveInstance(instance);
            MySingleInstance.Release(instance);
        }

        private void RemoveAndDeallocateInstance(MySingleInstance instance)
        {
            RemoveInstance(instance);
            MySingleInstance.Deallocate(instance);
        }

        internal static void ClearInvalidInstances(InstancingId id)
        {
            foreach (var component in MyComponentFactory<MyInstanceLodComponent>.GetAll())
            {
                component.ClearInstances(id);
            }
        }

        internal void RemoveAllInstanceLods(InstancingId instancing)
        {
            foreach (var instance in m_instances)
            {
                if (instance.Key.Id == instancing)
                {   
                    MyInstancing.Instancings.Data[instance.Key.Id.Index].SetVisibility(instance.Key.InstanceIndex, true);
                    SetAlphaForInstance(instance.Key.Id, instance.Key.InstanceIndex, 0);
                }
            }

            ClearInstances(instancing);

            //Debug.Assert(MyInstancing.Instancings.Data[instancing.Index].NonVisibleInstanceCount == 0);
        }
    }
}
