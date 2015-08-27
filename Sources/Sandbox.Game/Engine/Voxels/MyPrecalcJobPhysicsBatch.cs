using Havok;
using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
using VRage.Generics;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    internal sealed class MyPrecalcJobPhysicsBatch : MyPrecalcJob
    {
        private static readonly MyDynamicObjectPool<MyPrecalcJobPhysicsBatch> m_instancePool = new MyDynamicObjectPool<MyPrecalcJobPhysicsBatch>(8);

        private MyVoxelPhysicsBody m_targetPhysics;
        internal HashSet<Vector3I> CellBatch = new HashSet<Vector3I>(Vector3I.Comparer);

        private Dictionary<Vector3I, HkBvCompressedMeshShape> m_newShapes = new Dictionary<Vector3I, HkBvCompressedMeshShape>(Vector3I.Comparer);
        private volatile bool m_isCancelled;

        public MyPrecalcJobPhysicsBatch() : base(true) { }

        public static void Start(MyVoxelPhysicsBody targetPhysics, ref HashSet<Vector3I> cellBatchForSwap)
        {
            var job = m_instancePool.Allocate();

            job.m_targetPhysics = targetPhysics;
            MyUtils.Swap(ref job.CellBatch, ref cellBatchForSwap);
            Debug.Assert(targetPhysics.RunningBatchTask == null);
            targetPhysics.RunningBatchTask = job;
            MyPrecalcComponent.EnqueueBack(job, false);
        }

        public override void DoWork()
        {
            ProfilerShort.Begin("MyPrecalcJobPhysicsBatch.DoWork");
            try
            {
                IMyStorage storage = m_targetPhysics.m_voxelMap.Storage;
                foreach (var cell in CellBatch)
                {
                    if (m_isCancelled)
                        break;

                    var geometryData = m_targetPhysics.CreateMesh(storage, cell);
                    if (m_isCancelled)
                        break;

                    if (!MyIsoMesh.IsEmpty(geometryData))
                    {
                        var meshShape = m_targetPhysics.CreateShape(geometryData);
                        m_newShapes.Add(cell, meshShape);
                    }
                    else
                    {
                        m_newShapes.Add(cell, (HkBvCompressedMeshShape)HkShape.Empty);
                    }
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        protected override void OnComplete()
        {
            base.OnComplete();

            if (MyPrecalcComponent.Loaded && !m_isCancelled)
            {
                Debug.Assert(m_targetPhysics.RunningBatchTask == this);
                m_targetPhysics.OnBatchTaskComplete(m_newShapes);
            }

            foreach (var newShape in m_newShapes.Values)
            {
                if (!newShape.Base.IsZero)
                    newShape.Base.RemoveReference();
            }

            if (m_targetPhysics.RunningBatchTask == this)
                m_targetPhysics.RunningBatchTask = null;
            m_targetPhysics = null;
            CellBatch.Clear();
            m_newShapes.Clear();
            m_isCancelled = false;
            m_instancePool.Deallocate(this);
        }

        public override void Cancel()
        {
            m_isCancelled = true;
        }
    }
}
