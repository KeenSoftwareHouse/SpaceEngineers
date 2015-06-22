using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    class MyCsgSimpleSphere : MyCsgShapeBase
    {
        private Vector3 m_translation;
        private float m_radius;

        public MyCsgSimpleSphere(Vector3 translation, float radius)
        {
            m_translation        = translation;
            m_radius             = radius;        
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            ContainmentType outerContainment, innerContainment;

            BoundingSphere sphere = new BoundingSphere(
                m_translation,
                m_radius + lodVoxelSize);

            sphere.Contains(ref queryAabb, out outerContainment);
            if (outerContainment == ContainmentType.Disjoint)
                return ContainmentType.Disjoint;

            sphere.Radius = m_radius - lodVoxelSize;
            sphere.Contains(ref queryAabb, out innerContainment);
            if (innerContainment == ContainmentType.Contains)
                return ContainmentType.Contains;

            return ContainmentType.Intersects;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;

            float distance = localPosition.Length();

            if ((m_radius - lodVoxelSize) > distance)
                return -1f;

            if ((m_radius + lodVoxelSize) < distance)
                return 1f;

            return SignedDistanceInternal(lodVoxelSize, distance);
        }

        private float SignedDistanceInternal(float lodVoxelSize, float distance)
        {
            float signedDistance = distance - m_radius;
            return signedDistance / lodVoxelSize;
        }

        internal override float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;

            float distance = localPosition.Length();

            return SignedDistanceInternal(lodVoxelSize, distance);
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            VRageRender.MyRenderProxy.DebugDrawSphere(worldTranslation + m_translation, m_radius, color.ToVector3(), alpha: 0.5f, depthRead: true, smooth: false);
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgSimpleSphere(
                m_translation,
                m_radius);
        }

        internal override void ShrinkTo(float percentage)
        {
            m_radius *= percentage;
        }

        internal override Vector3 Center()
        {
            return m_translation;
        }
    }
}
