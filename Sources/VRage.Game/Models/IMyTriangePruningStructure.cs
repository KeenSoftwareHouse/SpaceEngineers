using System.Collections.Generic;
using VRageMath;
using VRage.Utils;
using VRage.ModAPI;
using VRage.Game.Components;

namespace VRage.Game.Models
{
    public interface IMyTriangePruningStructure
    {
        MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(IMyEntity entity, ref LineD line, IntersectionFlags flags = IntersectionFlags.DIRECT_TRIANGLES);
        MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(IMyEntity entity, ref LineD line, ref MatrixD customInvMatrix, IntersectionFlags flags = IntersectionFlags.DIRECT_TRIANGLES);

        void GetTrianglesIntersectingLine(IMyEntity entity, ref LineD line, ref MatrixD customInvMatrix, IntersectionFlags flags, List<MyIntersectionResultLineTriangleEx> result);
        void GetTrianglesIntersectingLine(IMyEntity entity, ref LineD line, IntersectionFlags flags, List<MyIntersectionResultLineTriangleEx> result);

        //  Return list of triangles intersecting specified sphere. Angle between every triangleVertexes normal vector and 'referenceNormalVector'
        //  is calculated, and if more than 'maxAngle', we ignore such triangleVertexes.
        //  Triangles are returned in 'retTriangles', and this list must be preallocated!
        //  IMPORTANT: Sphere must be in model space, so don't transform it!
        void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, Vector3? referenceNormalVector, float? maxAngle, List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles);

        //  Return true if object intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        bool GetIntersectionWithSphere(IMyEntity physObject, ref BoundingSphereD sphere);

        //  Return list of triangles intersecting specified sphere. Angle between every triangleVertexes normal vector and 'referenceNormalVector'
        //  is calculated, and if more than 'maxAngle', we ignore such triangleVertexes.
        //  Triangles are returned in 'retTriangles', and this list must be preallocated!
        //  IMPORTANT: Sphere must be in model space, so don't transform it!
        void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, List<MyTriangle_Vertex_Normal> retTriangles, int maxNeighbourTriangles);


        void GetTrianglesIntersectingAABB(ref BoundingBoxD sphere, List<MyTriangle_Vertex_Normal> retTriangles, int maxNeighbourTriangles);

        void Close();

        int Size { get; }
    }
}
