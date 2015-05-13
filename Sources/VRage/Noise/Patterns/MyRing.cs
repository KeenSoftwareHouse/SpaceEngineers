using System;

namespace VRage.Noise.Patterns
{
    /// <summary>
    /// Noise that outputs dounut-like ring
    /// </summary>
    public class MyRing : IMyModule
    {
        public double Radius { get; set; }

        private double m_thickness;
        private double m_thicknessSqr;
        public double Thickness
        {
            get { return m_thickness; }
            set
            {
                m_thickness = value;
                m_thicknessSqr = value * value;
            }
        }

        public MyRing(double radius, double thickness)
        {
            Radius = radius;
            Thickness = thickness;
        }

        public double GetValue(double x)
        {
            double dstFromCenter = System.Math.Sqrt(x * x);
            double dstFromRing = dstFromCenter - Radius;
            return clampToRing(dstFromRing * dstFromRing);
        }

        public double GetValue(double x, double y)
        {

            double dstFromCenter = System.Math.Sqrt(x * x + y * y);
            double dstFromRing = dstFromCenter - Radius;

            return clampToRing(dstFromRing * dstFromRing);
        }

        public double GetValue(double x, double y, double z)
        {
            if (Math.Abs(z) < Thickness)
            {
                double dstFromCenterInXY = System.Math.Sqrt(x * x + y * y);
                double dstFromRingInXY = Math.Abs(dstFromCenterInXY - Radius);

                if (dstFromRingInXY < Thickness)
                {
                    double distInX = (x / dstFromCenterInXY) * Radius - x;
                    double distInY = (y / dstFromCenterInXY) * Radius - y;

                    double dstFromRing = distInX * distInX + distInY * distInY + z * z;
                    return clampToRing(dstFromRing);
                }
                else
                    return 0.0;
            }
            else
                return 0.0;
        }

        private double clampToRing(double squareDstFromRing)
        {
            if (squareDstFromRing < m_thicknessSqr)
                return (1.0 - squareDstFromRing / m_thicknessSqr);
            else
                return 0;
        }
    }
}
