using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    /// <summary>
    /// A struct containing options about how an IWork instance can be executed.
    /// </summary>
    public struct WorkOptions
    {
        /// <summary>
        /// Gets or sets a value indicating if the work will be created detached from its parent.
        /// If <c>false</c>, the parent task will wait for this work to complete before itself completing.
        /// </summary>
        public bool DetachFromParent { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of threads which can concurrently execute this work.
        /// </summary>
        public int MaximumThreads { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that this work should be queued in a first in first out fashion.
        /// </summary>
        public bool QueueFIFO { get; set; }
    }
}
