using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{
    /// <summary>
    /// A semaphore class.
    /// </summary>
    public class Semaphore
    {
        AutoResetEvent gate;
        int free;
        object free_lock = new object();

        /// <summary>
        /// Creates a new instance of the <see cref="Semaphore"/> class.
        /// </summary>
        /// <param name="maximumCount"></param>
        public Semaphore(int maximumCount)
        {
            free = maximumCount;
            gate = new AutoResetEvent(free > 0);
        }

        /// <summary>
        /// Blocks the calling thread until resources are made available, then consumes one resource.
        /// </summary>
        public void WaitOne()
        {
            gate.WaitOne(); //Enter and close gate

            lock (free_lock)
            {
                free--;

                if (free > 0)// If not is full
                    gate.Set(); //Open gate
            }
        }

        /// <summary>
        /// Adds one resource.
        /// </summary>
        public void Release()
        {
            lock (free_lock)
            {
                free++;
                gate.Set();//Open gate
            }
        }
    }
}
