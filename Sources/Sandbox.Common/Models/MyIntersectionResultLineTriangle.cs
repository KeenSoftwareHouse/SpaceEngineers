using System;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.Common
{
    //  Result of intersection between a ray and a triangle. This structure can be used only if intersection was found!
    //  If returned intersection is with voxel, all coordinates are in absolute/world space
    //  If returned intersection is with model instance, all coordinates are in model's local space (so for drawing we need to trasform them using world matrix)
    public struct MyIntersectionResultLineTriangle
    {
        //  IMPORTANT: Use these members only for readonly acces. Change them only inside the constructor.
        //  We can't mark them 'readonly' because sometimes they are sent to different methods through "ref"

        //  Distance to the intersection point (calculated as distance from 'line.From' to 'intersection point')
        public double Distance;
        
        //  World coordinates of intersected triangle. It is also used as input parameter for col/det functions.
        public MyTriangle_Vertexes InputTriangle;

        //  Normals of vertexes of intersected triangle
        public Vector3 InputTriangleNormal;

        public MyIntersectionResultLineTriangle(ref MyTriangle_Vertexes triangle, ref Vector3 triangleNormal, double distance)
        {
            InputTriangle = triangle;
            InputTriangleNormal = triangleNormal;
            Distance = distance;
        }
        
        //  Find and return closer intersection of these two. If intersection is null then it's not really an intersection.
        public static MyIntersectionResultLineTriangle? GetCloserIntersection(ref MyIntersectionResultLineTriangle? a, ref MyIntersectionResultLineTriangle? b)
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

    //  More detailed version of MyIntersectionResultLineTriangle, contains some calculated data, etc. This is usually 
    //  used as a result of triangle intersection searches
    public struct MyIntersectionResultLineTriangleEx
    {
        //  IMPORTANT: Use these members only for readonly acces. Change them only inside the constructor.
        //  We can't mark them 'readonly' because sometimes they are sent to different methods through "ref"

        public MyIntersectionResultLineTriangle Triangle;

        //  Point of intersection, always in object space. Use only if intersection with object.
        public Vector3 IntersectionPointInObjectSpace;

        //  Point of intersection - always in world space
        public Vector3D IntersectionPointInWorldSpace;

        //  If intersection occured with phys object, here will be it
        public IMyEntity Entity;

        //  Normal vector of intersection triangle - always in world space. Can be calculaed from input positions.
        public Vector3 NormalInWorldSpace;

        //  Normal vector of intersection triangle, always in object space. Use only if intersection with object.
        public Vector3 NormalInObjectSpace;

        //  Line used to get intersection, transformed to object space. For voxels it is also in world space, but for objects, use GetLineInWorldSpace()
        public LineD InputLineInObjectSpace;

        public MyIntersectionResultLineTriangleEx(MyIntersectionResultLineTriangle triangle, IMyEntity entity, ref LineD line)
        {
            Triangle = triangle;
            Entity = entity;
            InputLineInObjectSpace = line;

            NormalInObjectSpace = MyUtils.GetNormalVectorFromTriangle(ref Triangle.InputTriangle);
            IntersectionPointInObjectSpace = line.From + line.Direction * Triangle.Distance;

            if (Entity is IMyVoxelBase)
            {
                IntersectionPointInWorldSpace = (Vector3D)IntersectionPointInObjectSpace;
                NormalInWorldSpace = NormalInObjectSpace;

                //  This will move intersection point from world space into voxel map's object space
                IntersectionPointInObjectSpace = IntersectionPointInObjectSpace - ((IMyVoxelBase)Entity).PositionLeftBottomCorner;
            }
            else
            {
                var worldMatrix = Entity.WorldMatrix;
                NormalInWorldSpace = (Vector3)MyUtils.GetTransformNormalNormalized((Vector3D)NormalInObjectSpace, ref worldMatrix);
                IntersectionPointInWorldSpace = Vector3D.Transform((Vector3D)IntersectionPointInObjectSpace, ref worldMatrix);
            }
        }

        public MyIntersectionResultLineTriangleEx(MyIntersectionResultLineTriangle triangle, IMyEntity entity, ref LineD line, Vector3D intersectionPointInWorldSpace, Vector3 normalInWorldSpace)
        {
            Triangle = triangle;
            Entity = entity;
            InputLineInObjectSpace = line;

            NormalInObjectSpace = NormalInWorldSpace = normalInWorldSpace;
            IntersectionPointInWorldSpace = intersectionPointInWorldSpace;
            IntersectionPointInObjectSpace = (Vector3)IntersectionPointInWorldSpace;
        }

        //  Find and return closer intersection of these two. If intersection is null then it's not really an intersection.
        public static MyIntersectionResultLineTriangleEx? GetCloserIntersection(ref MyIntersectionResultLineTriangleEx? a, ref MyIntersectionResultLineTriangleEx? b)
        {
            if (((a == null) && (b != null)) ||
                ((a != null) && (b != null) && (b.Value.Triangle.Distance < a.Value.Triangle.Distance)))
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

        //  Find if distance between two intersections is less than "tolerance distance".
        public static bool IsDistanceLessThanTolerance(ref MyIntersectionResultLineTriangleEx? a, ref MyIntersectionResultLineTriangleEx? b,
            float distanceTolerance)
        {
            if (((a == null) && (b != null)) ||
                ((a != null) && (b != null) && (Math.Abs(b.Value.Triangle.Distance - a.Value.Triangle.Distance) <= distanceTolerance)))
            {
                return true;
            }
            else
            {
                //  This will be returned also when ((a == null) && (b == null))
                return false;
            }
        }
    }
}
