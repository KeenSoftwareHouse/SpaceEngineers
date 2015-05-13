using System;
using VRageMath;
using VRage.Utils;
using System.Runtime.InteropServices;

//  Place for declaration of various structs, classes and enums

namespace VRageRender.Utils
{
    //  2D rectangle defined by floats (that is his difference to XNA's Rectangle who uses ints)
    struct MyRectangle2D
    {
        public Vector2 LeftTop;     //  Coordinate of left/top point
        public Vector2 Size;        //  Width and height

        public MyRectangle2D(Vector2 leftTop, Vector2 size)
        {
            LeftTop = leftTop;
            Size = size;
        }
    }

    enum MySpherePlaneIntersectionEnum : byte
    {
        BEHIND,
        FRONT,
        INTERSECTS
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

    internal struct MyPlane
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

        public MyPlane(ref MyTriangle_Vertexes triangle)
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MyVoxelTriangle
    {
        public short VertexIndex0;
        public short VertexIndex1;
        public short VertexIndex2;

        public short this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return VertexIndex0;
                    case 1:
                        return VertexIndex1;
                    case 2:
                        return VertexIndex2;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            set
            {
                switch (i)
                {
                    case 0:
                        VertexIndex0 = value;
                        break;
                    case 1:
                        VertexIndex1 = value;
                        break;
                    case 2:
                        VertexIndex2 = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    //  Structure for holding voxel triangleVertexes used in JLX's collision-detection
    // size is 100 B
    struct MyColDetVoxelTriangle
    {
        public Vector3 Vertex0;
        public Vector3 Vertex1;
        public Vector3 Vertex2;
        public VRageMath.Plane Plane;
        Vector3 m_origin;
        Vector3 m_edge0;
        Vector3 m_edge1;
        Vector3 m_normal;

        //  Points specified so that pt1-pt0 is edge0 and p2-pt0 is edge1
        public void Update(ref Vector3 vertex0, ref Vector3 vertex1, ref Vector3 vertex2)
        {
            Vertex0 = vertex0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;

            Plane = new VRageMath.Plane(Vertex0, Vertex1, Vertex2);

            m_origin = vertex0;
            m_edge0 = vertex1 - vertex0;
            m_edge1 = vertex2 - vertex0;

            m_normal = Vector3.Cross(m_edge0, m_edge1);
            m_normal.Normalize();
        }

        //  Same numbering as in the constructor
        public Vector3 GetPoint(int i)
        {
            if (i == 1)
                return Vertex1;

            if (i == 2)
                return Vertex2;

            return Vertex0;
        }

        //  Same numbering as in the constructor
        public void GetPoint(int i, out Vector3 point)
        {
            if (i == 1)
            {
                point = Vertex1;
                return;
            }

            if (i == 2)
            {
                point = Vertex2;
                return;
            }

            point = Vertex0;
        }

        //  Returns the point parameterised by t0 and t1
        public Vector3 GetPoint(float t0, float t1)
        {
            return m_origin + t0 * m_edge0 + t1 * m_edge1;
        }

        /*
        //  Gets the minimum and maximum extents of the triangleVertexes along the axis
        public void GetSpan(out float min, out float max, Vector3 axis)
        {
            float d0 = Vector3.Dot(GetPoint(0), axis);
            float d1 = Vector3.Dot(GetPoint(1), axis);
            float d2 = Vector3.Dot(GetPoint(2), axis);

            min = MyPhysicsUtils.Min(d0, d1, d2);
            max = MyPhysicsUtils.Max(d0, d1, d2);
        }  */

        public Vector3 Centre
        {
            get { return m_origin + 0.333333333333f * (m_edge0 + m_edge1); }
        }

        public Vector3 Origin
        {
            get { return m_origin; }
            set { m_origin = value; }
        }

        public Vector3 Edge0
        {
            get { return m_edge0; }
            set { m_edge0 = value; }
        }

        public Vector3 Edge1
        {
            get { return m_edge1; }
            set { m_edge1 = value; }
        }

        //  Edge2 goes from pt1 to pt2
        public Vector3 Edge2
        {
            get { return m_edge1 - m_edge0; }
        }

        //  Gets the triangleVertexes normal. If degenerate it will be normalised, but
        //  the direction may be wrong!
        public Vector3 Normal
        {
            get { return m_normal; }
        }
    }
}
