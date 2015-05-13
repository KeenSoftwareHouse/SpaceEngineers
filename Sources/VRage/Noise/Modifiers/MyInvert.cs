
namespace VRage.Noise.Modifiers
{
    /// <summary>
    /// Inverts the output value from a source module.
    /// </summary>
    public class MyInvert : IMyModule
    {
        public IMyModule Module { get; set; }

        public MyInvert(IMyModule module)
        {
            System.Diagnostics.Debug.Assert(module != null);

            Module = module;
        }

        public double GetValue(double x)
        {
            return -Module.GetValue(x);
        }

        public double GetValue(double x, double y)
        {
            return -Module.GetValue(x, y);
        }

        public double GetValue(double x, double y, double z)
        {
            return -Module.GetValue(x, y, z);
        }
    }
}
