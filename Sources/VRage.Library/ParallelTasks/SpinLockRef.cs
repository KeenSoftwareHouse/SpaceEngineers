using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    public class SpinLockRef
    {
        public struct Token : IDisposable
        {
            SpinLockRef m_lock;

            public Token(SpinLockRef spin)
            {
                m_lock = spin;
                m_lock.Enter();
            }

            public void Dispose()
            {
                m_lock.Exit();
            }
        }

        private SpinLock m_spinLock;

        public Token Acquire()
        {
            return new Token(this);
        }

        private void Enter()
        {
            m_spinLock.Enter();
        }

        private void Exit()
        {
            m_spinLock.Exit();
        }
    }
}
