using VRageMath;

namespace VRage.Noise.Combiners
{
    public class MyBlend : IMyModule
    {
        public IMyModule Source1 { get; set; }
        public IMyModule Source2 { get; set; }
        public IMyModule Weight  { get; set; }

        public MyBlend(IMyModule sourceModule1, IMyModule sourceModule2, IMyModule weight)
        {
            System.Diagnostics.Debug.Assert(sourceModule1 != null &&
                                            sourceModule2 != null &&
                                            weight != null);

            Source1 = sourceModule1;
            Source2 = sourceModule2;
            Weight  = weight;
        }

        public double GetValue(double x)
        {
            return MathHelper.Lerp(Source1.GetValue(x),
                                   Source2.GetValue(x),
                                   MathHelper.Saturate(Weight.GetValue(x)));
        }

        public double GetValue(double x, double y)
        {
            return MathHelper.Lerp(Source1.GetValue(x, y),
                                   Source2.GetValue(x, y),
                                   MathHelper.Saturate(Weight.GetValue(x, y)));
        }

        public double GetValue(double x, double y, double z)
        {
            return MathHelper.Lerp(Source1.GetValue(x, y, z),
                                   Source2.GetValue(x, y, z),
                                   MathHelper.Saturate(Weight.GetValue(x, y, z)));
        }
    }
}
