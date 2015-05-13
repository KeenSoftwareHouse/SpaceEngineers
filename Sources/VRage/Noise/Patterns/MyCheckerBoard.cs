using VRageMath;

namespace VRage.Noise.Patterns
{
    public class MyCheckerBoard : IMyModule
    {
        public double GetValue(double x)
        {
            var xi = MathHelper.Floor(x) & 0x1;

            return (xi == 0x1) ? -1.0 : 1.0;
        }

        public double GetValue(double x, double y)
        {
            var xi = MathHelper.Floor(x) & 0x1;
            var yi = MathHelper.Floor(y) & 0x1;

            return ((xi ^ yi) == 0x1) ? -1.0 : 1.0;
        }

        public double GetValue(double x, double y, double z)
        {
            var xi = MathHelper.Floor(x) & 0x1;
            var yi = MathHelper.Floor(y) & 0x1;
            var zi = MathHelper.Floor(z) & 0x1;

            return ((xi ^ yi ^ zi) == 0x1) ? -1.0 : 1.0;
        }
    }
}
