using VRageMath;

namespace VRage.Noise.Patterns
{
    /// <summary>
    /// Noise that outputs concentric cylinders.
    /// Each cylinder extends infinitely along the y axis.
    /// </summary>
    class MyCylinders : IMyModule
    {
        public double Frequency { get; set; }

        public MyCylinders(double frequnecy = 1.0)
        {
            Frequency = frequnecy;
        }

        public double GetValue(double x)
        {
            x *= Frequency;

            var dstFromCenter  = System.Math.Sqrt(x*x + x*x);
            var dstFromCenteri = MathHelper.Floor(dstFromCenter);

            double dstFromSmall = dstFromCenter - dstFromCenteri;
            double dstFromLarge = 1.0 - dstFromSmall;
            double nearestDst   = System.Math.Min(dstFromSmall, dstFromLarge);

            return 1.0 - nearestDst*4.0;
        }

        public double GetValue(double x, double z)
        {
            x *= Frequency;
            z *= Frequency;

            var dstFromCenter  = System.Math.Sqrt(x*x + z*z);
            var dstFromCenteri = MathHelper.Floor(dstFromCenter);

            double dstSmallSphere = dstFromCenter - dstFromCenteri;
            double dstLargeSphere = 1.0 - dstSmallSphere;
            double nearestDst     = System.Math.Min(dstSmallSphere, dstLargeSphere);

            return 1.0 - nearestDst*4.0;
        }

        public double GetValue(double x, double y, double z)
        {
            throw new System.NotImplementedException();
        }
    }
}
