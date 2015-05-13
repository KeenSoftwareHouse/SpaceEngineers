using System.Collections.Generic;
using VRageMath;

namespace VRage.Noise.Modifiers
{
    public struct MyCurveControlPoint
    {
        public double Input;
        public double Output;
    }

    /// <summary>
    /// Maps the output value from a source module onto an arbitrary function curve.
    /// </summary>
    public class MyCurve : IMyModule
    {
        public IMyModule Module { get; set; }

        public List<MyCurveControlPoint> ControlPoints;

        public MyCurve(IMyModule module)
        {
            System.Diagnostics.Debug.Assert(module != null);

            Module = module;

            ControlPoints = new List<MyCurveControlPoint>(4);
        }

        public double GetValue(double x)
        {
            var value  = Module.GetValue(x);
            int mCount = ControlPoints.Count - 1;

            int index;
            for (index = 0; index <= mCount; ++index)
            {
                if (value < ControlPoints[index].Input)
                    break;
            }

            int index0 = MathHelper.Clamp(index - 2, 0, mCount);
            int index1 = MathHelper.Clamp(index - 1, 0, mCount);
            int index2 = MathHelper.Clamp(index, 0, mCount);
            int index3 = MathHelper.Clamp(index + 1, 0, mCount);

            if (index1 == index2)
                return ControlPoints[index1].Output;

            double t = (value - ControlPoints[index1].Input) / (ControlPoints[index2].Input - ControlPoints[index1].Input);

            return MathHelper.CubicInterp(ControlPoints[index0].Output,
                                          ControlPoints[index1].Output,
                                          ControlPoints[index2].Output,
                                          ControlPoints[index3].Output, t);
        }

        public double GetValue(double x, double y)
        {
            var value  = Module.GetValue(x, y);
            int mCount = ControlPoints.Count - 1;

            int index;
            for (index = 0; index <= mCount; ++index)
            {
                if (value < ControlPoints[index].Input)
                    break;
            }

            int index0 = MathHelper.Clamp(index - 2, 0, mCount);
            int index1 = MathHelper.Clamp(index - 1, 0, mCount);
            int index2 = MathHelper.Clamp(index    , 0, mCount);
            int index3 = MathHelper.Clamp(index + 1, 0, mCount);

            if (index1 == index2)
                return ControlPoints[index1].Output;

            double t = (value - ControlPoints[index1].Input) / (ControlPoints[index2].Input - ControlPoints[index1].Input);

            return MathHelper.CubicInterp(ControlPoints[index0].Output,
                                          ControlPoints[index1].Output,
                                          ControlPoints[index2].Output,
                                          ControlPoints[index3].Output, t);
        }

        public double GetValue(double x, double y, double z)
        {
            System.Diagnostics.Debug.Assert(ControlPoints.Count < 4, "At least 4 control points must be specified.");

            var value  = Module.GetValue(x, y, z);
            int mCount = ControlPoints.Count - 1;

            // Find the first element in the control point array that has an input value
            // larger than the output value from the source module.
            int index;
            for (index = 0; index <= mCount; ++index)
            {
                if (value < ControlPoints[index].Input)
                    break;
            }

            // Find the four nearest control points so that we can perform cubic interpolation.
            int index0 = MathHelper.Clamp(index - 2, 0, mCount);
            int index1 = MathHelper.Clamp(index - 1, 0, mCount);
            int index2 = MathHelper.Clamp(index    , 0, mCount);
            int index3 = MathHelper.Clamp(index + 1, 0, mCount);

            // If some control points are missing (which occurs if the value from the
            // source module is greater than the largest input value or less than the
            // smallest input value of the control point array), get the corresponding
            // output value of the nearest control point and exit now.
            if (index1 == index2)
                return ControlPoints[index1].Output;

            // Compute the alpha value used for cubic interpolation.
            double t = (value - ControlPoints[index1].Input) / (ControlPoints[index2].Input - ControlPoints[index1].Input);

            // Now perform the cubic interpolation given the alpha value.
            return MathHelper.CubicInterp(ControlPoints[index0].Output,
                                          ControlPoints[index1].Output,
                                          ControlPoints[index2].Output,
                                          ControlPoints[index3].Output, t);
        }
    }
}
