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
        Dictionary<MyDefinitionId, ConcurrentQueue<HkdBreakableShape>> m_pools = new Dictionary<MyDefinitionId, ConcurrentQueue<HkdBreakableShape>>();
        MyWorkTracker<MyDefinitionId, MyBreakableShapeCloneJob> m_tracker = new MyWorkTracker<MyDefinitionId, MyBreakableShapeCloneJob>();

        public void Preallocate()
        {
            foreach (var groupName in MyDefinitionManager.Static.GetDefinitionPairNames())
            {
                var group = MyDefinitionManager.Static.GetDefinitionGroup(groupName);
                
                if (group.Large != null && group.Large.Public)
                {
                    var definition = group.Large;
                    AllocateForDefinition(definition, PREALLOCATE_COUNT);
                }

                if (group.Small != null && group.Small.Public)
                {
                    AllocateForDefinition(group.Small, PREALLOCATE_COUNT);
                }
            }
        }

        public void AllocateForDefinition(MyPhysicalModelDefinition definition, int count)
        {
            if (string.IsNullOrEmpty(definition.Model))
                return;
            ProfilerShort.Begin("Clone");
            var data = MyModels.GetModelOnlyData(definition.Model);
            if (MyFakes.LAZY_LOAD_DESTRUCTION && data.HavokBreakableShapes == null)
            {
                MyDestructionData.Static.LoadModelDestruction(definition, false, data.BoundingBoxSize);
            }               
            if (data.HavokBreakableShapes != null && data.HavokBreakableShapes.Length > 0)
            {
                if(!m_pools.ContainsKey(definition.Id))
                    m_pools[definition.Id] = new ConcurrentQueue<HkdBreakableShape>();
                var queue = m_pools[definition.Id];
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
                    if (pool.Value.Count < PREALLOCATE_COUNT)
                    {
                        MyPhysicalModelDefinition def;
                        MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(pool.Key, out def);
                        int cloneCount = Math.Min(PREALLOCATE_COUNT - pool.Value.Count, MAX_CLONE_PER_FRAME - clonedThisFrame);
                        AllocateForDefinition(def, cloneCount);
                        clonedThisFrame += cloneCount;
                    }
                    if (clonedThisFrame == MAX_CLONE_PER_FRAME)
                        break;
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
                if (pool.Value.Count < PREALLOCATE_COUNT && !m_tracker.Exists(pool.Key))
                {
                    MyPhysicalModelDefinition def;
                    MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(pool.Key, out def);
                    MyBreakableShapeCloneJob.Args args = new MyBreakableShapeCloneJob.Args();
                    args.DefId = pool.Key;
                    args.ShapeToClone = MyModels.GetModelOnlyData(def.Model).HavokBreakableShapes[0];
                    args.Count = PREALLOCATE_COUNT - pool.Value.Count;
                    args.Tracker = m_tracker;
                    MyBreakableShapeCloneJob.Start(args);
                }
            }
        }

        public HkdBreakableShape GetBreakableShape(MyCubeBlockDefinition block)
        {
            m_dequeuedThisFrame = true;
            ProfilerShort.Begin("GetBreakableShape");
            if ((!block.Public || MyFakes.LAZY_LOAD_DESTRUCTION) && !m_pools.ContainsKey(block.Id))
                m_pools[block.Id] = new ConcurrentQueue<HkdBreakableShape>();
            var queue = m_pools[block.Id];
            if (queue.Count == 0)
            {
                AllocateForDefinition(block, 1);
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
                foreach (var shape in pool)
                {
                    shape.RemoveReference();
                }
            }
            m_pools.Clear();
        }

        public void EnqueShapes(MyDefinitionId id, List<HkdBreakableShape> shapes)
        {
            foreach (var shape in shapes)
                m_pools[id].Enqueue(shape);
            m_missing -= shapes.Count;
        }

        public void EnqueShape(MyDefinitionId id, HkdBreakableShape shape)
        {
            m_pools[id].Enqueue(shape);
            m_missing--;
        }
    }
}
