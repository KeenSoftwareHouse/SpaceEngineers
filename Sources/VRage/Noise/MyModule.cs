using VRageMath;

namespace VRage.Noise
{
    /// <summary>
    /// High quality noise module that combines properties of Value noise and gradient noise.
    /// Value noise is used as input for gradient function. This leads to no artifacts or zero values at integer points.
    /// It's so called Value-Gradient noise.
    /// </summary>
    public abstract class MyModule : IMyModule
    {
        private double GradNoise(double fx, int ix, long seed)
        {
            long vectorIdx = (MyNoiseDefaults.X_NOISE_GEN*ix
                            + MyNoiseDefaults.SEED_NOISE*seed) & 0xFFFFFFFF;

            vectorIdx = ((vectorIdx >> MyNoiseDefaults.SHIFT_NOISE) ^ vectorIdx) & 0xFF;

            return MyNoiseDefaults.RandomVectors[vectorIdx] * (fx - ix);
        }

        private double GradNoise(double fx, double fy, int ix, int iy, long seed)
        {
            long vectorIdx = (MyNoiseDefaults.X_NOISE_GEN*ix
                            + MyNoiseDefaults.Y_NOISE_GEN*iy
                            + MyNoiseDefaults.SEED_NOISE*seed) & 0xFFFFFFFF;

            vectorIdx = (((vectorIdx >> MyNoiseDefaults.SHIFT_NOISE) ^ vectorIdx) & 0xFF) << 1;

            double xGrad = MyNoiseDefaults.RandomVectors[vectorIdx];
            double yGrad = MyNoiseDefaults.RandomVectors[vectorIdx + 1];

            double xt = fx - ix;
            double yt = fy - iy;

            return xGrad*xt + yGrad*yt;
        }

        private double GradNoise(double fx, double fy, double fz, int ix, int iy, int iz, long seed)
        {
            long vectorIdx = (MyNoiseDefaults.X_NOISE_GEN*ix
                            + MyNoiseDefaults.Y_NOISE_GEN*iy
                            + MyNoiseDefaults.Z_NOISE_GEN*iz
                            + MyNoiseDefaults.SEED_NOISE*seed) & 0x7FFFFFFF;

            vectorIdx = (((vectorIdx >> MyNoiseDefaults.SHIFT_NOISE) ^ vectorIdx) & 0xFF) * 3;

            double xGrad = MyNoiseDefaults.RandomVectors[vectorIdx];
            double yGrad = MyNoiseDefaults.RandomVectors[vectorIdx + 1];
            double zGrad = MyNoiseDefaults.RandomVectors[vectorIdx + 2];

            double xt = fx - ix;
            double yt = fy - iy;
            double zt = fz - iz;

            return xGrad*xt + yGrad*yt + zGrad*zt;
        }

        protected double GradCoherentNoise(double x, int seed, MyNoiseQuality quality)
        {
            int x0 = MathHelper.Floor(x);

            double xs = 0;

            switch (quality)
            {
                case MyNoiseQuality.Low:      xs = x - x0; break;
                case MyNoiseQuality.Standard: xs = MathHelper.SCurve3(x - x0); break;
                case MyNoiseQuality.High:     xs = MathHelper.SCurve5(x - x0); break;
            }

            return MathHelper.Lerp(GradNoise(x, x0, seed), GradNoise(x, x0 + 1, seed), xs);
        }

        protected double GradCoherentNoise(double x, double y, int seed, MyNoiseQuality quality)
        {
            int x0 = MathHelper.Floor(x);
            int y0 = MathHelper.Floor(y);

            int x1 = x0 + 1;
            int y1 = y0 + 1;

            double xs = 0;
            double ys = 0;

            switch (quality)
            {
                case MyNoiseQuality.Low:
                    xs = x - x0;
                    ys = y - y0;
                    break;
                case MyNoiseQuality.Standard:
                    xs = MathHelper.SCurve3(x - x0);
                    ys = MathHelper.SCurve3(y - y0);
                    break;
                case MyNoiseQuality.High:
                    xs = MathHelper.SCurve5(x - x0);
                    ys = MathHelper.SCurve5(y - y0);
                    break;
            }

            return MathHelper.Lerp(MathHelper.Lerp(GradNoise(x, y, x0, y0, seed), GradNoise(x, y, x1, y0, seed), xs),
                                   MathHelper.Lerp(GradNoise(x, y, x0, y1, seed), GradNoise(x, y, x1, y1, seed), xs), ys);
        }

        protected double GradCoherentNoise(double x, double y, double z, int seed, MyNoiseQuality quality)
        {
            int x0 = MathHelper.Floor(x);
            int y0 = MathHelper.Floor(y);
            int z0 = MathHelper.Floor(z);

            int x1 = x0 + 1;
            int y1 = y0 + 1;
            int z1 = z0 + 1;

            double xs = 0;
            double ys = 0;
            double zs = 0;

            switch (quality)
            {
                case MyNoiseQuality.Low:
                    xs = x - x0;
                    ys = y - y0;
                    zs = z - z0;
                    break;
                case MyNoiseQuality.Standard:
                    xs = MathHelper.SCurve3(x - x0);
                    ys = MathHelper.SCurve3(y - y0);
                    zs = MathHelper.SCurve3(z - z0);
                    break;
                case MyNoiseQuality.High:
                    xs = MathHelper.SCurve5(x - x0);
                    ys = MathHelper.SCurve5(y - y0);
                    zs = MathHelper.SCurve5(z - z0);
                    break;
            }

            return MathHelper.Lerp(MathHelper.Lerp(MathHelper.Lerp(GradNoise(x, y, z, x0, y0, z0, seed), GradNoise(x, y, z, x1, y0, z0, seed), xs),
                                                   MathHelper.Lerp(GradNoise(x, y, z, x0, y1, z0, seed), GradNoise(x, y, z, x1, y1, z0, seed), xs), ys),
                                   MathHelper.Lerp(MathHelper.Lerp(GradNoise(x, y, z, x0, y0, z1, seed), GradNoise(x, y, z, x1, y0, z1, seed), xs),
                                                   MathHelper.Lerp(GradNoise(x, y, z, x0, y1, z1, seed), GradNoise(x, y, z, x1, y1, z1, seed), xs), ys), zs);
        }

        public abstract double GetValue(double x);
        public abstract double GetValue(double x, double y);
        public abstract double GetValue(double x, double y, double z);
    }
}
