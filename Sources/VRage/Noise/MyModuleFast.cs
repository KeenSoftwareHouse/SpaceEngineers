using VRageMath;

namespace VRage.Noise
{
    /// <summary>
    /// Faster version of MyModule.
    /// This time we do not compute the gradient position directly but we're using gradient table lookup via permutation table.
    /// This leads to more 'grid' result as the local min and max (like in Value noise) are always appearing at integer points.
    /// </summary>
    public abstract class MyModuleFast : IMyModule
    {
        private int m_seed;

        private byte[]  m_perm = new byte[512];
        private float[] m_grad = new float[512];

        protected double GradCoherentNoise(double x, MyNoiseQuality quality)
        {
            int x0 = MathHelper.Floor(x);

            int X = x0 & 255;

            double ut = 0;

            switch (quality)
            {
                case MyNoiseQuality.Low:      ut = x - x0; break;
                case MyNoiseQuality.Standard: ut = MathHelper.SCurve3(x - x0); break;
                case MyNoiseQuality.High:     ut = MathHelper.SCurve5(x - x0); break;
            }

            return MathHelper.Lerp(m_grad[m_perm[X]], m_grad[m_perm[X + 1]], ut);
        }

        protected double GradCoherentNoise(double x, double y, MyNoiseQuality quality)
        {
            int x0 = MathHelper.Floor(x);
            int y0 = MathHelper.Floor(y);

            int X = x0 & 255;
            int Y = y0 & 255;

            double ut = 0;
            double vt = 0;

            switch (quality)
            {
                case MyNoiseQuality.Low:
                    ut = x - x0;
                    vt = y - y0;
                    break;
                case MyNoiseQuality.Standard:
                    ut = MathHelper.SCurve3(x - x0);
                    vt = MathHelper.SCurve3(y - y0);
                    break;
                case MyNoiseQuality.High:
                    ut = MathHelper.SCurve5(x - x0);
                    vt = MathHelper.SCurve5(y - y0);
                    break;
            }

            int A = m_perm[X]     + Y;
            int B = m_perm[X + 1] + Y;

            int AA = m_perm[A];
            int AB = m_perm[A + 1];
            int BA = m_perm[B];
            int BB = m_perm[B + 1];

            return MathHelper.Lerp(MathHelper.Lerp(m_grad[AA], m_grad[BA], ut),
                                   MathHelper.Lerp(m_grad[AB], m_grad[BB], ut), vt);
        }

        protected double GradCoherentNoise(double x, double y, double z, MyNoiseQuality quality)
        {
            int x0 = MathHelper.Floor(x);
            int y0 = MathHelper.Floor(y);
            int z0 = MathHelper.Floor(z);

            int X = x0 & 255;
            int Y = y0 & 255;
            int Z = z0 & 255;

            double ut = 0;
            double vt = 0;
            double wt = 0;

            switch (quality)
            {
                case MyNoiseQuality.Low:
                    ut = x - x0;
                    vt = y - y0;
                    wt = z - z0;
                    break;
                case MyNoiseQuality.Standard:
                    ut = MathHelper.SCurve3(x - x0);
                    vt = MathHelper.SCurve3(y - y0);
                    wt = MathHelper.SCurve3(z - z0);
                    break;
                case MyNoiseQuality.High:
                    ut = MathHelper.SCurve5(x - x0);
                    vt = MathHelper.SCurve5(y - y0);
                    wt = MathHelper.SCurve5(z - z0);
                    break;
            }

            int A = m_perm[X]     + Y;
            int B = m_perm[X + 1] + Y;

            int AA = m_perm[A    ] + Z;
            int AB = m_perm[A + 1] + Z;
            int BA = m_perm[B    ] + Z;
            int BB = m_perm[B + 1] + Z;

            return MathHelper.Lerp(MathHelper.Lerp(MathHelper.Lerp(m_grad[AA]    , m_grad[BA]    , ut),
                                                   MathHelper.Lerp(m_grad[AB]    , m_grad[BB]    , ut), vt),
                                   MathHelper.Lerp(MathHelper.Lerp(m_grad[AA + 1], m_grad[BA + 1], ut),
                                                   MathHelper.Lerp(m_grad[AB + 1], m_grad[BB + 1], ut), vt), wt);
        }

        public virtual int Seed
        {
            get { return m_seed; }
            set
            {
                m_seed = value;

                // Generate new random permutations with this seed.
                System.Random random = new System.Random(VRage.Library.Utils.MyRandom.DisableRandomSeed ? 1 : m_seed);

                for (int i = 0; i < 256; i++)
                {
                    var rnd = (byte)random.Next(255);
                    m_perm[i]       = rnd;
                    m_perm[256 + i] = rnd;

                    m_grad[i]       = -1f + 2f*(m_perm[i] / 255f);
                    m_grad[256 + i] = m_grad[i];
                }
            }
        }

        public abstract double GetValue(double x);
        public abstract double GetValue(double x, double y);
        public abstract double GetValue(double x, double y, double z);
    }
}
