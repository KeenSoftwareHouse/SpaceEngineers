using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Noise;
using VRage.Noise.Modifiers;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    class MySphereDensityFunction : IMyAsteroidFieldDensityFunction
    {
        private Vector3D m_center;
        private BoundingSphereD m_sphereMax;

        private double m_innerRadius;
        private double m_outerRadius;
        private double m_middleRadius;
        private double m_halfFalloff;

        public MySphereDensityFunction(Vector3D center, double radius, double additionalFalloff)
        {
            m_center = center;
            m_sphereMax = new BoundingSphereD(center, radius + additionalFalloff);
            m_innerRadius = radius;
            m_halfFalloff = additionalFalloff / 2.0;
            m_middleRadius = radius + m_halfFalloff;
            m_outerRadius = radius + additionalFalloff;
        }

        public bool ExistsInCell(ref BoundingBoxD bbox)
        {
            return m_sphereMax.Contains(bbox) != ContainmentType.Disjoint;
        }

        public double GetValue(double x)
        {
            throw new NotImplementedException();
        }

        public double GetValue(double x, double y)
        {
            throw new NotImplementedException();
        }

        public double GetValue(double x, double y, double z)
        {
            double distance = Vector3D.Distance(m_center, new Vector3D(x, y, z));
            if (distance > m_outerRadius)
            {
                return 1;
            }
            else if (distance < m_innerRadius)
            {
                return -1;
            }
            else
            {
                if (distance > m_middleRadius)
                {
                    return (m_middleRadius - distance) / -m_halfFalloff;
                }
                else
                {
                    return (distance - m_middleRadius) / m_halfFalloff;
                }
            }
        }
    }
}
