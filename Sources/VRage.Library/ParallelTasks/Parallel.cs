using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Collections;

namespace ParallelTasks
{
    /// <summary>
    /// A static class containing factory methods for creating tasks.
    /// </summary>
    public static class Parallel
    {
        public static readonly WorkOptions DefaultOptions = new WorkOptions() { DetachFromParent = false, MaximumThreads = 1 };

        static IWorkScheduler scheduler;
        static Pool<List<Task>> taskPool = new Pool<List<Task>>();

        static readonly Dictionary<Thread, ConcurrentCachingList<WorkItem>> Buffers = new Dictionary<Thread, ConcurrentCachingList<WorkItem>>(8);

        [ThreadStatic]
        static ConcurrentCachingList<WorkItem> m_callbackBuffer;
        public static ConcurrentCachingList<WorkItem> CallbackBuffer
        {
            get
            {
                if (m_callbackBuffer == null)
                {
                    m_callbackBuffer = new ConcurrentCachingList<WorkItem>(16);
                    lock (Buffers)
                    {
                        Buffers.Add(Thread.CurrentThread, m_callbackBuffer);
                    }
                }
                return m_callbackBuffer;
            }
        }

        /// <summary>
        /// Executes all task callbacks for the thread calling this function.
        /// It is thread safe.
        /// </summary>
        public static void RunCallbacks()
        {
            CallbackBuffer.ApplyChanges();
            for (int i = 0; i < CallbackBuffer.Count; ++i)
            {
                WorkItem item = CallbackBuffer[i];

                Debug.Assert(item != null);
                if (item == null)
                    continue;

                if (item.Callback != null)
                {
                    item.Callback();
                    item.Callback = null;
                }
                if (item.DataCallback != null)
                {
                    item.DataCallback(item.WorkData);
                    item.DataCallback = null;
                }
                item.WorkData = null;
                item.Requeue();
            }

            CallbackBuffer.ClearList();
        }

        // MartinG@DigitalRune: I made the processor affinity configurable. In some cases a we want 
        // to dedicate a hardware thread to a certain service and don't want the ParallelTasks worker 
        // to run on that same hardware thread.

        /// <summary>
        /// Gets or sets the processor affinity of the worker threads.
        /// </summary>
        /// <value>
        /// The processor affinity of the worker threads. The default value is <c>{ 3, 4, 5, 1 }</c>.
        /// </value>
        /// <remarks>
        /// <para>
        /// In the .NET Compact Framework for Xbox 360 the processor affinity determines the processors 
        /// on which a thread runs. 
        /// </para>
        /// <para>
        /// <strong>Note:</strong> The processor affinity is only relevant in the .NET Compact Framework 
        /// for Xbox 360. Setting the processor affinity has no effect in Windows!
        /// </para>
        /// <para>
        /// <strong>Important:</strong> The processor affinity needs to be set before any parallel tasks
        /// are created. Changing the processor affinity afterwards has no effect.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified array is empty or contains invalid values.
        /// </exception>
        public static int[] ProcessorAffinity
        {
            get { return _processorAffinity; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Length < 1)
                    throw new ArgumentException("The Parallel.ProcessorAffinity must contain at least one value.", "value");

                if (value.Any(id => id < 0))
                    throw new ArgumentException("The processor affinity must not be negative.", "value");

#if XBOX
                if (value.Any(id => id == 0 || id == 2))
                    throw new ArgumentException("The hardware threads 0 and 2 are reserved and should not be used on Xbox 360.", "value");

                if (value.Any(id => id > 5))
                    throw new ArgumentException("Invalid value. The Xbox 360 has max. 6 hardware threads.", "value");
#endif

                _processorAffinity = value;
            }
        }
        private static int[] _processorAffinity = { 3, 4, 5, 1 };


