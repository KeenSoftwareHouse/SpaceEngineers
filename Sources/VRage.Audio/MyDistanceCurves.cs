using VRage.Audio.X3DAudio;

namespace VRage.Audio
{
    internal static class MyDistanceCurves
    {
        /// <summary>
        /// Curve y = 1 - x in [0..1]
        /// </summary>
        internal static DistanceCurve CURVE_LINEAR = new DistanceCurve
        (
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        );

        /// <summary>
        /// Curve y = 1 - x^2 in [0..1]
        /// </summary>
        internal static DistanceCurve CURVE_QUADRATIC = new DistanceCurve
        (
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 0.25f, DspSetting = 0.9375f },
            new CurvePoint() { Distance = 0.5f, DspSetting = 0.75f },
            new CurvePoint() { Distance = 0.75f, DspSetting = 0.4375f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        );

        /// <summary>
        /// Curve y = (1 - x)^2 in [0..1]
        /// </summary>
        internal static DistanceCurve CURVE_INVQUADRATIC = new DistanceCurve
        (
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 0.25f, DspSetting = 0.5625f },
            new CurvePoint() { Distance = 0.5f, DspSetting = 0.25f },
            new CurvePoint() { Distance = 0.75f, DspSetting = 0.0625f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        );

        internal static DistanceCurve CURVE_CUSTOM_1 = new DistanceCurve
        (
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 0.038462f, DspSetting = 0.979592f },
            new CurvePoint() { Distance = 0.384615f, DspSetting = 0.938776f },
            new CurvePoint() { Distance = 0.576923f, DspSetting = 0.928571f },
            new CurvePoint() { Distance = 0.769231f, DspSetting = 0.826531f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        );

        internal static DistanceCurve[] Curves = new DistanceCurve[] { CURVE_LINEAR, CURVE_QUADRATIC, CURVE_INVQUADRATIC, CURVE_CUSTOM_1 };
    }
}
