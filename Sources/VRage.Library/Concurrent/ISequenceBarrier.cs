using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public interface ISequenceBarrier
    {
        /// <summary>
        /// Wait for the given sequence to be available for consumption.
        /// </summary>
        /// <param name="sequence">sequence to wait for</param>
        /// <returns>the sequence up to which is available</returns>
        /// <exception cref="AlertException">if a status change has occurred for the Disruptor</exception>
        long WaitFor(long sequence);

        /// <summary>
        /// Wait for the given sequence to be available for consumption with a time out.
        /// </summary>
        /// <param name="sequence">sequence to wait for</param>
        /// <param name="timeout">timeout value</param>
        /// <returns>the sequence up to which is available</returns>
        /// <exception cref="AlertException">if a status change has occurred for the Disruptor</exception>
        long WaitFor(long sequence, TimeSpan timeout);

        /// <summary>
        /// Delegate a call to the <see cref="Sequencer.Cursor"/>
        /// Returns the value of the cursor for events that have been published.
        /// </summary>
        long Cursor { get; }

        /// <summary>
        /// The current alert status for the barrier.
        /// Returns true if in alert otherwise false.
        /// </summary>
        bool IsAlerted { get; }

        /// <summary>
        ///  Alert the <see cref="IEventProcessor"/> of a status change and stay in this status until cleared.
        /// </summary>
        void Alert();

        /// <summary>
        /// Clear the current alert status.
        /// </summary>
        void ClearAlert();

        /// <summary>
        /// Check if an alert has been raised and throw an <see cref="AlertException"/> if it has.
        /// </summary>
        /// <exception cref="AlertException">AlertException if alert has been raised.</exception>
        void CheckAlert();
    }
}
