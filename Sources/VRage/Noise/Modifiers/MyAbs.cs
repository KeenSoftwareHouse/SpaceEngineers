
namespace VRage.Noise.Modifiers
{
    /// <summary>
    /// Outputs the absolute value of the output value from a source module.
    /// </summary>
    public class MyAbs : IMyModule
    {
        public IMyModule Module { get; set; }

        public MyAbs(IMyModule module)
        {
            System.Diagnostics.Debug.Assert(module != null);

            Module = module;
        }

        public double GetValue(double x)
        {
            return System.Math.Abs(Module.GetValue(x));
        }

        public double GetValue(double x, double y)
        {
            return System.Math.Abs(Module.GetValue(x, y));
        }

        public double GetValue(double x, double y, double z)
        {
            return System.Math.Abs(Module.GetValue(x, y, z));
        }
    }
}
