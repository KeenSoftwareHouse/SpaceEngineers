using Havok;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Generics;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    internal sealed class MyPrecalcJobPhysicsBatch : MyPrecalcJob
    {
        private static readonly MyDynamicObjectPool<MyPrecalcJobPhysicsBatch> m_instancePool = new MyDynamicObjectPool<MyPrecalcJobPhysicsBatch>(8);

        private MyVoxelPhysicsBody m_targetPhysics;
        internal HashSet<Vector3I> CellBatch = new HashSet<Vector3I>(Vector3I.Comparer);

        private Dictionary<Vector3I, HkShape> m_newShapes = new Dictionary<Vector3I, HkShape>(Vector3I.Comparer);
        private volatile bool m_isCancelled;

        public int Lod;

        public MyPrecalcJobPhysicsBatch() : base(true) { }

        public static void Start(MyVoxelPhysicsBody targetPhysics, ref HashSet<Vector3I> cellBatchForSwap, int lod)
        {
            var job = m_instancePool.Allocate();

            job.Lod = lod;

            job.m_targetPhysics = targetPhysics;
            MyUtils.Swap(ref job.CellBatch, ref cellBatchForSwap);
            Debug.Assert(targetPhysics.RunningBatchTask[lod] == null);
            targetPhysics.RunningBatchTask[lod] = job;
            MyPrecalcComponent.EnqueueBack(job);
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

                    var geometryData = m_targetPhysics.CreateMesh(storage, new MyCellCoord(Lod, cell));
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
                Debug.Assert(m_targetPhysics.RunningBatchTask[Lod] == this);
                m_targetPhysics.OnBatchTaskComplete(m_newShapes, Lod);
            }

            foreach (var newShape in m_newShapes.Values)
            {
                if (!newShape.IsZero)
                    newShape.RemoveReference();
            }

            if (m_targetPhysics.RunningBatchTask[Lod] == this)
                m_targetPhysics.RunningBatchTask[Lod] = null;
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
