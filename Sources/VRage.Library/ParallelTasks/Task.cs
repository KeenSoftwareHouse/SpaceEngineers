using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{
    /// <summary>
    /// A struct which represents a single execution of an IWork instance.
    /// </summary>
    public struct Task
    {
        public bool valid ;
        internal WorkItem Item { get; private set; }
        internal int ID { get; private set; }

        /// <summary>
        /// Gets a value which indicates if this task has completed.
        /// </summary>
        public bool IsComplete
        {
            get { return !valid || Item.RunCount != ID; }
        }

        /// <summary>
        /// Gets an array containing any exceptions thrown by this task.
        /// </summary>
        public Exception[] Exceptions
        {
            get
            {
                if (valid && IsComplete)
                {
                    Exception[] e;
                    Item.Exceptions.TryGet(ID, out e);
                    return e;
                }
                return null;
            }
        }

        internal Task(WorkItem item)
            : this()
        {
            ID = item.RunCount;
            Item = item;
            valid = true;
        }

        /// <summary>
        /// Waits for the task to complete.
        /// </summary>
        public void Wait()
        {
            if (valid)
            {
                var currentTask = WorkItem.CurrentTask;
                if (currentTask.HasValue && currentTask.Value.Item == Item && currentTask.Value.ID == ID)
                    throw new InvalidOperationException("A task cannot wait on itself.");
                Item.Wait(ID);
            }
        }

        internal void DoWork()
        {
            if (valid)
                Item.DoWork(ID);
        }
    }
}
