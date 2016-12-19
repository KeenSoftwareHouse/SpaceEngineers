using System;
using System.Collections.Generic;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines an axis-aligned box-shaped 3D volume.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct BoundingBoxI : IEquatable<BoundingBoxI>
    {
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingBoxI.
        /// </summary>
        public const int CornerCount = 8;
        /// <summary>
        /// The minimum point the BoundingBoxI contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector3I Min;
        /// <summary>
        /// The maximum point the BoundingBoxI contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector3I Max;

        /// <summary>
        /// Creates an instance of BoundingBoxI.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBoxI includes.</param><param name="max">The maximum point the BoundingBoxI includes.</param>
        public BoundingBoxI(BoundingBox box)
        {
            this.Min = new Vector3I(box.Min);
            this.Max = new Vector3I(box.Max);
        }

        /// <summary>
        /// Creates an instance of BoundingBoxI.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBoxI includes.</param><param name="max">The maximum point the BoundingBoxI includes.</param>
        public BoundingBoxI(Vector3I min, Vector3I max)
        {
            this.Min = min;
            this.Max = max;
        }

        /// <summary>
        /// Creates an instance of BoundingBoxI.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBoxI includes.</param><param name="max">The maximum point the BoundingBoxI includes.</param>
        public BoundingBoxI(int min, int max)
        {
            Min = new Vector3I(min);
            Max = new Vector3I(max);
        }

        /// <summary>
        /// Determines whether two instances of BoundingBoxI are equal.
        /// </summary>
        /// <param name="a">BoundingBoxI to compare.</param><param name="b">BoundingBoxI to compare.</param>
        public static bool operator ==(BoundingBoxI a, BoundingBoxI b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingBoxI are not equal.
        /// </summary>
        /// <param name="a">The object to the left of the inequality operator.</param><param name="b">The object to the right of the inequality operator.</param>
        public static bool operator !=(BoundingBoxI a, BoundingBoxI b)
        {
            if (!(a.Min != b.Min))
                return a.Max != b.Max;
            else
                return true;
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingBoxI.
        /// </summary>
        public Vector3I[] GetCorners()
        {
            return new Vector3I[8]
      {
        new Vector3I(this.Min.X, this.Max.Y, this.Max.Z),
        new Vector3I(this.Max.X, this.Max.Y, this.Max.Z),
        new Vector3I(this.Max.X, this.Min.Y, this.Max.Z),
        new Vector3I(this.Min.X, this.Min.Y, this.Max.Z),
        new Vector3I(this.Min.X, this.Max.Y, this.Min.Z),
        new Vector3I(this.Max.X, this.Max.Y, this.Min.Z),
        new Vector3I(this.Max.X, this.Min.Y, this.Min.Z),
        new Vector3I(this.Min.X, this.Min.Y, this.Min.Z)
      };
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBoxI.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3I points where the corners of the BoundingBoxI are written.</param>
        public void GetCorners(Vector3I[] corners)
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
        /// Gets the array of points that make up the corners of the BoundingBoxI.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3I points where the corners of the BoundingBoxI are written.</param>
		[Unsharper.UnsharperDisableReflection()]
		public unsafe void GetCornersUnsafe(Vector3I* corners)
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
        /// Determines whether two instances of BoundingBoxI are equal.
        /// </summary>
        /// <param name="other">The BoundingBoxI to compare with the current BoundingBoxI.</param>
        public bool Equals(BoundingBoxI other)
        {
            if (this.Min == other.Min)
                return this.Max == other.Max;
            else
                return false;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBoxI are equal.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingBoxI.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is BoundingBoxI)
                flag = this.Equals((BoundingBoxI)obj);
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
        /// Returns a String that represents the current BoundingBoxI.
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
        /// Creates the smallest BoundingBoxI that contains the two specified BoundingBoxI instances.
        /// </summary>
        /// <param name="original">One of the BoundingBoxIs to contain.</param><param name="additional">One of the BoundingBoxIs to contain.</param>
        public static BoundingBoxI CreateMerged(BoundingBoxI original, BoundingBoxI additional)
        {
            BoundingBoxI BoundingBoxI;
            Vector3I.Min(ref original.Min, ref additional.Min, out BoundingBoxI.Min);
            Vector3I.Max(ref original.Max, ref additional.Max, out BoundingBoxI.Max);
            return BoundingBoxI;
        }

        /// <summary>
        /// Creates the smallest BoundingBoxI that contains the two specified BoundingBoxI instances.
        /// </summary>
        /// <param name="original">One of the BoundingBoxI instances to contain.</param><param name="additional">One of the BoundingBoxI instances to contain.</param><param name="result">[OutAttribute] The created BoundingBoxI.</param>
        public static void CreateMerged(ref BoundingBoxI original, ref BoundingBoxI additional, out BoundingBoxI result)
        {
            Vector3I result1;
            Vector3I.Min(ref original.Min, ref additional.Min, out result1);
            Vector3I result2;
            Vector3I.Max(ref original.Max, ref additional.Max, out result2);
            result.Min = result1;
            result.Max = result2;
        }

        /// <summary>
        /// Creates the smallest BoundingBoxI that will contain the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to contain.</param>
        public static BoundingBoxI CreateFromSphere(BoundingSphere sphere)
        {
            BoundingBoxI BoundingBoxI;
            BoundingBoxI.Min.X = (int) (sphere.Center.X - sphere.Radius);
            BoundingBoxI.Min.Y = (int) (sphere.Center.Y - sphere.Radius);
            BoundingBoxI.Min.Z = (int) (sphere.Center.Z - sphere.Radius);
            BoundingBoxI.Max.X = (int) (sphere.Center.X + sphere.Radius);
            BoundingBoxI.Max.Y = (int) (sphere.Center.Y + sphere.Radius);
            BoundingBoxI.Max.Z = (int) (sphere.Center.Z + sphere.Radius);
            return BoundingBoxI;
        }

        /// <summary>
        /// Creates the smallest BoundingBoxI that will contain the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to contain.</param><param name="result">[OutAttribute] The created BoundingBoxI.</param>
        public static void CreateFromSphere(ref BoundingSphere sphere, out BoundingBoxI result)
        {
            result.Min.X = (int) (sphere.Center.X - sphere.Radius);
            result.Min.Y = (int) (sphere.Center.Y - sphere.Radius);
            result.Min.Z = (int) (sphere.Center.Z - sphere.Radius);
            result.Max.X = (int) (sphere.Center.X + sphere.Radius);
            result.Max.Y = (int) (sphere.Center.Y + sphere.Radius);
            result.Max.Z = (int) (sphere.Center.Z + sphere.Radius);
        }

        /// <summary>
        /// Creates the smallest BoundingBoxI that will contain a group of points.
        /// </summary>
        /// <param name="points">A list of points the BoundingBoxI should contain.</param>
        public static BoundingBoxI CreateFromPoints(IEnumerable<Vector3I> points)
        {
            if (points == null)
                throw new ArgumentNullException();
            bool flag = false;
            Vector3I result1 = new Vector3I(int.MaxValue);
            Vector3I result2 = new Vector3I(int.MinValue);
            foreach (Vector3I v3i in points)
            {
                Vector3I vec3 = v3i;
                Vector3I.Min(ref result1, ref vec3, out result1);
                Vector3I.Max(ref result2, ref vec3, out result2);
                flag = true;
            }
            if (!flag)
                throw new ArgumentException();
            else
                return new BoundingBoxI(result1, result2);
        }

        /// <summary>
        /// Returns bounding box which is intersection of this and box
        /// It's called 'Prunik'
        /// Result is invalid box when there's no intersection (Min > Max)
        /// </summary>
        public void IntersectWith(ref BoundingBoxI box)
        {
            Min.X = Math.Max(this.Min.X, box.Min.X);
            Min.Y = Math.Max(this.Min.Y, box.Min.Y);
            Min.Z = Math.Max(this.Min.Z, box.Min.Z);
            Max.X = Math.Min(this.Max.X, box.Max.X);
            Max.Y = Math.Min(this.Max.Y, box.Max.Y);
            Max.Z = Math.Min(this.Max.Z, box.Max.Z);
        }

        /// <summary>
        /// Returns bounding box which is intersection of this and box
        /// It's called 'Prunik'
        /// Result is invalid box when there's no intersection (Min > Max)
        /// </summary>
        public BoundingBoxI Intersect(BoundingBox box)
        {
            BoundingBoxI result;
            result.Min.X = (int)Math.Max((float)this.Min.X, box.Min.X);
            result.Min.Y = (int)Math.Max((float)this.Min.Y, box.Min.Y);
            result.Min.Z = (int)Math.Max((float)this.Min.Z, box.Min.Z);
            result.Max.X = (int)Math.Min((float)this.Max.X, box.Max.X);
            result.Max.Y = (int)Math.Min((float)this.Max.Y, box.Max.Y);
            result.Max.Z = (int)Math.Min((float)this.Max.Z, box.Max.Z);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingBoxI intersects another BoundingBoxI.
        /// </summary>
        /// <param name="box">The BoundingBoxI to check for intersection with.</param>
        public bool Intersects(BoundingBoxI box)
        {
            return Intersects(ref box);
        }

        public bool Intersects(ref BoundingBoxI box)
        {
            return (double)this.Max.X >= (double)box.Min.X && (double)this.Min.X <= (double)box.Max.X && ((double)this.Max.Y >= (double)box.Min.Y && (double)this.Min.Y <= (double)box.Max.Y) && ((double)this.Max.Z >= (double)box.Min.Z && (double)this.Min.Z <= (double)box.Max.Z);
        }

        /// <summary>
        /// Checks whether the current BoundingBoxI intersects another BoundingBoxI.
        /// </summary>
        /// <param name="box">The BoundingBoxI to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBoxI instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingBoxI box, out bool result)
        {
            result = false;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return;
            result = true;
        }

        public bool IntersectsTriangle(Vector3I v0, Vector3I v1, Vector3I v2)
        {
            return IntersectsTriangle(ref v0, ref v1, ref v2);
        }

        public bool IntersectsTriangle(ref Vector3I v0, ref Vector3I v1, ref Vector3I v2)
        {
            // This code is based on: Akenine-Moeller, Thomas - "Fast 3D Triangle-Box Overlap Testing"

            // Test 1) - Separation of triangle and BB by the bounding box's 6 planes
            Vector3I min, max;
            Vector3I.Min(ref v0, ref v1, out min);
            Vector3I.Min(ref min, ref v2, out min);
            Vector3I.Max(ref v0, ref v1, out max);
            Vector3I.Max(ref max, ref v2, out max);

            if (min.X > Max.X) return false;
            if (max.X < Min.X) return false;
            if (min.Y > Max.Y) return false;
            if (max.Y < Min.Y) return false;
            if (min.Z > Max.Z) return false;
            if (max.Z < Min.Z) return false;

            // Test 2) - Separation by the triangle's plane
            Vector3I f0 = v1 - v0;
            Vector3I f1 = v2 - v1;
            Vector3I triN; Vector3I.Cross(ref f0, ref f1, out triN);
            int d; Vector3I.Dot(ref v0, ref triN, out d);

            // The triangle's plane. It does not have to be normalized
            Plane triPlane = new Plane(triN, -d);

            PlaneIntersectionType intersection;
            Intersects(ref triPlane, out intersection);
            if (intersection == PlaneIntersectionType.Back) return false;
            if (intersection == PlaneIntersectionType.Front) return false;

            // Test 3) - Separation by planes that are perpendicular to coordinate axes e0, e1, e2 and triangle edges f0, f1, f2
            Vector3I center = Center;
            BoundingBoxI tmpBox = new BoundingBoxI(Min - center, Max - center);
            Vector3I originHalf = tmpBox.HalfExtents;
            Vector3I f2 = v0 - v2;

            Vector3I v0sh = v0 - center;
            Vector3I v1sh = v1 - center;
            Vector3I v2sh = v2 - center;

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
        public Vector3I Center
        {
            get { return (Min + Max) / 2; }
        }

        public Vector3I HalfExtents
        {
            get { return (Max - Min) / 2; }
        }

        /// <summary>
        /// Checks whether the current BoundingBoxI intersects a BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with.</param>
        //public bool Intersects(BoundingFrustum frustum)
        //{
        //    if ((BoundingFrustum)null == frustum)
        //        throw new ArgumentNullException("frustum");
        //    else
        //        return frustum.Intersects(this);
        //}

        /// <summary>
        /// Checks whether the current BoundingBoxI intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            Vector3I Vector3I_1;
            Vector3I_1.X = (double)plane.Normal.X >= 0.0 ? this.Min.X : this.Max.X;
            Vector3I_1.Y = (double)plane.Normal.Y >= 0.0 ? this.Min.Y : this.Max.Y;
            Vector3I_1.Z = (double)plane.Normal.Z >= 0.0 ? this.Min.Z : this.Max.Z;
            Vector3I Vector3I_2;
            Vector3I_2.X = (double)plane.Normal.X >= 0.0 ? this.Max.X : this.Min.X;
            Vector3I_2.Y = (double)plane.Normal.Y >= 0.0 ? this.Max.Y : this.Min.Y;
            Vector3I_2.Z = (double)plane.Normal.Z >= 0.0 ? this.Max.Z : this.Min.Z;
            if ((double)plane.Normal.X * (double)Vector3I_1.X + (double)plane.Normal.Y * (double)Vector3I_1.Y + (double)plane.Normal.Z * (double)Vector3I_1.Z + (double)plane.D > 0.0)
                return PlaneIntersectionType.Front;
            return (double)plane.Normal.X * (double)Vector3I_2.X + (double)plane.Normal.Y * (double)Vector3I_2.Y + (double)plane.Normal.Z * (double)Vector3I_2.Z + (double)plane.D < 0.0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current BoundingBoxI intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the BoundingBoxI intersects the Plane.</param>
        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            Vector3I Vector3I_1;
            Vector3I_1.X = (double)plane.Normal.X >= 0.0 ? this.Min.X : this.Max.X;
            Vector3I_1.Y = (double)plane.Normal.Y >= 0.0 ? this.Min.Y : this.Max.Y;
            Vector3I_1.Z = (double)plane.Normal.Z >= 0.0 ? this.Min.Z : this.Max.Z;
            Vector3I Vector3I_2;
            Vector3I_2.X = (double)plane.Normal.X >= 0.0 ? this.Max.X : this.Min.X;
            Vector3I_2.Y = (double)plane.Normal.Y >= 0.0 ? this.Max.Y : this.Min.Y;
            Vector3I_2.Z = (double)plane.Normal.Z >= 0.0 ? this.Max.Z : this.Min.Z;
            if ((double)plane.Normal.X * (double)Vector3I_1.X + (double)plane.Normal.Y * (double)Vector3I_1.Y + (double)plane.Normal.Z * (double)Vector3I_1.Z + (double)plane.D > 0.0)
                result = PlaneIntersectionType.Front;
            else if ((double)plane.Normal.X * (double)Vector3I_2.X + (double)plane.Normal.Y * (double)Vector3I_2.Y + (double)plane.Normal.Z * (double)Vector3I_2.Z + (double)plane.D < 0.0)
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
        /// Checks whether the current BoundingBoxI intersects a Ray.
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
        /// Checks whether the current BoundingBoxI intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param><param name="result">[OutAttribute] Distance at which the ray intersects the BoundingBoxI, or null if there is no intersection.</param>
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
        /// Checks whether the current BoundingBoxI intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        //public bool Intersects(BoundingSphere sphere)
        //{
        //    return Intersects(ref sphere);
        //}

        /// <summary>
        /// Checks whether the current BoundingBoxI intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBoxI and BoundingSphere intersect; false otherwise.</param>
        //public void Intersects(ref BoundingSphere sphere, out bool result)
        //{
        //    Vector3I result1;
        //    Vector3I.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
        //    float result2;
        //    Vector3I.DistanceSquared(ref sphere.Center, ref result1, out result2);
        //    result = (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        //}

        //public bool Intersects(ref BoundingSphere sphere)
        //{
        //    Vector3I result1;
        //    Vector3I.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
        //    float result2;
        //    Vector3I.DistanceSquared(ref sphere.Center, ref result1, out result2);
        //    return (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        //}

        //public bool Intersects(ref BoundingSphereD sphere)
        //{
        //    Vector3I result1;
        //    Vector3I center = (Vector3I)sphere.Center;
        //    Vector3I.Clamp(ref center, ref this.Min, ref this.Max, out result1);
        //    float result2;
        //    Vector3I.DistanceSquared(ref center, ref result1, out result2);
        //    return (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        //}

        public float Distance(Vector3I point)
        {
            var clamp = Vector3I.Clamp(point, Min, Max);
            return (clamp - point).Length();
        }

        /// <summary>
        /// Tests whether the BoundingBoxI contains another BoundingBoxI.
        /// </summary>
        /// <param name="box">The BoundingBoxI to test for overlap.</param>
        public ContainmentType Contains(BoundingBoxI box)
        {
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return ContainmentType.Disjoint;
            return (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)box.Min.Z || (double)box.Max.Z > (double)this.Max.Z) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBoxI contains a BoundingBoxI.
        /// </summary>
        /// <param name="box">The BoundingBoxI to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBoxI box, out ContainmentType result)
        {
            result = ContainmentType.Disjoint;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return;
            result = (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)box.Min.Z || (double)box.Max.Z > (double)this.Max.Z) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBoxI contains a BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to test for overlap.</param>
        //public ContainmentType Contains(BoundingFrustum frustum)
        //{
        //    if (!frustum.Intersects(this))
        //        return ContainmentType.Disjoint;
        //    foreach (Vector3I point in frustum.cornerArray)
        //    {
        //        if (this.Contains(point) == ContainmentType.Disjoint)
        //            return ContainmentType.Intersects;
        //    }
        //    return ContainmentType.Contains;
        //}

        /// <summary>
        /// Tests whether the BoundingBoxI contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param>
        public ContainmentType Contains(Vector3I point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        public ContainmentType Contains(Vector3 point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBoxI contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3I point, out ContainmentType result)
        {
            result = (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBoxI contains a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param>
        //public ContainmentType Contains(BoundingSphere sphere)
        //{
        //    Vector3I result1;
        //    Vector3I.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
        //    float result2;
        //    Vector3I.DistanceSquared(ref sphere.Center, ref result1, out result2);
        //    float num = sphere.Radius;
        //    if ((double)result2 > (double)num * (double)num)
        //        return ContainmentType.Disjoint;
        //    return (double)this.Min.X + (double)num > (double)sphere.Center.X || (double)sphere.Center.X > (double)this.Max.X - (double)num || ((double)this.Max.X - (double)this.Min.X <= (double)num || (double)this.Min.Y + (double)num > (double)sphere.Center.Y) || ((double)sphere.Center.Y > (double)this.Max.Y - (double)num || (double)this.Max.Y - (double)this.Min.Y <= (double)num || ((double)this.Min.Z + (double)num > (double)sphere.Center.Z || (double)sphere.Center.Z > (double)this.Max.Z - (double)num)) || (double)this.Max.X - (double)this.Min.X <= (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        //}

        /// <summary>
        /// Tests whether the BoundingBoxI contains a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        //public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        //{
        //    Vector3I result1;
        //    Vector3I.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
        //    float result2;
        //    Vector3I.DistanceSquared(ref sphere.Center, ref result1, out result2);
        //    float num = sphere.Radius;
        //    if ((double)result2 > (double)num * (double)num)
        //        result = ContainmentType.Disjoint;
        //    else
        //        result = (double)this.Min.X + (double)num > (double)sphere.Center.X || (double)sphere.Center.X > (double)this.Max.X - (double)num || ((double)this.Max.X - (double)this.Min.X <= (double)num || (double)this.Min.Y + (double)num > (double)sphere.Center.Y) || ((double)sphere.Center.Y > (double)this.Max.Y - (double)num || (double)this.Max.Y - (double)this.Min.Y <= (double)num || ((double)this.Min.Z + (double)num > (double)sphere.Center.Z || (double)sphere.Center.Z > (double)this.Max.Z - (double)num)) || (double)this.Max.X - (double)this.Min.X <= (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        //}

        internal void SupportMapping(ref Vector3I v, out Vector3I result)
        {
            result.X = (double)v.X >= 0.0 ? this.Max.X : this.Min.X;
            result.Y = (double)v.Y >= 0.0 ? this.Max.Y : this.Min.Y;
            result.Z = (double)v.Z >= 0.0 ? this.Max.Z : this.Min.Z;
        }

        /// Translate
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="worldMatrix"></param>
        /// <returns></returns>
        //public BoundingBoxI Translate(Matrix worldMatrix)
        //{
        //    Min += worldMatrix.Translation;
        //    Max += worldMatrix.Translation;
        //    return this;
        //}


        /// <summary>
        /// Translate
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="vctTranlsation"></param>
        /// <returns></returns>
        public BoundingBoxI Translate(Vector3I vctTranlsation)
        {
            Min += vctTranlsation;
            Max += vctTranlsation;
            return this;
        }

        /// <summary>
        /// Size
        /// </summary>
        /// <returns></returns>
        public Vector3I Size
        {
            get 
            {
                return Max - Min;
            }
        }

        /// <summary>
        /// Matrix of AABB, respection center and size
        /// </summary>
        //public Matrix Matrix
        //{
        //    get
        //    {
        //        var center = Center;
        //        var size = Size;

        //        Matrix result;
        //        Matrix.CreateTranslation(ref center, out result);
        //        Matrix.Rescale(ref result, ref size);
        //        return result;
        //    }
        //}

        //public unsafe BoundingBoxI Transform(Matrix worldMatrix)
        //{
        //    return Transform(ref worldMatrix);
        //}

        //public unsafe BoundingBoxID Transform(MatrixD worldMatrix)
        //{
        //    return Transform(ref worldMatrix);
        //}

        //public unsafe BoundingBoxI Transform(ref Matrix worldMatrix)
        //{
        //    BoundingBoxI oobb = BoundingBoxI.CreateInvalid();

        //    Vector3I* temporaryCorners = stackalloc Vector3I[8];

        //    GetCornersUnsafe((Vector3I*)temporaryCorners);

        //    for (int i = 0; i < 8; i++)
        //    {
        //        Vector3I vctTransformed = Vector3I.Transform(temporaryCorners[i], worldMatrix);
        //        oobb = oobb.Include(ref vctTransformed);
        //    }

        //    return oobb;
        //}

        //public unsafe BoundingBoxID Transform(ref MatrixD worldMatrix)
        //{
        //    BoundingBoxID oobb = BoundingBoxID.CreateInvalid();

        //    Vector3I* temporaryCorners = stackalloc Vector3I[8];

        //    GetCornersUnsafe((Vector3I*)temporaryCorners);

        //    for (int i = 0; i < 8; i++)
        //    {
        //        Vector3ID vctTransformed = Vector3I.Transform(temporaryCorners[i], worldMatrix);
        //        oobb = oobb.Include(ref vctTransformed);
        //    }

        //    return oobb;
        //}

        /// <summary>
        /// return expanded aabb (abb include point)
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public BoundingBoxI Include(ref Vector3I point)
        {
            if (point.X < Min.X)
                Min.X = point.X;

            if (point.Y < Min.Y)
                Min.Y = point.Y;

            if (point.Z < Min.Z)
                Min.Z = point.Z;


            if (point.X > Max.X)
                Max.X = point.X;

            if (point.Y > Max.Y)
                Max.Y = point.Y;

            if (point.Z > Max.Z)
                Max.Z = point.Z;

            return this;
        }

        public BoundingBoxI GetIncluded(Vector3I point)
        {
            BoundingBoxI b = this;
            b.Include(point);
            return b;
        }

        public BoundingBoxI Include(Vector3I point)
        {
            return Include(ref point);
        }

        public BoundingBoxI Include(Vector3I p0, Vector3I p1, Vector3I p2)
        {
            return Include(ref p0, ref p1, ref p2);
        }

        public BoundingBoxI Include(ref Vector3I p0, ref Vector3I p1, ref Vector3I p2)
        {
            Include(ref p0);
            Include(ref p1);
            Include(ref p2);

            return this;
        }

        /// <summary>
        /// return expanded aabb (abb include point)
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public BoundingBoxI Include(ref BoundingBoxI box)
        {
            Min = Vector3I.Min(Min, box.Min);
            Max = Vector3I.Max(Max, box.Max);
            return this;
        }
        public BoundingBoxI Include(BoundingBoxI box)
        {
            return Include(ref box);
        }

        //public void Include(ref Line line)
        //{
        //    Include(ref line.From);
        //    Include(ref line.To);
        //}


        //public BoundingBoxI Include(BoundingSphere sphere)
        //{
        //    return Include(ref sphere);
        //}

        //public BoundingBoxI Include(ref BoundingSphere sphere)
        //{
        //    Vector3I radius = new Vector3I(sphere.Radius);
        //    Vector3I minSphere = sphere.Center;
        //    Vector3I maxSphere = sphere.Center;

        //    Vector3I.Subtract(ref minSphere, ref radius, out minSphere);
        //    Vector3I.Add(ref maxSphere, ref radius, out maxSphere);

        //    Include(ref minSphere);
        //    Include(ref maxSphere);

        //    return this;
        //}

        //static Vector3I[] m_frustumPoints = null;

        //public BoundingBoxI Include(ref BoundingFrustum frustum)
        //{
        //    if (m_frustumPoints == null)
        //        m_frustumPoints = new Vector3I[8];

        //    frustum.GetCorners(m_frustumPoints);

        //    Include(ref m_frustumPoints[0]);
        //    Include(ref m_frustumPoints[1]);
        //    Include(ref m_frustumPoints[2]);
        //    Include(ref m_frustumPoints[3]);
        //    Include(ref m_frustumPoints[4]);
        //    Include(ref m_frustumPoints[5]);
        //    Include(ref m_frustumPoints[6]);
        //    Include(ref m_frustumPoints[7]);

        //    return this;
        //}

        public static BoundingBoxI CreateInvalid()
        {
            BoundingBoxI bbox = new BoundingBoxI();
            Vector3I vctMin = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3I vctMax = new Vector3I(int.MinValue, int.MinValue, int.MinValue);

            bbox.Min = vctMin;
            bbox.Max = vctMax;

            return bbox;
        }

        public float SurfaceArea()
        {
            Vector3I span = Max - Min;
            return 2 * (span.X * span.Y + span.X * span.Z + span.Y * span.Z);
        }

        public float Volume()
        {
            Vector3I span = Max - Min;
            return span.X * span.Y * span.Z;
        }

        //public float ProjectedArea(Vector3I viewDir)
        //{
        //    Vector3I span = Max - Min;
        //    Vector3I size = new Vector3I(span.Y, span.Z, span.X) * new Vector3I(span.Z, span.X, span.Y);
        //    return Vector3I.Abs(viewDir).Dot(size);
        //}

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

        public void Inflate(int size)
        {
            Max += new Vector3I(size);
            Min -= new Vector3I(size);
        }

        public void InflateToMinimum(Vector3I minimumSize)
        {
            Vector3I minCenter = Center;
            if (Size.X < minimumSize.X)
            {
                Min.X = minCenter.X - minimumSize.X / 2;
                Max.X = minCenter.X + minimumSize.X / 2;
            }
            if (Size.Y < minimumSize.Y)
            {
                Min.Y = minCenter.Y - minimumSize.Y / 2;
                Max.Y = minCenter.Y + minimumSize.Y / 2;
            }
            if (Size.Z < minimumSize.Z)
            {
                Min.Z = minCenter.Z - minimumSize.Z / 2;
                Max.Z = minCenter.Z + minimumSize.Z / 2;
            }
        }

        public bool IsValid
        {
            get { return Min.X <= Max.X && Min.Y <= Max.Y && Min.Z <= Max.Z; }
        }

        //#region Comparer

        //public class ComparerType : IEqualityComparer<BoundingBoxD>
        //{
        //    public bool Equals(BoundingBoxD x, BoundingBoxD y)
        //    {
        //        return x.Min == y.Min && x.Max == y.Max;
        //    }

        //    public int GetHashCode(BoundingBoxD obj)
        //    {
        //        return obj.Min.GetHashCode() ^ obj.Max.GetHashCode();
        //    }
        //}

        //public static readonly ComparerType Comparer = new ComparerType();

        //#endregion
    }
}
