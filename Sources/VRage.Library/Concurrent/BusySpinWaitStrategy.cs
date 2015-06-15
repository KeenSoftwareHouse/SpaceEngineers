using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Concurrent
{
    /// <summary>
    /// Busy Spin strategy that uses a busy spin loop for <see cref="ITaskProcessor"/>s waiting on a barrier.
    /// 
    /// This strategy will use CPU resource to avoid syscalls which can introduce latency jitter.  It is best
    /// used when threads can be bound to specific CPU cores.
    /// </summary>
    public sealed class BusySpinWaitStrategy : IWaitStrategy
    {
        /// <summary>
        /// Wait for the given sequence to be available
        /// </summary>
        /// <param name="sequence">sequence to be waited on.</param>
        /// <param name="cursor">Ring buffer cursor on which to wait.</param>
        /// <param name="dependents">dependents further back the chain that must advance first</param>
        /// <param name="barrier">barrier the <see cref="IEventProcessor"/> is waiting on.</param>
        /// <returns>the sequence that is available which may be greater than the requested sequence.</returns>
        public long WaitFor(long sequence, Sequence cursor, Sequence[] dependents, ISequenceBarrier barrier)
        {
            long availableSequence;

            if (dependents.Length == 0)
            {
                while ((availableSequence = cursor.Value) < sequence) // volatile read
                {
                    barrier.CheckAlert();
                }
            }
            else
            {
                while ((availableSequence = Util.GetMinimumSequence(dependents)) < sequence)
                {
                    barrier.CheckAlert();
                }
            }

            return availableSequence;
        }

        /// <summary>
        /// Wait for the given sequence to be available with a timeout specified.
        /// </summary>
        /// <param name="sequence">sequence to be waited on.</param>
        /// <param name="cursor">cursor on which to wait.</param>
        /// <param name="dependents">dependents further back the chain that must advance first</param>
        /// <param name="barrier">barrier the processor is waiting on.</param>
        /// <param name="timeout">timeout value to abort after.</param>
        /// <returns>the sequence that is available which may be greater than the requested sequence.</returns>
        /// <exception cref="AlertException">AlertException if the status of the Disruptor has changed.</exception>
        public long WaitFor(long sequence, Sequence cursor, Sequence[] dependents, ISequenceBarrier barrier, TimeSpan timeout)
        {
            long availableSequence;
            var sw = Stopwatch.StartNew();

            if (dependents.Length == 0)
            {
                while ((availableSequence = cursor.Value) < sequence)
                {
                    barrier.CheckAlert();

                    if (sw.Elapsed > timeout)
                    {
                        break;
                    }
                }
            }
            else
            {
                while ((availableSequence = Util.GetMinimumSequence(dependents)) < sequence)
                {
                    barrier.CheckAlert();

                    if (sw.Elapsed > timeout)
                    {
                        break;
                    }
                }
            }

            return availableSequence;
        }

        /// <summary>
        /// Signal those <see cref="ITaskProcessor"/> waiting that the cursor has advanced.
        /// </summary>
        public void SignalAllWhenBlocking()
        {
        }
    }
}
