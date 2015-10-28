using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    /// <summary>
    /// Shape used in CSG operations in MyCompositeShapeProvider.
    /// It can provide signed distance from the surface of the shape as culling if this signed distance
    /// will not be within certain range of the surface given a query range. For simple implementation,
    /// see MyCsgBox.
    /// 
    /// For information on signed distance of some simple shapes go to http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm .
    /// In general, signed distance is distance to the closest point on the surface of the shape,
    /// with sign depending on whether it is outside (positive) or inside (negative).
    /// You can think of it as a step parameter for raymarching which determines how far and in which direction (forward +, backward -) you can move
    /// before hitting surface along the normalized ray direction from sampled point, assuming the origin is outside the shape.
    /// </summary>
    abstract class MyCsgShapeBase
    {
        protected bool m_enableModulation;
        protected float m_detailSize;

        protected MyCsgShapeBase()
        {
            m_enableModulation = true;
            m_detailSize = 6f;
        }

        /// <summary>
        /// Test whether points sampled inside AABB would be affected by this CSG shape.
        /// Some shapes might only be able to perform efficient culling against sphere,
        /// so bounding sphere of AABB is also provided. AABB is usually more accurate though.
        /// </summary>
        internal abstract ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize);

        /// <summary>
        /// Returns signed distance (or approximation, when exact result is not possible).
        /// mk:TODO: Both functions do exactly the same thing, but "SignedDistance" clamps the result to -1;1 range. Consider changing semantics so they can be combined.
        /// </summary>
        /// <param name="position">Sample position</param>
        /// <param name="lodVoxelSize">Normalization range. Resulting signed distance will be normalized such that range from -lodVoxelSize to lodVoxelSize falls into -1 to 1 range.</param>
        /// <param name="macroModulator">Noise function for modifying surface on large scale.</param>
        /// <param name="detailModulator">Noise function for modifying surface on small scale.</param>
        /// <returns>Signed distance normalized using lodVoxelSize.</returns>
        internal abstract float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator);
        internal abstract float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator);

        internal virtual void DebugDraw(ref Vector3D worldTranslation, Color color) { }

        internal abstract MyCsgShapeBase DeepCopy();

        /// <param name="percentage">Percentage given as value in range 0 to 1.</param>
        internal abstract void ShrinkTo(float percentage);

        internal abstract Vector3 Center();
        internal virtual void ReleaseMaps() { }
    }
}
