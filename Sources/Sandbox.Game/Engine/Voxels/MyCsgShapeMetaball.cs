using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    class MyCsgShapeMetaball : MyCsgShapeBase
    {
        private const float SHRINK_FACTOR = 2;

        private Vector3 m_translation;
        private Vector3[] m_balls;
        private float[] m_weights;
        private float m_radius;
        private float m_halfDeviation;
        private float m_deviationFrequency;
        private float m_detailFrequency;

        private float m_outerRadius;
        private float m_innerRadius;

        public MyCsgShapeMetaball(Vector3 translation, Vector3[] balls, float[] weights, float radius, float halfDeviation = 0,
            float deviationFrequency = 0, float detailFrequency = 0)
        {
            m_translation = translation;
            m_balls = balls;
            m_weights = weights;
            m_radius = radius;
            m_halfDeviation = halfDeviation;
            m_deviationFrequency = deviationFrequency;
            m_detailFrequency = detailFrequency;

            ComputeDerivedProperties();
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            double minSum = 0;
            double maxSum = 0;
            Vector3 localPosition = querySphere.Center - m_translation;
            for (int i = 0; i < m_balls.Length; i++)
            {
                var distance = (localPosition - m_balls[i]).Length();
                double closeDistance = Math.Max(distance - querySphere.Radius, 0.0001);
                double farDistance = distance + querySphere.Radius;
                var weight = m_weights[i];
                if (weight > 0)
                {
                    minSum += weight / (farDistance * farDistance);
                    maxSum += weight / (closeDistance * closeDistance);
                }
                else
                {
                    minSum += weight / (closeDistance * closeDistance);
                    maxSum += weight / (farDistance * farDistance);
                }
            }

            if (minSum < 0)
                minSum = 0.0001;

            float minDistance = (float) Math.Sqrt(1 / maxSum) * SHRINK_FACTOR;
            float maxDistance = (float) Math.Sqrt(1 / minSum) * SHRINK_FACTOR;

            if (minDistance > m_outerRadius + lodVoxelSize)
                return ContainmentType.Disjoint;
            if (maxDistance < m_innerRadius - lodVoxelSize)
                return ContainmentType.Contains;
            return ContainmentType.Intersects;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;

            double sum = 0;
            for (int i = 0; i < m_balls.Length; i++)
            {
                double distSquared = (localPosition - m_balls[i]).LengthSquared();
                sum += m_weights[i] / distSquared;
            }
            if (sum < 0)
                return 1f;

            float distance = (float)Math.Sqrt(1 / sum) * SHRINK_FACTOR;
            if ((m_innerRadius - lodVoxelSize) > distance)
                return -1f;
            if ((m_outerRadius + lodVoxelSize) < distance)
                return 1f;

            float halfDeviationRatio;
            if (m_enableModulation)
            {
                Debug.Assert(m_deviationFrequency != 0f);
                var tmp = localPosition * m_deviationFrequency;
                halfDeviationRatio = (float)macroModulator.GetValue(tmp.X, tmp.Y, tmp.Z);
            }
            else
            {
                halfDeviationRatio = 0f;
            }

            float signedDistance = distance - m_radius - halfDeviationRatio * m_halfDeviation;
            if (m_enableModulation && -m_detailSize < signedDistance && signedDistance < m_detailSize)
            {
                Debug.Assert(m_detailFrequency != 0f);
                var tmp = localPosition * m_detailFrequency;
                signedDistance += m_detailSize * (float)detailModulator.GetValue(tmp.X, tmp.Y, tmp.Z);
            }

            return signedDistance / lodVoxelSize;
        }

        internal override Vector3 Center()
        {
            return m_translation;
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            for (int i = 0; i < m_balls.Length; i++)
                VRageRender.MyRenderProxy.DebugDrawSphere(worldTranslation + m_translation + m_balls[i], m_radius, color.ToVector3(), alpha: 0.5f, depthRead: true, smooth: false);
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgShapeMetaball(m_translation, m_balls, m_weights, m_radius, m_halfDeviation, m_deviationFrequency, m_detailFrequency);
        }

        internal override void ShrinkTo(float percentage)
        {
            m_radius *= percentage;
            m_halfDeviation *= percentage;
            ComputeDerivedProperties();
        }

        private void ComputeDerivedProperties()
        {
            m_outerRadius = m_radius + m_halfDeviation + m_detailSize;
            m_innerRadius = m_radius - m_halfDeviation - m_detailSize;
        }
    }
}
