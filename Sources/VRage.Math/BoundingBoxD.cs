using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
//using VRage.Utils;

namespace VRageMath
{
    /// <summary>
    /// Defines an axis-aligned box-shaped 3D volume.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct BoundingBoxD : IEquatable<BoundingBoxD>
    {
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingBox.
        /// </summary>
        public const int CornerCount = 8;
        /// <summary>
        /// The minimum point the BoundingBox contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector3D Min;
        /// <summary>
        /// The maximum point the BoundingBox contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector3D Max;

        /// <summary>
        /// Creates an instance of BoundingBox.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBox includes.</param><param name="max">The maximum point the BoundingBox includes.</param>
        public BoundingBoxD(Vector3D min, Vector3D max)
        {
            this.Min = min;
            this.Max = max;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox are equal.
        /// </summary>
        /// <param name="a">BoundingBox to compare.</param><param name="b">BoundingBox to compare.</param>
        public static bool operator ==(BoundingBoxD a, BoundingBoxD b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox are not equal.
        /// </summary>
        /// <param name="a">The object to the left of the inequality operator.</param><param name="b">The object to the right of the inequality operator.</param>
        public static bool operator !=(BoundingBoxD a, BoundingBoxD b)
        {
            if (!(a.Min != b.Min))
                return a.Max != b.Max;
            else
                return true;
        }

        public static BoundingBoxD operator +(BoundingBoxD a, Vector3D b)
        {
            BoundingBoxD c;
            c.Max = a.Max + b;
            c.Min = a.Min + b;

            return c;
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingBox. ALLOCATION!
        /// </summary>
        public Vector3D[] GetCorners()
        {
            return new Vector3D[8]
      {
        new Vector3D(this.Min.X, this.Max.Y, this.Max.Z),
        new Vector3D(this.Max.X, this.Max.Y, this.Max.Z),
        new Vector3D(this.Max.X, this.Min.Y, this.Max.Z),
        new Vector3D(this.Min.X, this.Min.Y, this.Max.Z),
        new Vector3D(this.Min.X, this.Max.Y, this.Min.Z),
        new Vector3D(this.Max.X, this.Max.Y, this.Min.Z),
        new Vector3D(this.Max.X, this.Min.Y, this.Min.Z),
        new Vector3D(this.Min.X, this.Min.Y, this.Min.Z)
      };
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3 points where the corners of the BoundingBox are written.</param>
        public void GetCorners(Vector3D[] corners)
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
		public unsafe void GetCornersUnsafe(Vector3D* corners)
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
        public bool Equals(BoundingBoxD other)
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
            if (obj is BoundingBoxD)
                flag = this.Equals((BoundingBoxD)obj);
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
        public static BoundingBoxD CreateMerged(BoundingBoxD original, BoundingBoxD additional)
        {
            BoundingBoxD boundingBox;
            Vector3D.Min(ref original.Min, ref additional.Min, out boundingBox.Min);
            Vector3D.Max(ref original.Max, ref additional.Max, out boundingBox.Max);
            return boundingBox;
        }

        /// <summary>
        /// Creates the smallest BoundingBox that contains the two specified BoundingBox instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox instances to contain.</param><param name="additional">One of the BoundingBox instances to contain.</param><param name="result">[OutAttribute] The created BoundingBox.</param>
        public static void CreateMerged(ref BoundingBoxD original, ref BoundingBoxD additional, out BoundingBoxD result)
        {
            Vector3D result1;
            Vector3D.Min(ref original.Min, ref additional.Min, out result1);
            Vector3D result2;
            Vector3D.Max(ref original.Max, ref additional.Max, out result2);
            result.Min = result1;
            result.Max = result2;
        }

        /// <summary>
        /// Creates the smallest BoundingBox that will contain the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to contain.</param>
        public static BoundingBoxD CreateFromSphere(BoundingSphereD sphere)
        {
            BoundingBoxD boundingBox;
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
        public static void CreateFromSphere(ref BoundingSphereD sphere, out BoundingBoxD result)
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
        public static BoundingBoxD CreateFromPoints(IEnumerable<Vector3D> points)
        {
            if (points == null)
                throw new ArgumentNullException();
            bool flag = false;
            Vector3D result1 = new Vector3D(double.MaxValue);
            Vector3D result2 = new Vector3D(double.MinValue);
            foreach (Vector3D vector3 in points)
            {
                Vector3D vec3 = vector3;
                Vector3D.Min(ref result1, ref vec3, out result1);
                Vector3D.Max(ref result2, ref vec3, out result2);
                flag = true;
            }
            if (!flag)
                throw new ArgumentException();
            else
                return new BoundingBoxD(result1, result2);
        }

        /// <summary>
        /// Returns bounding box which is intersection of this and box
        /// It's called 'Prunik'
        /// Result is invalid box when there's no intersection (Min > Max)
        /// </summary>
        public BoundingBoxD Intersect(BoundingBoxD box)
        {
            BoundingBoxD result;
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
        public bool Intersects(BoundingBoxD box)
        {
            return Intersects(ref box);
        }

        public bool Intersects(ref BoundingBoxD box)
        {
            return (double)this.Max.X >= (double)box.Min.X && (double)this.Min.X <= (double)box.Max.X && ((double)this.Max.Y >= (double)box.Min.Y && (double)this.Min.Y <= (double)box.Max.Y) && ((double)this.Max.Z >= (double)box.Min.Z && (double)this.Min.Z <= (double)box.Max.Z);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects another BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBox instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingBoxD box, out bool result)
        {
            result = false;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return;
            result = true;
        }
        public void Intersects(ref BoundingBox box, out bool result)
        {
            result = false;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return;
            result = true;
        }

        public bool IntersectsTriangle(Vector3D v0, Vector3D v1, Vector3D v2)
        {
            return IntersectsTriangle(ref v0, ref v1, ref v2);
        }

        public bool IntersectsTriangle(ref Vector3D v0, ref Vector3D v1, ref Vector3D v2)
        {
            // This code is based on: Akenine-Moeller, Thomas - "Fast 3D Triangle-Box Overlap Testing"

            // Test 1) - Separation of triangle and BB by the bounding box's 6 planes
            Vector3D min, max;
            Vector3D.Min(ref v0, ref v1, out min);
            Vector3D.Min(ref min, ref v2, out min);
            Vector3D.Max(ref v0, ref v1, out max);
            Vector3D.Max(ref max, ref v2, out max);

            if (min.X > Max.X) return false;
            if (max.X < Min.X) return false;
            if (min.Y > Max.Y) return false;
            if (max.Y < Min.Y) return false;
            if (min.Z > Max.Z) return false;
            if (max.Z < Min.Z) return false;

            // Test 2) - Separation by the triangle's plane
            Vector3D f0 = v1 - v0;
            Vector3D f1 = v2 - v1;
            Vector3D triN; Vector3D.Cross(ref f0, ref f1, out triN);
            double d; Vector3D.Dot(ref v0, ref triN, out d);

            // The triangle's plane. It does not have to be normalized
            PlaneD triPlane = new PlaneD(triN, -d);

            PlaneIntersectionType intersection;
            Intersects(ref triPlane, out intersection);
            if (intersection == PlaneIntersectionType.Back) return false;
            if (intersection == PlaneIntersectionType.Front) return false;

            // Test 3) - Separation by planes that are perpendicular to coordinate axes e0, e1, e2 and triangle edges f0, f1, f2
            Vector3D center = Center;
            BoundingBoxD tmpBox = new BoundingBoxD(Min - center, Max - center);
            Vector3D originHalf = tmpBox.HalfExtents;
            Vector3D f2 = v0 - v2;

            Vector3D v0sh = v0 - center;
            Vector3D v1sh = v1 - center;
            Vector3D v2sh = v2 - center;

            double boxR, p0, p1, p2;

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
        public Vector3D Center
        {
            get { return (Min + Max) * 0.5; }
        }

        public Vector3D HalfExtents
        {
            get { return (Max - Min) * 0.5; }
        }

        public Vector3D Extents
        {
            get { return (Max - Min); }
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with.</param>
        public bool Intersects(BoundingFrustumD frustum)
        {
            if ((BoundingFrustumD)null == frustum)
                throw new ArgumentNullException("frustum");
            else
                return frustum.Intersects(this);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param>
        public PlaneIntersectionType Intersects(PlaneD plane)
        {
            Vector3D vector3_1;
            vector3_1.X = (double)plane.Normal.X >= 0.0 ? this.Min.X : this.Max.X;
            vector3_1.Y = (double)plane.Normal.Y >= 0.0 ? this.Min.Y : this.Max.Y;
            vector3_1.Z = (double)plane.Normal.Z >= 0.0 ? this.Min.Z : this.Max.Z;
            Vector3D vector3_2;
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
        public void Intersects(ref PlaneD plane, out PlaneIntersectionType result)
        {
            Vector3D vector3_1;
            vector3_1.X = (double)plane.Normal.X >= 0.0 ? this.Min.X : this.Max.X;
            vector3_1.Y = (double)plane.Normal.Y >= 0.0 ? this.Min.Y : this.Max.Y;
            vector3_1.Z = (double)plane.Normal.Z >= 0.0 ? this.Min.Z : this.Max.Z;
            Vector3D vector3_2;
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


        public bool Intersects(ref LineD line)
        {
            double? f = Intersects(new RayD(line.From, line.Direction));
            if (!f.HasValue)
                return false;

            if (f.Value < 0)
                return false;

            if (f.Value > line.Length)
                return false;

            return true;
        }

        public bool Intersects(ref LineD line, out double distance)
        {
            distance = 0f;
            double? f = Intersects(new RayD(line.From, line.Direction));
            if (!f.HasValue)
                return false;

            if (f.Value < 0)
                return false;

            if (f.Value > line.Length)
                return false;

            distance = f.Value;
            return true;
        }


        public double? Intersects(Ray ray)
        {
            RayD r = new RayD((Vector3D)ray.Position, (Vector3D)ray.Direction);
            return Intersects(r);
        }
        /// <summary>
        /// Checks whether the current BoundingBox intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param>
        public double? Intersects(RayD ray)
        {
            double num1 = 0.0;
            double num2 = double.MaxValue;
            if ((double)Math.Abs(ray.Direction.X) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.X < (double)this.Min.X || (double)ray.Position.X > (double)this.Max.X)
                    return new double?();
            }
            else
            {
                double num3 = 1 / ray.Direction.X;
                double num4 = (this.Min.X - ray.Position.X) * num3;
                double num5 = (this.Max.X - ray.Position.X) * num3;
                if ((double)num4 > (double)num5)
                {
                    double num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                num2 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num2)
                    return new double?();
            }
            if ((double)Math.Abs(ray.Direction.Y) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.Y < (double)this.Min.Y || (double)ray.Position.Y > (double)this.Max.Y)
                    return new double?();
            }
            else
            {
                double num3 = 1 / ray.Direction.Y;
                double num4 = (this.Min.Y - ray.Position.Y) * num3;
                double num5 = (this.Max.Y - ray.Position.Y) * num3;
                if ((double)num4 > (double)num5)
                {
                    double num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                num2 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num2)
                    return new double?();
            }
            if ((double)Math.Abs(ray.Direction.Z) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.Z < (double)this.Min.Z || (double)ray.Position.Z > (double)this.Max.Z)
                    return new double?();
            }
            else
            {
                double num3 = 1 / ray.Direction.Z;
                double num4 = (this.Min.Z - ray.Position.Z) * num3;
                double num5 = (this.Max.Z - ray.Position.Z) * num3;
                if ((double)num4 > (double)num5)
                {
                    double num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                double num7 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num7)
                    return new double?();
            }
            return new double?(num1);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param><param name="result">[OutAttribute] Distance at which the ray intersects the BoundingBox, or null if there is no intersection.</param>
        public void Intersects(ref RayD ray, out double? result)
        {
            result = new double?();
            double num1 = 0.0f;
            double num2 = double.MaxValue;
            if ((double)Math.Abs(ray.Direction.X) < 9.99999997475243E-07)
            {
                if ((double)ray.Position.X < (double)this.Min.X || (double)ray.Position.X > (double)this.Max.X)
                    return;
            }
            else
            {
                double num3 = 1f / ray.Direction.X;
                double num4 = (this.Min.X - ray.Position.X) * num3;
                double num5 = (this.Max.X - ray.Position.X) * num3;
                if ((double)num4 > (double)num5)
                {
                    double num6 = num4;
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
                double num3 = 1f / ray.Direction.Y;
                double num4 = (this.Min.Y - ray.Position.Y) * num3;
                double num5 = (this.Max.Y - ray.Position.Y) * num3;
                if ((double)num4 > (double)num5)
                {
                    double num6 = num4;
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
                double num3 = 1f / ray.Direction.Z;
                double num4 = (this.Min.Z - ray.Position.Z) * num3;
                double num5 = (this.Max.Z - ray.Position.Z) * num3;
                if ((double)num4 > (double)num5)
                {
                    double num6 = num4;
                    num4 = num5;
                    num5 = num6;
                }
                num1 = MathHelper.Max(num4, num1);
                double num7 = MathHelper.Min(num5, num2);
                if ((double)num1 > (double)num7)
                    return;
            }
            result = new double?(num1);
        }

        public bool Intersect(ref LineD line, out LineD intersectedLine)
        {
            var ray = new RayD(line.From, line.Direction);

            double t1, t2;
            if (!Intersect(ref ray, out t1, out t2))
            {
                intersectedLine = line;
                return false;
            }

            t1 = Math.Max(t1, 0);
            t2 = Math.Min(t2, line.Length);

            intersectedLine.From = line.From + line.Direction*t1;
            intersectedLine.To = line.From + line.Direction*t2;
            intersectedLine.Direction = line.Direction;
            intersectedLine.Length = t2 - t1;

            return true;
        }

        public bool Intersect(ref LineD line, out double t1, out double t2)
        {
            var ray = new RayD(line.From, line.Direction);
            return Intersect(ref ray, out t1, out t2);
        }

        public bool Intersect(ref RayD ray, out double tmin, out double tmax)
        {
            // r.dir is unit direction vector of ray
            var recipx = 1.0f / ray.Direction.X;
            var recipy = 1.0f / ray.Direction.Y;
            var recipz = 1.0f / ray.Direction.Z;
            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            double t1 = (Min.X - ray.Position.X) * recipx;
            double t2 = (Max.X - ray.Position.X) * recipx;
            double t3 = (Min.Y - ray.Position.Y) * recipy;
            double t4 = (Max.Y - ray.Position.Y) * recipy;
            double t5 = (Min.Z - ray.Position.Z) * recipz;
            double t6 = (Max.Z - ray.Position.Z) * recipz;

            tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            // if tmax < 0, ray (line) is intersecting AABB, but whole AABB is behing us
            if (tmax < 0)
            {
                return false;
            }

            // if tmin > tmax, ray doesn't intersect AABB
            if (tmin > tmax)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        public bool Intersects(BoundingSphereD sphere)
        {
            return Intersects(ref sphere);
        }

        /// <summary>
        /// Checks whether the current BoundingBox intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBox and BoundingSphere intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphereD sphere, out bool result)
        {
            Vector3D result1;
            Vector3D.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            double result2;
            Vector3D.DistanceSquared(ref sphere.Center, ref result1, out result2);
            result = (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        }

        public bool Intersects(ref BoundingSphereD sphere)
        {
            Vector3D result1;
            Vector3D.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            double result2;
            Vector3D.DistanceSquared(ref sphere.Center, ref result1, out result2);
            return (double)result2 <= (double)sphere.Radius * (double)sphere.Radius;
        }

        public double Distance(Vector3D point)
        {
            if (Contains(point) == ContainmentType.Contains)
                return 0;

            var clamp = Vector3D.Clamp(point, Min, Max);
            return Vector3D.Distance(clamp, point);
        }

        public double DistanceSquared(Vector3D point)
        {
            if (Contains(point) == ContainmentType.Contains)
                return 0;

            var clamp = Vector3D.Clamp(point, Min, Max);
            return Vector3D.DistanceSquared(clamp, point);
        }

        /// <summary>
        /// Tests whether the BoundingBox contains another BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for overlap.</param>
        public ContainmentType Contains(BoundingBoxD box)
        {
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y) || ((double)this.Max.Z < (double)box.Min.Z || (double)this.Min.Z > (double)box.Max.Z))
                return ContainmentType.Disjoint;
            return (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)box.Min.Z || (double)box.Max.Z > (double)this.Max.Z) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBoxD box, out ContainmentType result)
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
        public ContainmentType Contains(BoundingFrustumD frustum)
        {
            if (!frustum.Intersects(this))
                return ContainmentType.Disjoint;
            foreach (Vector3D point in frustum.cornerArray)
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
        public ContainmentType Contains(Vector3D point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3D point, out ContainmentType result)
        {
            result = (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) || ((double)this.Min.Z > (double)point.Z || (double)point.Z > (double)this.Max.Z) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param>
        public ContainmentType Contains(BoundingSphereD sphere)
        {
            Vector3D result1;
            Vector3D.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            double result2;
            Vector3D.DistanceSquared(ref sphere.Center, ref result1, out result2);
            double num = sphere.Radius;
            if ((double)result2 > (double)num * (double)num)
                return ContainmentType.Disjoint;
            return (double)this.Min.X + (double)num > (double)sphere.Center.X || (double)sphere.Center.X > (double)this.Max.X - (double)num || ((double)this.Max.X - (double)this.Min.X <= (double)num || (double)this.Min.Y + (double)num > (double)sphere.Center.Y) || ((double)sphere.Center.Y > (double)this.Max.Y - (double)num || (double)this.Max.Y - (double)this.Min.Y <= (double)num || ((double)this.Min.Z + (double)num > (double)sphere.Center.Z || (double)sphere.Center.Z > (double)this.Max.Z - (double)num)) || (double)this.Max.X - (double)this.Min.X <= (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox contains a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphereD sphere, out ContainmentType result)
        {
            Vector3D result1;
            Vector3D.Clamp(ref sphere.Center, ref this.Min, ref this.Max, out result1);
            double result2;
            Vector3D.DistanceSquared(ref sphere.Center, ref result1, out result2);
            double num = sphere.Radius;
            if ((double)result2 > (double)num * (double)num)
                result = ContainmentType.Disjoint;
            else
                result = (double)this.Min.X + (double)num > (double)sphere.Center.X || (double)sphere.Center.X > (double)this.Max.X - (double)num || ((double)this.Max.X - (double)this.Min.X <= (double)num || (double)this.Min.Y + (double)num > (double)sphere.Center.Y) || ((double)sphere.Center.Y > (double)this.Max.Y - (double)num || (double)this.Max.Y - (double)this.Min.Y <= (double)num || ((double)this.Min.Z + (double)num > (double)sphere.Center.Z || (double)sphere.Center.Z > (double)this.Max.Z - (double)num)) || (double)this.Max.X - (double)this.Min.X <= (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        internal void SupportMapping(ref Vector3D v, out Vector3D result)
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
        public BoundingBoxD Translate(MatrixD worldMatrix)
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
        public BoundingBoxD Translate(Vector3D vctTranlsation)
        {
            Min += vctTranlsation;
            Max += vctTranlsation;
            return this;
        }

        /// <summary>
        /// Size
        /// </summary>
        /// <returns></returns>
        public Vector3D Size
        {
            get
            {
                return Max - Min;
            }
        }

        /// <summary>
        /// Matrix of AABB, respection center and size
        /// </summary>
        public MatrixD Matrix
        {
            get
            {
                var center = Center;
                var size = Size;

                MatrixD result;
                MatrixD.CreateTranslation(ref center, out result);
                MatrixD.Rescale(ref result, ref size);
                return result;
            }
        }

        /// <summary>
        /// Transform this AABB by matrix.
        /// </summary>
        /// <param name="m">transformation matrix</param>
        /// <returns>transformed aabb</returns>
        public unsafe BoundingBoxD TransformSlow(MatrixD m)
        {
            return TransformSlow(ref m);
        }

        /// <summary>
        /// Transform this AABB by matrix.
        /// </summary>
        /// <param name="m">transformation matrix</param>
        /// <returns>transformed aabb</returns>
        public unsafe BoundingBoxD TransformSlow(ref MatrixD worldMatrix)
        {
            BoundingBoxD oobb = BoundingBoxD.CreateInvalid();

            Vector3D* temporaryCorners = stackalloc Vector3D[8];

            GetCornersUnsafe((Vector3D*)temporaryCorners);

            for (int i = 0; i < 8; i++)
            {
                Vector3D vctTransformed = Vector3D.Transform(temporaryCorners[i], worldMatrix);
                oobb = oobb.Include(ref vctTransformed);
            }

            return oobb;
        }

        /// <summary>
        /// Transform this AABB by matrix. Matrix has to be only rotation and translation.
        /// </summary>
        /// <param name="m">transformation matrix</param>
        /// <returns>transformed aabb</returns>
        public BoundingBoxD TransformFast(MatrixD m)
        {
            var bb = BoundingBoxD.CreateInvalid();
            TransformFast(ref m, ref bb);
            return bb;
        }

        /// <summary>
        /// Transform this AABB by matrix. Matrix has to be only rotation and translation.
        /// </summary>
        /// <param name="m">transformation matrix</param>
        /// <returns>transformed aabb</returns>
        public BoundingBoxD TransformFast(ref MatrixD m)
        {
            var bb = BoundingBoxD.CreateInvalid();
            TransformFast(ref m, ref bb);
            return bb;
        }

        /// <summary>
        /// Transform this AABB by matrix. Matrix has to be only rotation and translation.
        /// </summary>
        /// <param name="m">transformation matrix</param>
        /// <param name="bb">output transformed aabb</param>
        public void TransformFast(ref MatrixD m, ref BoundingBoxD bb)
        {
            Debug.Assert(Math.Abs(m.Up.LengthSquared() - 1) < 1e-4f, "Warning 1/5: Rotation part of matrix is not orthogonal. Transform will be wrong. Use TransformSlow instead.");
            Debug.Assert(Math.Abs(m.Right.LengthSquared() - 1) < 1e-4f, "Warning 2/5: Rotation part of matrix is not orthogonal. Transform will be wrong. Use TransformSlow instead.");
            Debug.Assert(Math.Abs(m.Forward.LengthSquared() - 1) < 1e-4f, "Warning 3/5: Rotation part of matrix is not orthogonal. Transform will be wrong. Use TransformSlow instead.");
            Debug.Assert(Math.Abs(m.Right.Dot(m.Up)) < 1e-4f, "Warning 4/5: Rotation part of matrix is not orthogonal. Transform will be wrong. Use TransformSlow instead.");
            Debug.Assert(Math.Abs(m.Right.Dot(m.Forward)) < 1e-4f, "Warning 5/5: Rotation part of matrix is not orthogonal. Transform will be wrong. Use TransformSlow instead. If you saw all warning you should really consider using TransformSlow.");

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
        public BoundingBoxD Include(ref Vector3D point)
        {
            Min.X = Math.Min(point.X, Min.X);
            Min.Y = Math.Min(point.Y, Min.Y);
            Min.Z = Math.Min(point.Z, Min.Z);

            Max.X = Math.Max(point.X, Max.X);
            Max.Y = Math.Max(point.Y, Max.Y);
            Max.Z = Math.Max(point.Z, Max.Z);
            return this;
        }

        public BoundingBoxD Include(Vector3D point)
        {
            return Include(ref point);
        }

        public BoundingBoxD Include(Vector3D p0, Vector3D p1, Vector3D p2)
        {
            return Include(ref p0, ref p1, ref p2);
        }

        public BoundingBoxD Include(ref Vector3D p0, ref Vector3D p1, ref Vector3D p2)
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
        public BoundingBoxD Include(ref BoundingBoxD box)
        {
            Min = Vector3D.Min(Min, box.Min);
            Max = Vector3D.Max(Max, box.Max);
            return this;
        }
        public BoundingBoxD Include(BoundingBoxD box)
        {
            return Include(ref box);
        }

        public void Include(ref LineD line)
        {
            Include(ref line.From);
            Include(ref line.To);
        }


        public BoundingBoxD Include(BoundingSphereD sphere)
        {
            return Include(ref sphere);
        }

        public BoundingBoxD Include(ref BoundingSphereD sphere)
        {
            Vector3D radius = new Vector3D(sphere.Radius);
            Vector3D minSphere = sphere.Center;
            Vector3D maxSphere = sphere.Center;

            Vector3D.Subtract(ref minSphere, ref radius, out minSphere);
            Vector3D.Add(ref maxSphere, ref radius, out maxSphere);

            Include(ref minSphere);
            Include(ref maxSphere);

            return this;
        }

        public unsafe BoundingBoxD Include(ref BoundingFrustumD frustum)
        {
            Vector3D* temporaryCorners = stackalloc Vector3D[8];

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

        public static BoundingBoxD CreateInvalid()
        {
            return new BoundingBoxD(new Vector3D(double.MaxValue), new Vector3D(double.MinValue));
        }

        public double SurfaceArea
        {
            get
            {
                Vector3D span = Max - Min;
                return 2 * (span.X * span.Y + span.X * span.Z + span.Y * span.Z);
            }
        }

        public double Volume
        {
            get
            {
                Vector3D span = Max - Min;
                return span.X * span.Y * span.Z;
            }
        }

        public double ProjectedArea(Vector3D viewDir)
        {
            Vector3D span = Max - Min;
            Vector3D size = new Vector3D(span.Y, span.Z, span.X) * new Vector3D(span.Z, span.X, span.Y);
            return Vector3D.Abs(viewDir).Dot(size);
        }

        /// <summary>
        /// return perimeter of edges
        /// </summary>
        /// <returns></returns>
        public double Perimeter
        {
            get
            {
                double wx = Max.X - Min.X;
                double wy = Max.Y - Min.Y;
                double wz = Max.Z - Min.Z;

                return 4.0 * (wx + wy + wz);
            }
        }

        public bool Valid
        {
            get
            {
                return Min != new Vector3D(double.MaxValue) && Max != new Vector3D(double.MinValue);
            }
        }

        public BoundingBoxD Inflate(double size)
        {
            Max += new Vector3D(size);
            Min -= new Vector3D(size);
            return this;
        }

        public BoundingBoxD Inflate(Vector3 size)
        {
            Max += size;
            Min -= size;
            return this;
        }

        public BoundingBoxD GetInflated(double size)
        {
            var bb = this;
            bb.Inflate(size);
            return bb;
        }

        public BoundingBoxD GetInflated(Vector3 size)
        {
            var bb = this;
            bb.Inflate(size);
            return bb;
        }

        public static explicit operator BoundingBoxD(BoundingBox b)
        {
            return new BoundingBoxD((Vector3D)b.Min, (Vector3D)b.Max);
        }

        public static explicit operator BoundingBox(BoundingBoxD b)
        {
            return new BoundingBox((Vector3)b.Min, (Vector3)b.Max);
        }

        #region Comparer

        public class ComparerType : IEqualityComparer<BoundingBox>
        {
            public bool Equals(BoundingBox x, BoundingBox y)
            {
                return x.Min == y.Min && x.Max == y.Max;
            }

            public int GetHashCode(BoundingBox obj)
            {
                return obj.Min.GetHashCode() ^ obj.Max.GetHashCode();
            }
        }

        public static readonly ComparerType Comparer = new ComparerType();

        #endregion

        public void InflateToMinimum(Vector3D minimumSize)
        {
            Vector3D minCenter = Center;
            if (Size.X < minimumSize.X)
            {
                Min.X = minCenter.X - minimumSize.X * 0.5;
                Max.X = minCenter.X + minimumSize.X * 0.5;
            }
            if (Size.Y < minimumSize.Y)
            {
                Min.Y = minCenter.Y - minimumSize.Y * 0.5;
                Max.Y = minCenter.Y + minimumSize.Y * 0.5;
            }
            if (Size.Z < minimumSize.Z)
            {
                Min.Z = minCenter.Z - minimumSize.Z * 0.5;
                Max.Z = minCenter.Z + minimumSize.Z * 0.5;
            }
        }

        public void InflateToMinimum(double minimumSize)
        {
            Vector3D minCenter = Center;
            if (Size.X < minimumSize)
            {
                Min.X = minCenter.X - minimumSize * 0.5;
                Max.X = minCenter.X + minimumSize * 0.5;
            }
            if (Size.Y < minimumSize)
            {
                Min.Y = minCenter.Y - minimumSize * 0.5;
                Max.Y = minCenter.Y + minimumSize * 0.5;
            }
            if (Size.Z < minimumSize)
            {
                Min.Z = minCenter.Z - minimumSize * 0.5;
                Max.Z = minCenter.Z + minimumSize * 0.5;
            }
        }

        [Conditional("DEBUG")]
        public void AssertIsValid()
        {
            Min.AssertIsValid();
            Max.AssertIsValid();
        }
    }
}
