using Atomic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrent
{
    /// <summary>
    /// This is a very finely tuned spinlock.
    /// Sleeping strategy that uses a <see cref="SpinWait"/> while the <see cref="ITaskProcessor"/>s are waiting on a barrier. 
    /// This strategy isa balance between performance and CPU resource. Latency spikes can occur after quiet periods.
    /// </summary>
    public sealed class SleepingWaitStrategy : IWaitStrategy
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
            var spinWait = default(SpinWait);

            if (dependents.Length == 0)
            {
                while ((availableSequence = cursor.Value) < sequence) // volatile read
                {
                    barrier.CheckAlert();
                    spinWait.SpinOnce();
                }
            }
            else
            {
                while ((availableSequence = Util.GetMinimumSequence(dependents)) < sequence)
                {
                    barrier.CheckAlert();
                    spinWait.SpinOnce();
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
            var spinWait = default(SpinWait);
            var sw = Stopwatch.StartNew();

            if (dependents.Length == 0)
            {
                while ((availableSequence = cursor.Value) < sequence) // volatile read
                {
                    barrier.CheckAlert();
                    spinWait.SpinOnce();

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
                    spinWait.SpinOnce();

                    if (sw.Elapsed > timeout)
                    {
                        break;
                    }
                }
            }

            return availableSequence;
        }

        /// <summary>
        /// Signal those <see cref="IEventProcessor"/> waiting that the cursor has advanced.
        /// </summary>
        public void SignalAllWhenBlocking()
        {
        }
    }
}
