using System;
using System.Collections.Generic;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines an axis-aligned box-shaped 3D volume.
    /// </summary>
    [Serializable]
    public struct BoundingBox2 : IEquatable<BoundingBox2>
    {
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingBox2.
        /// </summary>
        public const int CornerCount = 8;
        /// <summary>
        /// The minimum point the BoundingBox2 contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector2 Min;
        /// <summary>
        /// The maximum point the BoundingBox2 contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector2 Max;

        /// <summary>
        /// Creates an instance of BoundingBox2.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBox2 includes.</param><param name="max">The maximum point the BoundingBox2 includes.</param>
        public BoundingBox2(Vector2 min, Vector2 max)
        {
            this.Min = min;
            this.Max = max;
        }

        /*/// <summary>
        /// Creates an instance of BoundingBox2 from BoundingBox2D (helper for transformed BBs)
        /// </summary>
        /// <param name="bbd"></param>
        public BoundingBox2(BoundingBox2D bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }

        public BoundingBox2(BoundingBox2I bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }*/

        /// <summary>
        /// Determines whether two instances of BoundingBox2 are equal.
        /// </summary>
        /// <param name="a">BoundingBox2 to compare.</param><param name="b">BoundingBox2 to compare.</param>
        public static bool operator ==(BoundingBox2 a, BoundingBox2 b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2 are not equal.
        /// </summary>
        /// <param name="a">The object to the left of the inequality operator.</param><param name="b">The object to the right of the inequality operator.</param>
        public static bool operator !=(BoundingBox2 a, BoundingBox2 b)
        {
            if (!(a.Min != b.Min))
                return a.Max != b.Max;
            else
                return true;
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingBox2.
        /// </summary>
        public Vector2[] GetCorners()
        {
            return new Vector2[8]
            {
                new Vector2(this.Min.X, this.Max.Y),
                new Vector2(this.Max.X, this.Max.Y),
                new Vector2(this.Max.X, this.Min.Y),
                new Vector2(this.Min.X, this.Min.Y),
                new Vector2(this.Min.X, this.Max.Y),
                new Vector2(this.Max.X, this.Max.Y),
                new Vector2(this.Max.X, this.Min.Y),
                new Vector2(this.Min.X, this.Min.Y)
            };
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox2.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector2 points where the corners of the BoundingBox2 are written.</param>
        public void GetCorners(Vector2[] corners)
        {
            corners[0].X = this.Min.X;
            corners[0].Y = this.Max.Y;
            corners[1].X = this.Max.X;
            corners[1].Y = this.Max.Y;
            corners[2].X = this.Max.X;
            corners[2].Y = this.Min.Y;
            corners[3].X = this.Min.X;
            corners[3].Y = this.Min.Y;
            corners[4].X = this.Min.X;
            corners[4].Y = this.Max.Y;
            corners[5].X = this.Max.X;
            corners[5].Y = this.Max.Y;
            corners[6].X = this.Max.X;
            corners[6].Y = this.Min.Y;
            corners[7].X = this.Min.X;
            corners[7].Y = this.Min.Y;
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox2.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector2 points where the corners of the BoundingBox2 are written.</param>
        public unsafe void GetCornersUnsafe(Vector2* corners)
        {
            corners[0].X = this.Min.X;
            corners[0].Y = this.Max.Y;
            corners[1].X = this.Max.X;
            corners[1].Y = this.Max.Y;
            corners[2].X = this.Max.X;
            corners[2].Y = this.Min.Y;
            corners[3].X = this.Min.X;
            corners[3].Y = this.Min.Y;
            corners[4].X = this.Min.X;
            corners[4].Y = this.Max.Y;
            corners[5].X = this.Max.X;
            corners[5].Y = this.Max.Y;
            corners[6].X = this.Max.X;
            corners[6].Y = this.Min.Y;
            corners[7].X = this.Min.X;
            corners[7].Y = this.Min.Y;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2 are equal.
        /// </summary>
        /// <param name="other">The BoundingBox2 to compare with the current BoundingBox2.</param>
        public bool Equals(BoundingBox2 other)
        {
            if (this.Min == other.Min)
                return this.Max == other.Max;
            else
                return false;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2 are equal.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingBox2.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is BoundingBox2)
                flag = this.Equals((BoundingBox2)obj);
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
        /// Returns a String that represents the current BoundingBox2.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Min:{0} Max:{1}", Min, Max);
        }

        /// <summary>
        /// Creates the smallest BoundingBox2 that contains the two specified BoundingBox2 instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox2s to contain.</param><param name="additional">One of the BoundingBox2s to contain.</param>
        public static BoundingBox2 CreateMerged(BoundingBox2 original, BoundingBox2 additional)
        {
            BoundingBox2 BoundingBox2;
            Vector2.Min(ref original.Min, ref additional.Min, out BoundingBox2.Min);
            Vector2.Max(ref original.Max, ref additional.Max, out BoundingBox2.Max);
            return BoundingBox2;
        }

        /// <summary>
        /// Creates the smallest BoundingBox2 that contains the two specified BoundingBox2 instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox2 instances to contain.</param><param name="additional">One of the BoundingBox2 instances to contain.</param><param name="result">[OutAttribute] The created BoundingBox2.</param>
        public static void CreateMerged(ref BoundingBox2 original, ref BoundingBox2 additional, out BoundingBox2 result)
        {
            Vector2 result1;
            Vector2.Min(ref original.Min, ref additional.Min, out result1);
            Vector2 result2;
            Vector2.Max(ref original.Max, ref additional.Max, out result2);
            result.Min = result1;
            result.Max = result2;
        }

        /// <summary>
        /// Creates the smallest BoundingBox2 that will contain a group of points.
        /// </summary>
        /// <param name="points">A list of points the BoundingBox2 should contain.</param>
        public static BoundingBox2 CreateFromPoints(IEnumerable<Vector2> points)
        {
            if (points == null)
                throw new ArgumentNullException();
            bool flag = false;
            Vector2 result1 = new Vector2(float.MaxValue);
            Vector2 result2 = new Vector2(float.MinValue);
            foreach (Vector2 Vector2 in points)
            {
                Vector2 vec3 = Vector2;
                Vector2.Min(ref result1, ref vec3, out result1);
                Vector2.Max(ref result2, ref vec3, out result2);
                flag = true;
            }
            if (!flag)
                throw new ArgumentException();
            else
                return new BoundingBox2(result1, result2);
        }

        public static BoundingBox2 CreateFromHalfExtent(Vector2 center, float halfExtent)
        {
            return CreateFromHalfExtent(center, new Vector2(halfExtent));
        }

        public static BoundingBox2 CreateFromHalfExtent(Vector2 center, Vector2 halfExtent)
        {
            return new BoundingBox2(center - halfExtent, center + halfExtent);
        }

        /// <summary>
        /// Returns bounding box which is intersection of this and box
        /// It's called 'Prunik'
        /// Result is invalid box when there's no intersection (Min > Max)
        /// </summary>
        public BoundingBox2 Intersect(BoundingBox2 box)
        {
            BoundingBox2 result;
            result.Min.X = Math.Max(this.Min.X, box.Min.X);
            result.Min.Y = Math.Max(this.Min.Y, box.Min.Y);
            result.Max.X = Math.Min(this.Max.X, box.Max.X);
            result.Max.Y = Math.Min(this.Max.Y, box.Max.Y);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingBox2 intersects another BoundingBox2.
        /// </summary>
        /// <param name="box">The BoundingBox2 to check for intersection with.</param>
        public bool Intersects(BoundingBox2 box)
        {
            return Intersects(ref box);
        }

        public bool Intersects(ref BoundingBox2 box)
        {
            return (double)this.Max.X >= (double)box.Min.X && (double)this.Min.X <= (double)box.Max.X && ((double)this.Max.Y >= (double)box.Min.Y && (double)this.Min.Y <= (double)box.Max.Y);
        }

        /// <summary>
        /// Checks whether the current BoundingBox2 intersects another BoundingBox2.
        /// </summary>
        /// <param name="box">The BoundingBox2 to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBox2 instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox2 box, out bool result)
        {
            result = false;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y))
                return;
            result = true;
        }

        /// <summary>
        /// Calculates center
        /// </summary>
        public Vector2 Center
        {
            get { return (Min + Max) / 2; }
        }

        public Vector2 HalfExtents
        {
            get { return (Max - Min) / 2; }
        }

        public Vector2 Extents
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

        public float Distance(Vector2 point)
        {
            var clamp = Vector2.Clamp(point, Min, Max);
            return Vector2.Distance(clamp, point);
        }

        /// <summary>
        /// Tests whether the BoundingBox2 contains another BoundingBox2.
        /// </summary>
        /// <param name="box">The BoundingBox2 to test for overlap.</param>
        public ContainmentType Contains(BoundingBox2 box)
        {
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y))
                return ContainmentType.Disjoint;
            return (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox2 contains a BoundingBox2.
        /// </summary>
        /// <param name="box">The BoundingBox2 to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox2 box, out ContainmentType result)
        {
            result = ContainmentType.Disjoint;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y))
                return;
            result = (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox2 contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param>
        public ContainmentType Contains(Vector2 point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /*public ContainmentType Contains(Vector2D point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }*/

        /// <summary>
        /// Tests whether the BoundingBox2 contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector2 point, out ContainmentType result)
        {
            result = (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        internal void SupportMapping(ref Vector2 v, out Vector2 result)
        {
            result.X = (double)v.X >= 0.0 ? this.Max.X : this.Min.X;
            result.Y = (double)v.Y >= 0.0 ? this.Max.Y : this.Min.Y;
        }

        /// <summary>
        /// Translate
        /// </summary>
        /// <param name="vctTranlsation"></param>
        /// <returns></returns>
        public BoundingBox2 Translate(Vector2 vctTranlsation)
        {
            Min += vctTranlsation;
            Max += vctTranlsation;
            return this;
        }

        /// <summary>
        /// Size
        /// </summary>
        /// <returns></returns>
        public Vector2 Size
        {
            get
            {
                return Max - Min;
            }
        }

        /// <summary>
        /// return expanded aabb (abb include point)
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public BoundingBox2 Include(ref Vector2 point)
        {
            Min.X = Math.Min(point.X, Min.X);
            Min.Y = Math.Min(point.Y, Min.Y);
            Max.X = Math.Max(point.X, Max.X);
            Max.Y = Math.Max(point.Y, Max.Y);
            return this;
        }

        public BoundingBox2 GetIncluded(Vector2 point)
        {
            BoundingBox2 b = this;
            b.Include(point);
            return b;
        }

        public BoundingBox2 Include(Vector2 point)
        {
            return Include(ref point);
        }

        public BoundingBox2 Include(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            return Include(ref p0, ref p1, ref p2);
        }

        public BoundingBox2 Include(ref Vector2 p0, ref Vector2 p1, ref Vector2 p2)
        {
            Include(ref p0);
            Include(ref p1);
            Include(ref p2);

            return this;
        }

        /// <summary>
        /// return expanded aabb (abb include point)
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public BoundingBox2 Include(ref BoundingBox2 box)
        {
            Min = Vector2.Min(Min, box.Min);
            Max = Vector2.Max(Max, box.Max);
            return this;
        }
        public BoundingBox2 Include(BoundingBox2 box)
        {
            return Include(ref box);
        }

        public static BoundingBox2 CreateInvalid()
        {
            BoundingBox2 bbox = new BoundingBox2();
            Vector2 vctMin = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 vctMax = new Vector2(float.MinValue, float.MinValue);

            bbox.Min = vctMin;
            bbox.Max = vctMax;

            return bbox;
        }

        public float Perimeter()
        {
            Vector2 span = Max - Min;
            return 2 * (span.X = span.Y);
        }

        public float Area()
        {
            Vector2 span = Max - Min;
            return span.X * span.Y;
        }

        public void Inflate(float size)
        {
            Max += new Vector2(size);
            Min -= new Vector2(size);
        }

        public void InflateToMinimum(Vector2 minimumSize)
        {
            Vector2 minCenter = Center;
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
        }

        /*#region Comparer

        public class ComparerType : IEqualityComparer<BoundingBox2D>
        {
            public bool Equals(BoundingBox2D x, BoundingBox2D y)
            {
                return x.Min == y.Min && x.Max == y.Max;
            }

            public int GetHashCode(BoundingBox2D obj)
            {
                return obj.Min.GetHashCode() ^ obj.Max.GetHashCode();
            }
        }

        public static readonly ComparerType Comparer = new ComparerType();

        #endregion*/

        public void Scale(Vector2 scale)
        {
            Vector2 center = Center;
            Vector2 scaled = HalfExtents * scale;
            Min = center - scaled;
            Max = center + scaled;
        }
    }
}
