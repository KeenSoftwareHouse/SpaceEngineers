using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VRage;
using VRage.Collections;
using VRage.Generics;
using VRage.Utils;
using VRageMath;
using IPrioritizedWork = ParallelTasks.IPrioritizedWork;
using IWork = ParallelTasks.IWork;
using Parallel = ParallelTasks.Parallel;
using WorkOptions = ParallelTasks.WorkOptions;
using WorkPriority = ParallelTasks.WorkPriority;

//  Good tutorials on thread synchronization events: 
//      http://www.codeproject.com/KB/threads/AutoManualResetEvents.aspx
//      http://www.albahari.com/threading/part2.aspx#_ProducerConsumerQWaitHandle
//      http://www.albahari.com/threading/part4.aspx#_Nonblocking_Synchronization

namespace Sandbox.Engine.Voxels
{
    // mk:TODO Think of some better scheme of prioritizing works (new render cell, invalid render cell, physics prefetch, invalid physics batch).
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class MyPrecalcComponent : MySessionComponentBase
    {
        [ThreadStatic]
        private static IMyIsoMesher m_isoMesher;
        public static IMyIsoMesher IsoMesher
        {
            get
            {
                if (m_isoMesher == null)
                    m_isoMesher = (IMyIsoMesher)Activator.CreateInstance(MyPerGameSettings.IsoMesherType);

                return m_isoMesher;
            }
        }

        [ThreadStatic]
        private static List<JobCancelCommand> m_threadCommandBuffer;
        private static List<JobCancelCommand> ThreadCommandBuffer
        {
            get
            {
                if (m_threadCommandBuffer != null)
                    return m_threadCommandBuffer;

                m_threadCommandBuffer = new List<JobCancelCommand>();
                lock (CommandBuffers)
                {
                    CommandBuffers.Add(m_threadCommandBuffer);
                }
                return m_threadCommandBuffer;
            }
        }

        private static readonly List<List<JobCancelCommand>> CommandBuffers = new List<List<JobCancelCommand>>();

        private static readonly MyConcurrentDeque<MyPrecalcJob> m_highPriorityJobs = new MyConcurrentDeque<MyPrecalcJob>();
        private static readonly MyConcurrentDeque<MyPrecalcJob> m_lowPriorityJobs = new MyConcurrentDeque<MyPrecalcJob>();
        private static readonly MyDynamicObjectPool<Work> m_workPool = new MyDynamicObjectPool<Work>(1);
        private static int m_worksInUse;
        private static MyPrecalcComponent m_instance;

        internal static readonly HashSet<MyVoxelPhysicsBody> PhysicsWithInvalidCells = new HashSet<MyVoxelPhysicsBody>();

        public static new bool Loaded
        {
            get { return m_instance != null; }
        }

        public static int InvalidatedRangeInflate
        {
            get { return IsoMesher.InvalidatedRangeInflate; }
        }

        public static void EnqueueBack(MyPrecalcJob job, bool isHighPriority)
        {
            if (isHighPriority)
                m_highPriorityJobs.EnqueueBack(job);
            else
                m_lowPriorityJobs.EnqueueBack(job);
        }

        internal static void QueueJobCancel(MyWorkTracker<Vector3I, MyPrecalcJobPhysicsPrefetch> tracker, Vector3I id)
        {
            ThreadCommandBuffer.Add(new JobCancelCommand
            {
                Tracker = tracker,
                Id = id,
            });
        }

        [Conditional("DEBUG")]
        public static void AssertUpdateThread()
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);
        }

        public override void LoadData()
        {
            m_instance = this;

            if (Environment.ProcessorCount <= 2)
            { // throttle down precalc on worker threads when it could hurt Update and Render thread.
                MyFakes.MAX_PRECALC_TIME_IN_MILLIS = 6f;
            }
            else
            {
                MyFakes.MAX_PRECALC_TIME_IN_MILLIS = 14f;
            }

        }

        public override bool UpdatedBeforeInit()
        {
            return true;
        }

        public override void UpdateBeforeSimulation()
        {
            if (m_highPriorityJobs.Count > 0)
            { // no upper bound on these, as there should be just a few high priority jobs
                for (int i = 0; i < Parallel.Scheduler.ThreadCount; ++i)
                {
                    var work = m_workPool.Allocate();
                    work.Queue = m_highPriorityJobs;
                    work.Priority = WorkPriority.Low;
                    work.MaxPrecalcTime = (long)MyFakes.MAX_PRECALC_TIME_IN_MILLIS;
                    ++m_worksInUse;
                    Parallel.Start(work, work.CompletionCallback);
                }
            }

            if (m_lowPriorityJobs.Count > 0 && m_worksInUse < 2 * Parallel.Scheduler.ThreadCount)
            {
                for (int i = 0; i < Parallel.Scheduler.ThreadCount; ++i)
                {
                    var work = m_workPool.Allocate();
                    work.Queue = m_lowPriorityJobs;
                    work.Priority = WorkPriority.VeryLow;
                    work.MaxPrecalcTime = (long)MyFakes.MAX_PRECALC_TIME_IN_MILLIS;
                    ++m_worksInUse;
                    Parallel.Start(work, work.CompletionCallback);
                }
            }

            foreach (var physics in PhysicsWithInvalidCells)
            {
                MyPrecalcJobPhysicsBatch.Start(physics, ref physics.InvalidCells);
            }
            PhysicsWithInvalidCells.Clear();

            Stats.Generic.Write("Precalc jobs in queue (low)", m_lowPriorityJobs.Count, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 0);
            Stats.Generic.Write("Precalc jobs in queue (high)", m_highPriorityJobs.Count, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 0);

            if (!MySandboxGame.IsGameReady)
            {
                var work = m_workPool.Allocate();
                work.Queue = m_lowPriorityJobs;
                work.MaxPrecalcTime = (long)MyFakes.MAX_PRECALC_TIME_IN_MILLIS;
                (work as IWork).DoWork();
                work.CompletionCallback();
            }

            base.UpdateAfterSimulation();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Applying all buffered commands that have been collected from threads during simulation step.
            foreach (var threadCommandBuffer in CommandBuffers)
            {
                foreach (var cancelCommand in threadCommandBuffer)
                {
                    cancelCommand.Tracker.Cancel(cancelCommand.Id);
                }
                threadCommandBuffer.Clear();
            }
        }

        protected override void UnloadData()
        {
            m_highPriorityJobs.Clear();
            m_lowPriorityJobs.Clear();
            m_instance = null;
        }

        class Work : IPrioritizedWork
        {
            private readonly List<MyPrecalcJob> m_finishedList = new List<MyPrecalcJob>();
            private readonly Stopwatch m_timer = new Stopwatch();
            private WorkPriority m_workPriority;
            public long MaxPrecalcTime;

            public readonly Action CompletionCallback;

            public MyConcurrentDeque<MyPrecalcJob> Queue;

            public Work()
            {
                CompletionCallback = OnComplete;
            }

            private void OnComplete()
            {
                foreach (var finished in m_finishedList)
                {
                    finished.OnCompleteDelegate();
                }
                Queue = null;
                m_finishedList.Clear();
                m_workPool.Deallocate(this);
                --m_worksInUse;
            }

            public WorkPriority Priority
            {
                get { return m_workPriority; }
                set { m_workPriority = value; }
            }

            void IWork.DoWork()
            {
                ProfilerShort.Begin("MyPrecalcWork");
                m_timer.Start();
                MyPrecalcJob work;
                while (Queue.TryDequeueFront(out work))
                {
                    work.DoWork();
                    m_finishedList.Add(work);
                    if (m_timer.ElapsedMilliseconds >= MaxPrecalcTime)
                        break;
                    if (MyFakes.ENABLE_YIELDING_IN_PRECALC_TASK)
                        Thread.Yield();
                }

                m_timer.Stop();
                m_timer.Reset();
                ProfilerShort.End();
            }

            WorkOptions IWork.Options
            {
                get { return Parallel.DefaultOptions; }
            }
        }

        struct JobCancelCommand
        {
            public MyWorkTracker<Vector3I, MyPrecalcJobPhysicsPrefetch> Tracker;
            public Vector3I Id;
        }

    }

}
