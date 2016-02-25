using Sandbox.Game.Entities;
using VRage.Game.Entity;

namespace Sandbox.Engine.Utils
{
    struct MyIntersectionResultLineBoundingSphere
    {
        public readonly float Distance;                      //  Distance to the intersection point (calculated as distance from 'line.From' to 'intersection point')
        public readonly MyEntity PhysObject;         //  If intersection occured with phys object, here will be it

        public MyIntersectionResultLineBoundingSphere(float distance, MyEntity physObject)
        {
            Distance = distance;
            PhysObject = physObject;
        }

        //  Find and return closer intersection of these two. If intersection is null then it's not really an intersection.
        public static MyIntersectionResultLineBoundingSphere? GetCloserIntersection(ref MyIntersectionResultLineBoundingSphere? a, ref MyIntersectionResultLineBoundingSphere? b)
        {
            if (((a == null) && (b != null)) ||
                ((a != null) && (b != null) && (b.Value.Distance < a.Value.Distance)))
            {
                //  If only "b" contains valid intersection, or when it's closer than "a"
                return b;
            }
            else
            {
                //  This will be returned also when ((a == null) && (b == null))
                return a;
            }
        }
    }
}
