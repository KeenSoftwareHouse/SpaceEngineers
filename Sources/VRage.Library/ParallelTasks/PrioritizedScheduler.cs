using System;
using System.Globalization;
using System.Threading;
using VRage.Collections;

namespace ParallelTasks
{
#if !UNSHARPER
    /// <summary>
    /// Sheduler that supports interruption of normal 
    /// </summary>
    public class PrioritizedScheduler : IWorkScheduler
    {
        private readonly int[] m_mappingPriorityToWorker = new[] { 0, 1, 1, 1, 2 };
        private readonly ThreadPriority[] m_mappingWorkerToThreadPriority = new[] { ThreadPriority.Highest, ThreadPriority.Normal, ThreadPriority.Lowest };
        
        private WorkerArray[] m_workerArrays;
        private WaitHandle[] m_hasNoWork;   // array of manual reset events from all workers (=from all worker arrays)

        /// <summary>
        /// Reveals only the count of one thread array. This value is intended to be used as a "number of threads that can run in parallel."
        /// </summary>
        public int ThreadCount
        {
            get { return m_workerArrays[0].Workers.Length; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="threadCount">Number of threads in each worker group.</param>
        public PrioritizedScheduler(int threadCount)
        {
            InitializeWorkerArrays(threadCount);
        }

        /// <summary>
        /// Initialize all worker arrays.
        /// </summary>
        /// <param name="threadCount">Each array of workers will contain number of threads = threadCount.</param>
        private void InitializeWorkerArrays(int threadCount)
        {
            // determine how many arrays from the mapping
            int maxIndex = 0;
            foreach (int mappingIndex in m_mappingPriorityToWorker)
                maxIndex = mappingIndex > maxIndex ? mappingIndex : maxIndex; 

            // initialize arrays
            m_workerArrays = new WorkerArray[maxIndex + 1];
            m_hasNoWork = new WaitHandle[(maxIndex + 1) * threadCount];
            int hasNoWorkIterator = 0;
            for (int i = 0; i <= maxIndex; i++)
            {
                m_workerArrays[i] = new WorkerArray(this, i, threadCount, m_mappingWorkerToThreadPriority[i]);
                for (int workerIndex = 0; workerIndex < threadCount; workerIndex++) // initialize workers inside the array
                {
                    m_hasNoWork[hasNoWorkIterator++] = m_workerArrays[i].Workers[workerIndex].HasNoWork;
                }
            }
        }

        private WorkerArray GetWorkerArray(WorkPriority priority)
        {
            return m_workerArrays[m_mappingPriorityToWorker[(int)priority]];
        }

        public void Schedule(Task task)
        {
            if (task.Item.Work == null)  // deny tasks with no content
                return;

            WorkPriority priority = WorkPriority.Normal;
            {
                var prioritizedWork = task.Item.Work as IPrioritizedWork;
                if (prioritizedWork != null)
                {
                    priority = prioritizedWork.Priority;
                }
            }
            GetWorkerArray(priority).Schedule(task, priority);
        }

        public bool WaitForTasksToFinish(TimeSpan waitTimeout)
        {
            return Parallel.WaitForAll(m_hasNoWork, waitTimeout);
        }

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// Worker array groups workers of the same thread priority.
        /// It also contains the queues of tasks belonging to this thread priority.
        /// </summary>
        class WorkerArray
        {
            /// <summary>
            /// Reference to the scheduler.
            /// </summary>
            private PrioritizedScheduler m_prioritizedScheduler;
            /// <summary>
            /// Index of this worker array.
            /// </summary>
            private readonly int m_workerArrayIndex;
            /// <summary>
            /// Task queues. Even in one worker group, tasks will be sorted according to their priority.
            /// </summary>
            private readonly MyConcurrentQueue<Task>[] m_taskQueuesByPriority = new MyConcurrentQueue<Task>[typeof(WorkPriority).GetEnumValues().Length];
            /// <summary>
            /// Total number of tasks inside the queue.
            /// </summary>
            private long m_scheduledTaskCount;
            /// <summary>
            /// Array of worker threads.
            /// </summary>
            private readonly Worker[] m_workers;

            private const int DEFAULT_QUEUE_CAPACITY = 64;

            /// <summary>
            /// Array of worker threads.
            /// </summary>
            public Worker[] Workers
            {
                get { return m_workers; }
            }

            /// <summary>
            /// Constructor of the worker array.
            /// </summary>
            /// <param name="prioritizedScheduler">Scheduler, owner of this group.</param>
            public WorkerArray(PrioritizedScheduler prioritizedScheduler, int workerArrayIndex, int threadCount, ThreadPriority systemThreadPriority)
            {
                for (int i = 0; i < m_taskQueuesByPriority.Length; i++)
                {
                    m_taskQueuesByPriority[i] = new MyConcurrentQueue<Task>(DEFAULT_QUEUE_CAPACITY);
                }
                m_workerArrayIndex = workerArrayIndex;
                m_prioritizedScheduler = prioritizedScheduler;
                m_workers = new Worker[threadCount];
                for (int workerIndex = 0; workerIndex < threadCount; workerIndex++) // initialize workers inside the array
                {
                    m_workers[workerIndex] = new Worker(this, "Parallel " + systemThreadPriority + "_" + workerIndex, systemThreadPriority);
                }
            }

            /// <summary>
            /// Try getting the task from the internal queue.
            /// </summary>
            /// <param name="task"></param>
            /// <returns></returns>
            public bool TryGetTask(out Task task)
            {
                while (m_scheduledTaskCount > 0)
                {
                    foreach (MyConcurrentQueue<Task> queue in m_taskQueuesByPriority)
                    {
                        if (queue.TryDequeue(out task))
                        {
                            Interlocked.Decrement(ref m_scheduledTaskCount);
                            return true;
                        }
                    }
                }
                task = default(Task);
                return false;
            }

            /// <summary>
            /// Schedule the task in this worker array.
            /// </summary>
            /// <param name="task"></param>
            /// <param name="priority"></param>
            public void Schedule(Task task, WorkPriority priority)
            {
                m_taskQueuesByPriority[(int)priority].Enqueue(task);
                Interlocked.Increment(ref m_scheduledTaskCount);

                foreach (var worker in m_workers)
                    worker.Gate.Set();
            }
        }

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// One worker thread of the prioritized scheduler.
        /// </summary>
        class Worker
        {
            // owner of this worker (worker group)
            private readonly WorkerArray m_workerArray; 
            // reference to OS Thread used by this worker
            private readonly Thread m_thread;
            // ---------------
            public readonly ManualResetEvent HasNoWork;
            public readonly AutoResetEvent Gate;
            // ---------------

            /// <summary>
            /// Get the underlying system thread.
            /// </summary>
            public Thread Thread
            {
                get { return m_thread; }
            }

            // ---------------
            public Worker(WorkerArray workerArray, string name, ThreadPriority priority)
            {
                m_workerArray = workerArray;
                m_thread = new Thread(WorkerLoop);
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
                    if (m_workerArray.TryGetTask(out task))
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
                // ReSharper disable once FunctionNeverReturns
            }
        }
    }

#endif //UNSHARPER

}
