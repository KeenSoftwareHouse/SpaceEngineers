using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    /// <summary>
    /// An interface for a piece of work which can be executed by ParallelTasks.
    /// </summary>
    public interface IWork
    {
        /// <summary>
        /// Executes the work.
        /// </summary>
        void DoWork();

        /// <summary>
        /// Gets options specifying how this work may be executed.
        /// </summary>
        WorkOptions Options { get; }
    }
}
