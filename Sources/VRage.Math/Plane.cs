using System;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a plane.
    /// </summary>
    [Serializable]
    public struct Plane : IEquatable<Plane>
    {
        /// <summary>
        /// The normal vector of the Plane.
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        /// The distance of the Plane along its normal from the origin.
        /// Note: Be careful! The distance is signed and is the opposite of what people usually expect.
        ///       If you look closely at the plane equation: (n dot P) - D = 0, you'll realize that D = - (n dot P) (that is, negative instead of positive)
        /// </summary>
        public float D;

        /// <summary>
        /// Creates a new instance of Plane.
        /// </summary>
        /// <param name="a">X component of the normal defining the Plane.</param><param name="b">Y component of the normal defining the Plane.</param><param name="c">Z component of the normal defining the Plane.</param><param name="d">Distance of the origin from the plane along its normal.</param>
        public Plane(float a, float b, float c, float d)
        {
            this.Normal.X = a;
            this.Normal.Y = b;
            this.Normal.Z = c;
            this.D = d;
        }

        /// <summary>
        /// Creates a new instance of Plane.
        /// </summary>
        /// <param name="normal">The normal vector to the Plane.</param><param name="d">Distance of the origin from the plane along its normal.</param>
        public Plane(Vector3 normal, float d)
        {
            this.Normal = normal;
            this.D = d;
        }

        /// <summary>
        /// Creates a new instance of Plane.
        /// </summary>
        /// <param name="position">A point that lies on the Plane</param><param name="normal">The normal vector to the Plane.</param>
        public Plane(Vector3 position, Vector3 normal)
        {
            //D = -(Ax + By + Cz)
            this.Normal = normal;
            this.D = -Vector3.Dot(position, normal);
        }

        /// <summary>
        /// Creates a new instance of Plane.
        /// </summary>
        /// <param name="value">Vector4 with X, Y, and Z components defining the normal of the Plane. The W component defines the distance of the origin from the plane along its normal.</param>
        public Plane(Vector4 value)
        {
            this.Normal.X = value.X;
            this.Normal.Y = value.Y;
            this.Normal.Z = value.Z;
            this.D = value.W;
        }

        /// <summary>
        /// Creates a new instance of Plane.
        /// </summary>
        /// <param name="point1">One point of a triangle defining the Plane.</param><param name="point2">One point of a triangle defining the Plane.</param><param name="point3">One point of a triangle defining the Plane.</param>
        public Plane(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            float num1 = point2.X - point1.X;
            float num2 = point2.Y - point1.Y;
            float num3 = point2.Z - point1.Z;
            float num4 = point3.X - point1.X;
            float num5 = point3.Y - point1.Y;
            float num6 = point3.Z - point1.Z;
            float num7 = (float)((double)num2 * (double)num6 - (double)num3 * (double)num5);
            float num8 = (float)((double)num3 * (double)num4 - (double)num1 * (double)num6);
            float num9 = (float)((double)num1 * (double)num5 - (double)num2 * (double)num4);
            float num10 = 1f / (float)Math.Sqrt((double)num7 * (double)num7 + (double)num8 * (double)num8 + (double)num9 * (double)num9);
            this.Normal.X = num7 * num10;
            this.Normal.Y = num8 * num10;
            this.Normal.Z = num9 * num10;
            this.D = (float)-((double)this.Normal.X * (double)point1.X + (double)this.Normal.Y * (double)point1.Y + (double)this.Normal.Z * (double)point1.Z);
        }

        /// <summary>
        /// Determines whether two instances of Plane are equal.
        /// </summary>
        /// <param name="lhs">The object to the left of the equality operator.</param><param name="rhs">The object to the right of the equality operator.</param>
        public static bool operator ==(Plane lhs, Plane rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Determines whether two instances of Plane are not equal.
        /// </summary>
        /// <param name="lhs">The object to the left of the inequality operator.</param><param name="rhs">The object to the right of the inequality operator.</param>
        public static bool operator !=(Plane lhs, Plane rhs)
        {
            if ((double)lhs.Normal.X == (double)rhs.Normal.X && (double)lhs.Normal.Y == (double)rhs.Normal.Y && (double)lhs.Normal.Z == (double)rhs.Normal.Z)
                return (double)lhs.D != (double)rhs.D;
            else
                return true;
        }

        /// <summary>
        /// Determines whether the specified Plane is equal to the Plane.
        /// </summary>
        /// <param name="other">The Plane to compare with the current Plane.</param>
        public bool Equals(Plane other)
        {
            if ((double)this.Normal.X == (double)other.Normal.X && (double)this.Normal.Y == (double)other.Normal.Y && (double)this.Normal.Z == (double)other.Normal.Z)
                return (double)this.D == (double)other.D;
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the Plane.
        /// </summary>
        /// <param name="obj">The Object to compare with the current Plane.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Plane)
                flag = this.Equals((Plane)obj);
            return flag;
        }

        /// <summary>
        /// Gets the hash code for this object.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Normal.GetHashCode() + this.D.GetHashCode();
        }

        /// <summary>
        /// Returns a String that represents the current Plane.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{Normal:{0} D:{1}}}", new object[2]
      {
        (object) this.Normal.ToString(),
        (object) this.D.ToString((IFormatProvider) currentCulture)
      });
        }

        /// <summary>
        /// Changes the coefficients of the Normal vector of this Plane to make it of unit length.
        /// </summary>
        public void Normalize()
        {
            float num1 = (float)((double)this.Normal.X * (double)this.Normal.X + (double)this.Normal.Y * (double)this.Normal.Y + (double)this.Normal.Z * (double)this.Normal.Z);
            if ((double)Math.Abs(num1 - 1f) < 1.19209289550781E-07)
                return;
            float num2 = 1f / (float)Math.Sqrt((double)num1);
            this.Normal.X *= num2;
            this.Normal.Y *= num2;
            this.Normal.Z *= num2;
            this.D *= num2;
        }

        /// <summary>
        /// Changes the coefficients of the Normal vector of a Plane to make it of unit length.
        /// </summary>
        /// <param name="value">The Plane to normalize.</param>
        public static Plane Normalize(Plane value)
        {
            float num1 = (float)((double)value.Normal.X * (double)value.Normal.X + (double)value.Normal.Y * (double)value.Normal.Y + (double)value.Normal.Z * (double)value.Normal.Z);
            if ((double)Math.Abs(num1 - 1f) < 1.19209289550781E-07)
            {
                Plane plane;
                plane.Normal = value.Normal;
                plane.D = value.D;
                return plane;
            }
            else
            {
                float num2 = 1f / (float)Math.Sqrt((double)num1);
                Plane plane;
                plane.Normal.X = value.Normal.X * num2;
                plane.Normal.Y = value.Normal.Y * num2;
                plane.Normal.Z = value.Normal.Z * num2;
                plane.D = value.D * num2;
                return plane;
            }
        }

        /// <summary>
        /// Changes the coefficients of the Normal vector of a Plane to make it of unit length.
        /// </summary>
        /// <param name="value">The Plane to normalize.</param><param name="result">[OutAttribute] An existing plane Plane filled in with a normalized version of the specified plane.</param>
        public static void Normalize(ref Plane value, out Plane result)
        {
            float num1 = (float)((double)value.Normal.X * (double)value.Normal.X + (double)value.Normal.Y * (double)value.Normal.Y + (double)value.Normal.Z * (double)value.Normal.Z);
            if ((double)Math.Abs(num1 - 1f) < 1.19209289550781E-07)
            {
                result.Normal = value.Normal;
                result.D = value.D;
            }
            else
            {
                float num2 = 1f / (float)Math.Sqrt((double)num1);
                result.Normal.X = value.Normal.X * num2;
                result.Normal.Y = value.Normal.Y * num2;
                result.Normal.Z = value.Normal.Z * num2;
                result.D = value.D * num2;
            }
        }

        /// <summary>
        /// Transforms a normalized Plane by a Matrix.
        /// </summary>
        /// <param name="plane">The normalized Plane to transform. This Plane must already be normalized, so that its Normal vector is of unit length, before this method is called.</param><param name="matrix">The transform Matrix to apply to the Plane.</param>
        public static Plane Transform(Plane plane, Matrix matrix)
        {
            Plane result;
            Transform(ref plane, ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Transforms a normalized Plane by a Matrix.
        /// </summary>
        /// <param name="plane">The normalized Plane to transform. This Plane must already be normalized, so that its Normal vector is of unit length, before this method is called.</param><param name="matrix">The transform Matrix to apply to the Plane.</param><param name="result">[OutAttribute] An existing Plane filled in with the results of applying the transform.</param>
        public static void Transform(ref Plane plane, ref Matrix matrix, out Plane result)
        {
            result = default(Plane);
            Vector3 origin = -plane.Normal * plane.D;
            Vector3.TransformNormal(ref plane.Normal, ref matrix, out result.Normal);
            Vector3.Transform(ref origin, ref matrix, out origin);
            Vector3.Dot(ref origin, ref result.Normal, out result.D);
            result.D = -result.D;
        }

        /// <summary>
        /// Calculates the dot product of a specified Vector4 and this Plane.
        /// </summary>
        /// <param name="value">The Vector4 to multiply this Plane by.</param>
        public float Dot(Vector4 value)
        {
            return (float)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z + (double)this.D * (double)value.W);
        }

        /// <summary>
        /// Calculates the dot product of a specified Vector4 and this Plane.
        /// </summary>
        /// <param name="value">The Vector4 to multiply this Plane by.</param><param name="result">[OutAttribute] The dot product of the specified Vector4 and this Plane.</param>
        public void Dot(ref Vector4 value, out float result)
        {
            result = (float)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z + (double)this.D * (double)value.W);
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the Normal vector of this Plane plus the distance (D) value of the Plane.
        /// </summary>
        /// <param name="value">The Vector3 to multiply by.</param>
        public float DotCoordinate(Vector3 value)
        {
            return (float)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z) + this.D;
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the Normal vector of this Plane plus the distance (D) value of the Plane.
        /// </summary>
        /// <param name="value">The Vector3 to multiply by.</param><param name="result">[OutAttribute] The resulting value.</param>
        public void DotCoordinate(ref Vector3 value, out float result)
        {
            result = (float)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z) + this.D;
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the Normal vector of this Plane.
        /// </summary>
        /// <param name="value">The Vector3 to multiply by.</param>
        public float DotNormal(Vector3 value)
        {
            return (float)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z);
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the Normal vector of this Plane.
        /// </summary>
        /// <param name="value">The Vector3 to multiply by.</param><param name="result">[OutAttribute] The resulting dot product.</param>
        public void DotNormal(ref Vector3 value, out float result)
        {
            result = (float)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z);
        }

        /// <summary>
        /// Checks whether the current Plane intersects a specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for intersection with.</param>
        public PlaneIntersectionType Intersects(BoundingBox box)
        {
            Vector3 vector3_1;
            vector3_1.X = (double)this.Normal.X >= 0.0 ? box.Min.X : box.Max.X;
            vector3_1.Y = (double)this.Normal.Y >= 0.0 ? box.Min.Y : box.Max.Y;
            vector3_1.Z = (double)this.Normal.Z >= 0.0 ? box.Min.Z : box.Max.Z;
            Vector3 vector3_2;
            vector3_2.X = (double)this.Normal.X >= 0.0 ? box.Max.X : box.Min.X;
            vector3_2.Y = (double)this.Normal.Y >= 0.0 ? box.Max.Y : box.Min.Y;
            vector3_2.Z = (double)this.Normal.Z >= 0.0 ? box.Max.Z : box.Min.Z;
            if ((double)this.Normal.X * (double)vector3_1.X + (double)this.Normal.Y * (double)vector3_1.Y + (double)this.Normal.Z * (double)vector3_1.Z + (double)this.D > 0.0)
                return PlaneIntersectionType.Front;
            return (double)this.Normal.X * (double)vector3_2.X + (double)this.Normal.Y * (double)vector3_2.Y + (double)this.Normal.Z * (double)vector3_2.Z + (double)this.D < 0.0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current Plane intersects a BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the Plane intersects the BoundingBox.</param>
        public void Intersects(ref BoundingBox box, out PlaneIntersectionType result)
        {
            Vector3 vector3_1;
            vector3_1.X = (double)this.Normal.X >= 0.0 ? box.Min.X : box.Max.X;
            vector3_1.Y = (double)this.Normal.Y >= 0.0 ? box.Min.Y : box.Max.Y;
            vector3_1.Z = (double)this.Normal.Z >= 0.0 ? box.Min.Z : box.Max.Z;
            Vector3 vector3_2;
            vector3_2.X = (double)this.Normal.X >= 0.0 ? box.Max.X : box.Min.X;
            vector3_2.Y = (double)this.Normal.Y >= 0.0 ? box.Max.Y : box.Min.Y;
            vector3_2.Z = (double)this.Normal.Z >= 0.0 ? box.Max.Z : box.Min.Z;
            if ((double)this.Normal.X * (double)vector3_1.X + (double)this.Normal.Y * (double)vector3_1.Y + (double)this.Normal.Z * (double)vector3_1.Z + (double)this.D > 0.0)
                result = PlaneIntersectionType.Front;
            else if ((double)this.Normal.X * (double)vector3_2.X + (double)this.Normal.Y * (double)vector3_2.Y + (double)this.Normal.Z * (double)vector3_2.Z + (double)this.D < 0.0)
                result = PlaneIntersectionType.Back;
            else
                result = PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current Plane intersects a specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with.</param>
        public PlaneIntersectionType Intersects(BoundingFrustum frustum)
        {
            return frustum.Intersects(this);
        }

        /// <summary>
        /// Checks whether the current Plane intersects a specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        public PlaneIntersectionType Intersects(BoundingSphere sphere)
        {
            float num = (float)((double)sphere.Center.X * (double)this.Normal.X + (double)sphere.Center.Y * (double)this.Normal.Y + (double)sphere.Center.Z * (double)this.Normal.Z) + this.D;
            if ((double)num > (double)sphere.Radius)
                return PlaneIntersectionType.Front;
            return (double)num < -(double)sphere.Radius ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current Plane intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the Plane intersects the BoundingSphere.</param>
        public void Intersects(ref BoundingSphere sphere, out PlaneIntersectionType result)
        {
            float num = (float)((double)sphere.Center.X * (double)this.Normal.X + (double)sphere.Center.Y * (double)this.Normal.Y + (double)sphere.Center.Z * (double)this.Normal.Z) + this.D;
            if ((double)num > (double)sphere.Radius)
                result = PlaneIntersectionType.Front;
            else if ((double)num < -(double)sphere.Radius)
                result = PlaneIntersectionType.Back;
            else
                result = PlaneIntersectionType.Intersecting;
        }

        static System.Random _random;

        // CH: TODO: This function looks weird.
        // I'd delete it, but have respect towards someone else's code. If you need it, consider doing it in a better way.
        public Vector3 RandomPoint()
        {
            if (_random == null)
            {
                if (VRage.Library.Utils.MyRandom.DisableRandomSeed)
                {
                    _random = new Random(1);
                }
                else
                {
                    _random = new Random();
                }
            }
            
            Vector3 random = new Vector3();
            Vector3 randomPoint;

            do
            {
                random.X = 2.0f * (float)_random.NextDouble() - 1.0f;
                random.Y = 2.0f * (float)_random.NextDouble() - 1.0f;
                random.Z = 2.0f * (float)_random.NextDouble() - 1.0f;
                randomPoint = Vector3.Cross(random, Normal);
            } while (randomPoint == Vector3.Zero);

            randomPoint.Normalize();
            randomPoint *= (float)Math.Sqrt(_random.NextDouble());

            return randomPoint;
        }
    }
}
