using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public interface IWaitStrategy
    {

        /// <summary>
        /// Wait for the given sequence to be available
        /// </summary>
        /// <param name="sequence">sequence to be waited on.</param>
        /// <param name="cursor">Circular queue cursor on which to wait.</param>
        /// <param name="dependents">dependents further back the chain that must advance first</param>
        /// <param name="barrier">barrier that is waiting on.</param>
        /// <returns>the sequence that is available which may be greater than the requested sequence.</returns>
        long WaitFor(long sequence, Sequence cursor, Sequence[] dependents, ISequenceBarrier barrier);


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
        long WaitFor(long sequence, Sequence cursor, Sequence[] dependents, ISequenceBarrier barrier, TimeSpan timeout);

        /// <summary>
        /// Signal those processors waiting that the cursor has advanced.
        /// </summary>
        void SignalAllWhenBlocking();
    }
}
