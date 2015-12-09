using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyRandomLocationSphere : IMyDestinationShape
    {
        private Vector3D m_center;
        private Vector3D m_relativeCenter;
        private Vector3D m_desiredDirection;
        private float m_radius;

        public MyRandomLocationSphere(Vector3D worldCenter, float radius, Vector3D direction)
        {
            Init(ref worldCenter, radius, direction);
        }

        public void Init(ref Vector3D worldCenter, float radius, Vector3D direction)
        {
            m_center = worldCenter;
            m_radius = radius;
            m_desiredDirection = direction;
        }

        public void SetRelativeTransform(MatrixD invWorldTransform)
        {
            Vector3D.Transform(ref m_center, ref invWorldTransform, out m_relativeCenter);
        }

        public void UpdateWorldTransform(MatrixD worldTransform)
        {
            Vector3D.Transform(ref m_relativeCenter, ref worldTransform, out m_center);
        }

        public float PointAdmissibility(Vector3D position, float tolerance)
        {
            Vector3D displacement = position - m_center;
            float dist = (float)displacement.Normalize();

            if (dist < m_radius + tolerance || displacement.Dot(ref m_desiredDirection) < 0.90)
                return float.PositiveInfinity;
            else
                return dist;
        }

        public Vector3D GetClosestPoint(Vector3D queryPoint)
        {
            Vector3D displacementVector = queryPoint - m_center;
            double length = displacementVector.Normalize();
            if (length > m_radius)
                return queryPoint;
            else
            {
                double cos = m_desiredDirection.Dot(ref displacementVector);
                if (cos > 0.9)
                    return m_center + displacementVector * m_radius;
                else
                    return m_center + m_desiredDirection * m_radius;
            }
        }

        public Vector3D GetBestPoint(Vector3D queryPoint)
        {
            Vector3D displacementVector = queryPoint - m_center;
            double length = displacementVector.Length();
            if (length > m_radius)
                return queryPoint;
            else
                return m_center + m_desiredDirection * m_radius;
        }

        public Vector3D GetDestination()
        {
            return m_center + m_desiredDirection * m_radius;
        }

        public void DebugDraw()
        {
            VRageRender.MyRenderProxy.DebugDrawSphere(m_center, m_radius, Color.Gainsboro, 1, true);
            VRageRender.MyRenderProxy.DebugDrawSphere(m_center + m_desiredDirection * m_radius, 4, Color.Aqua, 1, true);
        }
    }
}
