using System;
using System.Collections.Generic;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines an axis-aligned box-shaped 3D volume.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct BoundingBox : IEquatable<BoundingBox>
    {
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingBox.
        /// </summary>
        public const int CornerCount = 8;
        /// <summary>
        /// The minimum point the BoundingBox contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector3 Min;
        /// <summary>
        /// The maximum point the BoundingBox contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector3 Max;

        /// <summary>
        /// Creates an instance of BoundingBox.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBox includes.</param><param name="max">The maximum point the BoundingBox includes.</param>
        public BoundingBox(Vector3 min, Vector3 max)
        {
            this.Min = min;
            this.Max = max;
        }

        /// <summary>
        /// Creates an instance of BoundingBox from BoundingBoxD (helper for transformed BBs)
        /// </summary>
        /// <param name="bbd"></param>
        public BoundingBox(BoundingBoxD bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }

        public BoundingBox(BoundingBoxI bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }

        public BoxCornerEnumerator Corners
        {
            get { return new BoxCornerEnumerator(Min, Max); }
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox are equal.
        /// </summary>
        /// <param name="a">BoundingBox to compare.</param><param name="b">BoundingBox to compare.</param>
        public static bool operator ==(BoundingBox a, BoundingBox b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox are not equal.
        /// </summary>
        /// <param name="a">The object to the left of the inequality operator.</param><param name="b">The object to the right of the inequality operator.</param>
        public static bool operator !=(BoundingBox a, BoundingBox b)
        {
            if (!(a.Min != b.Min))
                return a.Max != b.Max;
            else
                return true;
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingBox. ALLOCATION!
        /// </summary>
        public Vector3[] GetCorners()
        {
            return new Vector3[8]
            {
                new Vector3(this.Min.X, this.Max.Y, this.Max.Z),
                new Vector3(this.Max.X, this.Max.Y, this.Max.Z),
                new Vector3(this.Max.X, this.Min.Y, this.Max.Z),
                new Vector3(this.Min.X, this.Min.Y, this.Max.Z),
                new Vector3(this.Min.X, this.Max.Y, this.Min.Z),
                new Vector3(this.Max.X, this.Max.Y, this.Min.Z),
                new Vector3(this.Max.X, this.Min.Y, this.Min.Z),
                new Vector3(this.Min.X, this.Min.Y, this.Min.Z)
            };
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3 points where the corners of the BoundingBox are written.</param>
        public void GetCorners(Vector3[] corners)
        {
            corners[0].X = this.Min.X;
            corners[0].Y = this.Max.Y;
            corners[0].Z = this.Max.Z;
            corners[1].X = this.Max.X;
            corners[1].Y = this.Max.Y;
            corners[1].Z = this.Max.Z;
            corners[2].X = this.Max.X;
            corners[2].Y = this.Min.Y;
            corners[2].Z = this.Max.Z;
            corners[3].X = this.Min.X;
            corners[3].Y = this.Min.Y;
            corners[3].Z = this.Max.Z;
            corners[4].X = this.Min.X;
            corners[4].Y = this.Max.Y;
            corners[4].Z = this.Min.Z;
            corners[5].X = this.Max.X;
            corners[5].Y = this.Max.Y;
            corners[5].Z = this.Min.Z;
            corners[6].X = this.Max.X;
            corners[6].Y = this.Min.Y;
            corners[6].Z = this.Min.Z;
            corners[7].X = this.Min.X;
            corners[7].Y = this.Min.Y;
            corners[7].Z = this.Min.Z;
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3 points where the corners of the BoundingBox are written.</param>
		[Unsharper.UnsharperDisableReflection()]
        public unsafe void GetCornersUnsafe(Vector3* corners)
        {
            corners[0].X = this.Min.X;
            corners[0].Y = this.Max.Y;
            corners[0].Z = this.Max.Z;
            corners[1].X = this.Max.X;
            corners[1].Y = this.Max.Y;
            corners[1].Z = this.Max.Z;
            corners[2].X = this.Max.X;
            corners[2].Y = this.Min.Y;
            corners[2].Z = this.Max.Z;
            corners[3].X = this.Min.X;
            corners[3].Y = this.Min.Y;
            corners[3].Z = this.Max.Z;
            corners[4].X = this.Min.X;
            corners[4].Y = this.Max.Y;
            corners[4].Z = this.Min.Z;
            corners[5].X = this.Max.X;
            corners[5].Y = this.Max.Y;
            corners[5].Z = this.Min.Z;
            corners[6].X = this.Max.X;
            corners[6].Y = this.Min.Y;
            corners[6].Z = this.Min.Z;
            corners[7].X = this.Min.X;
            corners[7].Y = this.Min.Y;
            corners[7].Z = this.Min.Z;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox are equal.
        /// </summary>
        /// <param name="other">The BoundingBox to compare with the current BoundingBox.</param>
        public bool Equals(BoundingBox other)
        {
            if (this.Min == other.Min)
                return this.Max == other.Max;
            else
                return false;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox are equal.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingBox.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is BoundingBox)
                flag = this.Equals((BoundingBox)obj);
            return flag;
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Min.GetHashCode() + this.Max.GetHashCode();
        }

        /// <summary>
        /// Returns a String that represents the current BoundingBox.
        /// </summary>
        public override string ToString()
        {
            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, "{{Min:{0} Max:{1}}}", new object[2]
      {
        (object) this.Min.ToString(),
        (object) this.Max.ToString()
      });
        }

        /// <summary>
        /// Creates the smallest BoundingBox that contains the two specified BoundingBox instances.
        /// </summary>
        /// <param name="original">One of the BoundingBoxs to contain.</param><param name="additional">One of the BoundingBoxs to contain.</param>
        public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional)
        {
            BoundingBox boundingBox;
            Vector3.Min(ref original.Min, ref additional.Min, out boundingBox.Min);
            Vector3.Max(ref original.Max, ref additional.Max, out boundingBox.Max);
            return boundingBox;
        }

        /// <summary>
        /// Creates the smallest BoundingBox that contains the two specified BoundingBox instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox instances to contain.</param><param name="additional">One of the BoundingBox instances to contain.</param><param name="result">[OutAttribute] The created BoundingBox.</param>
        public static void CreateMerged(ref BoundingBox original, ref BoundingBox additional, out BoundingBox result)
        {
            Vector3 result1;
            Vector3.Min(ref original.Min, ref additional.Min, out result1);
            Vector3 result2;
            Vector3.Max(ref original.Max, ref additional.Max, out result2);
            result.Min = result1;
            result.Max = result2;
        }

        /// <summary>
        /// Creates the smallest BoundingBox that will contain the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to contain.</param>
        public static BoundingBox CreateFromSphere(BoundingSphere sphere)
        {
            BoundingBox boundingBox;
            boundingBox.Min.X = sphere.Center.X - sphere.Radius;
            boundingBox.Min.Y = sphere.Center.Y - sphere.Radius;
            boundingBox.Min.Z = sphere.Center.Z - sphere.Radius;
            boundingBox.Max.X = sphere.Center.X + sphere.Radius;
            boundingBox.Max.Y = sphere.Center.Y + sphere.Radius;
            boundingBox.Max.Z = sphere.Center.Z + sphere.Radius;
            return boundingBox;
        }

        /// <summary>
        /// Creates the smallest BoundingBox that will contain the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to contain.</param><param name="result">[OutAttribute] The created BoundingBox.</param>
        public static void CreateFromSphere(ref BoundingSphere sphere, out BoundingBox result)
        {
            result.Min.X = sphere.Center.X - sphere.Radius;
            result.Min.Y = sphere.Center.Y - sphere.Radius;
            result.Min.Z = sphere.Center.Z - sphere.Radius;
            result.Max.X = sphere.Center.X + sphere.Radius;
            result.Max.Y = sphere.Center.Y + sphere.Radius;
            result.Max.Z = sphere.Center.Z + sphere.Radius;
        }

        /// <summary>
        /// Creates the smallest BoundingBox that will contain a group of points.
        /// </summary>
        /// <param name="points">A list of points the BoundingBox should contain.</param>
        public static BoundingBox CreateFromPoints(IEnumerable<Vector3> points)
        {
            if (points == null)
                throw new ArgumentNullException();
            bool flag = false;
            Vector3 result1 = new Vector3(float.MaxValue);
            Vector3 result2 = new Vector3(float.MinValue);
            foreach (Vector3 vector3 in points)
            {
                Vector3 vec3 = vector3;
                Vector3.Min(ref result1, ref vec3, out result1);
                Vector3.Max(ref result2, ref vec3, out result2);
                flag = true;
            }
            if (!flag)
                throw new ArgumentException();
            else
                return new BoundingBox(result1, result2);
        }

        public static BoundingBox CreateFromHalfExtent(Vector3 center, float halfExtent)
        {
            return CreateFromHalfExtent(center, new Vector3(halfExtent));
        }

        public static BoundingBox CreateFromHalfExtent(Vector3 center, Vector3 halfExtent)
        {
            return new BoundingBox(center - halfExtent, center + halfExtent);
        }

        /// <summary>
        /// Returns bounding box which is intersection of this and box
        /// It's called 'Prunik'
        /// Result is invalid box when there's no intersection (Min > Max)
        /// </summary>
        public BoundingBox Intersect(BoundingBox box)
        {
            BoundingBox result;
            result.Min.X = Math.Max(this.Min.X, box.Min.X);
            result.Min.Y = Math.Max(this.Min.Y, box.Min.Y);
            result.Min.Z = Math.Max(this.Min.Z, box.Min.Z);
            result.Max.X = Math.Min(this.Max.X, box.Max.X);
            result.Max.Y = Math.Min(this.Max.Y, box.Max.Y);
            result.Max.Z = Math.Min(this.Max.Z, box.Max.Z);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects another BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param>
        public bool Intersects(BoundingBox box)
        {
            return Intersects(ref box);
        }

        public bool Intersects(ref BoundingBox box)
        {
            return (double)this.Max.X >= (double)box.Min.X && (double)this.Min.X <= (double)box.Max.X && ((double)this.Max.Y >= (double)box.Min.Y && (double)this.Min.Y <= (double)box.Max.Y) && ((double)this.Max.Z >= (double)box.Min.Z && (double)this.Min.Z <= (double)box.Max.Z);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects another BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBox instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox box, out bool result)
        {
            result = false;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return;
            result = true;
        }

        public bool IntersectsTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            return IntersectsTriangle(ref v0, ref v1, ref v2);
        }

        public bool IntersectsTriangle(ref Vector3 v0, ref Vector3 v1, ref Vector3 v2)
        {
            // This code is based on: Akenine-Moeller, Thomas - "Fast 3D Triangle-Box Overlap Testing"

            // Test 1) - Separation of triangle and BB by the bounding box's 6 planes
            Vector3 min, max;
            Vector3.Min(ref v0, ref v1, out min);
            Vector3.Min(ref min, ref v2, out min);
            Vector3.Max(ref v0, ref v1, out max);
            Vector3.Max(ref max, ref v2, out max);

            if (min.X > Max.X) return false;
            if (max.X < Min.X) return false;
            if (min.Y > Max.Y) return false;
            if (max.Y < Min.Y) return false;
            if (min.Z > Max.Z) return false;
            if (max.Z < Min.Z) return false;

            // Test 2) - Separation by the triangle's plane
            Vector3 f0 = v1 - v0;
            Vector3 f1 = v2 - v1;
            Vector3 triN; Vector3.Cross(ref f0, ref f1, out triN);
            float d; Vector3.Dot(ref v0, ref triN, out d);

            // The triangle's plane. It does not have to be normalized
            Plane triPlane = new Plane(triN, -d);

            PlaneIntersectionType intersection;
            Intersects(ref triPlane, out intersection);
            if (intersection == PlaneIntersectionType.Back) return false;
            if (intersection == PlaneIntersectionType.Front) return false;

            // Test 3) - Separation by planes that are perpendicular to coordinate axes e0, e1, e2 and triangle edges f0, f1, f2
            Vector3 center = Center;
            BoundingBox tmpBox = new BoundingBox(Min - center, Max - center);
            Vector3 originHalf = tmpBox.HalfExtents;
            Vector3 f2 = v0 - v2;

            Vector3 v0sh = v0 - center;
            Vector3 v1sh = v1 - center;
            Vector3 v2sh = v2 - center;

            float boxR, p0, p1, p2;

            // Does a plane that has axis e0 x f0 separate the triangle and BB?
            boxR = originHalf.Y * Math.Abs(f0.Z) + originHalf.Z * Math.Abs(f0.Y);  // "Radius" of the BB, if moved to the origin
            p0 = v0sh.Z * v1sh.Y - v0sh.Y * v1sh.Z;                                // Projection of v0sh and also v1sh (axis is perpendicular on f0 = v1sh - v0sh) onto the axis
            p2 = v2sh.Z * f0.Y - v2sh.Y * f0.Z;                                    // Projection of v2sh on the axis
            if (Math.Min(p0, p2) > boxR || Math.Max(p0, p2) < -boxR) return false; // Now we can test projection of the triangle against the projection of the BB (which is (-boxR, +boxR))

            // Now for the remaining 8 combinations...:
            // e1 x f0
            boxR = originHalf.X * Math.Abs(f0.Z) + originHalf.Z * Math.Abs(f0.X);
            p0 = v0sh.X * v1sh.Z - v0sh.Z * v1sh.X;
            p2 = v2sh.X * f0.Z - v2sh.Z * f0.X;
            if (Math.Min(p0, p2) > boxR || Math.Max(p0, p2) < -boxR) return false;

            // e2 x f0
            boxR = originHalf.X * Math.Abs(f0.Y) + originHalf.Y * Math.Abs(f0.X);
            p0 = v0sh.Y * v1sh.X - v0sh.X * v1sh.Y;
            p2 = v2sh.Y * f0.X - v2sh.X * f0.Y;
            if (Math.Min(p0, p2) > boxR || Math.Max(p0, p2) < -boxR) return false;

            // e0 x f1
            boxR = originHalf.Y * Math.Abs(f1.Z) + originHalf.Z * Math.Abs(f1.Y);
            p1 = v1sh.Z * v2sh.Y - v1sh.Y * v2sh.Z;
            p0 = v0sh.Z * f1.Y - v0sh.Y * f1.Z;
            if (Math.Min(p1, p0) > boxR || Math.Max(p1, p0) < -boxR) return false;

            // e1 x f1
            boxR = originHalf.X * Math.Abs(f1.Z) + originHalf.Z * Math.Abs(f1.X);
            p1 = v1sh.X * v2sh.Z - v1sh.Z * v2sh.X;
            p0 = v0sh.X * f1.Z - v0sh.Z * f1.X;
            if (Math.Min(p1, p0) > boxR || Math.Max(p1, p0) < -boxR) return false;

            // e2 x f1
            boxR = originHalf.X * Math.Abs(f1.Y) + originHalf.Y * Math.Abs(f1.X);
            p1 = v1sh.Y * v2sh.X - v1sh.X * v2sh.Y;
            p0 = v0sh.Y * f1.X - v0sh.X * f1.Y;
            if (Math.Min(p1, p0) > boxR || Math.Max(p1, p0) < -boxR) return false;

            // e0 x f2
            boxR = originHalf.Y * Math.Abs(f2.Z) + originHalf.Z * Math.Abs(f2.Y);
            p2 = v2sh.Z * v0sh.Y - v2sh.Y * v0sh.Z;
            p1 = v1sh.Z * f2.Y - v1sh.Y * f2.Z;
            if (Math.Min(p2, p1) > boxR || Math.Max(p2, p1) < -boxR) return false;

            // e1 x f2
            boxR = originHalf.X * Math.Abs(f2.Z) + originHalf.Z * Math.Abs(f2.X);
            p2 = v2sh.X * v0sh.Z - v2sh.Z * v0sh.X;
            p1 = v1sh.X * f2.Z - v1sh.Z * f2.X;
            if (Math.Min(p2, p1) > boxR || Math.Max(p2, p1) < -boxR) return false;

            // e2 x f2
            boxR = originHalf.X * Math.Abs(f2.Y) + originHalf.Y * Math.Abs(f2.X);
            p2 = v2sh.Y * v0sh.X - v2sh.X * v0sh.Y;
            p1 = v1sh.Y * f2.X - v1sh.X * f2.Y;
            if (Math.Min(p2, p1) > boxR || Math.Max(p2, p1) < -boxR) return false;

            return true;
        }

        /// <summary>
        /// Calculates center
        /// </summary>
        public Vector3 Center
        {
            get { return (Min + Max) * 0.5f; }
        }

        public Vector3 HalfExtents
        {
            get { return (Max - Min) * 0.5f; }
        }

        public Vector3 Extents
        {
            get { return Max - Min; }
        }

        public float Width
        {
            get { return Max.X - Min.X; }
        }

        public float Height
        {
            get { return Max.Y - Min.Y; }
        }

        public float Depth
        {
            get { return Max.Z - Min.Z; }
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with.</param>
        public bool Intersects(BoundingFrustum frustum)
        {
            if ((BoundingFrustum)null == frustum)
                throw new ArgumentNullException("frustum");
            else
                return frustum.Intersects(this);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            Vector3 vector3_1;
            vector3_1.X = (double)plane.Normal.X >= 0.0 ? this.Min.X : this.Max.X;
            vector3_1.Y = (double)plane.Normal.Y >= 0.0 ? this.Min.Y : this.Max.Y;
            vector3_1.Z = (double)plane.Normal.Z >= 0.0 ? this.Min.Z : this.Max.Z;
            Vector3 vector3_2;
            vector3_2.X = (double)plane.Normal.X >= 0.0 ? this.Max.X : this.Min.X;
            vector3_2.Y = (double)plane.Normal.Y >= 0.0 ? this.Max.Y : this.Min.Y;
            vector3_2.Z = (double)plane.Normal.Z >= 0.0 ? this.Max.Z : this.Min.Z;
            if ((double)plane.Normal.X * (double)vector3_1.X + (double)plane.Normal.Y * (double)vector3_1.Y + (double)plane.Normal.Z * (double)vector3_1.Z + (double)plane.D > 0.0)
                return PlaneIntersectionType.Front;
            return (double)plane.Normal.X * (double)vector3_2.X + (double)plane.Normal.Y * (double)vector3_2.Y + (double)plane.Normal.Z * (double)vector3_2.Z + (double)plane.D < 0.0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the BoundingBox intersects the Plane.</param>
        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            Vector3 vector3_1;
            vector3_1.X = (double)plane.Normal.X >= 0.0 ? this.Min.X : this.Max.X;
            vector3_1.Y = (double)plane.Normal.Y >= 0.0 ? this.Min.Y : this.Max.Y;
            vector3_1.Z = (double)plane.Normal.Z >= 0.0 ? this.Min.Z : this.Max.Z;
            Vector3 vector3_2;
            vector3_2.X = (double)plane.Normal.X >= 0.0 ? this.Max.X : this.Min.X;
            vector3_2.Y = (double)plane.Normal.Y >= 0.0 ? this.Max.Y : this.Min.Y;
            vector3_2.Z = (double)plane.Normal.Z >= 0.0 ? this.Max.Z : this.Min.Z;
            if ((double)plane.Normal.X * (double)vector3_1.X + (double)plane.Normal.Y * (double)vector3_1.Y + (double)plane.Normal.Z * (double)vector3_1.Z + (double)plane.D > 0.0)
                result = PlaneIntersectionType.Front;
            else if ((double)plane.Normal.X * (double)vector3_2.X + (double)plane.Normal.Y * (double)vector3_2.Y + (double)plane.Normal.Z * (double)vector3_2.Z + (double)plane.D < 0.0)
                result = PlaneIntersectionType.Back;
            else
                result = PlaneIntersectionType.Intersecting;
        }


        public bool Intersects(Line line, out float distance)
        {
            distance = 0f;
            float? f = Intersects(new Ray(line.From, line.Direction));
            if (!f.HasValue)
                return false;

            if (f.Value < 0)
                return false;

            if (f.Value > line.Length)
                return false;

            distance = f.Value;
            return true;
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param>
        public float? Intersects(Ray ray)
        {
            float num1 = 0.0f;
            float num2 = float.MaxValue;
            if ((double)Math.Abs(ray.Direction.X) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.X < (double)this.Min.X || (double)ray.Position.X > (double)this.Max.X)
                    return new float?();
            }
            else
            {
                float num3 = 1f / ray.Direction.X;
                float num4 = (this.Min.X - ray.Position.X) * num3;
                float num5 = (this.Max.X - ray.Position.X) * num3;
                if ((double)num4 > (double)num5)
                {
                    float num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                num2 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num2)
                    return new float?();
            }
            if ((double)Math.Abs(ray.Direction.Y) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.Y < (double)this.Min.Y || (double)ray.Position.Y > (double)this.Max.Y)
                    return new float?();
            }
            else
            {
                float num3 = 1f / ray.Direction.Y;
                float num4 = (this.Min.Y - ray.Position.Y) * num3;
                float num5 = (this.Max.Y - ray.Position.Y) * num3;
                if ((double)num4 > (double)num5)
                {
                    float num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                num2 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num2)
                    return new float?();
            }
            if ((double)Math.Abs(ray.Direction.Z) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.Z < (double)this.Min.Z || (double)ray.Position.Z > (double)this.Max.Z)
                    return new float?();
            }
            else
            {
                float num3 = 1f / ray.Direction.Z;
                float num4 = (this.Min.Z - ray.Position.Z) * num3;
                float num5 = (this.Max.Z - ray.Position.Z) * num3;
                if ((double)num4 > (double)num5)
                {
                    float num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                float num7 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num7)
                    return new float?();
            }
            return new float?(num1);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param><param name="result">[OutAttribute] Distance at which the ray intersects the BoundingBox, or null if there is no intersection.</param>
        public void Intersects(ref Ray ray, out float? result)
        {
            result = new float?();
            float num1 = 0.0f;
            float num2 = float.MaxValue;
            if ((double)Math.Abs(ray.Direction.X) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.X < (double)this.Min.X || (double)ray.Position.X > (double)this.Max.X)
                    return;
            }
            else
            {
                float num3 = 1f / ray.Direction.X;
                float num4 = (this.Min.X - ray.Position.X) * num3;
                float num5 = (this.Max.X - ray.Position.X) * num3;
                if ((double)num4 > (double)num5)
                {
                    float num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                num2 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num2)
                    return;
            }
            if ((double)Math.Abs(ray.Direction.Y) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.Y < (double)this.Min.Y || (double)ray.Position.Y > (double)this.Max.Y)
                    return;
            }
            else
            {
                float num3 = 1f / ray.Direction.Y;
                float num4 = (this.Min.Y - ray.Position.Y) * num3;
                float num5 = (this.Max.Y - ray.Position.Y) * num3;
                if ((double)num4 > (double)num5)
                {
                    float num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                num2 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num2)
                    return;
            }
            if ((double)Math.Abs(ray.Direction.Z) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.Z < (double)this.Min.Z || (double)ray.Position.Z > (double)this.Max.Z)
                    return;
            }
            else
            {
                float num3 = 1f / ray.Direction.Z;
                float num4 = (this.Min.Z - ray.Position.Z) * num3;
                float num5 = (this.Max.Z - ray.Position.Z) * num3;
                if ((double)num4 > (double)num5)
                {
                    float num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                float num7 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num7)
                    return;
            }
            result = new float?(num1);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        public bool Intersects(BoundingSphere sphere)
        {
            return Intersects(ref sphere);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBox and BoundingSphere intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            Vector3 result1;
            Vector3.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            float result2;
            Vector3.DistanceSquared(ref sphere.Center, ref result1, out result2);
            result = (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        }

        public bool Intersects(ref BoundingSphere sphere)
        {
            Vector3 result1;
            Vector3.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            float result2;
            Vector3.DistanceSquared(ref sphere.Center, ref result1, out result2);
            return (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        }

        public bool Intersects(ref BoundingSphereD sphere)
        {
            Vector3 result1;
            Vector3 center = (Vector3)sphere.Center;
            Vector3.Clamp(ref center, ref this.Min, ref this.Max, out result1);
            float result2;
            Vector3.DistanceSquared(ref center, ref result1, out result2);
            return (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        }

        public float Distance(Vector3 point)
        {
            if (Contains(point) == ContainmentType.Contains)
                return 0f;

            var clamp = Vector3.Clamp(point, Min, Max);
            return Vector3.Distance(clamp, point);
        }

        /// <summary>
        /// Tests whether the BoundingBox contains another BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for overlap.</param>
        public ContainmentType Contains(BoundingBox box)
        {
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return ContainmentType.Disjoint;
            return (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)box.Min.Z || (double)box.Max.Z > (double)this.Max.Z) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            result = ContainmentType.Disjoint;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return;
            result = (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)box.Min.Z || (double)box.Max.Z > (double)this.Max.Z) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to test for overlap.</param>
        public ContainmentType Contains(BoundingFrustum frustum)
        {
            if (!frustum.Intersects(this))
                return ContainmentType.Disjoint;
            foreach (Vector3 point in frustum.cornerArray)
            {
                if (this.Contains(point) == ContainmentType.Disjoint)
                    return ContainmentType.Intersects;
            }
            return ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param>
        public ContainmentType Contains(Vector3 point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        public ContainmentType Contains(Vector3D point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            result = (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param>
        public ContainmentType Contains(BoundingSphere sphere)
        {
            Vector3 result1;
            Vector3.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            float result2;
            Vector3.DistanceSquared(ref sphere.Center, ref result1, out result2);
            float num = sphere.Radius;
            if ((double)result2 > (double)num * (double)num)
                return ContainmentType.Disjoint;
            return (double)this.Min.X + (double)num > (double)sphere.Center.X || (double)sphere.Center.X > (double)this.Max.X - (double)num || ((double)this.Max.X - (double)this.Min.X <= (double)num || (double)this.Min.Y + (double)num > (double)sphere.Center.Y) || ((double)sphere.Center.Y > (double)this.Max.Y - (double)num || (double)this.Max.Y - (double)this.Min.Y <= (double)num || ((double)this.Min.Z + (double)num > (double)sphere.Center.Z || (double)sphere.Center.Z > (double)this.Max.Z - (double)num)) || (double)this.Max.X - (double)this.Min.X <= (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            Vector3 result1;
            Vector3.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            float result2;
            Vector3.DistanceSquared(ref sphere.Center, ref result1, out result2);
            float num = sphere.Radius;
            if ((double)result2 > (double)num * (double)num)
                result = ContainmentType.Disjoint;
            else
                result = (double)this.Min.X + (double)num > (double)sphere.Center.X || (double)sphere.Center.X > (double)this.Max.X - (double)num || ((double)this.Max.X - (double)this.Min.X <= (double)num || (double)this.Min.Y + (double)num > (double)sphere.Center.Y) || ((double)sphere.Center.Y > (double)this.Max.Y - (double)num || (double)this.Max.Y - (double)this.Min.Y <= (double)num || ((double)this.Min.Z + (double)num > (double)sphere.Center.Z || (double)sphere.Center.Z > (double)this.Max.Z - (double)num)) || (double)this.Max.X - (double)this.Min.X <= (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        internal void SupportMapping(ref Vector3 v, out Vector3 result)
        {
            result.X = (double)v.X >= 0.0 ? this.Max.X : this.Min.X;
            result.Y = (double)v.Y >= 0.0 ? this.Max.Y : this.Min.Y;
            result.Z = (double)v.Z >= 0.0 ? this.Max.Z : this.Min.Z;
        }

        /// <summary>
        /// Translate
        /// </summary>
        /// <param name="worldMatrix"></param>
        /// <returns></returns>
        public BoundingBox Translate(Matrix worldMatrix)
        {
            Min += worldMatrix.Translation;
            Max += worldMatrix.Translation;
            return this;
        }


        /// <summary>
        /// Translate
        /// </summary>
        /// <param name="vctTranlsation"></param>
        /// <returns></returns>
        public BoundingBox Translate(Vector3 vctTranlsation)
        {
            Min += vctTranlsation;
            Max += vctTranlsation;
            return this;
        }

        /// <summary>
        /// Size
        /// </summary>
        /// <returns></returns>
        public Vector3 Size
        {
            get
            {
                return Max - Min;
            }
        }

        /// <summary>
        /// Matrix of AABB, respection center and size
        /// </summary>
        public Matrix Matrix
        {
            get
            {
                var center = Center;
                var size = Size;

                Matrix result;
                Matrix.CreateTranslation(ref center, out result);
                Matrix.Rescale(ref result, ref size);
                return result;
            }
        }

        public unsafe BoundingBox Transform(Matrix worldMatrix)
        {
            return Transform(ref worldMatrix);
        }

        public unsafe BoundingBoxD Transform(MatrixD worldMatrix)
        {
            return Transform(ref worldMatrix);
        }

        public BoundingBox Transform(ref Matrix m)
        {
            var bb = BoundingBox.CreateInvalid();
            Transform(ref m, ref bb);
            return bb;
        }

        public void Transform(ref Matrix m, ref BoundingBox bb)
        {
            bb.Min = bb.Max = m.Translation;
            Vector3 min = m.Right * Min.X;
            Vector3 max = m.Right * Max.X;
            Vector3.MinMax(ref min, ref max);
            bb.Min += min;
            bb.Max += max;

            min = m.Up * Min.Y;
            max = m.Up * Max.Y;
            Vector3.MinMax(ref min, ref max);
            bb.Min += min;
            bb.Max += max;

            min = m.Backward * Min.Z;
            max = m.Backward * Max.Z;
            Vector3.MinMax(ref min, ref max);
            bb.Min += min;
            bb.Max += max;
        }

        public BoundingBoxD Transform (ref MatrixD m)
        {
            var bb = BoundingBoxD.CreateInvalid();
            Transform(ref m, ref bb);
            return bb;
        }

        public void Transform(ref MatrixD m, ref BoundingBoxD bb)
        {
            bb.Min = bb.Max = m.Translation;
            Vector3D min = m.Right * Min.X;
            Vector3D max = m.Right * Max.X;
            Vector3D.MinMax(ref min, ref max);
            bb.Min += min;
            bb.Max += max;

            min = m.Up * Min.Y;
            max = m.Up * Max.Y;
            Vector3D.MinMax(ref min, ref max);
            bb.Min += min;
            bb.Max += max;

            min = m.Backward * Min.Z;
            max = m.Backward * Max.Z;
            Vector3D.MinMax(ref min, ref max);
            bb.Min += min;
            bb.Max += max;
        }

        /// <summary>
        /// return expanded aabb (aabb include point)
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public BoundingBox Include(ref Vector3 point)
        {
            Min.X = Math.Min(point.X, Min.X);
            Min.Y = Math.Min(point.Y, Min.Y);
            Min.Z = Math.Min(point.Z, Min.Z);

            Max.X = Math.Max(point.X, Max.X);
            Max.Y = Math.Max(point.Y, Max.Y);
            Max.Z = Math.Max(point.Z, Max.Z);
            return this;
        }

        public BoundingBox GetIncluded(Vector3 point)
        {
            BoundingBox b = this;
            b.Include(point);
            return b;
        }

        public BoundingBox Include(Vector3 point)
        {
            return Include(ref point);
        }

        public BoundingBox Include(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return Include(ref p0, ref p1, ref p2);
        }

        public BoundingBox Include(ref Vector3 p0, ref Vector3 p1, ref Vector3 p2)
        {
            Include(ref p0);
            Include(ref p1);
            Include(ref p2);

            return this;
        }

        /// <summary>
        /// return expanded aabb (aabb include aabb)
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public BoundingBox Include(ref BoundingBox box)
        {
            Min = Vector3.Min(Min, box.Min);
            Max = Vector3.Max(Max, box.Max);
            return this;
        }
        public BoundingBox Include(BoundingBox box)
        {
            return Include(ref box);
        }

        public void Include(ref Line line)
        {
            Include(ref line.From);
            Include(ref line.To);
        }


        public BoundingBox Include(BoundingSphere sphere)
        {
            return Include(ref sphere);
        }

        public BoundingBox Include(ref BoundingSphere sphere)
        {
            Vector3 radius = new Vector3(sphere.Radius);
            Vector3 minSphere = sphere.Center;
            Vector3 maxSphere = sphere.Center;

            Vector3.Subtract(ref minSphere, ref radius, out minSphere);
            Vector3.Add(ref maxSphere, ref radius, out maxSphere);

            Include(ref minSphere);
            Include(ref maxSphere);

            return this;
        }

        public unsafe BoundingBox Include(ref BoundingFrustum frustum)
        {
            Vector3* temporaryCorners = stackalloc Vector3[8];

            frustum.GetCornersUnsafe(temporaryCorners);

            Include(ref temporaryCorners[0]);
            Include(ref temporaryCorners[1]);
            Include(ref temporaryCorners[2]);
            Include(ref temporaryCorners[3]);
            Include(ref temporaryCorners[4]);
            Include(ref temporaryCorners[5]);
            Include(ref temporaryCorners[6]);
            Include(ref temporaryCorners[7]);

            return this;
        }

        public static BoundingBox CreateInvalid()
        {
            BoundingBox bbox = new BoundingBox();
            Vector3 vctMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 vctMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            bbox.Min = vctMin;
            bbox.Max = vctMax;

            return bbox;
        }

        public float SurfaceArea()
        {
            Vector3 span = Max - Min;
            return 2 * (span.X * span.Y + span.X * span.Z + span.Y * span.Z);
        }

        public float Volume()
        {
            Vector3 span = Max - Min;
            return span.X * span.Y * span.Z;
        }

        public float ProjectedArea(Vector3 viewDir)
        {
            Vector3 span = Max - Min;
            Vector3 size = new Vector3(span.Y, span.Z, span.X) * new Vector3(span.Z, span.X, span.Y);
            return Vector3.Abs(viewDir).Dot(size);
        }

        /// <summary>
        /// return perimeter of edges
        /// </summary>
        /// <returns></returns>
        public float Perimeter
        {
            get
            {
                float wx = Max.X - Min.X;
                float wy = Max.Y - Min.Y;
                float wz = Max.Z - Min.Z;

                return 4.0f * (wx + wy + wz);
            }
        }

        public void Inflate(float size)
        {
            Max += new Vector3(size);
            Min -= new Vector3(size);
        }

        public void Inflate(Vector3 size)
        {
            Max += size;
            Min -= size;
        }

        public void InflateToMinimum(Vector3 minimumSize)
        {
            Vector3 minCenter = Center;
            if (Size.X < minimumSize.X)
            {
                Min.X = minCenter.X - minimumSize.X * 0.5f;
                Max.X = minCenter.X + minimumSize.X * 0.5f;
            }
            if (Size.Y < minimumSize.Y)
            {
                Min.Y = minCenter.Y - minimumSize.Y * 0.5f;
                Max.Y = minCenter.Y + minimumSize.Y * 0.5f;
            }
            if (Size.Z < minimumSize.Z)
            {
                Min.Z = minCenter.Z - minimumSize.Z * 0.5f;
                Max.Z = minCenter.Z + minimumSize.Z * 0.5f;
            }
        }
        #region Comparer

        public class ComparerType : IEqualityComparer<BoundingBoxD>
        {
            public bool Equals(BoundingBoxD x, BoundingBoxD y)
            {
                return x.Min == y.Min && x.Max == y.Max;
            }

            public int GetHashCode(BoundingBoxD obj)
            {
                return obj.Min.GetHashCode() ^ obj.Max.GetHashCode();
            }
        }

        public static readonly ComparerType Comparer = new ComparerType();

        #endregion

        public void Scale(Vector3 scale)
        {
            Vector3 center = Center;
            Vector3 scaled = HalfExtents * scale;
            Min = center - scaled;
            Max = center + scaled;
        }
    }
}
