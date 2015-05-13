
namespace VRage.Noise.Models
{
    /// <summary>
    /// Maps the output of a module onto a cylinder.
    /// </summary>
    class MyCylinder : IMyModule
    {
        public IMyModule Module { get; set; }

        public MyCylinder(IMyModule module)
        {
            System.Diagnostics.Debug.Assert(module != null);

            Module = module;
        }

        public double GetValue(double x)
        {
            throw new System.NotImplementedException();
        }

        public double GetValue(double angle, double height)
        {
            double x = System.Math.Cos(angle);
            double y = height;
            double z = System.Math.Sin(angle);

            return Module.GetValue(x, y, z);
        }

        public double GetValue(double x, double y, double z)
        {
            throw new System.NotImplementedException();
        }
    }
}
