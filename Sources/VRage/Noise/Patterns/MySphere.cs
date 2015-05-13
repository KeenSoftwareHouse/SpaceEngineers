using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Noise.Patterns
{
    public class MySphere : IMyModule
    {
        private double m_outerRadiusBlendingSqrDist;

        private double m_innerRadius;
        private double m_innerRadiusSqr;
        public double InnerRadius
        {
            get { return m_innerRadius; }
            set
            {
                m_innerRadius = value;
                m_innerRadiusSqr = value * value;
                UpdateBlendingDistnace();
            }
        }
        private double m_outerRadius;
        private double m_outerRadiusSqr;
        public double OuterRadius
        {
            get { return m_outerRadius; }
            set
            {
                m_outerRadius = value;
                m_outerRadiusSqr = value * value;
                UpdateBlendingDistnace();
            }
        }

        private void UpdateBlendingDistnace()
        {
            m_outerRadiusBlendingSqrDist = m_outerRadiusSqr - m_innerRadiusSqr;
        }
        
        public MySphere(double innerRadius, double outerRadius)
        {
            System.Diagnostics.Debug.Assert(innerRadius > 0);
            System.Diagnostics.Debug.Assert(innerRadius < outerRadius);

            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }

        public double GetValue(double x)
        {
            double distanceSqr = x*x;
            return ClampDistanceToRadius(distanceSqr);
        }

        public double GetValue(double x, double y)
        {
            double distanceSqr = x*x + y*y;
            return ClampDistanceToRadius(distanceSqr);
        }

        public double GetValue(double x, double y, double z)
        {
            double distanceSqr = x*x + y*y + z*z;
            return ClampDistanceToRadius(distanceSqr);
        }

        private double ClampDistanceToRadius(double distanceSqr)
        {
            if (distanceSqr < m_outerRadiusSqr)
            {
                if (distanceSqr < m_innerRadiusSqr)
                    return 1.0;
                else
                    return 1.0 - (distanceSqr - m_innerRadiusSqr)/m_outerRadiusBlendingSqrDist;
            }
            else
                return 0;
        }
    }
}
