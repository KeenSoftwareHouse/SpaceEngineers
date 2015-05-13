
namespace VRage.Noise
{
    public class MyPerlin : MyModule
    {
        public MyNoiseQuality Quality { get; set; }

        public int OctaveCount { get; set; }
        public int Seed        { get; set; }

        public double Frequency   { get; set; }
        public double Lacunarity  { get; set; }
        public double Persistence { get; set; }

        public MyPerlin(MyNoiseQuality quality     = MyNoiseQuality.Standard,
                        int            octaveCount = 6,
                        int            seed        = 0,
                        double         frequency   = 1.0,
                        double         lacunarity  = 2.0,
                        double         persistence = 0.5)
        {
            Quality     = quality;
            OctaveCount = octaveCount;
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

            for (int i = 0; i < OctaveCount; ++i)
            {
                seed = (Seed + i) & 0xFFFFFFFF;
                signal = GradCoherentNoise(x, (int)seed, Quality);
                value += signal * persist;

                x       *= Lacunarity;
                persist *= Persistence;
            }
            return value;
        }

        public override double GetValue(double x, double y)
        {
            double value   = 0.0;
            double signal  = 0.0;
            double persist = 1.0;
            long seed;

            x *= Frequency;
            y *= Frequency;

            for (int i = 0; i < OctaveCount; ++i)
            {
                seed   = (Seed + i) & 0xFFFFFFFF;
                signal = GradCoherentNoise(x, y, (int)seed, Quality);
                value += signal * persist;

                x *= Lacunarity;
                y *= Lacunarity;
                persist *= Persistence;
            }
            return value;
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

            for (int i = 0; i < OctaveCount; ++i)
            {
                seed   = (Seed + i) & 0xFFFFFFFF;
                signal = GradCoherentNoise(x, y, z, (int)seed, Quality);
                value += signal * persist;

                x *= Lacunarity;
                y *= Lacunarity;
                z *= Lacunarity;
                persist *= Persistence;
            }
            return value;
        }
    }
}
