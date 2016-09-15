using System;
using VRageMath;

namespace VRage.Utils
{
    public static partial class MyUtils
    {
        /// <summary>
        /// Returns intersection point between sphere and its edges. But only if there is intersection between sphere and one of the edges.
        /// If sphere intersects somewhere inside the triangle, this method will not detect it.
        /// </summary>
        public static Vector3? GetEdgeSphereCollision(ref Vector3D sphereCenter, float sphereRadius, ref MyTriangle_Vertices triangle)
        {
            Vector3 intersectionPoint;

            // This returns the closest point on the current edge to the center of the sphere.
            intersectionPoint = GetClosestPointOnLine(ref triangle.Vertex0, ref triangle.Vertex1, ref sphereCenter);

            // Now, we want to calculate the distance between the closest point and the center
            float distance1 = Vector3.Distance(intersectionPoint, sphereCenter);

            // If the distance is less than the radius, there must be a collision so return true
            if (distance1 < sphereRadius)
            {
                return intersectionPoint;
            }

            // This returns the closest point on the current edge to the center of the sphere.
            intersectionPoint = GetClosestPointOnLine(ref triangle.Vertex1, ref triangle.Vertex2, ref sphereCenter);

            // Now, we want to calculate the distance between the closest point and the center
            float distance2 = Vector3.Distance(intersectionPoint, sphereCenter);

            // If the distance is less than the radius, there must be a collision so return true
            if (distance2 < sphereRadius)
            {
                return intersectionPoint;
            }

            // This returns the closest point on the current edge to the center of the sphere.
            intersectionPoint = GetClosestPointOnLine(ref triangle.Vertex2, ref triangle.Vertex0, ref sphereCenter);

            // Now, we want to calculate the distance between the closest point and the center
            float distance3 = Vector3.Distance(intersectionPoint, sphereCenter);

            // If the distance is less than the radius, there must be a collision so return true
            if (distance3 < sphereRadius)
            {
                return intersectionPoint;
            }

            // The was no intersection of the sphere and the edges of the polygon
            return null;
        }
        /// <summary>
        /// Return true if point is inside the triangle.
        /// </summary>
        public static bool GetInsidePolygonForSphereCollision(ref Vector3D point, ref MyTriangle_Vertices triangle)
        {
            const float MATCH_FACTOR = 0.99f;		// Used to cover up the error in floating point
            float angle = 0.0f;						// Initialize the angle

            //	Spocitame uhol medzi bodmi trojuholnika a intersection bodu (ale na vypocet uhlov pouzivame funkciu ktora je
            //	bezpecna aj pre sphere coldet, problem so SafeACos())
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex0 - point, triangle.Vertex1 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex1 - point, triangle.Vertex2 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex2 - point, triangle.Vertex0 - point);	// Find the angle between the 2 vectors and add them all up as we go along

            if (angle >= (MATCH_FACTOR * (2.0 * MathHelper.Pi)))	// If the angle is greater than 2 PI, (360 degrees)
            {
                return true;							// The point is inside of the polygon
            }

            return false;								// If you get here, it obviously wasn't inside the polygon, so Return FALSE
        }
        /// <summary>
        /// Return true if point is inside the triangle.
        /// </summary>
        public static bool GetInsidePolygonForSphereCollision(ref Vector3 point, ref MyTriangle_Vertices triangle)
        {
            const float MATCH_FACTOR = 0.99f;		// Used to cover up the error in floating point
            float angle = 0.0f;						// Initialize the angle

            //	Spocitame uhol medzi bodmi trojuholnika a intersection bodu (ale na vypocet uhlov pouzivame funkciu ktora je
            //	bezpecna aj pre sphere coldet, problem so SafeACos())
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex0 - point, triangle.Vertex1 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex1 - point, triangle.Vertex2 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex2 - point, triangle.Vertex0 - point);	// Find the angle between the 2 vectors and add them all up as we go along

            if (angle >= (MATCH_FACTOR * (2.0 * MathHelper.Pi)))	// If the angle is greater than 2 PI, (360 degrees)
            {
                return true;							// The point is inside of the polygon
            }

