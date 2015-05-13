using VRageMath;

namespace VRage.Noise.Modifiers
{
    /// <summary>
    /// Clamps the output value from a source module to a range of values.
    /// </summary>
    public class MyClamp : IMyModule
    {
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }

        public IMyModule Module { get; set; }

        public MyClamp(IMyModule module, double lowerBound = -1.0, double upperBound = 1.0)
        {
            System.Diagnostics.Debug.Assert(module != null);

            LowerBound = lowerBound;
            UpperBound = upperBound;

            Module = module;
        }

        public double GetValue(double x)
        {
            return MathHelper.Clamp(Module.GetValue(x), LowerBound, UpperBound);
        }

        public double GetValue(double x, double y)
        {
            return MathHelper.Clamp(Module.GetValue(x, y), LowerBound, UpperBound);
        }

        public double GetValue(double x, double y, double z)
        {
            return MathHelper.Clamp(Module.GetValue(x, y, z), LowerBound, UpperBound);
        }
    }
}
