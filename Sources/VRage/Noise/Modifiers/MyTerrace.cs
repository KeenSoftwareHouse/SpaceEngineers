using System.Collections.Generic;
using VRageMath;

namespace VRage.Noise.Modifiers
{
    class MyTerrace : IMyModule
    {
        private double Terrace(double value, int countMask)
        {
            // Find the first element in the control point array that has a value
            // larger than the output value from the source module.
            int index;
            for (index = 0; index <= countMask; index++)
            {
                if (value < ControlPoints[index])
                    break;
            }

            // Find the two nearest control points so that we can map their values
            // onto a quadratic curve.
            int index0 = MathHelper.Clamp(index - 1, 0, countMask);
            int index1 = MathHelper.Clamp(index    , 0, countMask);

            // If some control points are missing (which occurs if the output value from
            // the source module is greater than the largest value or less than the
            // smallest value of the control point array), get the value of the nearest
            // control point and exit now.
            if (index0 == index1)
                return ControlPoints[index1];

            // Compute the alpha value used for linear interpolation.
            double value0 = ControlPoints[index0];
            double value1 = ControlPoints[index1];
            double alpha = (value - value0) / (value1 - value0);

            if (Invert)
            {
                alpha = 1.0 - alpha;

                var tmp = value0;
                value0 = value1;
                value1 = tmp;
            }

            // Squaring the alpha produces the terrace effect.
            alpha *= alpha;

            // Now perform the linear interpolation given the alpha value.
            return MathHelper.Lerp(value0, value1, alpha);
        }

        public IMyModule Module { get; set; }

        public List<double> ControlPoints;

        public bool Invert { get; set; }

        public MyTerrace(IMyModule module, bool invert = false)
        {
            Module = module;
            Invert = invert;

            ControlPoints = new List<double>(2);
        }

        public double GetValue(double x)
        {
            System.Diagnostics.Debug.Assert(Module == null         , "Module must be specified");
            System.Diagnostics.Debug.Assert(ControlPoints.Count < 2, "At least 2 control points must be specified.");

            return Terrace(Module.GetValue(x), ControlPoints.Count - 1);
        }

        public double GetValue(double x, double y)
        {
            System.Diagnostics.Debug.Assert(Module == null         , "Module must be specified");
            System.Diagnostics.Debug.Assert(ControlPoints.Count < 2, "At least 2 control points must be specified.");

            return Terrace(Module.GetValue(x, y), ControlPoints.Count - 1);
        }

        public double GetValue(double x, double y, double z)
        {
            System.Diagnostics.Debug.Assert(Module == null         , "Module must be specified");
            System.Diagnostics.Debug.Assert(ControlPoints.Count < 2, "At least 2 control points must be specified.");

            return Terrace(Module.GetValue(x, y, z), ControlPoints.Count - 1);
        }
    }
}
