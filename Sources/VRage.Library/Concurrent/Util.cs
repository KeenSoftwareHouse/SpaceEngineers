using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public static class Util
    {
        public static long GetMinimumSequence(Sequence[] sequences)
        {
            if (sequences.Length == 0)
            {
                return long.MaxValue;
            }

            long min = long.MaxValue;

            for (int i = 0; i < sequences.Length; i++)
            {
                Sequence sequence = sequences[i];
                min = min < sequence.Value ? min : sequence.Value;
            }

            return min;
        }

        /// <summary>
        /// Calculate the next power of 2, greater than or equal to x.
        /// </summary>
        /// <param name="x">Value to round up</param>
        /// <returns>The next power of 2 from x inclusive</returns>
        public static int CeilingNextPowerOfTwo(this int x)
        {
            var result = 2;

            while (result < x)
            {
                result <<= 1;
            }

            return result;
        }

        /// <summary>
        /// Test whether a given integer is a power of 2 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static bool IsPowerOf2(this int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }


        public static Sequence[] GetSequencesFor(params ITaskProcessor[] processors)
        {
            var sequences = new Sequence[processors.Length];
            for (int i = 0; i < sequences.Length; i++)
            {
                sequences[i] = processors[i].Sequence;
            }

            return sequences;
        }
    }
}
