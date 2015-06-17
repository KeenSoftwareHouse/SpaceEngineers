using Havok;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage;
using VRage.Utils;

namespace Sandbox.Engine.Physics
{
    public class MyBlockShapePool
    {
        public const int PREALLOCATE_COUNT = 50;
        const int MAX_CLONE_PER_FRAME = 3;
        Dictionary<MyDefinitionId, Dictionary<string, ConcurrentQueue<HkdBreakableShape>>> m_pools = new Dictionary<MyDefinitionId, Dictionary<string, ConcurrentQueue<HkdBreakableShape>>>();
        MyWorkTracker<MyDefinitionId, MyBreakableShapeCloneJob> m_tracker = new MyWorkTracker<MyDefinitionId, MyBreakableShapeCloneJob>();

        public void Preallocate()
        {
            foreach (var groupName in MyDefinitionManager.Static.GetDefinitionPairNames())
            {
                var group = MyDefinitionManager.Static.GetDefinitionGroup(groupName);
                
                if (group.Large != null && group.Large.Public)
                {
                    var definition = group.Large;
                    AllocateForDefinition(group.Large.Model, definition, PREALLOCATE_COUNT);
                    foreach(var model in group.Large.BuildProgressModels)
                        AllocateForDefinition(model.File, definition, PREALLOCATE_COUNT);
                }

                if (group.Small != null && group.Small.Public)
                {
                    AllocateForDefinition(group.Small.Model, group.Small, PREALLOCATE_COUNT);
                    foreach (var model in group.Large.BuildProgressModels)
                        AllocateForDefinition(model.File, group.Small, PREALLOCATE_COUNT);
                }
            }
        }

        public void AllocateForDefinition(string model, MyPhysicalModelDefinition definition, int count)
        {
            if (string.IsNullOrEmpty(model))
                return;
            ProfilerShort.Begin("Clone");
            var data = MyModels.GetModelOnlyData(model);
            if (MyFakes.LAZY_LOAD_DESTRUCTION && data.HavokBreakableShapes == null)
            {
                MyDestructionData.Static.LoadModelDestruction(model, definition, false, data.BoundingBoxSize);
            }               
            if (data.HavokBreakableShapes != null && data.HavokBreakableShapes.Length > 0)
            {
                if (!m_pools.ContainsKey(definition.Id))
                    m_pools[definition.Id] = new Dictionary<string, ConcurrentQueue<HkdBreakableShape>>();
                if(!m_pools[definition.Id].ContainsKey(model))
                    m_pools[definition.Id][model] = new ConcurrentQueue<HkdBreakableShape>();
                var queue = m_pools[definition.Id][model];
                for (int i = 0; i < count; i++)
                {
                    var shape = data.HavokBreakableShapes[0].Clone();
                    queue.Enqueue(shape);
                    if (i == 0)
                    {
                        var mp = new HkMassProperties();
                        shape.BuildMassProperties(ref mp);
                        if (!mp.InertiaTensor.IsValid())
                        {
                            MyLog.Default.WriteLine(string.Format("Block with wrong destruction! (q.isOk): {0}", definition.Model));
                            break;
                        }
                    }
                }
            }
            ProfilerShort.End();
        }

        private int m_missing = 0;
        private bool m_dequeuedThisFrame = false;
        public void RefillPools()
        {
            if (m_missing == 0)
                return;
            if (m_dequeuedThisFrame && !MyFakes.CLONE_SHAPES_ON_WORKER)
            {
                m_dequeuedThisFrame = false;
                return;
            }
            ProfilerShort.Begin("Clone");
            int clonedThisFrame = 0;
            if (MyFakes.CLONE_SHAPES_ON_WORKER)
                StartJobs();
            else
            {
                foreach (var pool in m_pools)
                {
                    foreach (var model in pool.Value)
                    {
                        if (pool.Value.Count < PREALLOCATE_COUNT)
                        {
                            MyCubeBlockDefinition def;
                            MyDefinitionManager.Static.TryGetDefinition<MyCubeBlockDefinition>(pool.Key, out def);
                            int cloneCount = Math.Min(PREALLOCATE_COUNT - pool.Value.Count, MAX_CLONE_PER_FRAME - clonedThisFrame);
                            AllocateForDefinition(model.Key, def, cloneCount);
                            clonedThisFrame += cloneCount;
                        }
                        if (clonedThisFrame == MAX_CLONE_PER_FRAME)
                            break;
                    }
                }
            }
           
            m_missing -= clonedThisFrame;
            ProfilerShort.End(clonedThisFrame);
            clonedThisFrame = 0;
        }

        private void StartJobs()
        {
            foreach (var pool in m_pools)
            {
                foreach (var model in pool.Value)
                {
                    if (model.Value.Count < PREALLOCATE_COUNT && !m_tracker.Exists(pool.Key))
                    {
                        MyPhysicalModelDefinition def;
                        MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(pool.Key, out def);
                        var modelData = MyModels.GetModelOnlyData(def.Model);
                        if (modelData.HavokBreakableShapes == null)
                            continue;
                        MyBreakableShapeCloneJob.Args args = new MyBreakableShapeCloneJob.Args();
                        args.Model = model.Key;
                        args.DefId = pool.Key;
                        args.ShapeToClone = modelData.HavokBreakableShapes[0];
                        args.Count = PREALLOCATE_COUNT - pool.Value.Count;
                        args.Tracker = m_tracker;
                        MyBreakableShapeCloneJob.Start(args);
                    }
                }
            }
        }

        public HkdBreakableShape GetBreakableShape(string model, MyCubeBlockDefinition block)
        {
            m_dequeuedThisFrame = true;
            ProfilerShort.Begin("GetBreakableShape");
            if (!block.Public || MyFakes.LAZY_LOAD_DESTRUCTION)
            {
                 if (!m_pools.ContainsKey(block.Id))
                    m_pools[block.Id] = new Dictionary<string, ConcurrentQueue<HkdBreakableShape>>();
                if(!m_pools[block.Id].ContainsKey(model))
                    m_pools[block.Id][model] = new ConcurrentQueue<HkdBreakableShape>();
            }
            var queue = m_pools[block.Id][model];
            if (queue.Count == 0)
            {
                AllocateForDefinition(model, block, 1);
            }
            else
                m_missing++;

            HkdBreakableShape result;
            queue.TryDequeue(out result);
            ProfilerShort.End();
            return result;
        }

        internal void Free()
        {
            m_tracker.CancelAll();
            foreach (var pool in m_pools.Values)
            {
                foreach (var model in pool.Values)
                    foreach (var shape in model)
                        shape.RemoveReference();
            }
            m_pools.Clear();
        }

        public void EnqueShapes(string model, MyDefinitionId id, List<HkdBreakableShape> shapes)
        {
            if (!m_pools.ContainsKey(id))
                m_pools[id] = new Dictionary<string, ConcurrentQueue<HkdBreakableShape>>();
            if (!m_pools[id].ContainsKey(model))
                m_pools[id][model] = new ConcurrentQueue<HkdBreakableShape>();

            foreach (var shape in shapes)
                m_pools[id][model].Enqueue(shape);

            m_missing -= shapes.Count;
        }

        public void EnqueShape(string model, MyDefinitionId id, HkdBreakableShape shape)
        {
            if (!m_pools.ContainsKey(id))
                m_pools[id] = new Dictionary<string, ConcurrentQueue<HkdBreakableShape>>();
            if (!m_pools[id].ContainsKey(model))
                m_pools[id][model] = new ConcurrentQueue<HkdBreakableShape>();

            m_pools[id][model].Enqueue(shape);
            m_missing--;
        }
    }
}
