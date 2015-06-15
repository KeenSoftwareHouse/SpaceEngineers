using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atomic
{
    /// <summary>
    /// A boolean value that may be updated atomically, useful for lowlevel synchonization
    /// </summary>
    public struct AtomicBoolean
    {
        // bool stored as an int, CAS not available on bool
        private int _value;
        private const int False = 0;
        private const int True = 1;

        /// <summary>
        /// Create a new <see cref="AtomicBoolean"/> with the given initial value.
        /// </summary>
        /// <param name="value">Initial value</param>
        public AtomicBoolean(bool value)
        {
            _value = value ? True : False;
        }

        /// <summary>
        /// Read the value without applying any fence
        /// </summary>
        /// <returns>The current value</returns>
        public bool ReadUnfenced()
        {
            return ToBool(_value);
        }

        /// <summary>
        /// Read the value applying acquire fence semantic
        /// </summary>
        /// <returns>The current value</returns>
        public bool ReadAcquireFence()
        {
            var value = ToBool(_value);
            Thread.MemoryBarrier();
            return value;
        }

        /// <summary>
        /// Read the value applying full fence semantic
        /// </summary>
        /// <returns>The current value</returns>
        public bool ReadFullFence()
        {
            var value = ToBool(_value);
            Thread.MemoryBarrier();
            return value;
        }

        /// <summary>
        /// Read the value applying a compiler only fence, no CPU fence is applied
        /// </summary>
        /// <returns>The current value</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public bool ReadCompilerOnlyFence()
        {
            return ToBool(_value);
        }

        /// <summary>
        /// Write the value applying release fence semantic
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteReleaseFence(bool newValue)
        {
            var newValueInt = ToInt(newValue);
            Thread.MemoryBarrier();
            _value = newValueInt;
        }

        /// <summary>
        /// Write the value applying full fence semantic
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteFullFence(bool newValue)
        {
            var newValueInt = ToInt(newValue);
            Thread.MemoryBarrier();
            _value = newValueInt;
        }

        /// <summary>
        /// Write the value applying a compiler fence only, no CPU fence is applied
        /// </summary>
        /// <param name="newValue">The new value</param>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void WriteCompilerOnlyFence(bool newValue)
        {
            _value = ToInt(newValue);
        }

        /// <summary>
        /// Write without applying any fence
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteUnfenced(bool newValue)
        {
            _value = ToInt(newValue);
        }

        /// <summary>
        /// Atomically set the value to the given updated value if the current value equals the comparand
        /// </summary>
        /// <param name="newValue">The new value</param>
        /// <param name="comparand">The comparand (expected value)</param>
        /// <returns></returns>
        public bool AtomicCompareExchange(bool newValue, bool comparand)
        {
            var newValueInt = ToInt(newValue);
            var comparandInt = ToInt(comparand);

            return Interlocked.CompareExchange(ref _value, newValueInt, comparandInt) == comparandInt;
        }

        /// <summary>
        /// Atomically set the value to the given updated value
        /// </summary>
        /// <param name="newValue">The new value</param>
        /// <returns>The original value</returns>
        public bool AtomicExchange(bool newValue)
        {
            var newValueInt = ToInt(newValue);
            var originalValue = Interlocked.Exchange(ref _value, newValueInt);
            return ToBool(originalValue);
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

        private static bool ToBool(int value)
        {
            if (value != False && value != True)
            {
                throw new ArgumentOutOfRangeException("value");
            }

            return value == True;
        }

        private static int ToInt(bool value)
        {
            return value ? True : False;
        }
    }
}
