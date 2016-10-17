using System;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a PlaneD.
    /// </summary>
    [Serializable]
    public struct PlaneD : IEquatable<PlaneD>
    {
        /// <summary>
        /// The normal vector of the PlaneD.
        /// </summary>
        public Vector3D Normal;
        /// <summary>
        /// The distance of the PlaneD along its normal from the origin.
        /// Note: Be careful! The distance is signed and is the opposite of what people usually expect.
        ///       If you look closely at the plane equation: (n dot P) + D = 0, you'll realize that D = - (n dot P) (that is, negative instead of positive)
        /// </summary>
        public double D;

        /// <summary>
        /// Creates a new instance of PlaneD.
        /// </summary>
        /// <param name="a">X component of the normal defining the PlaneD.</param><param name="b">Y component of the normal defining the PlaneD.</param><param name="c">Z component of the normal defining the PlaneD.</param><param name="d">Distance of the origin from the PlaneD along its normal.</param>
        public PlaneD(double a, double b, double c, double d)
        {
            this.Normal.X = a;
            this.Normal.Y = b;
            this.Normal.Z = c;
            this.D = d;
        }

        /// <summary>
        /// Creates a new instance of PlaneD.
        /// </summary>
        /// <param name="normal">The normal vector to the PlaneD.</param><param name="d">The distance of the origin from the PlaneD along its normal.</param>
        public PlaneD(Vector3D normal, double d)
        {
            this.Normal = normal;
            this.D = d;
        }

        /// <summary>
        /// Creates a new instance of Plane.
        /// </summary>
        /// <param name="position">A point that lies on the Plane</param><param name="normal">The normal vector to the Plane.</param>
        public PlaneD(Vector3D position, Vector3D normal)
        {
            //D = -(Ax + By + Cz)
            this.Normal = normal;
            this.D = -Vector3D.Dot(position, normal);
        }

        /// <summary>
        /// Creates a new instance of Plane.
        /// </summary>
        /// <param name="position">A point that lies on the Plane</param><param name="normal">The normal vector to the Plane.</param>
        public PlaneD(Vector3D position, Vector3 normal)
        {
            //D = -(Ax + By + Cz)
            this.Normal = normal;
            this.D = -Vector3D.Dot(position, normal);
        }        

        /// <summary>
        /// Creates a new instance of PlaneD.
        /// </summary>
        /// <param name="value">Vector4 with X, Y, and Z components defining the normal of the PlaneD. The W component defines the distance of the origin from the PlaneD along its normal.</param>
        public PlaneD(Vector4 value)
        {
            this.Normal.X = value.X;
            this.Normal.Y = value.Y;
            this.Normal.Z = value.Z;
            this.D = value.W;
        }

        /// <summary>
        /// Creates a new instance of PlaneD.
        /// </summary>
        /// <param name="point1">One point of a triangle defining the PlaneD.</param><param name="point2">One point of a triangle defining the PlaneD.</param><param name="point3">One point of a triangle defining the PlaneD.</param>
        public PlaneD(Vector3D point1, Vector3D point2, Vector3D point3)
        {
            double num1 = point2.X - point1.X;
            double num2 = point2.Y - point1.Y;
            double num3 = point2.Z - point1.Z;
            double num4 = point3.X - point1.X;
            double num5 = point3.Y - point1.Y;
            double num6 = point3.Z - point1.Z;
            double num7 = (double)((double)num2 * (double)num6 - (double)num3 * (double)num5);
            double num8 = (double)((double)num3 * (double)num4 - (double)num1 * (double)num6);
            double num9 = (double)((double)num1 * (double)num5 - (double)num2 * (double)num4);
            double num10 = 1f / (double)Math.Sqrt((double)num7 * (double)num7 + (double)num8 * (double)num8 + (double)num9 * (double)num9);
            this.Normal.X = num7 * num10;
            this.Normal.Y = num8 * num10;
            this.Normal.Z = num9 * num10;
            this.D = (double)-((double)this.Normal.X * (double)point1.X + (double)this.Normal.Y * (double)point1.Y + (double)this.Normal.Z * (double)point1.Z);
        }

        /// <summary>
        /// Determines whether two instances of PlaneD are equal.
        /// </summary>
        /// <param name="lhs">The object to the left of the equality operator.</param><param name="rhs">The object to the right of the equality operator.</param>
        public static bool operator ==(PlaneD lhs, PlaneD rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Determines whether two instances of PlaneD are not equal.
        /// </summary>
        /// <param name="lhs">The object to the left of the inequality operator.</param><param name="rhs">The object to the right of the inequality operator.</param>
        public static bool operator !=(PlaneD lhs, PlaneD rhs)
        {
            if ((double)lhs.Normal.X == (double)rhs.Normal.X && (double)lhs.Normal.Y == (double)rhs.Normal.Y && (double)lhs.Normal.Z == (double)rhs.Normal.Z)
                return (double)lhs.D != (double)rhs.D;
            else
                return true;
        }

        /// <summary>
        /// Determines whether the specified PlaneD is equal to the PlaneD.
        /// </summary>
        /// <param name="other">The PlaneD to compare with the current PlaneD.</param>
        public bool Equals(PlaneD other)
        {
            if ((double)this.Normal.X == (double)other.Normal.X && (double)this.Normal.Y == (double)other.Normal.Y && (double)this.Normal.Z == (double)other.Normal.Z)
                return (double)this.D == (double)other.D;
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the PlaneD.
        /// </summary>
        /// <param name="obj">The Object to compare with the current PlaneD.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is PlaneD)
                flag = this.Equals((PlaneD)obj);
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
        /// Returns a String that represents the current PlaneD.
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
        /// Changes the coefficients of the Normal vector of this PlaneD to make it of unit length.
        /// </summary>
        public void Normalize()
        {
            double num1 = (double)((double)this.Normal.X * (double)this.Normal.X + (double)this.Normal.Y * (double)this.Normal.Y + (double)this.Normal.Z * (double)this.Normal.Z);
            if ((double)Math.Abs(num1 - 1f) < 1.19209289550781E-07)
                return;
            double num2 = 1f / (double)Math.Sqrt((double)num1);
            this.Normal.X *= num2;
            this.Normal.Y *= num2;
            this.Normal.Z *= num2;
            this.D *= num2;
        }

        /// <summary>
        /// Changes the coefficients of the Normal vector of a PlaneD to make it of unit length.
        /// </summary>
        /// <param name="value">The PlaneD to normalize.</param>
        public static PlaneD Normalize(PlaneD value)
        {
            double num1 = (double)((double)value.Normal.X * (double)value.Normal.X + (double)value.Normal.Y * (double)value.Normal.Y + (double)value.Normal.Z * (double)value.Normal.Z);
            if ((double)Math.Abs(num1 - 1f) < 1.19209289550781E-07)
            {
                PlaneD PlaneD;
                PlaneD.Normal = value.Normal;
                PlaneD.D = value.D;
                return PlaneD;
            }
            else
            {
                double num2 = 1f / (double)Math.Sqrt((double)num1);
                PlaneD PlaneD;
                PlaneD.Normal.X = value.Normal.X * num2;
                PlaneD.Normal.Y = value.Normal.Y * num2;
                PlaneD.Normal.Z = value.Normal.Z * num2;
                PlaneD.D = value.D * num2;
                return PlaneD;
            }
        }

        /// <summary>
        /// Changes the coefficients of the Normal vector of a PlaneD to make it of unit length.
        /// </summary>
        /// <param name="value">The PlaneD to normalize.</param><param name="result">[OutAttribute] An existing PlaneD PlaneD filled in with a normalized version of the specified PlaneD.</param>
        public static void Normalize(ref PlaneD value, out PlaneD result)
        {
            double num1 = (double)((double)value.Normal.X * (double)value.Normal.X + (double)value.Normal.Y * (double)value.Normal.Y + (double)value.Normal.Z * (double)value.Normal.Z);
            if ((double)Math.Abs(num1 - 1f) < 1.19209289550781E-07)
            {
                result.Normal = value.Normal;
                result.D = value.D;
            }
            else
            {
                double num2 = 1f / (double)Math.Sqrt((double)num1);
                result.Normal.X = value.Normal.X * num2;
                result.Normal.Y = value.Normal.Y * num2;
                result.Normal.Z = value.Normal.Z * num2;
                result.D = value.D * num2;
            }
        }

        /// <summary>
        /// Transforms a normalized PlaneD by a Matrix.
        /// </summary>
        /// <param name="PlaneD">The normalized PlaneD to transform. This PlaneD must already be normalized, so that its Normal vector is of unit length, before this method is called.</param><param name="matrix">The transform Matrix to apply to the PlaneD.</param>
        public static PlaneD Transform(PlaneD plane, MatrixD matrix)
        {
            PlaneD result;
            Transform(ref plane, ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Transforms a normalized PlaneD by a Matrix.
        /// </summary>
        /// <param name="PlaneD">The normalized PlaneD to transform. This PlaneD must already be normalized, so that its Normal vector is of unit length, before this method is called.</param><param name="matrix">The transform Matrix to apply to the PlaneD.</param><param name="result">[OutAttribute] An existing PlaneD filled in with the results of applying the transform.</param>
        public static void Transform(ref PlaneD plane, ref MatrixD matrix, out PlaneD result)
        {
            result = default(PlaneD);
            Vector3D origin = -plane.Normal * plane.D;
            Vector3D.TransformNormal(ref plane.Normal, ref matrix, out result.Normal);
            Vector3D.Transform(ref origin, ref matrix, out origin);
            Vector3D.Dot(ref origin, ref result.Normal, out result.D);
            result.D = -result.D;
        }

        /// <summary>
        /// Calculates the dot product of a specified Vector4 and this PlaneD.
        /// </summary>
        /// <param name="value">The Vector4 to multiply this PlaneD by.</param>
        public double Dot(Vector4 value)
        {
            return (double)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z + (double)this.D * (double)value.W);
        }

        /// <summary>
        /// Calculates the dot product of a specified Vector4 and this PlaneD.
        /// </summary>
        /// <param name="value">The Vector4 to multiply this PlaneD by.</param><param name="result">[OutAttribute] The dot product of the specified Vector4 and this PlaneD.</param>
        public void Dot(ref Vector4 value, out double result)
        {
            result = (double)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z + (double)this.D * (double)value.W);
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3D and the Normal vector of this PlaneD plus the distance (D) value of the PlaneD.
        /// </summary>
        /// <param name="value">The Vector3D to multiply by.</param>
        public double DotCoordinate(Vector3D value)
        {
            return (double)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z) + this.D;
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3D and the Normal vector of this PlaneD plus the distance (D) value of the PlaneD.
        /// </summary>
        /// <param name="value">The Vector3D to multiply by.</param><param name="result">[OutAttribute] The resulting value.</param>
        public void DotCoordinate(ref Vector3D value, out double result)
        {
            result = (double)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z) + this.D;
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3D and the Normal vector of this PlaneD.
        /// </summary>
        /// <param name="value">The Vector3D to multiply by.</param>
        public double DotNormal(Vector3D value)
        {
            return (double)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z);
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3D and the Normal vector of this PlaneD.
        /// </summary>
        /// <param name="value">The Vector3D to multiply by.</param><param name="result">[OutAttribute] The resulting dot product.</param>
        public void DotNormal(ref Vector3D value, out double result)
        {
            result = (double)((double)this.Normal.X * (double)value.X + (double)this.Normal.Y * (double)value.Y + (double)this.Normal.Z * (double)value.Z);
        }

        /// <summary>
        /// Checks whether the current PlaneD intersects a specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for intersection with.</param>
        public PlaneIntersectionType Intersects(BoundingBoxD box)
        {
            Vector3D Vector3D_1;
            Vector3D_1.X = (double)this.Normal.X >= 0.0 ? box.Min.X : box.Max.X;
            Vector3D_1.Y = (double)this.Normal.Y >= 0.0 ? box.Min.Y : box.Max.Y;
            Vector3D_1.Z = (double)this.Normal.Z >= 0.0 ? box.Min.Z : box.Max.Z;
            Vector3D Vector3D_2;
            Vector3D_2.X = (double)this.Normal.X >= 0.0 ? box.Max.X : box.Min.X;
            Vector3D_2.Y = (double)this.Normal.Y >= 0.0 ? box.Max.Y : box.Min.Y;
            Vector3D_2.Z = (double)this.Normal.Z >= 0.0 ? box.Max.Z : box.Min.Z;
            if ((double)this.Normal.X * (double)Vector3D_1.X + (double)this.Normal.Y * (double)Vector3D_1.Y + (double)this.Normal.Z * (double)Vector3D_1.Z + (double)this.D > 0.0)
                return PlaneIntersectionType.Front;
            return (double)this.Normal.X * (double)Vector3D_2.X + (double)this.Normal.Y * (double)Vector3D_2.Y + (double)this.Normal.Z * (double)Vector3D_2.Z + (double)this.D < 0.0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current PlaneD intersects a BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the PlaneD intersects the BoundingBox.</param>
        public void Intersects(ref BoundingBoxD box, out PlaneIntersectionType result)
        {
            Vector3D Vector3D_1;
            Vector3D_1.X = (double)this.Normal.X >= 0.0 ? box.Min.X : box.Max.X;
            Vector3D_1.Y = (double)this.Normal.Y >= 0.0 ? box.Min.Y : box.Max.Y;
            Vector3D_1.Z = (double)this.Normal.Z >= 0.0 ? box.Min.Z : box.Max.Z;
            Vector3D Vector3D_2;
            Vector3D_2.X = (double)this.Normal.X >= 0.0 ? box.Max.X : box.Min.X;
            Vector3D_2.Y = (double)this.Normal.Y >= 0.0 ? box.Max.Y : box.Min.Y;
            Vector3D_2.Z = (double)this.Normal.Z >= 0.0 ? box.Max.Z : box.Min.Z;
            if ((double)this.Normal.X * (double)Vector3D_1.X + (double)this.Normal.Y * (double)Vector3D_1.Y + (double)this.Normal.Z * (double)Vector3D_1.Z + (double)this.D > 0.0)
                result = PlaneIntersectionType.Front;
            else if ((double)this.Normal.X * (double)Vector3D_2.X + (double)this.Normal.Y * (double)Vector3D_2.Y + (double)this.Normal.Z * (double)Vector3D_2.Z + (double)this.D < 0.0)
                result = PlaneIntersectionType.Back;
            else
                result = PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current PlaneD intersects a specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with.</param>
        public PlaneIntersectionType Intersects(BoundingFrustumD frustum)
        {
            return frustum.Intersects(this);
        }

        /// <summary>
        /// Checks whether the current PlaneD intersects a specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        public PlaneIntersectionType Intersects(BoundingSphereD sphere)
        {
            double num = (double)((double)sphere.Center.X * (double)this.Normal.X + (double)sphere.Center.Y * (double)this.Normal.Y + (double)sphere.Center.Z * (double)this.Normal.Z) + this.D;
            if ((double)num > (double)sphere.Radius)
                return PlaneIntersectionType.Front;
            return (double)num < -(double)sphere.Radius ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Checks whether the current PlaneD intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the PlaneD intersects the BoundingSphere.</param>
        public void Intersects(ref BoundingSphere sphere, out PlaneIntersectionType result)
        {
            double num = (double)((double)sphere.Center.X * (double)this.Normal.X + (double)sphere.Center.Y * (double)this.Normal.Y + (double)sphere.Center.Z * (double)this.Normal.Z) + this.D;
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
        public Vector3D RandomPoint()
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

            Vector3D random = new Vector3D();
            Vector3D randomPoint;

            do
            {
                random.X = 2.0f * (double)_random.NextDouble() - 1.0f;
                random.Y = 2.0f * (double)_random.NextDouble() - 1.0f;
                random.Z = 2.0f * (double)_random.NextDouble() - 1.0f;
                randomPoint = Vector3D.Cross(random, Normal);
            } while (randomPoint == Vector3D.Zero);

            randomPoint.Normalize();
            randomPoint *= (double)Math.Sqrt(_random.NextDouble());

            return randomPoint;
        }

        public double DistanceToPoint(Vector3D point)
        {
            var dot = Vector3D.Dot(Normal, point);
            return dot + D;
        }

        public double DistanceToPoint(ref Vector3D point)
        {
            var dot = Vector3D.Dot(Normal, point);
            return dot + D;
        }

        public Vector3D ProjectPoint(ref Vector3D point)
        {
            return point - Normal * DistanceToPoint(ref point);
        }

        /// <summary>
        /// Gets intersection point in Plane.
        /// </summary>
        /// <param name="from">Starting point of a ray.</param>
        /// <param name="direction">Ray direction.</param>
        /// <returns>Point of intersection.</returns>
        public Vector3D Intersection(ref Vector3D from, ref Vector3D direction)
        {
            var t = - (DotNormal(from) + D) / DotNormal(direction);

            return new Vector3D(from + t * direction);
        }
    }
}
