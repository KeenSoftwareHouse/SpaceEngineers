using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public class WorkProcessor<T> : ITaskProcessor where T : class
    {
        private readonly Sequence workSequence;
        private readonly Sequence sequence = new Sequence(-1);
        private MyConcurrentCircularQueue<T> queue;
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly Action<T> workHandler;
        private readonly Action callback;
        private AtomicBoolean isRunning = new AtomicBoolean(false);
        private IExceptionHandler exceptionHandler;

        public WorkProcessor(MyConcurrentCircularQueue<T> queue, ISequenceBarrier sequenceBarrier, Action<T> workHandler, Action callback, IExceptionHandler exceptionHandler, long workSequence)
            : this(queue, sequenceBarrier, workHandler, callback, exceptionHandler, new Sequence(workSequence))
        {
        }

        public WorkProcessor(MyConcurrentCircularQueue<T> queue, ISequenceBarrier sequenceBarrier, Action<T> workHandler, Action callback, IExceptionHandler exceptionHandler, Sequence workSequence)
        {
            this.queue = queue;
            this.sequenceBarrier = sequenceBarrier;
            this.workHandler = workHandler;
            this.exceptionHandler = exceptionHandler;
            this.workSequence = workSequence;
            this.callback = callback;
        }

        public void Halt()
        {
            isRunning.WriteFullFence(false);
            sequenceBarrier.Alert();
        }

        public Sequence Sequence
        {
            get { return sequence; }
        }

        public void DoWork()
        {
            if (!isRunning.AtomicCompareExchange(true, false))
            {
                throw new InvalidOperationException("Thread is already running");
            }
            sequenceBarrier.ClearAlert();
            var processedSequence = true;
            long nextSequence = sequence.Value;
            T workRef = null;
            while (true)
            {
                try
                {
                    if (processedSequence)
                    {
                        processedSequence = false;
                        nextSequence = workSequence.IncrementAndGet();
                        sequence.Value = nextSequence - 1L;
                    }

                    sequenceBarrier.WaitFor(nextSequence);

                    workRef = queue[nextSequence];
                    workHandler(workRef);

                    if (callback != null)
                    {
                        callback();
                    }

                    processedSequence = true;
                }
                catch (AlertException)
                {
                    if (isRunning.ReadFullFence() == false)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                    {
                        exceptionHandler.OnException(ex, nextSequence);
                    }
                    processedSequence = true;
                }
            }

            isRunning.WriteFullFence(false);
        }
    }
}
