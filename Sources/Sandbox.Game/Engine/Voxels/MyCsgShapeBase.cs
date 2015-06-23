using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    abstract class MyCsgShapeBase
    {
        protected bool m_enableModulation;
        protected float m_detailSize;

        protected MyCsgShapeBase()
        {
            m_enableModulation = true;
            m_detailSize = 6f;
        }

        internal abstract ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize);

        internal abstract float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator);
        internal abstract float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator);

        internal abstract Vector3 Center();

        internal virtual void DebugDraw(ref Vector3D worldTranslation, Color color) { }

        internal abstract MyCsgShapeBase DeepCopy();

        /// <param name="percentage">Percentage given as value in range 0 to 1.</param>
        internal abstract void ShrinkTo(float percentage);
    }
}
