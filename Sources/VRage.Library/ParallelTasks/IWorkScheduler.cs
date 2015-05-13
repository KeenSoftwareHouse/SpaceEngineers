using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    /// <summary>
    /// An interface defining a task scheduler.
    /// </summary>
    public interface IWorkScheduler
    {
        /// <summary>
        /// Gets a number of threads.
        /// This number must be correct, because it's used to run per-thread initialization task on all threads (by using barrier)
        /// </summary>
        int ThreadCount { get; }

        /// <summary>
        /// Schedules a task for execution.
        /// </summary>
        /// <param name="item">The task to schedule.</param>
        void Schedule(Task item);

        /// <summary>
        /// Wait for all tasks to finish the work.
        /// </summary>
        bool WaitForTasksToFinish(TimeSpan waitTimeout);
    }
}