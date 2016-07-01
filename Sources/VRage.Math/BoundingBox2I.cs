using System;
using System.Collections.Generic;
using ProtoBuf;

namespace VRageMath
{
    /// <summary>
    /// Defines an axis-aligned box-shaped 3D volume.
    /// </summary>
    [Serializable]
    public struct BoundingBox2I : IEquatable<BoundingBox2I>
    {
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingBox2I.
        /// </summary>
        public const int CornerCount = 8;
        /// <summary>
        /// The minimum point the BoundingBox2I contains.
        /// </summary>
        [ProtoMember]
        public Vector2I Min;
        /// <summary>
        /// The maximum point the BoundingBox2I contains.
        /// </summary>
        [ProtoMember]
        public Vector2I Max;

        /// <summary>
        /// Creates an instance of BoundingBox2I.
        /// </summary>
        /// <param name="min">The minimum point the BoundingBox2I includes.</param><param name="max">The maximum point the BoundingBox2I includes.</param>
        public BoundingBox2I(Vector2I min, Vector2I max)
        {
            Min = min;
            Max = max;
        }

        /*/// <summary>
        /// Creates an instance of BoundingBox2I from BoundingBox2ID (helper for transformed BBs)
        /// </summary>
        /// <param name="bbd"></param>
        public BoundingBox2I(BoundingBox2ID bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }

        public BoundingBox2I(BoundingBox2II bbd)
        {
            this.Min = bbd.Min;
            this.Max = bbd.Max;
        }*/

