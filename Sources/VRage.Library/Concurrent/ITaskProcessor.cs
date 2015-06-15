using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{

    /// <summary>
    /// <see cref="ITaskProcessor"/> waitFor events to become available for consumption from the <see cref="MyConcurrentCircularQueue{T}"/>
    /// </summary>
    public interface ITaskProcessor
    {
        /// <summary>
        /// Return a reference to the <see cref="Sequence"/> being used by this <see cref="ITaskProcessor"/>
        /// </summary>
        Sequence Sequence { get; }

        /// <summary>
        /// Signal that this <see cref="ITaskProcessor"/> should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="ISequenceBarrier.Alert"/> to notify the thread to check status.
        /// </summary>
        void Halt();

        /// <summary>
        /// Starts this instance 
        /// </summary>
        void DoWork();
    }
}
