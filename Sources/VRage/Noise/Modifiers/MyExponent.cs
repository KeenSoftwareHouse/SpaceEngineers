
namespace VRage.Noise.Modifiers
{
    /// <summary>
    /// Maps the output value from a source module onto an exponential curve.
    /// </summary>
    public class MyExponent : IMyModule
    {
        public double Exponent { get; set; }

        public IMyModule Module { get; set; }

        public MyExponent(IMyModule module, double exponent = 2.0)
        {
            System.Diagnostics.Debug.Assert(module != null);

            Exponent = exponent;

            Module = module;
        }

        public double GetValue(double x)
        {
            return System.Math.Pow((Module.GetValue(x) + 1.0)*0.5, Exponent)*2.0 - 1.0;
        }

        public double GetValue(double x, double y)
        {
            return System.Math.Pow((Module.GetValue(x, y) + 1.0)*0.5, Exponent)*2.0 - 1.0;
        }

        public double GetValue(double x, double y, double z)
        {
            return System.Math.Pow((Module.GetValue(x, y, z) + 1.0)*0.5, Exponent)*2.0 - 1.0;
        }
    }
}