        /// <summary>
        /// Determines whether two instances of BoundingBox2I are equal.
        /// </summary>
        /// <param name="a">BoundingBox2I to compare.</param><param name="b">BoundingBox2I to compare.</param>
        public static bool operator ==(BoundingBox2I a, BoundingBox2I b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2I are not equal.
        /// </summary>
        /// <param name="a">The object to the left of the inequality operator.</param><param name="b">The object to the right of the inequality operator.</param>
        public static bool operator !=(BoundingBox2I a, BoundingBox2I b)
        {
            if (!(a.Min != b.Min))
                return a.Max != b.Max;
            return true;
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingBox2I.
        /// </summary>
        public Vector2I[] GetCorners()
        {
            return new Vector2I[8]
            {
                new Vector2I(Min.X, Max.Y),
                new Vector2I(Max.X, Max.Y),
                new Vector2I(Max.X, Min.Y),
                new Vector2I(Min.X, Min.Y),
                new Vector2I(Min.X, Max.Y),
                new Vector2I(Max.X, Max.Y),
                new Vector2I(Max.X, Min.Y),
                new Vector2I(Min.X, Min.Y)
            };
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox2I.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector2I points where the corners of the BoundingBox2I are written.</param>
        public void GetCorners(Vector2I[] corners)
        {
            corners[0].X = Min.X;
            corners[0].Y = Max.Y;
            corners[1].X = Max.X;
            corners[1].Y = Max.Y;
            corners[2].X = Max.X;
            corners[2].Y = Min.Y;
            corners[3].X = Min.X;
            corners[3].Y = Min.Y;
            corners[4].X = Min.X;
            corners[4].Y = Max.Y;
            corners[5].X = Max.X;
            corners[5].Y = Max.Y;
            corners[6].X = Max.X;
            corners[6].Y = Min.Y;
            corners[7].X = Min.X;
            corners[7].Y = Min.Y;
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox2I.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector2I points where the corners of the BoundingBox2I are written.</param>
        public unsafe void GetCornersUnsafe(Vector2I* corners)
        {
            corners[0].X = Min.X;
            corners[0].Y = Max.Y;
            corners[1].X = Max.X;
            corners[1].Y = Max.Y;
            corners[2].X = Max.X;
            corners[2].Y = Min.Y;
            corners[3].X = Min.X;
            corners[3].Y = Min.Y;
            corners[4].X = Min.X;
            corners[4].Y = Max.Y;
            corners[5].X = Max.X;
            corners[5].Y = Max.Y;
            corners[6].X = Max.X;
            corners[6].Y = Min.Y;
            corners[7].X = Min.X;
            corners[7].Y = Min.Y;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2I are equal.
        /// </summary>
        /// <param name="other">The BoundingBox2I to compare with the current BoundingBox2I.</param>
        public bool Equals(BoundingBox2I other)
        {
            if (Min == other.Min)
                return Max == other.Max;
            return false;
        }

        /// <summary>
        /// Determines whether two instances of BoundingBox2I are equal.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingBox2I.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is BoundingBox2I)
                flag = Equals((BoundingBox2I)obj);
            return flag;
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return Min.GetHashCode() + Max.GetHashCode();
        }

        /// <summary>
        /// Returns a String that represents the current BoundingBox2I.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Min:{0} Max:{1}", Min, Max);
        }

        /// <summary>
        /// Creates the smallest BoundingBox2I that contains the two specified BoundingBox2I instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox2Is to contain.</param><param name="additional">One of the BoundingBox2Is to contain.</param>
        public static BoundingBox2I CreateMerged(BoundingBox2I original, BoundingBox2I additional)
        {
            BoundingBox2I boundingBox2I;
            Vector2I.Min(ref original.Min, ref additional.Min, out boundingBox2I.Min);
            Vector2I.Max(ref original.Max, ref additional.Max, out boundingBox2I.Max);
            return boundingBox2I;
        }

        /// <summary>
        /// Creates the smallest BoundingBox2I that contains the two specified BoundingBox2I instances.
        /// </summary>
        /// <param name="original">One of the BoundingBox2I instances to contain.</param><param name="additional">One of the BoundingBox2I instances to contain.</param><param name="result">[OutAttribute] The created BoundingBox2I.</param>
        public static void CreateMerged(ref BoundingBox2I original, ref BoundingBox2I additional, out BoundingBox2I result)
        {
            Vector2I result1;
            Vector2I.Min(ref original.Min, ref additional.Min, out result1);
            Vector2I result2;
            Vector2I.Max(ref original.Max, ref additional.Max, out result2);
            result.Min = result1;
            result.Max = result2;
        }

        /// <summary>
        /// Creates the smallest BoundingBox2I that will contain a group of points.
        /// </summary>
        /// <param name="points">A list of points the BoundingBox2I should contain.</param>
        public static BoundingBox2I CreateFromPoints(IEnumerable<Vector2I> points)
        {
            if (points == null)
                throw new ArgumentNullException();
            bool flag = false;
            Vector2I result1 = new Vector2I(int.MaxValue);
            Vector2I result2 = new Vector2I(int.MinValue);
            foreach (Vector2I vector2I in points)
            {
                Vector2I vec3 = vector2I;
                Vector2I.Min(ref result1, ref vec3, out result1);
                Vector2I.Max(ref result2, ref vec3, out result2);
                flag = true;
            }
            if (!flag)
                throw new ArgumentException();
            return new BoundingBox2I(result1, result2);
        }

        public static BoundingBox2I CreateFromHalfExtent(Vector2I center, int halfExtent)
        {
            return CreateFromHalfExtent(center, new Vector2I(halfExtent));
        }

        public static BoundingBox2I CreateFromHalfExtent(Vector2I center, Vector2I halfExtent)
        {
            return new BoundingBox2I(center - halfExtent, center + halfExtent);
        }

        /// <summary>
        /// Returns bounding box which is intersection of this and box
        /// It's called 'Prunik'
        /// Result is invalid box when there's no intersection (Min > Max)
        /// </summary>
        public BoundingBox2I Intersect(BoundingBox2I box)
        {
            BoundingBox2I result;
            result.Min.X = Math.Max(Min.X, box.Min.X);
            result.Min.Y = Math.Max(Min.Y, box.Min.Y);
            result.Max.X = Math.Min(Max.X, box.Max.X);
            result.Max.Y = Math.Min(Max.Y, box.Max.Y);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingBox2I intersects another BoundingBox2I.
        /// </summary>
        /// <param name="box">The BoundingBox2I to check for intersection with.</param>
        public bool Intersects(BoundingBox2I box)
        {
            return Intersects(ref box);
        }

        public bool Intersects(ref BoundingBox2I box)
        {
            return Max.X >= (double)box.Min.X && Min.X <= (double)box.Max.X && (Max.Y >= (double)box.Min.Y && Min.Y <= (double)box.Max.Y);
        }

        /// <summary>
        /// Checks whether the current BoundingBox2I intersects another BoundingBox2I.
        /// </summary>
        /// <param name="box">The BoundingBox2I to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingBox2I instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox2I box, out bool result)
        {
            result = false;
            if (Max.X < (double)box.Min.X || Min.X > (double)box.Max.X || (Max.Y < (double)box.Min.Y || Min.Y > (double)box.Max.Y))
                return;
            result = true;
        }

        /// <summary>
        /// Calculates center
        /// </summary>
        public Vector2I Center
        {
            get { return (Min + Max) / 2; }
        }

        public Vector2I HalfExtents
        {
            get { return (Max - Min) / 2; }
        }

        public Vector2I Extents
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

        /// <summary>
        /// Tests whether the BoundingBox2I contains another BoundingBox2I.
        /// </summary>
        /// <param name="box">The BoundingBox2I to test for overlap.</param>
        public ContainmentType Contains(BoundingBox2I box)
        {
            if (Max.X < (double)box.Min.X || Min.X > (double)box.Max.X || (Max.Y < (double)box.Min.Y || Min.Y > (double)box.Max.Y))
                return ContainmentType.Disjoint;
            return (double)Min.X > (double)box.Min.X || (double)box.Max.X > (double)Max.X || ((double)Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)Max.Y) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox2I contains a BoundingBox2I.
        /// </summary>
        /// <param name="box">The BoundingBox2I to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox2I box, out ContainmentType result)
        {
            result = ContainmentType.Disjoint;
            if (Max.X < (double)box.Min.X || Min.X > (double)box.Max.X || (Max.Y < (double)box.Min.Y || Min.Y > (double)box.Max.Y))
                return;
            result = (double)Min.X > (double)box.Min.X || (double)box.Max.X > (double)Max.X || ((double)Min.Y > (double)box.Min.Y || (double)box.Max.Y > (double)Max.Y) ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the BoundingBox2I contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param>
        public ContainmentType Contains(Vector2I point)
        {
            return (double)Min.X > (double)point.X || (double)point.X > (double)Max.X || ((double)Min.Y > (double)point.Y || (double)point.Y > (double)Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /*public ContainmentType Contains(Vector2ID point)
        {
            return (double)this.Min.X > (double)point.X || (double)point.X > (double)this.Max.X || ((double)this.Min.Y > (double)point.Y || (double)point.Y > (double)this.Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }*/

        /// <summary>
        /// Tests whether the BoundingBox2I contains a point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector2I point, out ContainmentType result)
        {
            result = (double)Min.X > (double)point.X || (double)point.X > (double)Max.X || ((double)Min.Y > (double)point.Y || (double)point.Y > (double)Max.Y) ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        internal void SupportMapping(ref Vector2I v, out Vector2I result)
        {
            result.X = (double)v.X >= 0.0 ? Max.X : Min.X;
            result.Y = (double)v.Y >= 0.0 ? Max.Y : Min.Y;
        }

        /// <summary>
        /// Translate
        /// </summary>
        /// <param name="vctTranlsation"></param>
        /// <returns></returns>
        public BoundingBox2I Translate(Vector2I vctTranlsation)
        {
            Min += vctTranlsation;
            Max += vctTranlsation;
            return this;
        }

        /// <summary>
        /// Size
        /// </summary>
        /// <returns></returns>
        public Vector2I Size
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
        public BoundingBox2I Include(ref Vector2I point)
        {
            Min.X = Math.Min(point.X, Min.X);
            Min.Y = Math.Min(point.Y, Min.Y);
            Max.X = Math.Max(point.X, Max.X);
            Max.Y = Math.Max(point.Y, Max.Y);
            return this;
        }

        public BoundingBox2I GetIncluded(Vector2I point)
        {
            BoundingBox2I b = this;
            b.Include(point);
            return b;
        }

        public BoundingBox2I Include(Vector2I point)
        {
            return Include(ref point);
        }

        public BoundingBox2I Include(Vector2I p0, Vector2I p1, Vector2I p2)
        {
            return Include(ref p0, ref p1, ref p2);
        }

        public BoundingBox2I Include(ref Vector2I p0, ref Vector2I p1, ref Vector2I p2)
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
        public BoundingBox2I Include(ref BoundingBox2I box)
        {
            Min = Vector2I.Min(Min, box.Min);
            Max = Vector2I.Max(Max, box.Max);
            return this;
        }
        public BoundingBox2I Include(BoundingBox2I box)
        {
            return Include(ref box);
        }

        public static BoundingBox2I CreateInvalid()
        {
            BoundingBox2I bbox = new BoundingBox2I();
            Vector2I vctMin = new Vector2I(int.MaxValue, int.MaxValue);
            Vector2I vctMax = new Vector2I(int.MinValue, int.MinValue);

            bbox.Min = vctMin;
            bbox.Max = vctMax;

            return bbox;
        }

        public float Perimeter()
        {
            Vector2I span = Max - Min;
            return 2 * (span.X = span.Y);
        }

        public float Area()
        {
            Vector2I span = Max - Min;
            return span.X * span.Y;
        }

        public void Inflate(int size)
        {
            Max += new Vector2I(size);
            Min -= new Vector2I(size);
        }

        public void InflateToMinimum(Vector2I minimumSize)
        {
            Vector2I minCenter = Center;
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

        public class ComparerType : IEqualityComparer<BoundingBox2ID>
        {
            public bool Equals(BoundingBox2ID x, BoundingBox2ID y)
            {
                return x.Min == y.Min && x.Max == y.Max;
            }

            public int GetHashCode(BoundingBox2ID obj)
            {
                return obj.Min.GetHashCode() ^ obj.Max.GetHashCode();
            }
        }

        public static readonly ComparerType Comparer = new ComparerType();

        #endregion*/

        public void Scale(Vector2I scale)
        {
            Vector2I center = Center;
            Vector2I scaled = HalfExtents;

            scaled.X *= scale.X;
            scaled.Y *= scale.Y;

            Min = center - scaled;
            Max = center + scaled;
        }
    }
}
