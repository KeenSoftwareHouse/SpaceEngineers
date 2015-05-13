using VRageMath;

namespace VRage.Noise
{
    public class MyRidgedMultifractal : MyModule
    {
        private const int MAX_OCTAVES = 20;

        private double m_lacunarity;

        private double[] m_spectralWeights = new double[MAX_OCTAVES];

        private void CalculateSpectralWeights()
        {
            double h = 1.0;

            double frequency = 1.0;
            for (int i = 0; i < MAX_OCTAVES; i++)
            {
                m_spectralWeights[i] = System.Math.Pow(frequency, -h);
                frequency *= Lacunarity;
            }
        }

        public MyNoiseQuality Quality { get; set; }

        public int LayerCount { get; set; }
        public int Seed       { get; set; }

        public double Frequency { get; set; }
        public double Gain      { get; set; }
        public double Lacunarity
        {
            get { return m_lacunarity; }
            set
            {
                m_lacunarity = value;
                CalculateSpectralWeights();
            }
        }
        public double Offset    { get; set; }

        public MyRidgedMultifractal(MyNoiseQuality quality    = MyNoiseQuality.Standard,
                                    int            layerCount = 6,
                                    int            seed       = 0,
                                    double         frequency  = 1.0,
                                    double         gain       = 2.0,
                                    double         lacunarity = 2.0,
                                    double         offset     = 1.0)
        {
            Quality    = quality;
            LayerCount = layerCount;
            Seed       = seed;
            Frequency  = frequency;
            Gain       = gain;
            Lacunarity = lacunarity;
            Offset     = offset;
        }

        public override double GetValue(double x)
        {
            double value  = 0.0;
            double signal = 0.0;
            double weight = 1.0;
            long seed;

            x *= Frequency;

            for (int i = 0; i < LayerCount; ++i)
            {
                seed = (Seed + i) & 0x7FFFFFFF;
                signal = GradCoherentNoise(x, (int)seed, Quality);

                signal = System.Math.Abs(signal);
                signal = Offset - signal;

                signal *= signal;
                signal *= weight;

                weight = MathHelper.Saturate(signal * Gain);
                value += signal * m_spectralWeights[i];

                x *= Lacunarity;
            }
            return value - 1.0;
        }

        public override double GetValue(double x, double y)
        {
            double value  = 0.0;
            double signal = 0.0;
            double weight = 1.0;
            long seed;

            x *= Frequency;
            y *= Frequency;

            for (int i = 0; i < LayerCount; ++i)
            {
                seed   = (Seed + i) & 0x7FFFFFFF;
                signal = GradCoherentNoise(x, y, (int)seed, Quality);

                signal = System.Math.Abs(signal);
                signal = Offset - signal;

                signal *= signal;
                signal *= weight;

                weight = MathHelper.Saturate(signal * Gain);
                value += signal * m_spectralWeights[i];

                x *= Lacunarity;
                y *= Lacunarity;
            }
            return value - 1.0;
        }

        public override double GetValue(double x, double y, double z)
        {
            double value  = 0.0;
            double signal = 0.0;
            double weight = 1.0;
            long seed;

            x *= Frequency;
            y *= Frequency;
            z *= Frequency;

            for (int i = 0; i < LayerCount; ++i)
            {
                seed   = (Seed + i) & 0x7FFFFFFF;
                signal = GradCoherentNoise(x, y, z, (int)seed, Quality);

                // Make the ridges.
                signal = System.Math.Abs(signal);
                signal = Offset - signal;

                // Square the signal to increase the sharpness of the ridges.
                signal *= signal;

                // The weighting from the previous octave is applied to the signal.
                // Larger values have higher weights, producing sharp points along the ridges.
                signal *= weight;

                // Weight successive contributions by the previous signal.
                weight = MathHelper.Saturate(signal * Gain);

                // Add the signal to the output value.
                value += (signal * m_spectralWeights[i]);

                x *= Lacunarity;
                y *= Lacunarity;
                z *= Lacunarity;
            }
            return value*1.25 - 1.0;
        }
    }
}
