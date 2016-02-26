using Havok;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Generics;

namespace Sandbox.Engine.Physics
{
    public class MyBreakableShapeCloneJob : MyPrecalcJob
    {
        public struct Args
        {
            public MyWorkTracker<MyDefinitionId, MyBreakableShapeCloneJob> Tracker;

            public string Model;
            public MyDefinitionId DefId;
            public HkdBreakableShape ShapeToClone;
            public int Count;
        }

        private static readonly MyDynamicObjectPool<MyBreakableShapeCloneJob> m_instancePool = new MyDynamicObjectPool<MyBreakableShapeCloneJob>(16);

        private Args m_args;
        private List<HkdBreakableShape> m_clonedShapes = new List<HkdBreakableShape>();
        private bool m_isCanceled;

        public static void Start(Args args)
        {
            var job = m_instancePool.Allocate();

            job.m_args = args;
            args.Tracker.Add(args.DefId, job);

            MyPrecalcComponent.EnqueueBack(job);
        }
        public MyBreakableShapeCloneJob():base(true) {}

        public override void DoWork()
        {
            for (int i = 0; i < m_args.Count; i++)
            {
                if (m_isCanceled && i > 0)
                    return;
                m_clonedShapes.Add(m_args.ShapeToClone.Clone());
            }
        }

        public override void Cancel()
        {
            m_isCanceled = true;
        }

        protected override void OnComplete()
        {
            base.OnComplete();
            System.Diagnostics.Debug.Assert(m_clonedShapes.Count > 0);
            if (MyDestructionData.Static != null && MyDestructionData.Static.BlockShapePool != null)
            {
                MyDestructionData.Static.BlockShapePool.EnqueShapes(m_args.Model, m_args.DefId, m_clonedShapes);
            }

            m_clonedShapes.Clear();
            m_args.Tracker.Complete(m_args.DefId);
            m_args = default(Args);
            m_isCanceled = false;
            m_instancePool.Deallocate(this);
        }
    }
}
