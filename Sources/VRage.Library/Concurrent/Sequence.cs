using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public class Sequence
    {
        private AtomicPaddedLong value = new AtomicPaddedLong(-1);
        public Sequence()
        {
        }

        /// <summary>
        /// Construct a new sequence counter that can be tracked across threads.
        /// </summary>
        /// <param name="initialValue">initial value for the counter</param>
        public Sequence(long initialValue)
        {
            value.WriteCompilerOnlyFence(initialValue);
        }


        /// <summary>
        /// Current sequence number
        /// </summary>
        public virtual long Value
        {
            get { return value.ReadFullFence(); }
            set { this.value.WriteFullFence(value); }
        }

        /// <summary>
        /// Eventually sets to the given value.
        /// </summary>
        /// <param name="value">the new value</param>
        public virtual void LazySet(long value)
        {
            this.value.LazySet(value);
        }

        /// <summary>
        /// Atomically set the value to the given updated value if the current value == the expected value.
        /// </summary>
        /// <param name="expectedSequence">the expected value for the sequence</param>
        /// <param name="nextSequence">the new value for the sequence</param>
        /// <returns>true if successful. False return indicates that the actual value was not equal to the expected value.</returns>
        public virtual bool CompareAndSet(long expectedSequence, long nextSequence)
        {
            return value.AtomicCompareExchange(nextSequence, expectedSequence);
        }

        /// <summary>
        /// Value of the <see cref="Sequence"/> as a String.
        /// </summary>
        /// <returns>String representation of the sequence.</returns>
        public override string ToString()
        {
            return value.ToString();
        }

        ///<summary>
        /// Increments the sequence and stores the result, as an atomic operation.
        ///</summary>
        ///<returns>incremented sequence</returns>
        public long IncrementAndGet()
        {
            return AddAndGet(1);
        }

        ///<summary>
        /// Increments the sequence and stores the result, as an atomic operation.
        ///</summary>
        ///<returns>incremented sequence</returns>
        public long AddAndGet(long value)
        {
            return this.value.AtomicAddAndGet(value);
        }
    }
}
