using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public class WorkerPool<T> where T : class
    {
        private AtomicBoolean isRunning = new AtomicBoolean(false);
        private readonly Sequence workSequence = new Sequence(-1);
        private readonly MyConcurrentCircularQueue<T> queue;
        private readonly WorkProcessor<T>[] workProcessors;

        public WorkerPool(MyConcurrentCircularQueue<T> queue,
            ISequenceBarrier sequenceBarrier,
            IExceptionHandler exceptionHandler,
            params Action<T>[] workHandlers)
        {
            this.queue = queue;
            int numWorkers = workHandlers.Length;
            workProcessors = new WorkProcessor<T>[numWorkers];

            for (int i = 0; i < numWorkers; i++)
            {
                workProcessors[i] = new WorkProcessor<T>(queue,
                                                         sequenceBarrier,
                                                         workHandlers[i],
                                                         null,
                                                         exceptionHandler,
                                                         workSequence);
            }
        }

        public WorkerPool(Func<T> taskFactory, 
            IWaitStrategy waitStrategy, 
            IExceptionHandler exceptionHandler,
            params Action<T>[] workHandlers)
        {
            queue = new MyConcurrentCircularQueue<T>(taskFactory, waitStrategy);
            var barrier = queue.NewBarrier();

            int numWorkers = workHandlers.Length;
            workProcessors = new WorkProcessor<T>[numWorkers];

            for (int i = 0; i < numWorkers; i++)
            {
                workProcessors[i] = new  WorkProcessor<T>(queue,
                                                         barrier,
                                                         workHandlers[i],
                                                         null,
                                                         exceptionHandler,
                                                         workSequence);
            }

            queue.SetGatingSequences(WorkerSequences);
        }

        public Sequence[] WorkerSequences
        {
            get
            {
                var sequences = new Sequence[workProcessors.Length];
                for (int i = 0; i < workProcessors.Length; i++)
                {
                    sequences[i] = workProcessors[i].Sequence;
                }

                return sequences;
            }
        }
    }
}
