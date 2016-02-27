using System;
using System.Collections.Generic;
using VRageMath;

namespace VRage.Game.Components
{
    public class MyHierarchyComponent<TYPE> : MyHierarchyComponentBase
    {
        public Action<BoundingBoxD, List<TYPE>> QueryAABBImpl;
        public Action<BoundingSphereD, List<TYPE>> QuerySphereImpl;
        public Action<LineD, List<MyLineSegmentOverlapResult<TYPE>>> QueryLineImpl;

        public void QueryAABB(ref BoundingBoxD aabb, List<TYPE> result)
        {
            if (Entity != null && !Entity.MarkedForClose && QueryAABBImpl != null)
            {
                QueryAABBImpl(aabb, result);
            }
        }

        public void QuerySphere(ref BoundingSphereD sphere, List<TYPE> result)
        {
            if (!Entity.MarkedForClose && QuerySphereImpl != null)
            {
                QuerySphereImpl(sphere, result);
            }
        }

        public void QueryLine(ref LineD line, List<MyLineSegmentOverlapResult<TYPE>> result)
        {
            if (!Entity.MarkedForClose && QueryLineImpl != null)
            {
                QueryLineImpl(line, result);
            }
        }
    }
}
