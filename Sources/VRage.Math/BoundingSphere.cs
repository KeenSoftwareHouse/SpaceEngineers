using System;
using System.Collections.Generic;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a sphere.
    /// </summary>
    [Serializable]
    public struct BoundingSphere : IEquatable<BoundingSphere>
    {
        /// <summary>
        /// The center point of the sphere.
        /// </summary>
        public Vector3 Center;
        /// <summary>
        /// The radius of the sphere.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Creates a new instance of BoundingSphere.
        /// </summary>
        /// <param name="center">Center point of the sphere.</param><param name="radius">Radius of the sphere.</param>
        public BoundingSphere(Vector3 center, float radius)
        {
            this.Center = center;
            this.Radius = radius;
        }

        /// <summary>
        /// Determines whether two instances of BoundingSphere are equal.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator ==(BoundingSphere a, BoundingSphere b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingSphere are not equal.
        /// </summary>
        /// <param name="a">The BoundingSphere to the left of the inequality operator.</param><param name="b">The BoundingSphere to the right of the inequality operator.</param>
        public static bool operator !=(BoundingSphere a, BoundingSphere b)
        {
            if (!(a.Center != b.Center))
                return (double)a.Radius != (double)b.Radius;
            else
                return true;
        }

        /// <summary>
        /// Determines whether the specified BoundingSphere is equal to the current BoundingSphere.
        /// </summary>
        /// <param name="other">The BoundingSphere to compare with the current BoundingSphere.</param>
        public bool Equals(BoundingSphere other)
        {
            if (this.Center == other.Center)
                return (double)this.Radius == (double)other.Radius;
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the BoundingSphere.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingSphere.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is BoundingSphere)
                flag = this.Equals((BoundingSphere)obj);
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
        /// Returns a String that represents the current BoundingSphere.
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
        /// Creates a BoundingSphere that contains the two specified BoundingSphere instances.
        /// </summary>
        /// <param name="original">BoundingSphere to be merged.</param><param name="additional">BoundingSphere to be merged.</param>
        public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
        {
            Vector3 result;
            Vector3.Subtract(ref additional.Center, ref original.Center, out result);
            float num1 = result.Length();
            float num2 = original.Radius;
            float num3 = additional.Radius;
            if ((double)num2 + (double)num3 >= (double)num1)
            {
                if ((double)num2 - (double)num3 >= (double)num1)
                    return original;
                if ((double)num3 - (double)num2 >= (double)num1)
                    return additional;
            }
            Vector3 vector3 = result * (1f / num1);
            float num4 = MathHelper.Min(-num2, num1 - num3);
            float num5 = (float)(((double)MathHelper.Max(num2, num1 + num3) - (double)num4) * 0.5);
            BoundingSphere boundingSphere;
            boundingSphere.Center = original.Center + vector3 * (num5 + num4);
            boundingSphere.Radius = num5;
            return boundingSphere;
        }

        /// <summary>
        /// Creates a BoundingSphere that contains the two specified BoundingSphere instances.
        /// </summary>
        /// <param name="original">BoundingSphere to be merged.</param><param name="additional">BoundingSphere to be merged.</param><param name="result">[OutAttribute] The created BoundingSphere.</param>
        public static void CreateMerged(ref BoundingSphere original, ref BoundingSphere additional, out BoundingSphere result)
        {
            Vector3 result1;
            Vector3.Subtract(ref additional.Center, ref original.Center, out result1);
            float num1 = result1.Length();
            float num2 = original.Radius;
            float num3 = additional.Radius;
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
            Vector3 vector3 = result1 * (1f / num1);
            float num4 = MathHelper.Min(-num2, num1 - num3);
            float num5 = (float)(((double)MathHelper.Max(num2, num1 + num3) - (double)num4) * 0.5);
            result.Center = original.Center + vector3 * (num5 + num4);
            result.Radius = num5;
        }

        /// <summary>
        /// Creates the smallest BoundingSphere that can contain a specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to create the BoundingSphere from.</param>
        public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
        {
            BoundingSphere result;
            result.Center = (box.Min + box.Max) * .5f;
            Vector3.Distance(ref result.Center, ref box.Max, out result.Radius);
            return result;
        }

        /// <summary>
        /// Creates the smallest BoundingSphere that can contain a specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to create the BoundingSphere from.</param><param name="result">[OutAttribute] The created BoundingSphere.</param>
        public static void CreateFromBoundingBox(ref BoundingBox box, out BoundingSphere result)
        {
            result.Center = (box.Min + box.Max) * .5f;
            Vector3.Distance(ref result.Center, ref box.Max, out result.Radius);
        }

        /// <summary>
        /// Creates a BoundingSphere that can contain a specified list of points.
        /// </summary>
        /// <param name="points">List of points the BoundingSphere must contain.</param>
        public static BoundingSphere CreateFromPoints(IEnumerable<Vector3> points)
        {
            IEnumerator<Vector3> enumerator = points.GetEnumerator();
            enumerator.MoveNext();
            Vector3 current;
            Vector3 vector3_1 = current = enumerator.Current;
            Vector3 vector3_2 = current;
            Vector3 vector3_3 = current;
            Vector3 vector3_4 = current;
            Vector3 vector3_5 = current;
            Vector3 vector3_6 = current;
            foreach (Vector3 vector3_7 in points)
            {
                if ((double)vector3_7.X < (double)vector3_6.X)
                    vector3_6 = vector3_7;
                if ((double)vector3_7.X > (double)vector3_5.X)
                    vector3_5 = vector3_7;
                if ((double)vector3_7.Y < (double)vector3_4.Y)
                    vector3_4 = vector3_7;
                if ((double)vector3_7.Y > (double)vector3_3.Y)
                    vector3_3 = vector3_7;
                if ((double)vector3_7.Z < (double)vector3_2.Z)
                    vector3_2 = vector3_7;
                if ((double)vector3_7.Z > (double)vector3_1.Z)
                    vector3_1 = vector3_7;
            }
            float result1;
            Vector3.Distance(ref vector3_5, ref vector3_6, out result1);
            float result2;
            Vector3.Distance(ref vector3_3, ref vector3_4, out result2);
            float result3;
            Vector3.Distance(ref vector3_1, ref vector3_2, out result3);
            Vector3 result4;
            float num1;
            if ((double)result1 > (double)result2)
            {
                if ((double)result1 > (double)result3)
                {
                    Vector3.Lerp(ref vector3_5, ref vector3_6, 0.5f, out result4);
                    num1 = result1 * 0.5f;
                }
                else
                {
                    Vector3.Lerp(ref vector3_1, ref vector3_2, 0.5f, out result4);
                    num1 = result3 * 0.5f;
                }
            }
            else if ((double)result2 > (double)result3)
            {
                Vector3.Lerp(ref vector3_3, ref vector3_4, 0.5f, out result4);
                num1 = result2 * 0.5f;
            }
            else
            {
                Vector3.Lerp(ref vector3_1, ref vector3_2, 0.5f, out result4);
                num1 = result3 * 0.5f;
            }
            foreach (Vector3 vector3_7 in points)
            {
                Vector3 vector3_8;
                vector3_8.X = vector3_7.X - result4.X;
                vector3_8.Y = vector3_7.Y - result4.Y;
                vector3_8.Z = vector3_7.Z - result4.Z;
                float num2 = vector3_8.Length();
                if ((double)num2 > (double)num1)
                {
                    num1 = (float)(((double)num1 + (double)num2) * 0.5);
                    result4 += (float)(1.0 - (double)num1 / (double)num2) * vector3_8;
                }
            }
            BoundingSphere boundingSphere;
            boundingSphere.Center = result4;
            boundingSphere.Radius = num1;
            return boundingSphere;
        }

        /// <summary>
        /// Creates the smallest BoundingSphere that can contain a specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to create the BoundingSphere with.</param>
        public static BoundingSphere CreateFromFrustum(BoundingFrustum frustum)
        {
            if (frustum == (BoundingFrustum)null)
                throw new ArgumentNullException("frustum");
            else
                return BoundingSphere.CreateFromPoints((IEnumerable<Vector3>)frustum.cornerArray);
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects with a specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with the current BoundingSphere.</param>
        public bool Intersects(BoundingBox box)
        {
            Vector3 result1;
            Vector3.Clamp(ref this.Center, ref box.Min, ref box.Max, out result1);
            float result2;
            Vector3.DistanceSquared(ref this.Center, ref result1, out result2);
            return (double)result2 <= (double)this.Radius * (double)this.Radius;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects a BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingSphere and BoundingBox intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox box, out bool result)
        {
            Vector3 result1;
            Vector3.Clamp(ref this.Center, ref box.Min, ref box.Max, out result1);
            float result2;
            Vector3.DistanceSquared(ref this.Center, ref result1, out result2);
            result = (double)result2 <= (double)this.Radius * (double)this.Radius;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects with a specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with the current BoundingSphere.</param>
        public bool Intersects(BoundingFrustum frustum)
        {
            bool result;
            frustum.Intersects(ref this, out result);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects with a specified Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with the current BoundingSphere.</param>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            return plane.Intersects(this);
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the BoundingSphere intersects the Plane.</param>
        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            plane.Intersects(ref this, out result);
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects with a specified Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with the current BoundingSphere.</param>
        public float? Intersects(Ray ray)
        {
            return ray.Intersects(this);
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param><param name="result">[OutAttribute] Distance at which the ray intersects the BoundingSphere or null if there is no intersection.</param>
        public void Intersects(ref Ray ray, out float? result)
        {
            ray.Intersects(ref this, out result);
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects with a specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with the current BoundingSphere.</param>
        public bool Intersects(BoundingSphere sphere)
        {
            float result;
            Vector3.DistanceSquared(ref this.Center, ref sphere.Center, out result);
            float num1 = this.Radius;
            float num2 = sphere.Radius;
            return (double)num1 * (double)num1 + 2.0 * (double)num1 * (double)num2 + (double)num2 * (double)num2 > (double)result;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere intersects another BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingSphere instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            float result1;
            Vector3.DistanceSquared(ref this.Center, ref sphere.Center, out result1);
            float num1 = this.Radius;
            float num2 = sphere.Radius;
            result = (double)num1 * (double)num1 + 2.0 * (double)num1 * (double)num2 + (double)num2 * (double)num2 > (double)result1;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere contains the specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check against the current BoundingSphere.</param>
        public ContainmentType Contains(BoundingBox box)
        {
            if (!box.Intersects(this))
                return ContainmentType.Disjoint;
            float num = this.Radius * this.Radius;
            Vector3 vector3;
            vector3.X = this.Center.X - box.Min.X;
            vector3.Y = this.Center.Y - box.Max.Y;
            vector3.Z = this.Center.Z - box.Max.Z;
            if ((double)vector3.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            vector3.X = this.Center.X - box.Max.X;
            vector3.Y = this.Center.Y - box.Max.Y;
            vector3.Z = this.Center.Z - box.Max.Z;
            if ((double)vector3.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            vector3.X = this.Center.X - box.Max.X;
            vector3.Y = this.Center.Y - box.Min.Y;
            vector3.Z = this.Center.Z - box.Max.Z;
            if ((double)vector3.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            vector3.X = this.Center.X - box.Min.X;
            vector3.Y = this.Center.Y - box.Min.Y;
            vector3.Z = this.Center.Z - box.Max.Z;
            if ((double)vector3.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            vector3.X = this.Center.X - box.Min.X;
            vector3.Y = this.Center.Y - box.Max.Y;
            vector3.Z = this.Center.Z - box.Min.Z;
            if ((double)vector3.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            vector3.X = this.Center.X - box.Max.X;
            vector3.Y = this.Center.Y - box.Max.Y;
            vector3.Z = this.Center.Z - box.Min.Z;
            if ((double)vector3.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            vector3.X = this.Center.X - box.Max.X;
            vector3.Y = this.Center.Y - box.Min.Y;
            vector3.Z = this.Center.Z - box.Min.Z;
            if ((double)vector3.LengthSquared() > (double)num)
                return ContainmentType.Intersects;
            vector3.X = this.Center.X - box.Min.X;
            vector3.Y = this.Center.Y - box.Min.Y;
            vector3.Z = this.Center.Z - box.Min.Z;
            return (double)vector3.LengthSquared() > (double)num ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere contains the specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            bool result1;
            box.Intersects(ref this, out result1);
            if (!result1)
            {
                result = ContainmentType.Disjoint;
            }
            else
            {
                float num = this.Radius * this.Radius;
                result = ContainmentType.Intersects;
                Vector3 vector3;
                vector3.X = this.Center.X - box.Min.X;
                vector3.Y = this.Center.Y - box.Max.Y;
                vector3.Z = this.Center.Z - box.Max.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                vector3.X = this.Center.X - box.Max.X;
                vector3.Y = this.Center.Y - box.Max.Y;
                vector3.Z = this.Center.Z - box.Max.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                vector3.X = this.Center.X - box.Max.X;
                vector3.Y = this.Center.Y - box.Min.Y;
                vector3.Z = this.Center.Z - box.Max.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                vector3.X = this.Center.X - box.Min.X;
                vector3.Y = this.Center.Y - box.Min.Y;
                vector3.Z = this.Center.Z - box.Max.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                vector3.X = this.Center.X - box.Min.X;
                vector3.Y = this.Center.Y - box.Max.Y;
                vector3.Z = this.Center.Z - box.Min.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                vector3.X = this.Center.X - box.Max.X;
                vector3.Y = this.Center.Y - box.Max.Y;
                vector3.Z = this.Center.Z - box.Min.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                vector3.X = this.Center.X - box.Max.X;
                vector3.Y = this.Center.Y - box.Min.Y;
                vector3.Z = this.Center.Z - box.Min.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                vector3.X = this.Center.X - box.Min.X;
                vector3.Y = this.Center.Y - box.Min.Y;
                vector3.Z = this.Center.Z - box.Min.Z;
                if ((double)vector3.LengthSquared() > (double)num)
                    return;
                result = ContainmentType.Contains;
            }
        }

        /// <summary>
        /// Checks whether the current BoundingSphere contains the specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check against the current BoundingSphere.</param>
        public ContainmentType Contains(BoundingFrustum frustum)
        {
            if (!frustum.Intersects(this))
                return ContainmentType.Disjoint;
            float num = this.Radius * this.Radius;
            foreach (Vector3 vector3_1 in frustum.cornerArray)
            {
                Vector3 vector3_2;
                vector3_2.X = vector3_1.X - this.Center.X;
                vector3_2.Y = vector3_1.Y - this.Center.Y;
                vector3_2.Z = vector3_1.Z - this.Center.Z;
                if ((double)vector3_2.LengthSquared() > (double)num)
                    return ContainmentType.Intersects;
            }
            return ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere contains the specified point.
        /// </summary>
        /// <param name="point">The point to check against the current BoundingSphere.</param>
        public ContainmentType Contains(Vector3 point)
        {
            return (double)Vector3.DistanceSquared(point, this.Center) >= (double)this.Radius * (double)this.Radius ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere contains the specified point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            float result1;
            Vector3.DistanceSquared(ref point, ref this.Center, out result1);
            result = (double)result1 < (double)this.Radius * (double)this.Radius ? ContainmentType.Contains : ContainmentType.Disjoint;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere contains the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check against the current BoundingSphere.</param>
        public ContainmentType Contains(BoundingSphere sphere)
        {
            float result;
            Vector3.Distance(ref this.Center, ref sphere.Center, out result);
            float num1 = this.Radius;
            float num2 = sphere.Radius;
            if ((double)num1 + (double)num2 < (double)result)
                return ContainmentType.Disjoint;
            return (double)num1 - (double)num2 < (double)result ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingSphere contains the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            float result1;
            Vector3.Distance(ref this.Center, ref sphere.Center, out result1);
            float num1 = this.Radius;
            float num2 = sphere.Radius;
            result = (double)num1 + (double)num2 >= (double)result1 ? ((double)num1 - (double)num2 >= (double)result1 ? ContainmentType.Contains : ContainmentType.Intersects) : ContainmentType.Disjoint;
        }

        internal void SupportMapping(ref Vector3 v, out Vector3 result)
        {
            float num = this.Radius / v.Length();
            result.X = this.Center.X + v.X * num;
            result.Y = this.Center.Y + v.Y * num;
            result.Z = this.Center.Z + v.Z * num;
        }

        /// <summary>
        /// Translates and scales the BoundingSphere using a given Matrix.
        /// </summary>
        /// <param name="matrix">A transformation matrix that might include translation, rotation, or uniform scaling. Note that BoundingSphere.Transform will not return correct results if there are non-uniform scaling, shears, or other unusual transforms in this transformation matrix. This is because there is no way to shear or non-uniformly scale a sphere. Such an operation would cause the sphere to lose its shape as a sphere.</param>
        public BoundingSphere Transform(Matrix matrix)
        {
            BoundingSphere boundingSphere = new BoundingSphere();
            boundingSphere.Center = Vector3.Transform(this.Center, matrix);
            float num = Math.Max((float)((double)matrix.M11 * (double)matrix.M11 + (double)matrix.M12 * (double)matrix.M12 + (double)matrix.M13 * (double)matrix.M13), Math.Max((float)((double)matrix.M21 * (double)matrix.M21 + (double)matrix.M22 * (double)matrix.M22 + (double)matrix.M23 * (double)matrix.M23), (float)((double)matrix.M31 * (double)matrix.M31 + (double)matrix.M32 * (double)matrix.M32 + (double)matrix.M33 * (double)matrix.M33)));
            boundingSphere.Radius = this.Radius * (float)Math.Sqrt((double)num);
            return boundingSphere;
        }

        /// <summary>
        /// Translates and scales the BoundingSphere using a given Matrix.
        /// </summary>
        /// <param name="matrix">A transformation matrix that might include translation, rotation, or uniform scaling. Note that BoundingSphere.Transform will not return correct results if there are non-uniform scaling, shears, or other unusual transforms in this transformation matrix. This is because there is no way to shear or non-uniformly scale a sphere. Such an operation would cause the sphere to lose its shape as a sphere.</param><param name="result">[OutAttribute] The transformed BoundingSphere.</param>
        public void Transform(ref Matrix matrix, out BoundingSphere result)
        {
            result.Center = Vector3.Transform(this.Center, matrix);
            float num = Math.Max((float)((double)matrix.M11 * (double)matrix.M11 + (double)matrix.M12 * (double)matrix.M12 + (double)matrix.M13 * (double)matrix.M13), Math.Max((float)((double)matrix.M21 * (double)matrix.M21 + (double)matrix.M22 * (double)matrix.M22 + (double)matrix.M23 * (double)matrix.M23), (float)((double)matrix.M31 * (double)matrix.M31 + (double)matrix.M32 * (double)matrix.M32 + (double)matrix.M33 * (double)matrix.M33)));
            result.Radius = this.Radius * (float)Math.Sqrt((double)num);
        }

        // NOTE: This function doesn't calculate the normal because it's easily derived for a sphere (p - center).
        public bool IntersectRaySphere(Ray ray, out float tmin, out float tmax)
        {
            tmin = 0;
            tmax = 0;
	        Vector3 CO = ray.Position - Center;
 
	        float a = ray.Direction.Dot(ray.Direction);
	        float b = 2.0f * CO.Dot(ray.Direction);
	        float c = CO.Dot(CO) - (Radius * Radius);
 
	        float discriminant = b * b - 4.0f * a * c;
	        if(discriminant < 0.0f)
		        return false;

            tmin = (-b - (float)Math.Sqrt(discriminant)) / (2.0f * a);
            tmax = (-b + (float)Math.Sqrt(discriminant)) / (2.0f * a);
	        if(tmin > tmax)
	        {
		        float temp = tmin;
		        tmin = tmax;
		        tmax = temp;
	        }
 
	        return true;
        }

        public BoundingSphere Include(BoundingSphere sphere)
        {
            BoundingSphere.Include(ref this, ref sphere);
            return this;
        }

        public static void Include(ref BoundingSphere sphere, ref BoundingSphere otherSphere)
        {
            if (sphere.Radius == float.MinValue)
            {
                sphere.Center = otherSphere.Center;
                sphere.Radius = otherSphere.Radius;
                return;
            }

            float distance = Vector3.Distance(sphere.Center, otherSphere.Center);
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

                float a = (distance + otherSphere.Radius - sphere.Radius) / (2.0f * distance);

                Vector3 center = Vector3.Lerp(sphere.Center, otherSphere.Center, a);
                float radius = (distance + sphere.Radius + otherSphere.Radius) / 2;

                sphere.Center = center;
                sphere.Radius = radius;
            }
        }
    }
}
