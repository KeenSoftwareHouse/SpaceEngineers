using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public class MySpherePlaceArea : MyPlaceArea
    {
        private float m_radiusSq;
        private float m_radius;
		public float Radius { get { return m_radius; } }

        public MySpherePlaceArea(float radius, MyStringHash areaType)
            : base(areaType)
        {
            m_radius = radius;
            m_radiusSq = radius * radius;
        }

        public override BoundingBoxD WorldAABB
        {
            get
            {
                Vector3D halfExtents = new Vector3D(m_radius);
                return new BoundingBoxD(GetPosition() - halfExtents, GetPosition() + halfExtents);
            }
        }

        public Vector3D GetPosition()
        {
            if (Container.Entity.PositionComp == null)
            {
                Debug.Assert(false, "Position component was null on entity with place area!");
                return Vector3D.Zero;
            }

            return Container.Entity.PositionComp.GetPosition();
        }

		public override double DistanceSqToPoint(Vector3D point)
		{
			double distance = (GetPosition() - point).Length() - m_radius;
			return (distance < 0 ? 0 : distance * distance);
		}

        public override bool TestPoint(Vector3D point)
        {
            return Vector3D.DistanceSquared(point, GetPosition()) <= m_radiusSq;
        }
    }
}
