using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atomic
{
    /// <summary>
    /// A long value that may be updated atomically and is guaranteed to live on its own 
    /// cache line (to prevent false sharing). Useful for lowlevel synchonization
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 64 * 2)]
    public struct AtomicPaddedLong
    {
        private const int CacheLineSize = 64;
        [FieldOffset(CacheLineSize)]
        private long value;


        /// <summary>
        /// Create a new <see cref="AtomicPaddedLong"/> with the given initial value.
        /// </summary>
        /// <param name="value">Initial value</param>
        public AtomicPaddedLong(long value)
        {
            this.value = value;
        }

        /// <summary>
        /// Read the value without applying any fence
        /// </summary>
        /// <returns>The current value</returns>
        public long ReadUnfenced()
        {
            return value; 
        }

        /// <summary>
        /// Read the value applying acquire fence semantic
        /// </summary>
        /// <returns>The current value</returns>
        public long ReadAcquireFence()
        {
            var value = this.value;
            Thread.MemoryBarrier();
            return value;
        }

        /// <summary>
        /// Read the value applying full fence semantic
        /// </summary>
        /// <returns>The current value</returns>
        public long ReadFullFence()
        {
            Thread.MemoryBarrier();
            return value;
        }

        /// <summary>
        /// Read the value applying a compiler only fence, no CPU fence is applied
        /// </summary>
        /// <returns>The current value</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public long ReadCompilerOnlyFence()
        {
            return value;
        }

        /// <summary>
        /// Write the value applying release fence semantic
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteReleaseFence(long newValue)
        {
            Thread.MemoryBarrier();
            value = newValue;
        }

        /// <summary>
        /// Write the value applying full fence semantic
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteFullFence(long newValue)
        {
            Thread.MemoryBarrier();
            value = newValue;
        }

        /// <summary>
        /// Write the value applying a compiler fence only, no CPU fence is applied
        /// </summary>
        /// <param name="newValue">The new value</param>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void WriteCompilerOnlyFence(long newValue)
        {
            value = newValue;
        }

        /// <summary>
        /// Write without applying any fence
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteUnfenced(long newValue)
        {
            value = newValue;
        }

        /// <summary>
        /// Atomically set the value to the given updated value if the current value equals the comparand
        /// </summary>
        /// <param name="newValue">The new value</param>
        /// <param name="comparand">The comparand (expected value)</param>
        /// <returns></returns>
        public bool AtomicCompareExchange(long newValue, long comparand)
        {
            return Interlocked.CompareExchange(ref value, newValue, comparand) == comparand;
        }

        /// <summary>
        /// Atomically set the value to the given updated value
        /// </summary>
        /// <param name="newValue">The new value</param>
        /// <returns>The original value</returns>
        public long AtomicExchange(long newValue)
        {
            return Interlocked.Exchange(ref value, newValue);
        }

        /// <summary>
        /// Atomically add the given value to the current value and return the sum
        /// </summary>
        /// <param name="delta">The value to be added</param>
        /// <returns>The sum of the current value and the given value</returns>
        public long AtomicAddAndGet(long delta)
        {
            return Interlocked.Add(ref value, delta);
        }

        /// <summary>
        /// Atomically increment the current value and return the new value
        /// </summary>
        /// <returns>The incremented value.</returns>
        public long AtomicIncrementAndGet()
        {
            return Interlocked.Increment(ref value);
        }

        /// <summary>
        /// Atomically increment the current value and return the new value
        /// </summary>
        /// <returns>The decremented value.</returns>
        public long AtomicDecrementAndGet()
        {
            return Interlocked.Decrement(ref value);
        }

        /// <summary>
        /// Eventually sets to the given value.
        /// </summary>
        /// <param name="value">the new value</param>
        public void LazySet(long value)
        {
            WriteCompilerOnlyFence(value);
        }

        /// <summary>
        /// Returns the string representation of the current value.
        /// </summary>
        /// <returns>the string representation of the current value.</returns>
        public override string ToString()
        {
            var value = ReadFullFence();
            return value.ToString();
        }
    }
}
