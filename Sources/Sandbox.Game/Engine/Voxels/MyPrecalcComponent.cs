using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ParallelTasks;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Generics;
using VRage.Library.Collections;
using VRage.Utils;
using VRageMath;
using VRage.Library;
using VRage.Profiler;
using VRage.Voxels;
using VRageRender.Voxels;
using VRageRender.Utils;
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
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class MyPrecalcComponent : MySessionComponentBase
    {
        static bool MULTITHREADED = true;

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

        private static readonly MyConcurrentSortableQueue<MyPrecalcJob> m_sortedJobs = new MyConcurrentSortableQueue<MyPrecalcJob>();
        private static readonly List<MyPrecalcJob> m_addedJobs = new List<MyPrecalcJob>();
        private static MyDynamicObjectPool<Work> m_workPool;
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

        public static void EnqueueBack(MyPrecalcJob job)
        {
            if (MyFakes.PRIORITIZE_PRECALC_JOBS)
            {
                ProfilerShort.Begin("SortedJobs.Add");
                m_addedJobs.Add(job);
                ProfilerShort.End();
                return;
            }
        }

        internal static void QueueJobCancel(MyWorkTracker<MyCellCoord, MyPrecalcJobPhysicsPrefetch> tracker, MyCellCoord id)
        {
            var threadCommandBuffer = ThreadCommandBuffer;
            lock (threadCommandBuffer)
            {
                ThreadCommandBuffer.Add(new JobCancelCommand
                {
                    Tracker = tracker,
                    Id = id,
                });
            }
        }

        [Conditional("DEBUG")]
        public static void AssertUpdateThread()
        {
            //Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);
        }

        public override void LoadData()
        {
            m_instance = this;

            if (MyEnvironment.ProcessorCount <= 2)
            { // throttle down precalc on worker threads when it could hurt Update and Render thread.
                MyFakes.MAX_PRECALC_TIME_IN_MILLIS = 6f;
            }
            else
            {
                MyFakes.MAX_PRECALC_TIME_IN_MILLIS = 14f;
            }

            m_workPool = new MyDynamicObjectPool<Work>(Parallel.Scheduler.ThreadCount);
        }

        public override bool UpdatedBeforeInit()
        {
            return true;
        }

        class MyPrecalcJobComparer : IComparer<MyPrecalcJob>
        {
            public int Compare(MyPrecalcJob x, MyPrecalcJob y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }

        private static MyPrecalcJobComparer m_comparer = new MyPrecalcJobComparer();


        private int m_counter;
        public override void UpdateBeforeSimulation()
        {
            m_sortedJobs.Lock();
            m_sortedJobs.List.AddList(m_addedJobs);
            m_sortedJobs.Unlock();
            m_addedJobs.Clear();
            m_counter++;
            if (m_counter % 30 == 0)
            {
                SortJobs();
                //m_sortedJobs.Sort(m_comparer);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_SORTED_JOBS)
            {
                try
                {
                    const float max = 255;
                    float shade = m_sortedJobs.Count > 0 ? m_sortedJobs.ListUnsafe.ItemAt((int)Math.Min(m_sortedJobs.ListUnsafe.Count - 1, max)).Priority : 1;
                    float minPriority = m_sortedJobs.Count > 0 ? m_sortedJobs.ListUnsafe.ItemAt(0).Priority : 1;
                    shade -= minPriority;
                    for (int xi = 0; xi < max; xi++)
                    {
                        if (xi + 5 > m_sortedJobs.Count)
                            break;
                        var job = m_sortedJobs.ListUnsafe.ItemAt(xi);
                        var p = job.Priority - minPriority;
                        job.DebugDraw(new Color((shade - p) / shade, 0.0f, p / shade, (max - xi) / max));
                    }
                }
                catch (Exception e)
                { }
            }

            if (m_sortedJobs.Count > 0)
            {
                while (m_workPool.Count > 0)
                {
                    var work = m_workPool.Allocate();
                    work.Queue = m_sortedJobs;
                    work.Priority = WorkPriority.Low;
                    work.MaxPrecalcTime = (long)MyFakes.MAX_PRECALC_TIME_IN_MILLIS;

                    if (MULTITHREADED)
                        Parallel.Start(work, work.CompletionCallback);
                    else
                    {
                        ((IWork)work).DoWork();
                        work.CompletionCallback();
                    }
                }
            }

            foreach (var physics in PhysicsWithInvalidCells)
            {
                for (int lod = 0; lod < physics.InvalidCells.Length; lod++)
                {
                    if (physics.InvalidCells[lod].Count > 0 && physics.RunningBatchTask[lod] == null)
                        MyPrecalcJobPhysicsBatch.Start(physics, ref physics.InvalidCells[lod], lod);
                }
            }
            PhysicsWithInvalidCells.Clear();

            Stats.Generic.Write("Precalc jobs in sorted", m_sortedJobs.Count, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 0);
            Stats.Generic.Write("Clipmap triangle cache", MyClipmap.CellsCache.Usage, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 2);

            base.UpdateAfterSimulation();
        }

        private void SortJobs()
        {
            ProfilerShort.Begin("SortJobs");
            m_sortedJobs.Lock();
            var list = m_sortedJobs.List;
            //int front = 0;
            //int mid = 1;
            //int back = list.Count - 1;

            //ProfilerShort.Begin("FirstPAss");
            //for (int i = 0; i <= back; i++)
            //{
            //    var p = list[i].Priority;
            //    if (p == int.MaxValue)
            //        list.Swap(i, back--);
            //}
            //ProfilerShort.BeginNextBlock("SecondPass");

            //int min = int.MaxValue;
            //if (list.Count > 100) 
            //for (int i = 0; i < 100; i++ )
            //{
            //    int x = VRage.Library.Utils.MyRandom.Instance.Next() % back;
            //    var p = list[x].Priority;
            //    if (p > 1 && p < min)
            //        min = p;
            //}
            ////avg *= 0.1f;
            //min += 100;
            //min = Math.Max(1, min);
            //for (int i = 0; i < list.Count; i++ )
            //{
            //    if (mid == list.Count)
            //        break;
            //    if(list[i].IsCanceled)
            //    {
            //        list.RemoveAtFast(i--);
            //        continue;
            //    }
            //    var p = list[i].Priority;
            //    if (p <= min)
            //    {
            //        if (p <= 1)
            //        {
            //            list.Swap(i, front++);
            //        }
            //        list.Swap(i, mid++);
            //    }
            //}
            ProfilerShort.Begin("Sort");
            //if (mid < list.Count)
            list.Sort(m_comparer);
            ProfilerShort.End();

            m_sortedJobs.Unlock();
            ProfilerShort.End();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Applying all buffered commands that have been collected from threads during simulation step.
            lock (CommandBuffers)
            {
                foreach (var threadCommandBuffer in CommandBuffers)
                {
                    lock (threadCommandBuffer)
                    {
                        foreach (var cancelCommand in threadCommandBuffer)
                            cancelCommand.Tracker.Cancel(cancelCommand.Id);

                        threadCommandBuffer.Clear();
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            m_instance = null;
        }

        class Work : IPrioritizedWork
        {
            private readonly List<MyPrecalcJob> m_finishedList = new List<MyPrecalcJob>();
            private readonly Stopwatch m_timer = new Stopwatch();
            private WorkPriority m_workPriority;
            public long MaxPrecalcTime;

            public readonly Action CompletionCallback;

            public IMyQueue<MyPrecalcJob> Queue;

            public Work()
            {
                CompletionCallback = OnComplete;
            }

            private void OnComplete()
            {
                ProfilerShort.Begin("MyPrecalcComponent - Complete");
                foreach (var finished in m_finishedList)
                {
                    finished.OnCompleteDelegate();
                }
                Queue = null;
                m_finishedList.Clear();
                m_workPool.Deallocate(this);
                ProfilerShort.End();
            }

            public WorkPriority Priority
            {
                get { return m_workPriority; }
                set { m_workPriority = value; }
            }

            void IWork.DoWork(ParallelTasks.WorkData workData = null)
            {
                m_timer.Start();
                MyPrecalcJob work;
                while (Queue.TryDequeueFront(out work))
                {
                    if (work.IsCanceled)
                    {
                        m_finishedList.Add(work);
                        continue;
                    }
                    try
                    {
                        ProfilerShort.Begin("MyPrecalcWork");
                        work.DoWork();
                    }
                    finally
                    {
                        ProfilerShort.End();
                    }
                    m_finishedList.Add(work);
                    if (m_timer.ElapsedMilliseconds >= MaxPrecalcTime)
                        break;
                    if (MyFakes.ENABLE_YIELDING_IN_PRECALC_TASK)
                        Thread.Yield();
                }

                m_timer.Stop();
                m_timer.Reset();
            }

            WorkOptions IWork.Options
            {
                get { return Parallel.DefaultOptions; }
            }
        }

        struct JobCancelCommand
        {
            public MyWorkTracker<MyCellCoord, MyPrecalcJobPhysicsPrefetch> Tracker;
            public MyCellCoord Id;
        }


    }

}
