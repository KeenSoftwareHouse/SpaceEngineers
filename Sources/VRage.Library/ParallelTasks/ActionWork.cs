using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    public class ActionWork: IWork
    {
        public readonly Action Action;

        public ActionWork(Action action)
            :this(action, Parallel.DefaultOptions)
        {
        }

        public ActionWork(Action action, WorkOptions options)
        {
            this.Action = action;
            this.Options = options;
        }

        public void DoWork()
        {
            Action();
        }

        public WorkOptions Options
        {
            get;
            private set;
        }
    }
}
