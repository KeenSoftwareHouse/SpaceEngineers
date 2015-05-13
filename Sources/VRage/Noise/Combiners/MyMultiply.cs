
namespace VRage.Noise.Combiners
{
    public class MyMultiply : IMyModule
    {
        public IMyModule Source1 { get; set; }
        public IMyModule Source2 { get; set; }

        public MyMultiply(IMyModule sourceModule1, IMyModule sourceModule2)
        {
            System.Diagnostics.Debug.Assert(sourceModule1 != null && sourceModule2 != null);

            Source1 = sourceModule1;
            Source2 = sourceModule2;
        }

        public double GetValue(double x)
        {
            return Source1.GetValue(x) * Source2.GetValue(x);
        }

        public double GetValue(double x, double y)
        {
            return Source1.GetValue(x, y) * Source2.GetValue(x, y);
        }

        public double GetValue(double x, double y, double z)
        {
            return Source1.GetValue(x, y, z) * Source2.GetValue(x, y, z);
        }
    }
}
