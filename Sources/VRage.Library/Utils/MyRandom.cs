using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Library.Utils
{
    /// <summary>
    /// Original C# implementation which allows settings the seed.
    /// </summary>
    [Serializable]
    public class MyRandom
    {
        [ThreadStatic]
        private static MyRandom m_instance;

        public unsafe struct State
        {
            public int Inext;
            public int Inextp;
            public fixed int Seed[0x38];
        }

        const int PREDEFINED_SIZE = 1024;
        int[] m_predefined = null;


#if !UNSHARPER

		public struct StateToken : IDisposable
        {
            MyRandom m_random;
            State m_state;

            public StateToken(MyRandom random)
            {
                m_random = random;
                random.GetState(out m_state);
            }

            public StateToken(MyRandom random, int newSeed)
            {
                m_random = random;
                random.GetState(out m_state);
                random.SetSeed(newSeed);
            }

            public void Dispose()
            {
                // This way we allow token which does nothing, it's useful for situations when you want to push conditionally
                if(m_random != null)
                    m_random.SetState(ref m_state);
            }
        }

#endif

		public static MyRandom Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new MyRandom();
                return m_instance;
            }
        }

        // Fields
        private int inext;
        private int inextp;
        private const int MBIG = 0x7fffffff;
        private const int MSEED = 0x9a4ec86;
        private const int MZ = 0;
        private int[] SeedArray;

        private byte[] m_tmpLongArray = new byte[8];

        //GR: Used only for testing
        internal static bool DisableRandomSeed = false;

        // Methods
        public MyRandom()
#if XB1
            : this(MyEnvironment.TickCount)
#else
            : this(MyEnvironment.TickCount + System.Threading.Thread.CurrentThread.ManagedThreadId)
#endif
        {
        }

        public MyRandom(int Seed)
        {
            this.SeedArray = new int[0x38];
            SetSeed(Seed);

            if (m_predefined == null)
            {
                m_predefined = new int[PREDEFINED_SIZE];
                using (PushSeed(12345))
                {
                    for (int i = 0; i < PREDEFINED_SIZE; i++)
                    {
                        m_predefined[i] = InternalSample();
                    }
                }
            }
        }

#if !UNSHARPER

        public StateToken PushSeed(int newSeed)
        {
            return new StateToken(this, newSeed);
        }

		public unsafe void GetState(out State state)
        {
            state.Inext = inext;
            state.Inextp = inextp;
            fixed (int* ptr = state.Seed)
            {
#if !XB1
                Marshal.Copy(SeedArray, 0, new IntPtr(ptr), 0x38);
#else // XB1
                for (int i = 0; i < SeedArray.Length; i++)
                {
                    ptr[i] = SeedArray[i];
                }
#endif // !XB1
            }
        }

        public unsafe void SetState(ref State state)
        {
            inext = state.Inext;
            inextp = state.Inextp;
            fixed (int* ptr = state.Seed)
            {
#if !XB1
                Marshal.Copy(new IntPtr(ptr), SeedArray, 0, 0x38);
#else // XB1
                for (int i = 0; i < SeedArray.Length; i++)
                {
                    SeedArray[i] = ptr[i];
                }
#endif // XB1
            }
        }

#endif

        public int CreateRandomSeed()
        {
            return MyEnvironment.TickCount ^ Next();
        }

        /// <summary>
        /// Sets new seed, only use this method when you have separate instance of MyRandom.
        /// Setting seed for RNG used for EntityId without reverting to previous state is dangerous.
        /// Use PushSeed for EntityId random generator.
        /// </summary>
        public void SetSeed(int Seed)
        {
            if (DisableRandomSeed)
            {
                Seed = 1;
            }
            int num4 = (Seed == -2147483648) ? 0x7fffffff : Math.Abs(Seed);
            int num2 = 0x9a4ec86 - num4;
            this.SeedArray[0x37] = num2;
            int num3 = 1;
            for (int i = 1; i < 0x37; i++)
            {
                int index = (0x15 * i) % 0x37;
                this.SeedArray[index] = num3;
                num3 = num2 - num3;
                if (num3 < 0)
                {
                    num3 += 0x7fffffff;
                }
                num2 = this.SeedArray[index];
            }
            for (int j = 1; j < 5; j++)
            {
                for (int k = 1; k < 0x38; k++)
                {
                    this.SeedArray[k] -= this.SeedArray[1 + ((k + 30) % 0x37)];
                    if (this.SeedArray[k] < 0)
                    {
                        this.SeedArray[k] += 0x7fffffff;
                    }
                }
            }
            this.inext = 0;
            this.inextp = 0x15;
            Seed = 1;
        }

        private double GetSampleForLargeRange()
        {
            int num = this.InternalSample();
            if ((this.InternalSample() % 2) == 0)
            {
                num = -num;
            }
            double num2 = num;
            num2 += 2147483646.0;
            return (num2 / 4294967293);
        }

        private double GetSampleForLargeRange(int hash)
        {
            int num = this.InternalSample(hash);
            if ((this.InternalSample(hash) % 2) == 0)
            {
                num = -num;
            }
            double num2 = num;
            num2 += 2147483646.0;
            return (num2 / 4294967293);
        }

        private int InternalSample()
        {
            int inext = this.inext;
            int inextp = this.inextp;
            if (++inext >= 0x38)
            {
                inext = 1;
            }
            if (++inextp >= 0x38)
            {
                inextp = 1;
            }
            int num = this.SeedArray[inext] - this.SeedArray[inextp];
            if (num == 0x7fffffff)
            {
                num--;
            }
            if (num < 0)
            {
                num += 0x7fffffff;
            }
            this.SeedArray[inext] = num;
            this.inext = inext;
            this.inextp = inextp;
            return num;
        }

        private int InternalSample(int hash)
        {
            // (x % m + m) % m;
            return m_predefined[(hash % PREDEFINED_SIZE + PREDEFINED_SIZE) % PREDEFINED_SIZE];
        }


        public int Next()
        {
            return this.InternalSample();
        }

        public int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException("maxValue");
            }
            return (int)(this.Sample() * maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue");
            }
            long num = maxValue - minValue;
            if (num <= 0x7fffffffL)
            {
                return (((int)(this.Sample() * num)) + minValue);
            }
            return (((int)((long)(this.GetSampleForLargeRange() * num))) + minValue);
        }

        public int Next(int hash, int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue");
            }
            long num = maxValue - minValue;
            if (num <= 0x7fffffffL)
            {
                return (((int)(this.Sample(hash) * num)) + minValue);
            }
            return (((int)((long)(this.GetSampleForLargeRange(hash) * num))) + minValue);
        }

        public long NextLong()
        {
            NextBytes(m_tmpLongArray);
            return BitConverter.ToInt64(m_tmpLongArray, 0);
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(this.InternalSample() % 0x100);
            }
        }

        /// Returns random number between 0 and 1.
        public float NextFloat()
        {
            return (float)NextDouble();
        }

        /// Returns random number between 0 and 1.
        public float NextFloat(int hash)
        {
            return (float)NextDouble(hash);
        }

        /// Returns random number between 0 and 1.
        public double NextDouble()
        {
            return this.Sample();
        }
        
        /// Returns random number between 0 and 1.
        public double NextDouble(int hash)
        {
            return this.Sample(hash);
        }

        protected double Sample()
        {
            return (this.InternalSample() * 4.6566128752457969E-10);
        }

        protected double Sample(int hash)
        {
            return (this.InternalSample(hash) * 4.6566128752457969E-10);
        }
    }
}
