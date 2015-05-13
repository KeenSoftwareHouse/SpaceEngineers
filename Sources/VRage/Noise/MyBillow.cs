using VRageMath;

namespace VRage.Noise
{
    public class MyBillow : MyModule
    {
        public MyNoiseQuality Quality { get; set; }

        public int LayerCount { get; set; }
        public int Seed       { get; set; }

        public double Frequency   { get; set; }
        public double Lacunarity  { get; set; }
        public double Persistence { get; set; }

        public MyBillow(MyNoiseQuality quality     = MyNoiseQuality.Standard,
                        int            layerCount  = 6,
                        int            seed        = 0,
                        double         frequency   = 1.0,
                        double         lacunarity  = 2.0,
                        double         persistence = 0.5)
        {
            Quality     = quality;
            LayerCount  = layerCount;
            Seed        = seed;
            Frequency   = frequency;
            Lacunarity  = lacunarity;
            Persistence = persistence;
        }

        public override double GetValue(double x)
        {
            double value   = 0.0;
            double signal  = 0.0;
            double persist = 1.0;
            long seed;

            x *= Frequency;

            for (int i = 0; i < LayerCount; ++i)
            {
                seed = (Seed + i) & 0xFFFFFFFF;
                signal = GradCoherentNoise(x, (int)seed, Quality);
                signal = 2.0 * System.Math.Abs(signal) - 1.0;
                value += signal * persist;

                x *= Lacunarity;
                persist *= Persistence;
            }
            return MathHelper.Clamp(value + 0.5, -1.0, 1.0);
        }

        public override double GetValue(double x, double y)
        {
            double value   = 0.0;
            double signal  = 0.0;
            double persist = 1.0;
            long seed;

            x *= Frequency;
            y *= Frequency;

            for (int i = 0; i < LayerCount; ++i)
            {
                seed   = (Seed + i) & 0xFFFFFFFF;
                signal = GradCoherentNoise(x, y, (int)seed, Quality);
                signal = 2.0*System.Math.Abs(signal) - 1.0;
                value += signal * persist;

                x *= Lacunarity;
                y *= Lacunarity;
                persist *= Persistence;
            }
            return MathHelper.Clamp(value + 0.5, -1.0, 1.0);
        }

        public override double GetValue(double x, double y, double z)
        {
            double value   = 0.0;
            double signal  = 0.0;
            double persist = 1.0;
            long seed;

            x *= Frequency;
            y *= Frequency;
            z *= Frequency;

            for (int i = 0; i < LayerCount; ++i)
            {
                seed   = (Seed + i) & 0xFFFFFFFF;
                signal = GradCoherentNoise(x, y, z, (int)seed, Quality);
                signal = 2.0*System.Math.Abs(signal) - 1.0;
                value += signal * persist;

                x *= Lacunarity;
                y *= Lacunarity;
                z *= Lacunarity;
                persist *= Persistence;
            }
            return MathHelper.Clamp(value + 0.5, -1.0, 1.0);
        }
    }
}
