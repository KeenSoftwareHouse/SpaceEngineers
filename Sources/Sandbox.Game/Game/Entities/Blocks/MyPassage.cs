﻿#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.TransparentGeometry;
using VRage.Components;
using VRageMath;
using VRageRender;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Passage))]
    class MyPassage : MyCubeBlock
    {
        //  Return true if object intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        public override bool GetIntersectionWithSphere(ref BoundingSphereD sphere)
        {
            return false;
        }

        //  Calculates intersection of line with any triangleVertexes in this model instance. Closest intersection and intersected triangleVertexes will be returned.
        internal override bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            t = null;
            return false;
        }
    }
}