            return false;								// If you get here, it obviously wasn't inside the polygon, so Return FALSE
        }
        public static float GetAngleBetweenVectorsForSphereCollision(Vector3 vector1, Vector3 vector2)
        {
            //	Get the dot product of the vectors
            float dotProduct = Vector3.Dot(vector1, vector2);

            //	Get the product of both of the vectors magnitudes
            float vectorsMagnitude = vector1.Length() * vector2.Length();

            float angle = (float)Math.Acos(dotProduct / vectorsMagnitude);

            //	Ak bol parameter pre acos() nie v ramci intervalo -1 az +1, tak to je zle, a funkcia musi vratit 0
            if (float.IsNaN(angle) == true)
            {
                return 0.0f;
            }

            //	Vysledny uhol
            return angle;
        }
        /// <summary>
        /// Checks whether a ray intersects a triangleVertexes. This uses the algorithm
        /// developed by Tomas Moller and Ben Trumbore, which was published in the
        /// Journal of Graphics Tools, pitch 2, "Fast, Minimum Storage Ray-Triangle
        /// Intersection".
        ///
        /// This method is implemented using the pass-by-reference versions of the
        /// XNA math functions. Using these overloads is generally not recommended,
        /// because they make the code less readable than the normal pass-by-value
        /// versions. This method can be called very frequently in a tight inner loop,
        /// however, so in this particular case the performance benefits from passing
        /// everything by reference outweigh the loss of readability.
        /// </summary>
        public static float? GetLineTriangleIntersection(ref Line line, ref MyTriangle_Vertices triangle)
        {
            // Compute vectors along two edges of the triangleVertexes.
            Vector3 edge1, edge2;

            Vector3.Subtract(ref triangle.Vertex1, ref triangle.Vertex0, out edge1);
            Vector3.Subtract(ref triangle.Vertex2, ref triangle.Vertex0, out edge2);

            // Compute the determinant.
            Vector3 directionCrossEdge2;
            Vector3.Cross(ref line.Direction, ref edge2, out directionCrossEdge2);

            float determinant;
            Vector3.Dot(ref edge1, ref directionCrossEdge2, out determinant);

            // If the ray is parallel to the triangleVertexes plane, there is no collision.
            if (determinant > -float.Epsilon && determinant < float.Epsilon)
            {
                return null;
            }

            float inverseDeterminant = 1.0f / determinant;

            // Calculate the U parameter of the intersection point.
            Vector3 distanceVector;
            Vector3.Subtract(ref line.From, ref triangle.Vertex0, out distanceVector);

            float triangleU;
            Vector3.Dot(ref distanceVector, ref directionCrossEdge2, out triangleU);
            triangleU *= inverseDeterminant;

            // Make sure it is inside the triangleVertexes.
            if (triangleU < 0 || triangleU > 1)
            {
                return null;
            }

            // Calculate the V parameter of the intersection point.
            Vector3 distanceCrossEdge1;
            Vector3.Cross(ref distanceVector, ref edge1, out distanceCrossEdge1);

            float triangleV;
            Vector3.Dot(ref line.Direction, ref distanceCrossEdge1, out triangleV);
            triangleV *= inverseDeterminant;

            // Make sure it is inside the triangleVertexes.
            if (triangleV < 0 || triangleU + triangleV > 1)
            {
                return null;
            }

            // Compute the distance along the ray to the triangleVertexes.
            float rayDistance;
            Vector3.Dot(ref edge2, ref distanceCrossEdge1, out rayDistance);
            rayDistance *= inverseDeterminant;

            // Is the triangleVertexes behind the ray origin?
            if (rayDistance < 0)
            {
                return null;
            }

            //  Does the intersection point lie on the line (ray hasn't end, but line does)
            if (rayDistance > line.Length) return null;

            return rayDistance;
        }
        public static Vector3 GetNormalVectorFromTriangle(ref MyTriangle_Vertices inputTriangle)
        {
            //return MyVRageUtils.Normalize(Vector3.Cross(inputTriangle.Vertex2 - inputTriangle.Vertex0, inputTriangle.Vertex1 - inputTriangle.Vertex0));
            return Vector3.Normalize(Vector3.Cross(inputTriangle.Vertex2 - inputTriangle.Vertex0, inputTriangle.Vertex1 - inputTriangle.Vertex0));
        }
        /// <summary>
        /// Method returns intersection point between sphere and triangle (which is defined by vertexes and plane).
        /// If no intersection found, method returns null.
        /// See below how intersection point can be calculated, because it's not so easy - for example sphere vs. triangle will 
        /// hardly generate just intersection point... more like intersection area or something.
        /// </summary>
        public static Vector3? GetSphereTriangleIntersection(ref BoundingSphereD sphere, ref MyPlane trianglePlane, ref MyTriangle_Vertices triangle)
        {
            //	Vzdialenost gule od roviny trojuholnika
            float distance;

            //	Zistim, ci sa gula nachadza pred alebo za rovinou trojuholnika, alebo ju presekava
            MySpherePlaneIntersectionEnum spherePlaneIntersection = GetSpherePlaneIntersection(ref sphere, ref trianglePlane, out distance);

            //	Ak gula presekava rovinu, tak hladam pseudo-priesecnik
            if (spherePlaneIntersection == MySpherePlaneIntersectionEnum.INTERSECTS)
            {
                //	Offset ktory pomoze vypocitat suradnicu stredu gule premietaneho na rovinu trojuholnika
                Vector3 offset = trianglePlane.Normal * distance;

                //	Priesecnik na rovine trojuholnika, je to premietnuty stred gule na rovine trojuholnika
                Vector3 intersectionPoint;
                intersectionPoint.X = (float)(sphere.Center.X - offset.X);
                intersectionPoint.Y = (float)(sphere.Center.Y - offset.Y);
                intersectionPoint.Z = (float)(sphere.Center.Z - offset.Z);

                if (GetInsidePolygonForSphereCollision(ref intersectionPoint, ref triangle))		//	Ak priesecnik nachadza v trojuholniku
                {
                    //	Toto je pripad, ked sa podarilo premietnut stred gule na rovinu trojuholnika a tento priesecnik sa
                    //	nachadza vnutri trojuholnika (tzn. sedia uhly)
                    return intersectionPoint;
                }
                else													//	Ak sa priesecnik nenachadza v trojuholniku, este stale sa moze nachadzat na hrane trojuholnika
                {
                    Vector3? edgeIntersection = GetEdgeSphereCollision(ref sphere.Center, (float)sphere.Radius / 1.0f, ref triangle);
                    if (edgeIntersection != null)
                    {
                        //	Toto je pripad, ked sa priemietnuty stred gule nachadza mimo trojuholnika, ale intersection gule a trojuholnika tam
                        //	je, pretoze gula presekava jednu z hran trojuholnika. Takze vratim suradnice priesecnika na jednej z hran.
                        return edgeIntersection.Value;
                    }
                }
            }

            //	Sphere doesn't collide with any triangle
            return null;
        }
        /// <summary>
        /// Method returns intersection point between sphere and triangle (which is defined by vertexes and plane).
        /// If no intersection found, method returns null.
        /// See below how intersection point can be calculated, because it's not so easy - for example sphere vs. triangle will 
        /// hardly generate just intersection point... more like intersection area or something.
        /// </summary>
        public static Vector3? GetSphereTriangleIntersection(ref BoundingSphereD sphere, ref PlaneD trianglePlane, ref MyTriangle_Vertices triangle)
        {
            //	Vzdialenost gule od roviny trojuholnika
            double distance;

            //	Zistim, ci sa gula nachadza pred alebo za rovinou trojuholnika, alebo ju presekava
            MySpherePlaneIntersectionEnum spherePlaneIntersection = GetSpherePlaneIntersection(ref sphere, ref trianglePlane, out distance);

            //	Ak gula presekava rovinu, tak hladam pseudo-priesecnik
            if (spherePlaneIntersection == MySpherePlaneIntersectionEnum.INTERSECTS)
            {
                //	Offset ktory pomoze vypocitat suradnicu stredu gule premietaneho na rovinu trojuholnika
                Vector3D offset = trianglePlane.Normal * distance;

                //	Priesecnik na rovine trojuholnika, je to premietnuty stred gule na rovine trojuholnika
                Vector3D intersectionPoint;
                intersectionPoint.X = sphere.Center.X - offset.X;
                intersectionPoint.Y = sphere.Center.Y - offset.Y;
                intersectionPoint.Z = sphere.Center.Z - offset.Z;

                if (GetInsidePolygonForSphereCollision(ref intersectionPoint, ref triangle))		//	Ak priesecnik nachadza v trojuholniku
                {
                    //	Toto je pripad, ked sa podarilo premietnut stred gule na rovinu trojuholnika a tento priesecnik sa
                    //	nachadza vnutri trojuholnika (tzn. sedia uhly)
                    return (Vector3)intersectionPoint;
                }
                else													//	Ak sa priesecnik nenachadza v trojuholniku, este stale sa moze nachadzat na hrane trojuholnika
                {
                    Vector3? edgeIntersection = GetEdgeSphereCollision(ref sphere.Center, (float)sphere.Radius / 1.0f, ref triangle);
                    if (edgeIntersection != null)
                    {
                        //	Toto je pripad, ked sa priemietnuty stred gule nachadza mimo trojuholnika, ale intersection gule a trojuholnika tam
                        //	je, pretoze gula presekava jednu z hran trojuholnika. Takze vratim suradnice priesecnika na jednej z hran.
                        return edgeIntersection.Value;
                    }
                }
            }

            //	Sphere doesn't collide with any triangle
            return null;
        }
    }
}
