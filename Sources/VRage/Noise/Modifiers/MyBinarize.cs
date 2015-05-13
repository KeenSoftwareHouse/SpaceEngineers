
namespace VRage.Common.Noise.Modifiers
{
    /// <summary>
    /// Maps the output value from a source module onto an set {0, 1} based on given threshold.
    /// </summary>
    public class MyBinarize : IMyModule
    {
        public double Threshold { get; set; }

        public IMyModule Module { get; set; }

        public MyBinarize(IMyModule module, double threshold = 0.0)
        {
            System.Diagnostics.Debug.Assert(module != null);

            Threshold = threshold;

            Module = module;
        }

        public double GetValue(double x)
        {
            return (Module.GetValue(x) > Threshold) ? 1.0 : 0.0;
        }

        public double GetValue(double x, double y)
        {
            return (Module.GetValue(x, y) > Threshold) ? 1.0 : 0.0;
        }

        public double GetValue(double x, double y, double z)
        {
            return (Module.GetValue(x, y, z) > Threshold) ? 1.0 : 0.0;
        }
    }
}
