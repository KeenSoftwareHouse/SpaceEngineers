using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sandbox.AI
{
    internal class MyAIThread
    {
        private Thread m_thread;
        private MyBotCollection m_bots;

        // Bool reads and writes are thread-safe
        private bool m_shouldRun;
        public bool ShouldRun
        {
            get { return m_shouldRun; }
            set { m_shouldRun = value; }
        }

        internal MyAIThread(MyBotCollection bots)
        {
            m_bots = bots;
            m_thread = new Thread(ThreadStart);
            m_shouldRun = true;
        }

        internal void Start()
        {
            m_thread.Start();
        }

        private void ThreadStart()
        {
            while (ShouldRun)
            {
                m_bots.Update();
            }
        }

        internal void StopAndJoin()
        {
            ShouldRun = false;
            m_thread.Join();
        }
    }
}
