using VRageMath;

namespace VRage.Noise.Patterns
{
    /// <summary>
    /// Noise that outputs concentric spheres.
    /// </summary>
    class MySpheres : IMyModule
    {
        public double Frequency { get; set; }

        public MySpheres(double frequnecy = 1.0)
        {
            Frequency = frequnecy;
        }

        public double GetValue(double x)
        {
            x *= Frequency;

            int xi = MathHelper.Floor(x);

            double dstFromCenter = System.Math.Sqrt(x*x + x*x);
            double dstFromSmall  = dstFromCenter - xi;
            double dstFromLarge  = 1.0 - dstFromSmall;
            double nearest       = System.Math.Min(dstFromSmall, dstFromLarge);

            return 1.0 - nearest*4.0;
        }

        public double GetValue(double x, double y)
        {
            x *= Frequency;
            y *= Frequency;

            int xi = MathHelper.Floor(x);

            double dstFromCenter = System.Math.Sqrt(x*x + y*y);
            double dstFromSmall  = dstFromCenter - xi;
            double dstFromLarge  = 1.0 - dstFromSmall;
            double nearest       = System.Math.Min(dstFromSmall, dstFromLarge);

            return 1.0 - nearest*4.0;
        }

        public double GetValue(double x, double y, double z)
        {
            throw new System.NotImplementedException();
        }
    }
}