        /// <summary>
        /// Gets or sets the work scheduler.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static IWorkScheduler Scheduler
        {
            get
            {
                if (scheduler == null)
                {
                    IWorkScheduler newScheduler = new WorkStealingScheduler();
                    Interlocked.CompareExchange(ref scheduler, newScheduler, null);
                }

                return scheduler;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Interlocked.Exchange(ref scheduler, value);
            }
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="work">The work to execute.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task StartBackground(IWork work)
        {
            return StartBackground(work, null);
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="work">The work to execute.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="work"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Invalid number of maximum threads set in <see cref="IWork.Options"/>.
        /// </exception>
        public static Task StartBackground(IWork work, Action completionCallback)
        {
            if (work == null)
                throw new ArgumentNullException("work");

            if (work.Options.MaximumThreads < 1)
                throw new ArgumentException("work.Options.MaximumThreads cannot be less than one.");
            var workItem = WorkItem.Get(Thread.CurrentThread);
            workItem.Callback = completionCallback;
            workItem.WorkData = null;
            var task = workItem.PrepareStart(work);
            BackgroundWorker.StartWork(task);
            return task;
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <returns>A task which represents one execution of the action.</returns>
        public static Task StartBackground(Action action)
        {
            return StartBackground(action, null);
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the action.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public static Task StartBackground(Action action, Action completionCallback)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var work = DelegateWork.GetInstance();
            work.Action = action;
            work.Options = DefaultOptions;
            return StartBackground(work, completionCallback);
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking work such as I/O.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <param name="workData">Data to be passed along both the work and the completion callback.</param>
        /// <returns>A task which represents one execution of the action.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public static Task StartBackground(Action<WorkData> action, Action<WorkData> completionCallback, WorkData workData)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var work = DelegateWork.GetInstance();
            work.DataAction = action;
            work.Options = DefaultOptions;

            var workItem = WorkItem.Get(Thread.CurrentThread);
            workItem.DataCallback = completionCallback;
            if (workData != null)
                workItem.WorkData = workData;
            else
                workItem.WorkData = new WorkData();

            var task = workItem.PrepareStart(work);
            BackgroundWorker.StartWork(task);
            return task;
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="work">The work to execute in parallel.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(IWork work)
        {
            return Start(work, null);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="work">The work to execute in parallel.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="work"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Invalid number of maximum threads set in <see cref="IWork.Options"/>.
        /// </exception>
        public static Task Start(IWork work, Action completionCallback)
        {
            if (work == null)
                throw new ArgumentNullException("work");

            if (work.Options.MaximumThreads < 1)
                throw new ArgumentException("work.Options.MaximumThreads cannot be less than one.");

            var workItem = WorkItem.Get(Thread.CurrentThread);
            workItem.CompletionCallbacks = CallbackBuffer;
            workItem.Callback = completionCallback;
            workItem.WorkData = null;
            var task = workItem.PrepareStart(work);
            Scheduler.Schedule(task);
            return task;
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action)
        {
            return Start(action, null);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action, Action completionCallback)
        {
            return Start(action, new WorkOptions() { MaximumThreads = 1, DetachFromParent = false, QueueFIFO = false }, completionCallback);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action, WorkOptions options)
        {
            return Start(action, options, null);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action, WorkOptions options, Action completionCallback)
        {
            if (options.MaximumThreads < 1)
                throw new ArgumentOutOfRangeException("options", "options.MaximumThreads cannot be less than 1.");
            var work = DelegateWork.GetInstance();
            work.Action = action;
            work.Options = options;
            return Start(work, completionCallback);
        }

        /// <summary>
        /// Creates and schedules a task to execute the given work with the given work data.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <param name="workData">Data to be passed along both the work and the completion callback.</param>
        /// <returns>A task which represents one execution of the action.</returns>
        public static Task Start(Action<WorkData> action, Action<WorkData> completionCallback, WorkData workData)
        {
            WorkOptions options = new WorkOptions() { MaximumThreads = 1, DetachFromParent = false, QueueFIFO = false };

            var work = DelegateWork.GetInstance();
            work.DataAction = action;
            work.Options = options;

            var workItem = WorkItem.Get(Thread.CurrentThread);
            workItem.CompletionCallbacks = CallbackBuffer;
            workItem.DataCallback = completionCallback;

            if (workData != null)
            {
                workItem.WorkData = workData;
                workData.WorkState = WorkData.WorkStateEnum.NOT_STARTED;
            }
            else
                workItem.WorkData = new WorkData();

            var task = workItem.PrepareStart(work);
            Scheduler.Schedule(task);
            return task;
        }

        /// <summary>
        /// Creates and schedules a task to execute on the given work-tracking thread.
        /// If the requested thread that does not execute completion callbacks the callback will never be called.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="workData">Data to be passed along both the work and the completion callback.</param>
        /// <param name="thread">Thread to execute the callback on. If not provided this is the calling thread.</param>
        /// <returns>A task which represents one execution of the action.</returns>
        public static Task ScheduleForThread(Action<WorkData> action, WorkData workData, Thread thread = null)
        {
            if(thread == null)
                thread = Thread.CurrentThread;

            WorkOptions options = new WorkOptions() { MaximumThreads = 1, DetachFromParent = false, QueueFIFO = false };

            var work = DelegateWork.GetInstance();
            work.Options = options;

            var workItem = WorkItem.Get(thread);
            lock (Buffers)
            {
                workItem.CompletionCallbacks = Buffers[thread];
            }
            workItem.DataCallback = action;


            if (workData != null)
                workItem.WorkData = workData;
            else
                workItem.WorkData = new WorkData();

            var task = workItem.PrepareStart(work, thread);

            CallbackBuffer.Add(workItem);
            return task;
        }


        private static void RunPerWorker(Action action, Barrier barrier)
        {
            barrier.SignalAndWait();
            action();
        }

        /// <summary>
        /// Starts same task on each worker, each worker executes this task exactly once.
        /// Good for initialization and release of per-thread resources.
        /// THIS CANNOT BE RUN FROM ANY WORKER!
        /// </summary>
        public static void StartOnEachWorker(Action action, bool waitForCompletion = true)
        {
            System.Diagnostics.Debug.Assert(!Thread.CurrentThread.Name.Contains("Parallel", StringComparison.InvariantCultureIgnoreCase), "StartOnEachWorker cannot be called from worker thread");

            Barrier barrier = new Barrier(Scheduler.ThreadCount);
            Action syncedAction = () => RunPerWorker(action, barrier);

            if (waitForCompletion)
            {
                barrier.AddParticipant();
                Task[] tasks = new Task[Scheduler.ThreadCount];
                for (int i = 0; i < Scheduler.ThreadCount; i++)
                {
                    tasks[i] = Start(syncedAction);
                }
                barrier.SignalAndWait();
                for (int i = 0; i < Scheduler.ThreadCount; i++)
                {
                    tasks[i].Wait();
                }
            }
            else
            {
                for (int i = 0; i < Scheduler.ThreadCount; i++)
                {
                    Start(syncedAction);
                }
            }
        }

        /// <summary>
        /// Creates and starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <returns>A future which represults one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function)
        {
            return Start(function, null);
        }

        /// <summary>
        /// Creates and starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A future which represults one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function, Action completionCallback)
        {
            return Start<T>(function, DefaultOptions, completionCallback);
        }

        /// <summary>
        /// Creates and starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <returns>A future which represents one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function, WorkOptions options)
        {
            return Start<T>(function, options, null);
        }

        /// <summary>
        /// Creates and starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A future which represents one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function, WorkOptions options, Action completionCallback)
        {
            if (options.MaximumThreads < 1)
                throw new ArgumentOutOfRangeException("options", "options.MaximumThreads cannot be less than 1.");
            var work = FutureWork<T>.GetInstance();
            work.Function = function;
            work.Options = options;
            var task = Start(work, completionCallback);
            return new Future<T>(task, work);
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="a">Work to execute.</param>
        /// <param name="b">Work to execute.</param>
        public static void Do(IWork a, IWork b)
        {
            Task task = Start(b);
            a.DoWork();
            task.Wait();
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="work">The work to execute.</param>
        public static void Do(params IWork[] work)
        {
            List<Task> tasks = taskPool.Get(Thread.CurrentThread);

            for (int i = 0; i < work.Length; i++)
            {
                tasks.Add(Start(work[i]));
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Wait();
            }

            tasks.Clear();
            taskPool.Return(Thread.CurrentThread, tasks);
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="action1">The work to execute.</param>
        /// <param name="action2">The work to execute.</param>
        public static void Do(Action action1, Action action2)
        {
            var work = DelegateWork.GetInstance();
            work.Action = action2;
            work.Options = DefaultOptions;
            var task = Start(work);
            action1();
            task.Wait();
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="actions">The work to execute.</param>
        public static void Do(params Action[] actions)
        {
            List<Task> tasks = taskPool.Get(Thread.CurrentThread);

            for (int i = 0; i < actions.Length; i++)
            {
                var work = DelegateWork.GetInstance();
                work.Action = actions[i];
                work.Options = DefaultOptions;
                tasks.Add(Start(work));
            }

            for (int i = 0; i < actions.Length; i++)
            {
                tasks[i].Wait();
            }

            tasks.Clear();
            taskPool.Return(Thread.CurrentThread, tasks);
        }

        /// <summary>
        /// Executes a for loop, where each iteration can potentially occur in parallel with others.
        /// </summary>
        /// <param name="startInclusive">The index (inclusive) at which to start iterating.</param>
        /// <param name="endExclusive">The index (exclusive) at which to end iterating.</param>
        /// <param name="body">The method to execute at each iteration. The current index is supplied as the parameter.</param>
        public static void For(int startInclusive, int endExclusive, Action<int> body)
        {
            For(startInclusive, endExclusive, body, 8);
        }

        /// <summary>
        /// Executes a for loop, where each iteration can potentially occur in parallel with others.
        /// </summary>
        /// <param name="startInclusive">The index (inclusive) at which to start iterating.</param>
        /// <param name="endExclusive">The index (exclusive) at which to end iterating.</param>
        /// <param name="body">The method to execute at each iteration. The current index is supplied as the parameter.</param>
        /// <param name="stride">The number of iterations that each processor takes at a time.</param>
        public static void For(int startInclusive, int endExclusive, Action<int> body, int stride)
        {
            var work = ForLoopWork.Get();
            work.Prepare(body, startInclusive, endExclusive, stride);
            var task = Start(work);
            task.Wait();
            work.Return();
        }

        /// <summary>
        /// Executes a foreach loop, where each iteration can potentially occur in parallel with others.
        /// </summary>
        /// <typeparam name="T">The type of item to iterate over.</typeparam>
        /// <param name="collection">The enumerable data source.</param>
        /// <param name="action">The method to execute at each iteration. The item to process is supplied as the parameter.</param>
        public static void ForEach<T>(IEnumerable<T> collection, Action<T> action)
        {
            var work = ForEachLoopWork<T>.Get();
            work.Prepare(action, collection.GetEnumerator());
            var task = Start(work);
            task.Wait();
            work.Return();
        }

        public static void Clean()
        {
            taskPool.Clean();
            lock (Buffers)
            {
                foreach (var b in Buffers.Values)
                {
                    b.ClearImmediate();
                }
                Buffers.Clear();
            }
            WorkItem.Clean();
        }

        /// <summary>
        /// Safe version of WaitHandle.WaitForMultiple, but create new MTA thread when called from STA thread
        /// </summary>
        /// <param name="waitHandles"></param>
        public static bool WaitForAll(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
                return WaitHandle.WaitAll(waitHandles, timeout);
            else
            {
                bool result = false;
                Thread t = new Thread(new ThreadStart(() => result = WaitHandle.WaitAll(waitHandles, timeout)));
                t.SetApartmentState(ApartmentState.MTA);
                t.Start();
                t.Join();
                return result;
            }
        }
    }
}