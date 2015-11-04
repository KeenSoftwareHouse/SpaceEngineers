using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    class MyCsgCapsule : MyCsgShapeBase
    {
        private Vector3 m_pointA;
        private Vector3 m_pointB;
        private float m_radius;
        private float m_halfDeviation;
        private float m_deviationFrequency;
        private float m_detailFrequency;

        private float m_potentialHalfDeviation;

        public MyCsgCapsule(Vector3 pointA, Vector3 pointB, float radius, float halfDeviation, float deviationFrequency, float detailFrequency)
        {
            m_pointA = pointA;
            m_pointB = pointB;
            m_radius = radius;
            m_halfDeviation = halfDeviation;
            m_deviationFrequency = deviationFrequency;
            m_detailFrequency = detailFrequency;

            if (deviationFrequency == 0)
                m_enableModulation = false;

            m_potentialHalfDeviation = m_halfDeviation + m_detailSize;
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            var v = (m_pointB - m_pointA);
            float len = v.Normalize();
            var q = querySphere.Center - m_pointA;
            float t = q.Dot(ref v);
            t = MathHelper.Clamp(t, 0f, len);
            q = m_pointA + t * v;

            var signedDistance = (querySphere.Center - q).Length() - m_radius;
            var threshold = m_potentialHalfDeviation + lodVoxelSize + querySphere.Radius;

            if (signedDistance > threshold)
                return ContainmentType.Disjoint;
            else if (signedDistance < -threshold)
                return ContainmentType.Contains;
            else
                return ContainmentType.Intersects;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            var pa = position - m_pointA;
            var ba = m_pointB - m_pointA;
            float h = MathHelper.Clamp(pa.Dot(ref ba) / ba.LengthSquared(), 0.0f, 1.0f);
            float signedDistance = (pa - ba * h).Length() - m_radius;

            var potentialHalfDeviation = m_potentialHalfDeviation + lodVoxelSize;
            if (signedDistance > potentialHalfDeviation)
                return 1f;
            else if (signedDistance < -potentialHalfDeviation)
                return -1f;

            return SignedDistanceInternal(ref position, lodVoxelSize, macroModulator, detailModulator, ref signedDistance);
        }

        private float SignedDistanceInternal(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator, ref float signedDistance)
        {
            if (m_enableModulation)
            {
                Debug.Assert(m_deviationFrequency != 0f);
                var halfDeviationRatio = (float)macroModulator.GetValue(
                    position.X * m_deviationFrequency,
                    position.Y * m_deviationFrequency,
                    position.Z * m_deviationFrequency);
                signedDistance -= halfDeviationRatio * m_halfDeviation;
            }

            if (m_enableModulation && -m_detailSize < signedDistance && signedDistance < m_detailSize)
            {
                Debug.Assert(m_detailFrequency != 0f);
                signedDistance += m_detailSize * (float)detailModulator.GetValue(
                    position.X * m_detailFrequency,
                    position.Y * m_detailFrequency,
                    position.Z * m_detailFrequency);
            }

            return signedDistance / lodVoxelSize;
        }

        internal override float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            var pa = position - m_pointA;
            var ba = m_pointB - m_pointA;
            float h = MathHelper.Clamp(pa.Dot(ref ba) / ba.LengthSquared(), 0.0f, 1.0f);
            float signedDistance = (pa - ba * h).Length() - m_radius;

            return SignedDistanceInternal(ref position, lodVoxelSize, macroModulator, detailModulator, ref signedDistance);
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            VRageRender.MyRenderProxy.DebugDrawCapsule(worldTranslation + m_pointA, worldTranslation + m_pointB, m_radius, color, depthRead: true, shaded: true);
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgCapsule(
                detailFrequency: m_detailFrequency,
                deviationFrequency: m_deviationFrequency,
                halfDeviation: m_halfDeviation,
                pointA: m_pointA,
                pointB: m_pointB,
                radius: m_radius
            );
        }

        internal override void ShrinkTo(float percentage)
        {
            m_radius *= percentage;
            m_halfDeviation *= percentage;
        }

        internal override Vector3 Center()
        {
            return (m_pointA + m_pointB) / 2;
        }
    }
}
