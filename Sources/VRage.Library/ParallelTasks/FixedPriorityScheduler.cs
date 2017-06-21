using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Collections;

namespace ParallelTasks
{
#if !UNSHARPER
    public class FixedPriorityScheduler : IWorkScheduler
    {
        private readonly MyConcurrentQueue<Task>[] m_taskQueuesByPriority;
        private readonly Worker[] m_workers;
        private readonly ManualResetEvent[] m_hasNoWork;
        private long m_scheduledTaskCount;

        public int ThreadCount
        {
            get { return m_workers.Length; }
        }

        public FixedPriorityScheduler(int threadCount, ThreadPriority priority)
        {
            m_taskQueuesByPriority = new MyConcurrentQueue<Task>[typeof(WorkPriority).GetEnumValues().Length];
            for (int i = 0; i < m_taskQueuesByPriority.Length; ++i)
                m_taskQueuesByPriority[i] = new MyConcurrentQueue<Task>();

            m_hasNoWork = new ManualResetEvent[threadCount];
            m_workers = new Worker[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                m_workers[i] = new Worker(this, "Parallel " + i, priority);
                m_hasNoWork[i] = m_workers[i].HasNoWork;
            }
        }

        private bool TryGetTask(out Task task)
        {
            while (m_scheduledTaskCount > 0)
            {
                for (int i = 0; i < m_taskQueuesByPriority.Length; ++i)
                {
                    if (m_taskQueuesByPriority[i].TryDequeue(out task))
                    {
                        Interlocked.Decrement(ref m_scheduledTaskCount);
                        return true;
                    }
                }
            }
            task = default(Task);
            return false;
        }

        public void Schedule(Task task)
        {
            //Unfortunatelly this can happen, who knows why
            if (task.Item.Work == null)
                return;

            WorkPriority priority = WorkPriority.Normal;
            {
                var prioritizedWork = task.Item.Work as IPrioritizedWork;
                if (prioritizedWork != null)
                {
                    priority = prioritizedWork.Priority;
                }
            }
            m_taskQueuesByPriority[(int)priority].Enqueue(task);
            Interlocked.Increment(ref m_scheduledTaskCount);

            foreach (var worker in m_workers)
                worker.Gate.Set();
        }

        public bool WaitForTasksToFinish(TimeSpan waitTimeout)
        {
            return Parallel.WaitForAll(m_hasNoWork, waitTimeout);
        }

        class Worker
        {
            private readonly FixedPriorityScheduler m_scheduler;
            private readonly Thread m_thread;
            public readonly ManualResetEvent HasNoWork;
            public readonly AutoResetEvent Gate;

            public Worker(FixedPriorityScheduler scheduler, string name, ThreadPriority priority)
            {
                m_scheduler = scheduler;
                m_thread = new Thread(new ParameterizedThreadStart(WorkerLoop));
                HasNoWork = new ManualResetEvent(false);
                Gate = new AutoResetEvent(false);

                m_thread.Name = name;
                m_thread.IsBackground = true;
                m_thread.Priority = priority;
                m_thread.CurrentCulture = CultureInfo.InvariantCulture;
                m_thread.CurrentUICulture = CultureInfo.InvariantCulture;
                m_thread.Start(null);
            }

            private void WorkerLoop(object o)
            {
                while (true)
                {
                    Task task;
                    if (m_scheduler.TryGetTask(out task))
                    {
                        task.DoWork();
                    }
                    else
                    {
                        HasNoWork.Set();
                        Gate.WaitOne();
                        HasNoWork.Reset();
                    }
                }
            }
        }
    }

#endif //UNSHARPER

}
