using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    public class ActionWork: IWork
    {
        public readonly Action<WorkData> Action;

        public ActionWork(Action<WorkData> action)
            :this(action, Parallel.DefaultOptions)
        {
        }

        public ActionWork(Action<WorkData> action, WorkOptions options)
        {
            this.Action = action;
            this.Options = options;
        }

        public void DoWork(WorkData workData = null)
        {
            Action(workData);
        }

        public WorkOptions Options
        {
            get;
            private set;
        }
    }
}
