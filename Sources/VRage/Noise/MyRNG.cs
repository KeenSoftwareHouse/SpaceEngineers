
using System.Diagnostics;
namespace VRage.Noise
{
    //!! TODO AR : measure times & values againts System.Random
    public struct MyRNG
    {
        const uint MAX_MASK = 0x7FFFFFFF;
        const float MAX_MASK_FLOAT = MAX_MASK;
        // set seed with a 31 bit integer <1, 0X7FFFFFFF>
        public uint Seed;

        public MyRNG(int seed = 1)
        {
            Seed = (uint)seed;
        }

        // provides the next pseudorandom number as an integer (31 bits)
        public uint NextInt()
        {
            return Gen();
        }

        // provides the next pseudorandom number as a float between nearly 0 and nearly 1.0.
        public float NextFloat()
        {
            return Gen() / MAX_MASK_FLOAT;
        }

        // provides the next pseudorandom number as an integer (31 bits) betweeen a given range.
        public int NextIntRange(float min, float max)
        {
            int result = (int)((min + (max - min)*NextFloat()) + 0.5f);
            Debug.Assert(min <= result && result <= max);
            return result;
        }
        
        // provides the next pseudorandom number as a float between a given range.
        public float NextFloatRange(float min, float max)
        {
            return min + ((max - min)*NextFloat());
        }
        
        // generator: new = (old * 16807) mod (2^31 - 1)
        private uint Gen()
        {
            return Seed = (Seed * 16807) & MAX_MASK;
        }
    }
}
