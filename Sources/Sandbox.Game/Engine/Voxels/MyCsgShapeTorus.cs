using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    sealed class MyCsgTorus : MyCsgShapeBase
    {
        private Vector3 m_translation;
        private Quaternion m_invRotation;
        private float m_primaryRadius;
        private float m_secondaryRadius;
        private float m_secondaryHalfDeviation;
        private float m_deviationFrequency;
        private float m_detailFrequency;

        private float m_potentialHalfDeviation;

        internal MyCsgTorus(
            Vector3 translation,
            Quaternion invRotation,
            float primaryRadius,
            float secondaryRadius,
            float secondaryHalfDeviation,
            float deviationFrequency,
            float detailFrequency)
        {
            m_translation        = translation;
            m_invRotation        = invRotation;
            m_primaryRadius      = primaryRadius;
            m_secondaryRadius    = secondaryRadius;
            m_deviationFrequency = deviationFrequency;
            m_detailFrequency    = detailFrequency;

            m_potentialHalfDeviation = m_secondaryHalfDeviation + m_detailSize;

            if (m_detailFrequency == 0)
                m_enableModulation = false;
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            var localQuerySphere = querySphere;
            localQuerySphere.Center -= m_translation;
            Vector3.Transform(ref localQuerySphere.Center, ref m_invRotation, out localQuerySphere.Center);
            localQuerySphere.Radius += lodVoxelSize;

            var primaryDistance = new Vector2(localQuerySphere.Center.X, localQuerySphere.Center.Z).Length() - m_primaryRadius;
            var signedDistance = new Vector2(primaryDistance, localQuerySphere.Center.Y).Length() - m_secondaryRadius;
            var threshold = m_potentialHalfDeviation + lodVoxelSize + localQuerySphere.Radius;

            if (signedDistance > threshold)
                return ContainmentType.Disjoint;
            else if (signedDistance < -threshold)
                return ContainmentType.Contains;
            else
                return ContainmentType.Intersects;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;
            Vector3.Transform(ref localPosition, ref m_invRotation, out localPosition);

            var primaryDistance = new Vector2(localPosition.X, localPosition.Z).Length() - m_primaryRadius;
            var signedDistance = new Vector2(primaryDistance, localPosition.Y).Length() - m_secondaryRadius;

            var potentialHalfDeviation = m_potentialHalfDeviation + lodVoxelSize;
            if (signedDistance > potentialHalfDeviation)
                return 1f;
            else if (signedDistance < -potentialHalfDeviation)
                return -1f;

            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, ref signedDistance);
        }

        private float SignedDistanceInternal(float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator, ref Vector3 localPosition, ref float signedDistance)
        {
            if (m_enableModulation)
            {
                Debug.Assert(m_deviationFrequency != 0f);
                float normalizer = 0.5f * m_deviationFrequency;
                var tmp = localPosition * normalizer;
                float halfDeviationRatio = (float)macroModulator.GetValue(tmp.X, tmp.Y, tmp.Z);
                signedDistance -= halfDeviationRatio * m_secondaryHalfDeviation;
            }

            if (m_enableModulation && -m_detailSize < signedDistance && signedDistance < m_detailSize)
            {
                Debug.Assert(m_detailFrequency != 0f);
                float normalizer = 0.5f * m_detailFrequency;
                var tmp = localPosition * normalizer;
                signedDistance += m_detailSize * (float)detailModulator.GetValue(tmp.X, tmp.Y, tmp.Z);
            }

            return signedDistance / lodVoxelSize;
        }

        internal override float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;
            Vector3.Transform(ref localPosition, ref m_invRotation, out localPosition);

            var primaryDistance = new Vector2(localPosition.X, localPosition.Z).Length() - m_primaryRadius;
            var signedDistance = new Vector2(primaryDistance, localPosition.Y).Length() - m_secondaryRadius;

            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, ref signedDistance);
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            var translation = MatrixD.CreateTranslation(worldTranslation + m_translation);
            var primary = (m_primaryRadius + m_secondaryRadius) * 2;
            var secondary = m_secondaryRadius * 2;
            var scale = MatrixD.CreateScale(primary, secondary, primary);
            MatrixD rotation;
            MatrixD.CreateFromQuaternion(ref m_invRotation, out rotation);
            MatrixD.Transpose(ref rotation, out rotation); // inverse
            var mat = scale * rotation * translation;
            VRageRender.MyRenderProxy.DebugDrawCylinder(mat, color.ToVector3(), 0.5f, true, false);
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgTorus(m_translation, m_invRotation, m_primaryRadius,
                m_secondaryRadius, m_secondaryHalfDeviation, m_deviationFrequency,
                m_detailFrequency);
        }

        internal override void ShrinkTo(float percentage)
        {
            m_secondaryRadius *= percentage;
            m_secondaryHalfDeviation *= percentage;
        }

        internal override Vector3 Center()
        {
            return m_translation;
        }
    }
}
