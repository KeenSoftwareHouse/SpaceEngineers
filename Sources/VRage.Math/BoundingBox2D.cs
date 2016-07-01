using System;
using System.Collections.Generic;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines an axis-aligned box-shaped 3D volume.
    /// </summary>
    [Serializable]
    public struct BoundingBox2D : IEquatable<BoundingBox2D>
    {
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingBox2D.
        /// </summary>
        public const int CornerCount = 8;
        /// <summary>
        /// The minimum point the BoundingBox2D contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector2D Min;
        /// <summary>
        /// The maximum point the BoundingBox2D contains.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public Vector2D Max;

        /// <summary>
        /// Creates an instance of BoundingBox2D.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBox2D includes.</param><param name="max">The maximum point the BoundingBox2D includes.</param>
        public BoundingBox2D(Vector2D min, Vector2D max)
        {
            this.Min = min;
            this.Max = max;
        }

        /*/// <summary>
        /// Creates an instance of BoundingBox2D from BoundingBox2DD (helper for transformed BBs)
        /// </summary>
        /// <param name="bbd"></param>
        public BoundingBox2D(BoundingBox2DD bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }

        public BoundingBox2D(BoundingBox2DI bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }*/

        /// <summary>
        /// Determines whether two instances of BoundingBox2D are equal.
        /// </summary>
        /// <param name="a">BoundingBox2D to compare.</param><param name="b">BoundingBox2D to compare.</param>
        public static bool operator ==(BoundingBox2D a, BoundingBox2D b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2D are not equal.
        /// </summary>
        /// <param name="a">The object to the left of the inequality operator.</param><param name="b">The object to the right of the inequality operator.</param>
        public static bool operator !=(BoundingBox2D a, BoundingBox2D b)
        {
            if (!(a.Min != b.Min))
                return a.Max != b.Max;
            else
                return true;
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingBox2D.
        /// </summary>
        public Vector2D[] GetCorners()
        {
            return new Vector2D[8]
            {
                new Vector2D(this.Min.X, this.Max.Y),
                new Vector2D(this.Max.X, this.Max.Y),
                new Vector2D(this.Max.X, this.Min.Y),
                new Vector2D(this.Min.X, this.Min.Y),
                new Vector2D(this.Min.X, this.Max.Y),
                new Vector2D(this.Max.X, this.Max.Y),
                new Vector2D(this.Max.X, this.Min.Y),
                new Vector2D(this.Min.X, this.Min.Y)
            };
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox2D.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector2D points where the corners of the BoundingBox2D are written.</param>
        public void GetCorners(Vector2D[] corners)
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
        /// Gets the array of points that make up the corners of the BoundingBox2D.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector2D points where the corners of the BoundingBox2D are written.</param>
        public unsafe void GetCornersUnsafe(Vector2D* corners)
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
        /// Determines whether two instances of BoundingBox2D are equal.
        /// </summary>
        /// <param name="other">The BoundingBox2D to compare with the current BoundingBox2D.</param>
        public bool Equals(BoundingBox2D other)
        {
            if (this.Min == other.Min)
                return this.Max == other.Max;
            else
                return false;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2D are equal.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingBox2D.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is BoundingBox2D)
                flag = this.Equals((BoundingBox2D)obj);
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
        /// Returns a String that represents the current BoundingBox2D.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Min:{0} Max:{1}", Min, Max);
        }

        /// <summary>
        /// Creates the smallest BoundingBox2D that contains the two specified BoundingBox2D instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox2Ds to contain.</param><param name="additional">One of the BoundingBox2Ds to contain.</param>
        public static BoundingBox2D CreateMerged(BoundingBox2D original, BoundingBox2D additional)
        {
            BoundingBox2D BoundingBox2D;
            Vector2D.Min(ref original.Min, ref additional.Min, out BoundingBox2D.Min);
            Vector2D.Max(ref original.Max, ref additional.Max, out BoundingBox2D.Max);
            return BoundingBox2D;
        }

        /// <summary>
        /// Creates the smallest BoundingBox2D that contains the two specified BoundingBox2D instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox2D instances to contain.</param><param name="additional">One of the BoundingBox2D instances to contain.</param><param name="result">[OutAttribute] The created BoundingBox2D.</param>
        public static void CreateMerged(ref BoundingBox2D original, ref BoundingBox2D additional, out BoundingBox2D result)
        {
            Vector2D result1;
            Vector2D.Min(ref original.Min, ref additional.Min, out result1);
            Vector2D result2;
            Vector2D.Max(ref original.Max, ref additional.Max, out result2);
            result.Min = result1;
            result.Max = result2;
        }

        /// <summary>
        /// Creates the smallest BoundingBox2D that will contain a group of points.
        /// </summary>
        /// <param name="points">A list of points the BoundingBox2D should contain.</param>
        public static BoundingBox2D CreateFromPoints(IEnumerable<Vector2D> points)
        {
            if (points == null)
                throw new ArgumentNullException();
            bool flag = false;
            Vector2D result1 = new Vector2D(double.MaxValue);
            Vector2D result2 = new Vector2D(double.MinValue);
            foreach (Vector2D Vector2D in points)
            {
                Vector2D vec3 = Vector2D;
                Vector2D.Min(ref result1, ref vec3, out result1);
                Vector2D.Max(ref result2, ref vec3, out result2);
                flag = true;
            }
            if (!flag)
                throw new ArgumentException();
            else
                return new BoundingBox2D(result1, result2);
        }

        public static BoundingBox2D CreateFromHalfExtent(Vector2D center, double halfExtent)
        {
            return CreateFromHalfExtent(center, new Vector2D(halfExtent));
        }

        public static BoundingBox2D CreateFromHalfExtent(Vector2D center, Vector2D halfExtent)
        {
            return new BoundingBox2D(center - halfExtent, center + halfExtent);
        }

        /// <summary>
        /// Returns bounding box which is intersection of this and box
        /// It's called 'Prunik'
        /// Result is invalid box when there's no intersection (Min > Max)
        /// </summary>
        public BoundingBox2D Intersect(BoundingBox2D box)
        {
            BoundingBox2D result;
            result.Min.X = Math.Max(this.Min.X, box.Min.X);
            result.Min.Y = Math.Max(this.Min.Y, box.Min.Y);
            result.Max.X = Math.Min(this.Max.X, box.Max.X);
            result.Max.Y = Math.Min(this.Max.Y, box.Max.Y);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingBox2D intersects another BoundingBox2D.
        /// </summary>
        /// <param name="box">The BoundingBox2D to check for intersection with.</param>
        public bool Intersects(BoundingBox2D box)
        {
            return Intersects(ref box);
        }

        public bool Intersects(ref BoundingBox2D box)
        {
            return (double)this.Max.X >= (double)box.Min.X && (double)this.Min.X <= (double)box.Max.X && ((double)this.Max.Y >= (double)box.Min.Y && (double)this.Min.Y <= (double)box.Max.Y);
        }

        /// <summary>
        /// Checks whether the current BoundingBox2D intersects another BoundingBox2D.
        /// </summary>
        /// <param name="box">The BoundingBox2D to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBox2D instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox2D box, out bool result)
        {
            result = false;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y))
                return;
            result = true;
        }

        /// <summary>
        /// Calculates center
        /// </summary>
        public Vector2D Center
        {
            get { return (Min + Max) / 2; }
        }

        public Vector2D HalfExtents
        {
            get { return (Max - Min) / 2; }
        }

        public Vector2D Extents
        {
            get { return Max - Min; }
        }

        public double Width
        {
            get { return Max.X - Min.X; }
        }

        public double Height
        {
            get { return Max.Y - Min.Y; }
        }

        public double Distance(Vector2D point)
        {
            var clamp = Vector2D.Clamp(point, Min, Max);
            return Vector2D.Distance(clamp, point);
        }

        /// <summary>
        /// Tests whether the BoundingBox2D contains another BoundingBox2D.
        /// </summary>
        /// <param name="box">The BoundingBox2D to test for overlap.</param>
        public ContainmentType Contains(BoundingBox2D box)
        {
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y))
                return ContainmentType.Disjoint;
            return (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox2D contains a BoundingBox2D.
        /// </summary>
        /// <param name="box">The BoundingBox2D to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox2D box, out ContainmentType result)
        {
            result = ContainmentType.Disjoint;
            if ((double)this.Max.X < (double)box.Min.X || (double)this.Min.X > (double)box.Max.X || ((double)this.Max.Y < (double)box.Min.Y || (double)this.Min.Y > (double)box.Max.Y))
                return;
            result = (double)this.Min.X > (double)box.Min.X || (double)box.Max.X > (double)this.Max.X || ((double)this.Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)this.Max.Y) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox2D contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param>
        public ContainmentType Contains(Vector2D point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /*public ContainmentType Contains(Vector2DD point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }*/

        /// <summary>
        /// Tests whether the BoundingBox2D contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector2D point, out ContainmentType result)
        {
            result = (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        internal void SupportMapping(ref Vector2D v, out Vector2D result)
        {
            result.X = (double)v.X >= 0.0 ? this.Max.X : this.Min.X;
            result.Y = (double)v.Y >= 0.0 ? this.Max.Y : this.Min.Y;
        }

        /// <summary>
        /// Translate
        /// </summary>
        /// <param name="vctTranlsation"></param>
        /// <returns></returns>
        public BoundingBox2D Translate(Vector2D vctTranlsation)
        {
            Min += vctTranlsation;
            Max += vctTranlsation;
            return this;
        }

        /// <summary>
        /// Size
        /// </summary>
        /// <returns></returns>
        public Vector2D Size
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
        public BoundingBox2D Include(ref Vector2D point)
        {
            Min.X = Math.Min(point.X, Min.X);
            Min.Y = Math.Min(point.Y, Min.Y);
            Max.X = Math.Max(point.X, Max.X);
            Max.Y = Math.Max(point.Y, Max.Y);
            return this;
        }

        public BoundingBox2D GetIncluded(Vector2D point)
        {
            BoundingBox2D b = this;
            b.Include(point);
            return b;
        }

        public BoundingBox2D Include(Vector2D point)
        {
            return Include(ref point);
        }

        public BoundingBox2D Include(Vector2D p0, Vector2D p1, Vector2D p2)
        {
            return Include(ref p0, ref p1, ref p2);
        }

        public BoundingBox2D Include(ref Vector2D p0, ref Vector2D p1, ref Vector2D p2)
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
        public BoundingBox2D Include(ref BoundingBox2D box)
        {
            Min = Vector2D.Min(Min, box.Min);
            Max = Vector2D.Max(Max, box.Max);
            return this;
        }
        public BoundingBox2D Include(BoundingBox2D box)
        {
            return Include(ref box);
        }

        public static BoundingBox2D CreateInvalid()
        {
            BoundingBox2D bbox = new BoundingBox2D();
            Vector2D vctMin = new Vector2D(double.MaxValue, double.MaxValue);
            Vector2D vctMax = new Vector2D(double.MinValue, double.MinValue);

            bbox.Min = vctMin;
            bbox.Max = vctMax;

            return bbox;
        }

        public double Perimeter()
        {
            Vector2D span = Max - Min;
            return 2 * (span.X = span.Y);
        }

        public double Area()
        {
            Vector2D span = Max - Min;
            return span.X * span.Y;
        }

        public void Inflate(double size)
        {
            Max += new Vector2D(size);
            Min -= new Vector2D(size);
        }

        public void InflateToMinimum(Vector2D minimumSize)
        {
            Vector2D minCenter = Center;
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

        public class ComparerType : IEqualityComparer<BoundingBox2DD>
        {
            public bool Equals(BoundingBox2DD x, BoundingBox2DD y)
            {
                return x.Min == y.Min && x.Max == y.Max;
            }

            public int GetHashCode(BoundingBox2DD obj)
            {
                return obj.Min.GetHashCode() ^ obj.Max.GetHashCode();
            }
        }

        public static readonly ComparerType Comparer = new ComparerType();

        #endregion*/

        public void Scale(Vector2D scale)
        {
            Vector2D center = Center;
            Vector2D scaled = HalfExtents * scale;
            Min = center - scaled;
            Max = center + scaled;
        }
    }
}
