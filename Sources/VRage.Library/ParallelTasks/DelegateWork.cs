using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Collections;

namespace ParallelTasks
{
    class DelegateWork: IWork
    {
        static MyConcurrentPool<DelegateWork> instances = new MyConcurrentPool<DelegateWork>(0,false);

        public Action Action { get; set; }
        public Action<WorkData> DataAction { get; set; }
        public WorkOptions Options { get; set; }

        public DelegateWork()
        {
        }

        public void DoWork(WorkData workData = null)
        {
            if (Action != null)
            {
                Action();
                Action = null;
            }
            if (DataAction != null)
            {
                DataAction(workData);
                DataAction = null;
            }
            instances.Return(this);
        }

        internal static DelegateWork GetInstance()
        {
            return instances.Get();
        }
    }
}
