using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParallelTasks;


namespace ParallelTasks
{
    /// <summary>
    /// These values are indices into array of queues which is searched starting at 0.
    /// </summary>
    public enum WorkPriority
    {
        VeryHigh,
        High,
        Normal,
        Low,
        VeryLow,
    }

    public interface IPrioritizedWork : IWork
    {
        WorkPriority Priority { get; }
    }
}
