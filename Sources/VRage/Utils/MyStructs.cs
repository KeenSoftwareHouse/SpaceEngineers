using System;
using System.Runtime.InteropServices;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;

//  Place for declaration of various structs, classes and enums

namespace VRage.Utils
{
    //  2D rectangle defined by floats (that is his difference to XNA's Rectangle who uses ints)
    public struct MyRectangle2D
    {
        public Vector2 LeftTop;     //  Coordinate of left/top point
        public Vector2 Size;        //  Width and height

        public MyRectangle2D(Vector2 leftTop, Vector2 size)
        {
            LeftTop = leftTop;
            Size = size;
        }
    }


    public class MyAtlasTextureCoordinate
    {
        public Vector2 Offset;
        public Vector2 Size;

        public MyAtlasTextureCoordinate(Vector2 offset, Vector2 size)
        {
            Offset = offset;
            Size = size;
        }
    }

    struct MyBox
    {
        public Vector3 Center;
        public Vector3 Size;

        public MyBox(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = size;
        }
    }

    public struct MyTriangle_Vertex_Normal
    {
        public MyTriangle_Vertices Vertexes;
        public Vector3 Normal;
    }

    public struct MyTriangle_Vertex_Normals_Tangents
    {
        public MyTriangle_Vertices Vertices;
        public MyTriangle_Normals Normals;
        public MyTriangle_Normals Tangents;
    }

    public struct MyTriangle_Vertex_Normals
    {
        public MyTriangle_Vertices Vertices;
        public MyTriangle_Normals Normals;
    }


    public enum MySpherePlaneIntersectionEnum : byte
    {
        BEHIND,
        FRONT,
        INTERSECTS
    }

    public struct MyPlane
    {
        public Vector3 Point;           //  Point on a plane
        public Vector3 Normal;          //  Normal vector of a plane

        public MyPlane(Vector3 point, Vector3 normal)
        {
            Point = point;
            Normal = normal;
        }

        public MyPlane(ref Vector3 point, ref Vector3 normal)
        {
            Point = point;
            Normal = normal;
        }

        public MyPlane(ref MyTriangle_Vertices triangle)
        {
            Point = triangle.Vertex0;
            Normal = MyUtils.Normalize(Vector3.Cross((triangle.Vertex1 - triangle.Vertex0), (triangle.Vertex2 - triangle.Vertex0)));
        }

        //	This returns the distance the plane is from the origin (0, 0, 0)
        //	It takes the normal to the plane, along with ANY point that lies on the plane (any corner)
        public float GetPlaneDistance()
        {
            //	Use the plane equation to find the distance (Ax + By + Cz + D = 0)  We want to find D.
            //	So, we come up with D = -(Ax + By + Cz)

            //	Basically, the negated dot product of the normal of the plane and the point. (More about the dot product in another tutorial)
            return -((Normal.X * Point.X) + (Normal.Y * Point.Y) + (Normal.Z * Point.Z));
        }
    }
}
