using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage
{
    public static class FastResourceLockExtensions
    {
        public struct MySharedLock : IDisposable
        {
            FastResourceLock m_lockObject;

            [DebuggerStepThrough]
            public MySharedLock(FastResourceLock lockObject)
            {
                m_lockObject = lockObject;
                m_lockObject.AcquireShared();
            }

            public void Dispose()
            {
                System.Diagnostics.Debug.Assert(m_lockObject.Owned);
                m_lockObject.ReleaseShared();
            }
        }

        public struct MyExclusiveLock : IDisposable
        {
            FastResourceLock m_lockObject;

            [DebuggerStepThrough]
            public MyExclusiveLock(FastResourceLock lockObject)
            {
                m_lockObject = lockObject;
                m_lockObject.AcquireExclusive();
            }

            public void Dispose()
            {
                System.Diagnostics.Debug.Assert(m_lockObject.Owned);
                m_lockObject.ReleaseExclusive();
            }
        }

        /// <summary>
        /// Call dispose or use using block to release lock
        /// </summary>
        [DebuggerStepThrough]
        public static MySharedLock AcquireSharedUsing(this FastResourceLock lockObject)
        {
            return new MySharedLock(lockObject);
        }

        /// <summary>
        /// Call dispose or use using block to release lock
        /// </summary>
        [DebuggerStepThrough]
        public static MyExclusiveLock AcquireExclusiveUsing(this FastResourceLock lockObject)
        {
            return new MyExclusiveLock(lockObject);
        }
    }
}
