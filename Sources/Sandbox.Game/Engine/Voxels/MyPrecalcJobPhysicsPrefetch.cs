using Havok;
using ParallelTasks;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
using VRage.Generics;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    internal sealed class MyPrecalcJobPhysicsPrefetch : MyPrecalcJob
    {
        public struct Args
        {
            public MyWorkTracker<MyCellCoord, MyPrecalcJobPhysicsPrefetch> Tracker;

            public IMyStorage Storage;
            public MyCellCoord GeometryCell;
            public MyVoxelPhysicsBody TargetPhysics;
        }

        private static readonly MyDynamicObjectPool<MyPrecalcJobPhysicsPrefetch> m_instancePool = new MyDynamicObjectPool<MyPrecalcJobPhysicsPrefetch>(16);

        private Args m_args;
        private volatile bool m_isCancelled;


        private HkBvCompressedMeshShape m_result;

        public MyPrecalcJobPhysicsPrefetch() : base(true) { }

        public static void Start(Args args)
        {
            var job = m_instancePool.Allocate();

            job.m_args = args;
            args.Tracker.Add(args.GeometryCell, job);

            MyPrecalcComponent.EnqueueBack(job);
        }

        public override void DoWork()
        {
            ProfilerShort.Begin("MyPrecalcJobPhysicsPrefetch.DoWork");
            try
            {
                if (m_isCancelled)
                    return;

                var geometryData = m_args.TargetPhysics.CreateMesh(m_args.Storage, m_args.GeometryCell);

                if (m_isCancelled)
                    return;

                if (!MyIsoMesh.IsEmpty(geometryData))
                {
                    m_result = m_args.TargetPhysics.CreateShape(geometryData);
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
                m_args.TargetPhysics.OnTaskComplete(m_args.GeometryCell, m_result);
            }

            if (!m_isCancelled)
            {
                m_args.Tracker.Complete(m_args.GeometryCell);
            }

            if (!m_result.Base.IsZero)
                m_result.Base.RemoveReference();

            m_args = default(Args);
            m_isCancelled = false;
            m_result = (HkBvCompressedMeshShape)HkShape.Empty;
            m_instancePool.Deallocate(this);
        }

        public override void Cancel()
        {
            m_isCancelled = true;
        }
    }
}
