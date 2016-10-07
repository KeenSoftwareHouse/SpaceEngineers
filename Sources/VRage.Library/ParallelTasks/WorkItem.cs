using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VRage;
using VRage.Collections;
using VRage.Library;

namespace ParallelTasks
{
    public class WorkItem
    {
        // MartinG@DigitalRune: I replaced the SpinLocks in this class with normal locks. The 
        // SpinLocks could cause severe problems where threads are blocked up to several milliseconds. 
        // (This behavior was extremely hard to reproduce.)

        // In my applications I often use nested parallel for-loops and need to keep track of all 
        // replicable tasks, not just the most recent. Otherwise, threads can run out of work, 
        // although there are still replicable tasks left. I store the replicable tasks in a stack 
        // (the most recent task on top). - Not sure if this makes sense in all cases.
        private static readonly Stack<Task> replicables = new Stack<Task>();
        private static readonly object replicablesLock = new object();
        private static Task? topReplicable;

        internal static Task? Replicable
        {
            get
            {
                bool taken = false;
                try
                {
                    taken = Monitor.TryEnter(replicablesLock);
                    if (taken)
                    {
                        return topReplicable;
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    if (taken)
                        Monitor.Exit(replicablesLock);
                }
            }
            set
            {
                lock (replicablesLock)
                {
                    replicables.Push(value.Value);
                    topReplicable = value.Value;
                }
            }
        }


        internal static void SetReplicableNull(Task? task)
        {
            if (!topReplicable.HasValue)
            {
                return;
            }

            if (!task.HasValue)
            {
                // SetReplicableNull(null) can be called to clear all replicables.
                lock (replicablesLock)
                {
                    replicables.Clear();
                    topReplicable = null;
                }
            }
            else
            {
                // When called for a specific task, the task is removed from the stack if it is the 
                // top item. (If it is not the top item or we don't get the lock ignore it.)       
                bool taken = false;
                try
                {
                    taken = Monitor.TryEnter(replicablesLock);
                    if (taken)
                    {
                        if (replicables.Count > 0)
                        {
                            Task replicable = replicables.Peek();
                            if (replicable.ID == task.Value.ID && replicable.Item == task.Value.Item)
                                replicables.Pop();

                            if (replicables.Count > 0)
                                topReplicable = replicables.Peek();
                            else
                                topReplicable = null;
                        }
                    }
                }
                finally
                {
                    if (taken)
                        Monitor.Exit(replicablesLock);
                }
            }
        }

        List<Exception> exceptionBuffer;
        Hashtable<int, Exception[]> exceptions;
        ManualResetEvent resetEvent;
        IWork work;
        volatile int runCount;
        volatile int executing;
        List<Task> children;
        volatile int waitCount;

        //object executionLock = new object();
        FastResourceLock executionLock = new FastResourceLock();

        Thread ExecutingThread;

        static Pool<WorkItem> idleWorkItems = new Pool<WorkItem>();

#if WINDOWS_PHONE
        static Hashtable<Thread, Stack<Task>> runningTasks = new Hashtable<Thread, Stack<Task>>(1);
#else
        static Hashtable<Thread, Stack<Task>> runningTasks = new Hashtable<Thread, Stack<Task>>(MyEnvironment.ProcessorCount);
#endif

        public int RunCount
        {
            get { return runCount; }
        }

        public Hashtable<int, Exception[]> Exceptions
        {
            get { return exceptions; }
        }

        public IWork Work
        {
            get { return work; }
        }

        public WorkData WorkData { get; set; }

        public Action Callback { get; set; }
        public Action<WorkData> DataCallback { get; set; }
        public ConcurrentCachingList<WorkItem> CompletionCallbacks { get; set; }

        public static Task? CurrentTask
        {
            get
            {
                Stack<Task> tasks;
                if (runningTasks.TryGet(Thread.CurrentThread, out tasks))
                {
                    if (tasks.Count > 0)
                        return tasks.Peek();
                }
                return null;
            }
        }

        public WorkItem()
        {
            resetEvent = new ManualResetEvent(true);
            exceptions = new Hashtable<int, Exception[]>(1);
            children = new List<Task>();
        }

        public Task PrepareStart(IWork work, Thread thread = null)
        {
            this.work = work;

            resetEvent.Reset();
            children.Clear();
            exceptionBuffer = null;
            ExecutingThread = thread ?? Thread.CurrentThread;

            var task = new Task(this);
            var currentTask = WorkItem.CurrentTask;
            if (currentTask.HasValue && currentTask.Value.Item == this)
                throw new Exception("whadafak?");
            if (!work.Options.DetachFromParent && currentTask.HasValue)
                currentTask.Value.Item.AddChild(task);

            return task;
        }

        public bool DoWork(int expectedID)
        {
            using (executionLock.AcquireExclusiveUsing())
            {
                if (expectedID < runCount)
                    return true;
                if (work == null)
                    return false;
                if (executing == work.Options.MaximumThreads)
                    return false;
                executing++;
            }

            // associate the current task with this thread, so that Task.CurrentTask gives the correct result
            Stack<Task> tasks = null;
            if (!runningTasks.TryGet(Thread.CurrentThread, out tasks))
            {
                tasks = new Stack<Task>();
                runningTasks.Add(Thread.CurrentThread, tasks);
            }
            tasks.Push(new Task(this));

            // execute the task
            try
            {
                // Set work data to running if able
                if (WorkData != null)
                    WorkData.WorkState = ParallelTasks.WorkData.WorkStateEnum.RUNNING;
                
                work.DoWork(WorkData);

                // Set work data to succeeded if able and not failed
                if (WorkData != null && WorkData.WorkState == ParallelTasks.WorkData.WorkStateEnum.RUNNING)
                    WorkData.WorkState = ParallelTasks.WorkData.WorkStateEnum.SUCCEEDED;
            }
            catch (Exception e)
            {
                if (exceptionBuffer == null)
                {
                    var newExceptions = new List<Exception>();
                    Interlocked.CompareExchange(ref exceptionBuffer, newExceptions, null);
                }

                lock (exceptionBuffer)
                    exceptionBuffer.Add(e);
            }

            if (tasks != null)
                tasks.Pop();

            using (executionLock.AcquireExclusiveUsing())
            {
                executing--;
                if (executing == 0)
                {
                    if (exceptionBuffer != null)
					{
#if UNSHARPER
						//workaround for volatile int to const int& casting problem in c++.
						int val = runCount;
						exceptions.Add(val, exceptionBuffer.ToArray());
#else
                        exceptions.Add(runCount, exceptionBuffer.ToArray());
#endif
					}
                    // wait for all children to complete
                    foreach (var child in children)
                        child.Wait();

                    runCount++;

                    // open the reset event, so tasks waiting on this one can continue
                    resetEvent.Set();

                    // wait for waiting tasks to all exit
                    while (waitCount > 0) ;

                    if (Callback == null && DataCallback == null)
                    {
                        Requeue();
                    }
                    else
                    {
                        // if we have a callback, then queue for execution
                        CompletionCallbacks.Add(this);
                    }

                    return true;
                }
                return false;
            }

        }

        public void Requeue()
        {
            // requeue the WorkItem for reuse, but only if the runCount hasnt reached the maximum value
            // dont requeue if an exception has been caught, to stop potential memory leaks.
            if (runCount < int.MaxValue && exceptionBuffer == null)
            {
                //Still problems
                work = null;

                idleWorkItems.Return(ExecutingThread, this);
            }
        }

        public void Wait(int id)
        {
            WaitOrExecute(id);

            Exception[] e;
            if (exceptions.TryGet(id, out e))
                throw new TaskException(e);
        }

        private void WaitOrExecute(int id)
        {
            if (runCount != id)
                return;

            if (DoWork(id))
                return;

            try
            {
#pragma warning disable 420
                // Interlocked is safe to use with volatile
                Interlocked.Increment(ref waitCount);
#pragma warning restore 420

                int i = 0;
                while (runCount == id)
                {
                    if (i > 1000)
                        resetEvent.WaitOne();
                    else
                        Thread.Sleep(0);
                    i++;
                }
            }
            finally
            {
#pragma warning disable 420
                // Interlocked is safe to use with volatile
                Interlocked.Decrement(ref waitCount);
#pragma warning restore 420
            }
        }

        public void AddChild(Task item)
        {
            using (executionLock.AcquireExclusiveUsing())
            {
                children.Add(item);
            }
        }

        public static WorkItem Get(Thread thread)
        {
            return idleWorkItems.Get(thread);
        }

        public static void Clean()
        {
            replicables.Clear();
            idleWorkItems.Clean();
        }
    }
}