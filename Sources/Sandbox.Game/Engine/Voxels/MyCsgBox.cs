#define USE_CORRECT_COMPUTATION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    /// <summary>
    /// Creates simple axis aligned box with no noise applied to it. Meant to be simple
    /// example of how to implement CSG shape.
    /// 
    /// NOTE: Using this box might not always produce sharp edges due to rasterization
    /// of distance function. They will be sharp when box aligns with raster grid (which
    /// is, simply put, for positions and sizes that are power of two).
    /// </summary>
    class MyCsgBox : MyCsgShapeBase
    {
        private Vector3 m_translation;
        private float m_halfExtents;

        public MyCsgBox(Vector3 translation, float halfExtents)
        {
            m_translation = translation;
            m_halfExtents = halfExtents;
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            ContainmentType outerContainment, innerContainment;

            BoundingBox box = BoundingBox.CreateFromHalfExtent(m_translation, m_halfExtents + lodVoxelSize);

            box.Contains(ref queryAabb, out outerContainment);
            if (outerContainment == ContainmentType.Disjoint)
                return ContainmentType.Disjoint;

            box = BoundingBox.CreateFromHalfExtent(m_translation, m_halfExtents - lodVoxelSize);
            box.Contains(ref queryAabb, out innerContainment);
            if (innerContainment == ContainmentType.Contains)
                return ContainmentType.Contains;

            return ContainmentType.Intersects;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
#if USE_CORRECT_COMPUTATION
            // The only difference between SignedDistanceUnchecked() and SignedDistance() is that the latter clamps the result.
            return MathHelper.Clamp(SignedDistanceUnchecked(ref position, lodVoxelSize, macroModulator, detailModulator), -1f, 1f);
#else
            // This attempts to optimize queries by detecting cases where the result would be clamped.
            // Given simple distance computation performed here, it's actually slower than computing distance and clamping the result,
            // but if noise was applied to the result, this culling can save a lot. However, it can also hide incorrect distance
            // computation, so before optimizing, be sure clamp variant above works correctly.

            ContainmentType outerContainment, innerContainment;

            BoundingBox box = BoundingBox.CreateFromHalfExtent(m_translation, m_halfExtents + lodVoxelSize);

            box.Contains(ref position, out outerContainment);
            if (outerContainment == ContainmentType.Disjoint)
                return 1;

            box = BoundingBox.CreateFromHalfExtent(m_translation, m_halfExtents - lodVoxelSize);
            box.Contains(ref position, out innerContainment);
            if (innerContainment == ContainmentType.Contains)
                return -1;

            return SignedDistanceUnchecked(ref position, lodVoxelSize, macroModulator, detailModulator);
#endif
        }

        internal override float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
#if USE_CORRECT_COMPUTATION
            // Correct computation of signed distance from the surface of box (based on materials by Iñigo Quilez).
            // Does not apply noise or anything. Just simple distance.
            var relPos = position - m_translation;
            var d = Vector3.Abs(relPos) - m_halfExtents;
            float signedDistance = Math.Min(Math.Max(d.X, Math.Max(d.Y, d.Z)), 0f) + (Vector3.Max(d, Vector3.Zero)).Length();
            return signedDistance / lodVoxelSize;
#else
            // Incorrect, since Clamp will not modify the position when it's inside
            // the box, resulting in -0f for all values inside, which is not correct distance.
            var min = m_translation - m_halfExtents;
            var max = m_translation + m_halfExtents;
            var box = new BoundingBox(min, max);
            var clamp = Vector3.Clamp(position, min, max);
            float sign = box.Contains(position) == ContainmentType.Contains ? -1 : 1;
            float distance = sign * Vector3.Distance(clamp, position);
            return distance / lodVoxelSize;
#endif
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            BoundingBoxD bb = new BoundingBoxD(worldTranslation + m_translation - m_halfExtents, worldTranslation + m_translation + m_halfExtents);
            VRageRender.MyRenderProxy.DebugDrawAABB(bb, color, 0.5f, 1, false);
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgBox(m_translation, m_halfExtents);
        }

        internal override void ShrinkTo(float percentage)
        {
            m_halfExtents *= percentage;
        }

        internal override Vector3 Center()
        {
            return m_translation;
        }

        internal float HalfExtents
        {
            get { return m_halfExtents; }
        }
    }
}
