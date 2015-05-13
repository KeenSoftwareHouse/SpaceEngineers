using VRageMath;

namespace VRage.Noise
{
    // 2D noise based on Perlin with variety functions to create complex noise
    public static class MyNoise2D
    {
        private static MyRNG m_rnd = new MyRNG();

        private const int B  = 256; // 0x100 (size)
        private const int BM = 255; // 0xff  (mask)

        private static float[] rand = new float[B];    // random table
        private static int[]   perm = new int[B << 1]; // permutation table

        public static void Init(int seed)
        {
            m_rnd.Seed = (uint)seed;

            for (int i = 0; i < B; ++i)
            {
                rand[i] = m_rnd.NextFloat();
                perm[i] = i; // assign value to permutation array
            }

            for (int i = 0; i < B; ++i) // randomly swap values in permutation array
            {
                int swapIndex = (int)m_rnd.NextInt() & BM;
                int temp = perm[swapIndex];

                perm[swapIndex] = perm[i];
                perm[i] = temp;
                perm[i + B] = perm[i];
            }
        }

        public static float Noise(float x, float y)
        {
            int xi = (int)x;
            int yi = (int)y;

            float tx = x - xi;
            float ty = y - yi;

            int rx0 = BM & xi;
            int rx1 = BM & (xi + 1);
            int ry0 = BM & yi;
            int ry1 = BM & (yi + 1);

            // random values at the corners of the cell using permutation table
            float c00 = rand[perm[perm[rx0] + ry0]];
            float c10 = rand[perm[perm[rx1] + ry0]];
            float c01 = rand[perm[perm[rx0] + ry1]];
            float c11 = rand[perm[perm[rx1] + ry1]];

            return MathHelper.SmoothStep(MathHelper.SmoothStep(c00, c10, tx),
                                         MathHelper.SmoothStep(c01, c11, tx), ty);
        }

        public static float Rotation(float x, float y, int numLayers)
        {
            float[] sa = new float[numLayers];
            float[] ca = new float[numLayers];

            for (int i = 0; i < numLayers; ++i)
            {
                sa[i] = (float)System.Math.Sin(0.436332313f * i); // 30 degrees
                ca[i] = (float)System.Math.Cos(0.436332313f * i);
            }

            var res = 0f;
            var sum = 0;

            for (int p = 0; p < numLayers; ++p)
            {
                res += Noise(x*ca[p] - y*sa[p], x*sa[p] + y*ca[p]);
                sum += 1;
            }
            return res / sum;
        }

        public static float Fractal(float x, float y, int numOctaves)
        {
            var freq = 1;
            var ampl = 1f;
            var sum  = 0f;
            var res  = 0f;

            for (int i = 0; i < numOctaves; ++i)
            {
                sum += ampl;
                res += Noise(x*freq, y*freq) * ampl;

                ampl  *= 0.5f;
                freq <<= 1; // double the frequency
            }
            return res / sum;
        }

        public static float FBM(float x, float y, int numLayers, float lacunarity, float gain)
        {
            var freq = 1f;
            var ampl = 1f;
            var sum  = 0f;
            var res  = 0f;

            for (int i = 0; i < numLayers; ++i)
            {
                sum += ampl;
                res += Noise(x*freq, y*freq) * ampl;

                ampl *= gain;
                freq *= lacunarity;
            }
            return res / sum;
        }

        public static float Billow(float x, float y, int numLayers)
        {
            var freq = 1;
            var ampl = 1f;
            var sum  = 0f;
            var res  = 0f;

            for (int p = 0; p < numLayers; ++p)
            {
                sum += ampl;
                res += System.Math.Abs(2*Noise(x*freq, y*freq) - 1) * ampl;

                ampl  *= 0.5f;
                freq <<= 1; // double the frequency
            }
            return res / sum;
        }

        public static float Marble(float x, float y, int numOctaves)
        {
            return ((float)System.Math.Sin(4*(x + Fractal(x*0.5f, y*0.5f, numOctaves))) + 1f) * 0.5f;
        }

        public static float Wood(float x, float y, float scale)
        {
            var s = Noise(x, y) * scale;
            return s - (int)s;
        }
    }
}
