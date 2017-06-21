using System;
using System.Collections.Generic;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a sphere.
    /// </summary>
    [Serializable]
    public struct BoundingSphereD : IEquatable<BoundingSphereD>
    {
        /// <summary>
        /// The center point of the sphere.
        /// </summary>
        public Vector3D Center;
        /// <summary>
        /// The radius of the sphere.
        /// </summary>
        public double Radius;

        /// <summary>
        /// Creates a new instance of BoundingSphereD.
        /// </summary>
        /// <param name="center">Center point of the sphere.</param><param name="radius">Radius of the sphere.</param>
        public BoundingSphereD(Vector3D center, double radius)
        {
            this.Center = center;
            this.Radius = radius;
        }

        /// <summary>
        /// Determines whether two instances of BoundingSphereD are equal.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator ==(BoundingSphereD a, BoundingSphereD b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingSphereD are not equal.
        /// </summary>
        /// <param name="a">The BoundingSphereD to the left of the inequality operator.</param><param name="b">The BoundingSphereD to the right of the inequality operator.</param>
        public static bool operator !=(BoundingSphereD a, BoundingSphereD b)
        {
            if (!(a.Center != b.Center))
                return (double)a.Radius != (double)b.Radius;
            else
                return true;
        }

        /// <summary>
        /// Determines whether the specified BoundingSphereD is equal to the current BoundingSphereD.
        /// </summary>
        /// <param name="other">The BoundingSphereD to compare with the current BoundingSphereD.</param>
        public bool Equals(BoundingSphereD other)
        {
            if (this.Center == other.Center)
                return (double)this.Radius == (double)other.Radius;
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the BoundingSphereD.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingSphereD.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is BoundingSphereD)
                flag = this.Equals((BoundingSphereD)obj);
            return flag;
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Center.GetHashCode() + this.Radius.GetHashCode();
        }

        /// <summary>
        /// Returns a String that represents the current BoundingSphereD.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{Center:{0} Radius:{1}}}", new object[2]
      {
        (object) this.Center.ToString(),
        (object) this.Radius.ToString((IFormatProvider) currentCulture)
      });
        }

        /// <summary>
        /// Creates a BoundingSphereD that contains the two specified BoundingSphereD instances.
        /// </summary>
        /// <param name="original">BoundingSphereD to be merged.</param><param name="additional">BoundingSphereD to be merged.</param>
        public static BoundingSphereD CreateMerged(BoundingSphereD original, BoundingSphereD additional)
        {
            Vector3D result;
            Vector3D.Subtract(ref additional.Center, ref original.Center, out result);
            double num1 = result.Length();
            double num2 = original.Radius;
            double num3 = additional.Radius;
            if ((double)num2 + (double)num3 >= (double)num1)
            {
                if ((double)num2 - (double)num3 >= (double)num1)
                    return original;
                if ((double)num3 - (double)num2 >= (double)num1)
                    return additional;
            }
            Vector3D vector = result * (1f / num1);
            double num4 = MathHelper.Min(-num2, num1 - num3);
            double num5 = (double)(((double)MathHelper.Max(num2, num1 + num3) - (double)num4) * 0.5);
            BoundingSphereD BoundingSphereD;
            BoundingSphereD.Center = original.Center + vector * (num5 + num4);
            BoundingSphereD.Radius = num5;
            return BoundingSphereD;
        }

        /// <summary>
        /// Creates a BoundingSphereD that contains the two specified BoundingSphereD instances.
        /// </summary>
        /// <param name="original">BoundingSphereD to be merged.</param><param name="additional">BoundingSphereD to be merged.</param><param name="result">[OutAttribute] The created BoundingSphereD.</param>
        public static void CreateMerged(ref BoundingSphereD original, ref BoundingSphereD additional, out BoundingSphereD result)
        {
            Vector3D result1;
            Vector3D.Subtract(ref additional.Center, ref original.Center, out result1);
            double num1 = result1.Length();
            double num2 = original.Radius;
            double num3 = additional.Radius;
            if ((double)num2 + (double)num3 >= (double)num1)
            {
                if ((double)num2 - (double)num3 >= (double)num1)
                {
                    result = original;
                    return;
                }
                else if ((double)num3 - (double)num2 >= (double)num1)
                {
                    result = additional;
                    return;
                }
            }
            Vector3D vector = result1 * (1f / num1);
            double num4 = MathHelper.Min(-num2, num1 - num3);
            double num5 = (double)(((double)MathHelper.Max(num2, num1 + num3) - (double)num4) * 0.5);
            result.Center = original.Center + vector * (num5 + num4);
            result.Radius = num5;
        }

        /// <summary>
        /// Creates the smallest BoundingSphereD that can contain a specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to create the BoundingSphereD from.</param>
        public static BoundingSphereD CreateFromBoundingBox(BoundingBoxD box)
        {
            BoundingSphereD BoundingSphereD;
            Vector3D.Lerp(ref box.Min, ref box.Max, 0.5f, out BoundingSphereD.Center);
            double result;
            Vector3D.Distance(ref box.Min, ref box.Max, out result);
            BoundingSphereD.Radius = result * 0.5f;
            return BoundingSphereD;
        }

        /// <summary>
        /// Creates the smallest BoundingSphereD that can contain a specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to create the BoundingSphereD from.</param><param name="result">[OutAttribute] The created BoundingSphereD.</param>
        public static void CreateFromBoundingBox(ref BoundingBoxD box, out BoundingSphereD result)
        {
            Vector3D.Lerp(ref box.Min, ref box.Max, 0.5f, out result.Center);
            double result1;
            Vector3D.Distance(ref box.Min, ref box.Max, out result1);
            result.Radius = result1 * 0.5f;
        }

        /// <summary>
        /// Creates a BoundingSphereD that can contain a specified list of points.
        /// </summary>
        /// <param name="points">List of points the BoundingSphereD must contain.</param>
        public static BoundingSphereD CreateFromPoints(Vector3D[] points)
        {
            Vector3D current;
            Vector3D Vector3D_1 = current = points[0];
            Vector3D Vector3D_2 = current;
            Vector3D Vector3D_3 = current;
            Vector3D Vector3D_4 = current;
            Vector3D Vector3D_5 = current;
            Vector3D Vector3D_6 = current;
            foreach (Vector3D Vector3D_7 in points)
            {
                if ((double)Vector3D_7.X < (double)Vector3D_6.X)
                    Vector3D_6 = Vector3D_7;
                if ((double)Vector3D_7.X > (double)Vector3D_5.X)
                    Vector3D_5 = Vector3D_7;
                if ((double)Vector3D_7.Y < (double)Vector3D_4.Y)
                    Vector3D_4 = Vector3D_7;
                if ((double)Vector3D_7.Y > (double)Vector3D_3.Y)
                    Vector3D_3 = Vector3D_7;
                if ((double)Vector3D_7.Z < (double)Vector3D_2.Z)
                    Vector3D_2 = Vector3D_7;
                if ((double)Vector3D_7.Z > (double)Vector3D_1.Z)
                    Vector3D_1 = Vector3D_7;
            }
            double result1;
            Vector3D.Distance(ref Vector3D_5, ref Vector3D_6, out result1);
            double result2;
            Vector3D.Distance(ref Vector3D_3, ref Vector3D_4, out result2);
            double result3;
            Vector3D.Distance(ref Vector3D_1, ref Vector3D_2, out result3);
            Vector3D result4;
            double num1;
            if ((double)result1 > (double)result2)
            {
                if ((double)result1 > (double)result3)
                {
                    Vector3D.Lerp(ref Vector3D_5, ref Vector3D_6, 0.5f, out result4);
                    num1 = result1 * 0.5f;
                }
                else
                {
                    Vector3D.Lerp(ref Vector3D_1, ref Vector3D_2, 0.5f, out result4);
                    num1 = result3 * 0.5f;
                }
            }
            else if ((double)result2 > (double)result3)
            {
                Vector3D.Lerp(ref Vector3D_3, ref Vector3D_4, 0.5f, out result4);
                num1 = result2 * 0.5f;
            }
            else
            {
                Vector3D.Lerp(ref Vector3D_1, ref Vector3D_2, 0.5f, out result4);
                num1 = result3 * 0.5f;
            }
            foreach (Vector3D Vector3D_7 in points)
            {
                Vector3D Vector3D_8;
                Vector3D_8.X = Vector3D_7.X - result4.X;
                Vector3D_8.Y = Vector3D_7.Y - result4.Y;
                Vector3D_8.Z = Vector3D_7.Z - result4.Z;
                double num2 = Vector3D_8.Length();
                if ((double)num2 > (double)num1)
                {
                    num1 = (double)(((double)num1 + (double)num2) * 0.5);
                    result4 += (double)(1.0 - (double)num1 / (double)num2) * Vector3D_8;
                }
            }
            BoundingSphereD BoundingSphereD;
            BoundingSphereD.Center = result4;
            BoundingSphereD.Radius = num1;
            return BoundingSphereD;
        }

        /// <summary>
        /// Creates the smallest BoundingSphereD that can contain a specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to create the BoundingSphereD with.</param>
        public static BoundingSphereD CreateFromFrustum(BoundingFrustumD frustum)
        {
            if (frustum == (BoundingFrustumD)null)
                throw new ArgumentNullException("frustum");
            else
                return BoundingSphereD.CreateFromPoints(frustum.cornerArray);
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD intersects with a specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to check for intersection with the current BoundingSphereD.</param>
        public bool Intersects(BoundingBoxD box)
        {
            Vector3D result1;
            Vector3D.Clamp(ref this.Center, ref box.Min, ref box.Max, out result1);
            double result2;
            Vector3D.DistanceSquared(ref this.Center, ref result1, out result2);
            return (double)result2 <= (double)this.Radius * (double)this.Radius;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD intersects a BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingSphereD and BoundingBoxD intersect; false otherwise.</param>
        public void Intersects(ref BoundingBoxD box, out bool result)
        {
            Vector3D result1;
            Vector3D.Clamp(ref this.Center, ref box.Min, ref box.Max, out result1);
            double result2;
            Vector3D.DistanceSquared(ref this.Center, ref result1, out result2);
            result = (double)result2 <= (double)this.Radius * (double)this.Radius;
        }

        public double? Intersects(RayD ray)
        {
            return ray.Intersects(this);
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD intersects with a specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with the current BoundingSphereD.</param>
        public bool Intersects(BoundingFrustumD frustum)
        {
            bool result;
            frustum.Intersects(ref this, out result);
            return result;
        }

        ///// <summary>
        ///// Checks whether the current BoundingSphereD intersects with a specified Plane.
        ///// </summary>
        ///// <param name="plane">The Plane to check for intersection with the current BoundingSphereD.</param>
        //public PlaneIntersectionType Intersects(Plane plane)
        //{
        //    return plane.Intersects(this);
        //}

        ///// <summary>
        ///// Checks whether the current BoundingSphereD intersects a Plane.
        ///// </summary>
        ///// <param name="plane">The Plane to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the BoundingSphereD intersects the Plane.</param>
        //public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        //{
        //    plane.Intersects(ref this, out result);
        //}

        ///// <summary>
        ///// Checks whether the current BoundingSphereD intersects with a specified Ray.
        ///// </summary>
        ///// <param name="ray">The Ray to check for intersection with the current BoundingSphereD.</param>
        //public double? Intersects(Ray ray)
        //{
        //    return ray.Intersects(this);
        //}

        ///// <summary>
        ///// Checks whether the current BoundingSphereD intersects a Ray.
        ///// </summary>
        ///// <param name="ray">The Ray to check for intersection with.</param><param name="result">[OutAttribute] Distance at which the ray intersects the BoundingSphereD or null if there is no intersection.</param>
        //public void Intersects(ref Ray ray, out float? result)
        //{
        //    ray.Intersects(ref this, out result);
        //}

        /// <summary>
        /// Checks whether the current BoundingSphereD intersects with a specified BoundingSphereD.
        /// </summary>
        /// <param name="sphere">The BoundingSphereD to check for intersection with the current BoundingSphereD.</param>
        public bool Intersects(BoundingSphereD sphere)
        {
            double result;
            Vector3D.DistanceSquared(ref this.Center, ref sphere.Center, out result);
            double num1 = this.Radius;
            double num2 = sphere.Radius;
            return (double)num1 * (double)num1 + 2.0 * (double)num1 * (double)num2 + (double)num2 * (double)num2 > (double)result;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD intersects another BoundingSphereD.
        /// </summary>
        /// <param name="sphere">The BoundingSphereD to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingSphereD instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphereD sphere, out bool result)
        {
            double result1;
            Vector3D.DistanceSquared(ref this.Center, ref sphere.Center, out result1);
            double num1 = this.Radius;
            double num2 = sphere.Radius;
            result = (double)num1 * (double)num1 + 2.0 * (double)num1 * (double)num2 + (double)num2 * (double)num2 > (double)result1;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD contains the specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to check against the current BoundingSphereD.</param>
        public ContainmentType Contains(BoundingBoxD box)
        {
            if (!box.Intersects(this))
                return ContainmentType.Disjoint;
            double num = this.Radius * this.Radius;
            Vector3D Vector3D;
            Vector3D.X = this.Center.X - box.Min.X;
            Vector3D.Y = this.Center.Y - box.Max.Y;
            Vector3D.Z = this.Center.Z - box.Max.Z;
            if ((double)Vector3D.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            Vector3D.X = this.Center.X - box.Max.X;
            Vector3D.Y = this.Center.Y - box.Max.Y;
            Vector3D.Z = this.Center.Z - box.Max.Z;
            if ((double)Vector3D.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            Vector3D.X = this.Center.X - box.Max.X;
            Vector3D.Y = this.Center.Y - box.Min.Y;
            Vector3D.Z = this.Center.Z - box.Max.Z;
            if ((double)Vector3D.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            Vector3D.X = this.Center.X - box.Min.X;
            Vector3D.Y = this.Center.Y - box.Min.Y;
            Vector3D.Z = this.Center.Z - box.Max.Z;
            if ((double)Vector3D.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            Vector3D.X = this.Center.X - box.Min.X;
            Vector3D.Y = this.Center.Y - box.Max.Y;
            Vector3D.Z = this.Center.Z - box.Min.Z;
            if ((double)Vector3D.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            Vector3D.X = this.Center.X - box.Max.X;
            Vector3D.Y = this.Center.Y - box.Max.Y;
            Vector3D.Z = this.Center.Z - box.Min.Z;
            if ((double)Vector3D.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            Vector3D.X = this.Center.X - box.Max.X;
            Vector3D.Y = this.Center.Y - box.Min.Y;
            Vector3D.Z = this.Center.Z - box.Min.Z;
            if ((double)Vector3D.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            Vector3D.X = this.Center.X - box.Min.X;
            Vector3D.Y = this.Center.Y - box.Min.Y;
            Vector3D.Z = this.Center.Z - box.Min.Z;
            return (double)Vector3D.LengthSquared() > (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD contains the specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBoxD box, out ContainmentType result)
        {
            bool result1;
            box.Intersects(ref this, out result1);
            if (!result1)
            {
                result = ContainmentType.Disjoint;
            }
            else
            {
                double num = this.Radius * this.Radius;
                result = ContainmentType.Intersects;
                Vector3D Vector3D;
                Vector3D.X = this.Center.X - box.Min.X;
                Vector3D.Y = this.Center.Y - box.Max.Y;
                Vector3D.Z = this.Center.Z - box.Max.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                Vector3D.X = this.Center.X - box.Max.X;
                Vector3D.Y = this.Center.Y - box.Max.Y;
                Vector3D.Z = this.Center.Z - box.Max.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                Vector3D.X = this.Center.X - box.Max.X;
                Vector3D.Y = this.Center.Y - box.Min.Y;
                Vector3D.Z = this.Center.Z - box.Max.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                Vector3D.X = this.Center.X - box.Min.X;
                Vector3D.Y = this.Center.Y - box.Min.Y;
                Vector3D.Z = this.Center.Z - box.Max.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                Vector3D.X = this.Center.X - box.Min.X;
                Vector3D.Y = this.Center.Y - box.Max.Y;
                Vector3D.Z = this.Center.Z - box.Min.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                Vector3D.X = this.Center.X - box.Max.X;
                Vector3D.Y = this.Center.Y - box.Max.Y;
                Vector3D.Z = this.Center.Z - box.Min.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                Vector3D.X = this.Center.X - box.Max.X;
                Vector3D.Y = this.Center.Y - box.Min.Y;
                Vector3D.Z = this.Center.Z - box.Min.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                Vector3D.X = this.Center.X - box.Min.X;
                Vector3D.Y = this.Center.Y - box.Min.Y;
                Vector3D.Z = this.Center.Z - box.Min.Z;
                if ((double)Vector3D.LengthSquared() > (double)num)
                    return;
                result = ContainmentType.Contains;
            }
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD contains the specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check against the current BoundingSphereD.</param>
        public ContainmentType Contains(BoundingFrustumD frustum)
        {
            if (!frustum.Intersects(this))
                return ContainmentType.Disjoint;
            double num = this.Radius * this.Radius;
            foreach (Vector3D Vector3D_1 in frustum.cornerArray)
            {
                Vector3D Vector3D_2;
                Vector3D_2.X = Vector3D_1.X - this.Center.X;
                Vector3D_2.Y = Vector3D_1.Y - this.Center.Y;
                Vector3D_2.Z = Vector3D_1.Z - this.Center.Z;
                if ((double)Vector3D_2.LengthSquared() > (double)num)
                    return ContainmentType.Intersects;
            }
            return ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD contains the specified point.
        /// </summary>
        /// <param name="point">The point to check against the current BoundingSphereD.</param>
        public ContainmentType Contains(Vector3D point)
        {
            return (double)Vector3D.DistanceSquared(point, this.Center) >= (double)this.Radius * (double)this.Radius ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD contains the specified point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3D point, out ContainmentType result)
        {
            double result1;
            Vector3D.DistanceSquared(ref point, ref this.Center, out result1);
            result = (double)result1 < (double)this.Radius * (double)this.Radius ? ContainmentType.Contains : ContainmentType.Disjoint;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD contains the specified BoundingSphereD.
        /// </summary>
        /// <param name="sphere">The BoundingSphereD to check against the current BoundingSphereD.</param>
        public ContainmentType Contains(BoundingSphereD sphere)
        {
            double result;
            Vector3D.Distance(ref this.Center, ref sphere.Center, out result);
            double num1 = this.Radius;
            double num2 = sphere.Radius;
            if ((double)num1 + (double)num2 < (double)result)
                return ContainmentType.Disjoint;
            return (double)num1 - (double)num2 < (double)result ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphereD contains the specified BoundingSphereD.
        /// </summary>
        /// <param name="sphere">The BoundingSphereD to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphereD sphere, out ContainmentType result)
        {
            double result1;
            Vector3D.Distance(ref this.Center, ref sphere.Center, out result1);
            double num1 = this.Radius;
            double num2 = sphere.Radius;
            result = (double)num1 + (double)num2 >= (double)result1 ? ((double)num1 - (double)num2 >= (double)result1 ? ContainmentType.Contains : ContainmentType.Intersects) : ContainmentType.Disjoint;
        }

        internal void SupportMapping(ref Vector3D v, out Vector3D result)
        {
            double num = this.Radius / v.Length();
            result.X = this.Center.X + v.X * num;
            result.Y = this.Center.Y + v.Y * num;
            result.Z = this.Center.Z + v.Z * num;
        }

        /// <summary>
        /// Translates and scales the BoundingSphereD using a given Matrix.
        /// </summary>
        /// <param name="matrix">A transformation matrix that might include translation, rotation, or uniform scaling. Note that BoundingSphereD.Transform will not return correct results if there are non-uniform scaling, shears, or other unusual transforms in this transformation matrix. This is because there is no way to shear or non-uniformly scale a sphere. Such an operation would cause the sphere to lose its shape as a sphere.</param>
        public BoundingSphereD Transform(MatrixD matrix)
        {
            BoundingSphereD bsd = new BoundingSphereD();
            bsd.Center = Vector3D.Transform(this.Center, matrix);
            double num = Math.Max((double)((double)matrix.M11 * (double)matrix.M11 + (double)matrix.M12 * (double)matrix.M12 + (double)matrix.M13 * (double)matrix.M13), Math.Max((double)((double)matrix.M21 * (double)matrix.M21 + (double)matrix.M22 * (double)matrix.M22 + (double)matrix.M23 * (double)matrix.M23), (double)((double)matrix.M31 * (double)matrix.M31 + (double)matrix.M32 * (double)matrix.M32 + (double)matrix.M33 * (double)matrix.M33)));
            bsd.Radius = this.Radius * (double)Math.Sqrt((double)num);
            return bsd;
        }

        /// <summary>
        /// Translates and scales the BoundingSphereD using a given Matrix.
        /// </summary>
        /// <param name="matrix">A transformation matrix that might include translation, rotation, or uniform scaling. Note that BoundingSphereD.Transform will not return correct results if there are non-uniform scaling, shears, or other unusual transforms in this transformation matrix. This is because there is no way to shear or non-uniformly scale a sphere. Such an operation would cause the sphere to lose its shape as a sphere.</param><param name="result">[OutAttribute] The transformed BoundingSphereD.</param>
        public void Transform(ref MatrixD matrix, out BoundingSphereD result)
        {
            result.Center = Vector3D.Transform(this.Center, matrix);
            double num = Math.Max((double)((double)matrix.M11 * (double)matrix.M11 + (double)matrix.M12 * (double)matrix.M12 + (double)matrix.M13 * (double)matrix.M13), Math.Max((double)((double)matrix.M21 * (double)matrix.M21 + (double)matrix.M22 * (double)matrix.M22 + (double)matrix.M23 * (double)matrix.M23), (double)((double)matrix.M31 * (double)matrix.M31 + (double)matrix.M32 * (double)matrix.M32 + (double)matrix.M33 * (double)matrix.M33)));
            result.Radius = this.Radius * (double)Math.Sqrt((double)num);
        }

        // NOTE: This function doesn't calculate the normal because it's easily derived for a sphere (p - center).
        public bool IntersectRaySphere(RayD ray, out double tmin, out double tmax)
        {
            tmin = 0;
            tmax = 0;
	        Vector3D CO = ray.Position - Center;
 
	        double a = ray.Direction.Dot(ray.Direction);
            double b = 2.0f * CO.Dot(ray.Direction);
	        double c = CO.Dot(CO) - (Radius * Radius);
 
	        double discriminant = b * b - 4.0f * a * c;
	        if(discriminant < 0.0f)
		        return false;

            tmin = (-b - Math.Sqrt(discriminant)) / (2.0 * a);
            tmax = (-b + Math.Sqrt(discriminant)) / (2.0 * a);
	        if(tmin > tmax)
	        {
		        double temp = tmin;
		        tmin = tmax;
		        tmax = temp;
	        }
 
	        return true;
        }

        public BoundingSphereD Include(BoundingSphereD sphere)
        {
            BoundingSphereD.Include(ref this, ref sphere);
            return this;
        }

        public static void Include(ref BoundingSphereD sphere, ref BoundingSphereD otherSphere)
        {
            if (sphere.Radius == double.MinValue)
            {
                sphere.Center = otherSphere.Center;
                sphere.Radius = otherSphere.Radius;
                return;
            }

            double distance = Vector3D.Distance(sphere.Center, otherSphere.Center);
            if (distance + otherSphere.Radius <= sphere.Radius) // Other sphere is contained in this sphere
                return;
            else if (distance + sphere.Radius <= otherSphere.Radius) // This sphere is contained in other sphere
            {
                sphere = otherSphere;
            }
            else
            {
                // Now we know that center will lie between the old centers. Let's calculate linterpolation factors a and b:
                // r_1 + a*d = r_2 + b*d   and   a + b = 1   give:
                // a = (d + r_2 - r_1) / (2*d)
                // b = (d + r_1 - r_2) / (2*d) = 1 - a

                double a = (distance + otherSphere.Radius - sphere.Radius) / (2.0f * distance);

                Vector3D center = Vector3D.Lerp(sphere.Center, otherSphere.Center, a);
                double radius = (distance + sphere.Radius + otherSphere.Radius) / 2;

                sphere.Center = center;
                sphere.Radius = radius;
            }
        }

        public static implicit operator BoundingSphereD(BoundingSphere b)
        {
            return new BoundingSphereD((Vector3D)b.Center, b.Radius);
        }

        public static implicit operator BoundingSphere(BoundingSphereD b)
        {
            return new BoundingSphere((Vector3)b.Center, (float)b.Radius);
        }
    }
}
