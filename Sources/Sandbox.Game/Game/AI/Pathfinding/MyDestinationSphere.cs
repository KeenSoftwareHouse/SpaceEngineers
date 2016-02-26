using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyDestinationSphere : IMyDestinationShape
    {
        private float m_radius;
        private Vector3D m_center;
        private Vector3D m_relativeCenter;

        public MyDestinationSphere(ref Vector3D worldCenter, float radius)
        {
            Init(ref worldCenter, radius);
        }

        public void Init(ref Vector3D worldCenter, float radius)
        {
            m_radius = radius;
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
            if (dist <= m_radius + tolerance)
            {
                return dist;
            }
            return float.PositiveInfinity;
        }

        public Vector3D GetClosestPoint(Vector3D queryPoint)
        {
            Vector3D displacementVector = queryPoint - m_center;
            double len = displacementVector.Length();
            if (len < m_radius)
            {
                return queryPoint;
            }
            else
            {
                return m_center + displacementVector / len * m_radius;
            }
        }

        public Vector3D GetBestPoint(Vector3D queryPoint)
        {
            return m_center;
        }

        public Vector3D GetDestination()
        {
            return m_center;
        }

        public void DebugDraw()
        {
            MyRenderProxy.DebugDrawSphere(m_center, Math.Max(m_radius, 0.05f), Color.Pink, 1.0f, false);
            MyRenderProxy.DebugDrawSphere(m_center, m_radius, Color.Pink, 1.0f, false);
            MyRenderProxy.DebugDrawText3D(m_center, "Destination", Color.Pink, 1, false, VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }
    }
}
