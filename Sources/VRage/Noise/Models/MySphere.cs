
namespace VRage.Noise.Models
{
    /// <summary>
    /// Maps the output of a module onto a sphere.
    /// </summary>
    class MySphere : IMyModule
    {
        protected void LatLonToXYZ(double lat, double lon, out double x, out double y, out double z)
        {
            const double DEG_TO_RAD = System.Math.PI / 180.0;

            var r = System.Math.Cos(DEG_TO_RAD*lat);

            x = System.Math.Cos(DEG_TO_RAD*lon)*r;
            y = System.Math.Sin(DEG_TO_RAD*lat);
            z = System.Math.Sin(DEG_TO_RAD*lon)*r;
        }

        public IMyModule Module { get; set; }

        public MySphere(IMyModule module)
        {
            System.Diagnostics.Debug.Assert(module != null);

            Module = module;
        }

        public double GetValue(double x)
        {
            throw new System.NotImplementedException();
        }

        public double GetValue(double latitude, double longitude)
        {
            double x, y, z;

            LatLonToXYZ(latitude, longitude, out x, out y, out z);

            return Module.GetValue(x, y, z);
        }

        public double GetValue(double x, double y, double z)
        {
            throw new System.NotImplementedException();
        }
    }
}
