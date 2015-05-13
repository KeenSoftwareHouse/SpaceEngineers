using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{
    /// <summary>
    /// A struct which implements a spin lock.
    /// </summary>
    public struct SpinLock
    {
        private Thread owner;
        private int recursion;

        /// <summary>
        /// Enters the lock. The calling thread will spin wait until it gains ownership of the lock.
        /// </summary>
        public void Enter()
        {
            // get the current thread
            var caller = Thread.CurrentThread;

            // early out: return if the current thread already has ownership.
            if (owner == caller)
            {
                Interlocked.Increment(ref recursion);
                return;
            }

            // only set the owner to this thread if the current owner is null. keep trying.
            while (Interlocked.CompareExchange(ref owner, caller, null) != null) ;
            Interlocked.Increment(ref recursion);
        }

        /// <summary>
        /// Tries to enter the lock.
        /// </summary>
        /// <returns><c>true</c> if the lock was successfully taken; else <c>false</c>.</returns>
        public bool TryEnter()
        {
            // get the current thead
            var caller = Thread.CurrentThread;

            // early out: return if the current thread already has ownership.
            if (owner == caller)
            {
                Interlocked.Increment(ref recursion);
                return true;
            }

            // try to take the lock, if the current owner is null.
            bool success = Interlocked.CompareExchange(ref owner, caller, null) == null;
            if (success)
                Interlocked.Increment(ref recursion);
            return success;
        }

        /// <summary>
        /// Exits the lock. This allows other threads to take ownership of the lock.
        /// </summary>
        public void Exit()
        {
            // get the current thread.
            var caller = Thread.CurrentThread;

            if (caller == owner)
            {
                Interlocked.Decrement(ref recursion);
                if (recursion == 0)
                    owner = null;
            }
            else
                throw new InvalidOperationException("Exit cannot be called by a thread which does not currently own the lock.");
        }
    }
}