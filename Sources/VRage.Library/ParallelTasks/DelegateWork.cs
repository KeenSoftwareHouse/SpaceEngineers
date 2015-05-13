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
        static MyConcurrentPool<DelegateWork> instances = new MyConcurrentPool<DelegateWork>();

        public Action Action { get; set; }
        public WorkOptions Options { get; set; }

        public DelegateWork()
        {
        }

        public void DoWork()
        {
            Action();
            Action = null;
            instances.Return(this);
        }

        internal static DelegateWork GetInstance()
        {
            return instances.Get();
        }
    }
}
