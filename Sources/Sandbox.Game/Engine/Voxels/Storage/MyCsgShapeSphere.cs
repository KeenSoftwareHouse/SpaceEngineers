using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    class MyCsgSphere : MyCsgShapeBase
    {
        private Vector3 m_translation;
        private float m_radius;
        private float m_halfDeviation;
        private float m_deviationFrequency;
        private float m_detailFrequency;

        private float m_outerRadius;
        private float m_innerRadius;

        public MyCsgSphere(Vector3 translation, float radius, float halfDeviation = 0, float deviationFrequency = 0, float detailFrequency = 0)
        {
            m_translation        = translation;
            m_radius             = radius;
            m_halfDeviation      = halfDeviation;
            m_deviationFrequency = deviationFrequency;
            m_detailFrequency    = detailFrequency;

            if (m_halfDeviation == 0 && m_deviationFrequency == 0 && detailFrequency == 0)
            {
                m_enableModulation = false;
                m_detailSize = 0;
            }

            ComputeDerivedProperties();
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            ContainmentType outerContainment, innerContainment;

            BoundingSphere sphere = new BoundingSphere(
                m_translation,
                m_outerRadius + lodVoxelSize);

            sphere.Contains(ref queryAabb, out outerContainment);
            if (outerContainment == ContainmentType.Disjoint)
                return ContainmentType.Disjoint;

            sphere.Radius = m_innerRadius - lodVoxelSize;
            sphere.Contains(ref queryAabb, out innerContainment);
            if (innerContainment == ContainmentType.Contains)
                return ContainmentType.Contains;

            return ContainmentType.Intersects;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;

            float distance = localPosition.Length();
            if ((m_innerRadius - lodVoxelSize) > distance)
                return -1f;
            if ((m_outerRadius + lodVoxelSize) < distance)
                return 1f;

            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, distance);
        }

        private float SignedDistanceInternal(float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator, ref Vector3 localPosition, float distance)
        {
            float halfDeviationRatio;
            if (m_enableModulation)
            {
                Debug.Assert(m_deviationFrequency != 0f);
                float distScale = 0f;
                if (distance != 0f)
                    distScale = 1f / distance;
                float normalizer = m_deviationFrequency * m_radius * distScale;
                var tmp = localPosition * normalizer;
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
                float normalizer = m_detailFrequency * m_radius / (distance == 0 ? 1 : distance);
                var tmp = localPosition * normalizer;
                signedDistance += m_detailSize * (float)detailModulator.GetValue(tmp.X, tmp.Y, tmp.Z);
            }

            return signedDistance / lodVoxelSize;
        }

        internal override float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;

            float distance = localPosition.Length();

            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, distance);
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            VRageRender.MyRenderProxy.DebugDrawSphere(worldTranslation + m_translation, m_radius, color.ToVector3(), alpha: 0.5f, depthRead: true, smooth: false);
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgSphere(
                m_translation,
                m_radius,
                m_halfDeviation,
                m_deviationFrequency,
                m_detailFrequency);
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

        internal override Vector3 Center()
        {
            return m_translation;
        }
    }
}
