using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{
    class BackgroundWorker
    {
        static Stack<BackgroundWorker> idleWorkers = new Stack<BackgroundWorker>();

        Thread thread;
        AutoResetEvent resetEvent;
        Task work;

        public BackgroundWorker()
        {
            resetEvent = new AutoResetEvent(false);
            thread = new Thread(WorkLoop);
            thread.IsBackground = true;
            thread.Start();
        }

        private void WorkLoop()
        {
            while (true)
            {
                resetEvent.WaitOne();
                work.DoWork();

                lock (idleWorkers)
                {
                    idleWorkers.Push(this);
                }
            }
        }

        private void Start(Task work)
        {
            this.work = work;
            this.resetEvent.Set();
        }

        public static void StartWork(Task work)
        {
            BackgroundWorker worker = null;
            lock (idleWorkers)
            {
                if (idleWorkers.Count > 0)
                    worker = idleWorkers.Pop();
            }

            if (worker == null)
                worker = new BackgroundWorker();

            worker.Start(work);
        }
    }
}
