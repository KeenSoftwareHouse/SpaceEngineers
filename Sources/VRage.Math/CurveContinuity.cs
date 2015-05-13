namespace VRageMath
{
    /// <summary>
    /// Defines the continuity of CurveKeys on a Curve.
    /// </summary>
    /// <param name="Smooth">Interpolation can be used between this CurveKey and the next.</param><param name="Step">Interpolation cannot be used between this CurveKey and the next. Specifying a position between the two points returns this point.</param>
    public enum CurveContinuity
    {
        Smooth,
        Step,
    }
}
