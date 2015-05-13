using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    class MyCsgShapeExcludedSphere : MyCsgShapeBase
    {
        private Vector3 m_translation;
        private float m_radius;
        private float m_halfDeviation;
        private float m_deviationFrequency;
        private float m_detailFrequency;

        MyCsgSphere m_sphere;
        MyCsgSphere m_exclusionSphere;

        public MyCsgShapeExcludedSphere(VRageMath.Vector3 translation, float radius,float exclusionRadius, float halfDeviation = 0, float deviationFrequency = 0, float detailFrequency = 0)
        {
            m_translation = translation;
            m_radius = radius;
            m_halfDeviation = halfDeviation;
            m_deviationFrequency = deviationFrequency;
            m_detailFrequency = detailFrequency;


            m_sphere = new MyCsgSphere(translation, radius, halfDeviation, deviationFrequency, detailFrequency);
            m_exclusionSphere = new MyCsgSphere(translation, exclusionRadius, halfDeviation, deviationFrequency, detailFrequency); 
        }

        internal override VRageMath.ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            VRageMath.ContainmentType contaiment = m_exclusionSphere.Contains(ref queryAabb, ref querySphere, lodVoxelSize);
            if (contaiment == ContainmentType.Contains) 
            {
                return ContainmentType.Disjoint;
            }
            return m_sphere.Contains(ref queryAabb, ref querySphere, lodVoxelSize);
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            if (m_exclusionSphere.SignedDistance(ref position, 0, macroModulator, detailModulator) < 0.0f)
            {
                return float.MaxValue;
            }
            return m_sphere.SignedDistance(ref position,  lodVoxelSize,  macroModulator,  detailModulator);
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            m_exclusionSphere.DebugDraw(ref worldTranslation, color);
            m_sphere.DebugDraw(ref worldTranslation, color);
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgShapeExcludedSphere(
                m_translation,
                m_radius,
                m_halfDeviation,
                m_deviationFrequency,
                m_detailFrequency);
        }

        internal override void ShrinkTo(float percentage)
        {
            m_sphere.ShrinkTo(percentage);
            m_exclusionSphere.ShrinkTo(percentage);
        }
      
        internal override Vector3 Center()
        {
            return m_sphere.Center();
        }
    }
}
