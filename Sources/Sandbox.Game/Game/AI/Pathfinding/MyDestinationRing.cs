using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyDestinationRing : IMyDestinationShape
    {
        private float m_innerRadius;
        private float m_outerRadius;   
        private Vector3D m_center;
        private Vector3D m_relativeCenter;

        public MyDestinationRing(ref Vector3D worldCenter, float innerRadius, float outerRadius)
        {
            Init(ref worldCenter, innerRadius, outerRadius);
        }

        public void Init(ref Vector3D worldCenter, float innerRadius, float outerRadius)
        {
            m_center = worldCenter;
            m_innerRadius = innerRadius;
            m_outerRadius = outerRadius;
        }

        public void Reinit(ref Vector3D worldCenter)
        {
            m_center = worldCenter;
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
            float dist = (float)Vector3D.Distance(position, m_center);

            if (dist < Math.Min(m_innerRadius - tolerance, 0) || dist > m_outerRadius + tolerance)
                return float.PositiveInfinity;
            else
                return dist;
        }

        public Vector3D GetClosestPoint(Vector3D queryPoint)
        {
            Vector3D displacementVector = queryPoint - m_center;
            double len = displacementVector.Length();
            if (len < m_innerRadius)
            {
                return m_center + displacementVector / len * m_innerRadius;
            }
            else if (len > m_outerRadius)
            {
                return m_center + displacementVector / len * m_outerRadius;
            }
            else
            {
                return queryPoint;
            }
        }

        public Vector3D GetBestPoint(Vector3D queryPoint)
        {
            Vector3D direction = Vector3D.Normalize(queryPoint - m_center);
            return m_center + direction * ((m_innerRadius + m_outerRadius) * 0.5f);
        }

        public Vector3D GetDestination()
        {
            return m_center;
        }

        public void DebugDraw()
        {
            VRageRender.MyRenderProxy.DebugDrawSphere(m_center, m_innerRadius, Color.RoyalBlue, 0.4f, true);
            VRageRender.MyRenderProxy.DebugDrawSphere(m_center, m_outerRadius, Color.Aqua, 0.4f, true);
        }
    }
}
