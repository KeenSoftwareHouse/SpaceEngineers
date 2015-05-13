namespace VRageMath
{
    /// <summary>
    /// Defines how the value of a Curve will be determined for positions before the first point on the Curve or after the last point on the Curve.
    /// </summary>
    /// <param name="Constant">The Curve will evaluate to its first key for positions before the first point in the Curve and to the last key for positions after the last point.</param><param name="Cycle">Positions specified past the ends of the curve will wrap around to the opposite side of the Curve.</param><param name="CycleOffset">Positions specified past the ends of the curve will wrap around to the opposite side of the Curve. The value will be offset by the difference between the values of the first and last CurveKey multiplied by the number of times the position wraps around. If the position is before the first point in the Curve, the difference will be subtracted from its value; otherwise, the difference will be added.</param><param name="Linear">Linear interpolation will be performed to determine the value.</param><param name="Oscillate">Positions specified past the ends of the Curve act as an offset from the same side of the Curve toward the opposite side.</param>
    public enum CurveLoopType
    {
        Constant,
        Cycle,
        CycleOffset,
        Oscillate,
        Linear,
    }
}
