
namespace VRage.Noise.Combiners
{
    public class MyPower : IMyModule
    {
        public IMyModule Base  { get; set; }
        public IMyModule Power { get; set; }
        private double powerOffset;

        public MyPower(IMyModule baseModule, IMyModule powerModule, double powerOffset = 0.0)
        {
            System.Diagnostics.Debug.Assert(baseModule != null && powerModule != null);

            Base  = baseModule;
            Power = powerModule;
            this.powerOffset = powerOffset;
        }

        public double GetValue(double x)
        {
            return System.Math.Pow(Base.GetValue(x), powerOffset + Power.GetValue(x));
        }

        public double GetValue(double x, double y)
        {
            return System.Math.Pow(Base.GetValue(x, y), powerOffset + Power.GetValue(x, y));
        }

        public double GetValue(double x, double y, double z)
        {
            return System.Math.Pow(Base.GetValue(x, y, z), powerOffset + Power.GetValue(x, y, z));
        }
    }
}
