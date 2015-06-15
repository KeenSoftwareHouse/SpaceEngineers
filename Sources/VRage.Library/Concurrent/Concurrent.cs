using ParallelTasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrent
{
    public static class Concurrent
    {
        private static readonly int MAX_NUMBER_OF_DAEMONS = 3;
        private static readonly int MAX_NUMBER_OF_WORKERS = 2; // Environment.ProcessorCount;
        private static MyConcurrentCircularQueue<IWork> queue;
        private static ISequenceBarrier sequenceBarrier;
        private static TaskScheduler scheduler;
        private static List<Thread> daemons = new List<Thread>(MAX_NUMBER_OF_DAEMONS);

        //need a better initialization strategy
        static Concurrent()
        {
            Action doNothing = () => { };
            ActionWork preallocate = new ActionWork(doNothing);
            queue = new MyConcurrentCircularQueue<IWork>(() => preallocate, 1024, new BusySpinWaitStrategy());
            sequenceBarrier = queue.NewBarrier();
            queue.SetGatingSequences(new NoOperationTaskProcessor(queue).Sequence);
            TaskScheduler newScheduler;
#if XBOX
                    newScheduler = new RoundRobinThreadAffinedTaskScheduler(4 , ProcessorAffinity);
#else
            newScheduler = new RoundRobinThreadAffinedTaskScheduler(MAX_NUMBER_OF_WORKERS); //if there are background threads I feel like the number of worker threads should be constrained
#endif
            Interlocked.CompareExchange(ref scheduler, newScheduler, null);
        }

        public static int[] ProcessorAffinity
        {
            get { return processorAffinity; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Length < 1)
                    throw new ArgumentException("The ProcessorAffinity must contain at least one value.", "value");
                if (value.Any(id => id < 0))
                    throw new ArgumentException("The processor affinity must not be negative.", "value");
#if XBOX
                if (value.Any(id => id == 0 || id == 2))
                    throw new ArgumentException("The hardware threads 0 and 2 are reserved and should not be used on Xbox 360.", "value");

                if (value.Any(id => id > 5))
                    throw new ArgumentException("Invalid value. The Xbox 360 has max. 6 hardware threads.", "value");
#endif
                processorAffinity = value;
            }
        }

        private static int[] processorAffinity = { 3, 4, 5, 1 }; //if there are background threads I feel like the number of worker threads should be constrained

        public static TaskScheduler Scheduler
        {
            get { return scheduler; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Interlocked.Exchange(ref scheduler, value);
            }
        }

        public static void StartDaemon(IWork work)
        {
            StartDaemon(work, null);
        }

        public static void StartDaemon(IWork work, Action callback)
        {
            ParallelTasks.Parallel.StartBackground(work, callback);
        }

        public static void StartDaemon(Action action)
        {
            StartDaemon(action, null);
        }

        public static void StartDaemon(Action action, Action callback)
        {
            ParallelTasks.Parallel.StartBackground(action);
        }

        public static void Start(IWork work)
        {
            Start(null, work);
        }

        public static void Start(Action callback, IWork work)
        {
            //TODO expand to batch tasks
            long claimSequence = queue.Next();

            ISequenceBarrier barrier = queue.NewBarrier();

            Action<IWork> doWork = (IWork w) => { work.DoWork(); };

            WorkProcessor<IWork> processor = new WorkProcessor<IWork>(queue, barrier, doWork, callback, NoCallbackExceptionHandler.Instance(), claimSequence);

            System.Threading.Tasks.Task.Factory.StartNew(() => processor.DoWork(), CancellationToken.None, TaskCreationOptions.None, scheduler);
        }

        public static void Start(Action action)
        {
            Start(null, action);
        }

        public static void Start(Action callback, Action action)
        {
            //TODO expand to batch tasks

            long claimSequence = queue.Next();

            ISequenceBarrier barrier = queue.NewBarrier();

            Action<IWork> doWork = (IWork w) => { action(); };

            WorkProcessor<IWork> processor = new WorkProcessor<IWork>(queue, barrier, doWork, callback, NoCallbackExceptionHandler.Instance(), claimSequence);

            System.Threading.Tasks.Task.Factory.StartNew(() => processor.DoWork(), CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
    }
}
