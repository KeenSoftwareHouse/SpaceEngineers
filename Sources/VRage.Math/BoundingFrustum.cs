using System;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a frustum and helps determine whether forms intersect with it.
    /// </summary>
    [Serializable]
    public class BoundingFrustum : IEquatable<BoundingFrustum>
    {
        private Plane[] planes = new Plane[6];
        internal Vector3[] cornerArray = new Vector3[8];
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingFrustum.
        /// </summary>
        public const int CornerCount = 8;
        private const int NearPlaneIndex = 0;
        private const int FarPlaneIndex = 1;
        private const int LeftPlaneIndex = 2;
        private const int RightPlaneIndex = 3;
        private const int TopPlaneIndex = 4;
        private const int BottomPlaneIndex = 5;
        private const int NumPlanes = 6;
        private Matrix matrix;
        private Gjk gjk;

        public Plane this[int index]
        {
            get
            {
                return this.planes[index];
            }
        }

        /// <summary>
        /// Gets the near plane of the BoundingFrustum.
        /// </summary>
        public Plane Near
        {
            get
            {
                return this.planes[0];
            }
        }

        /// <summary>
        /// Gets the far plane of the BoundingFrustum.
        /// </summary>
        public Plane Far
        {
            get
            {
                return this.planes[1];
            }
        }

        /// <summary>
        /// Gets the left plane of the BoundingFrustum.
        /// </summary>
        public Plane Left
        {
            get
            {
                return this.planes[2];
            }
        }

        /// <summary>
        /// Gets the right plane of the BoundingFrustum.
        /// </summary>
        public Plane Right
        {
            get
            {
                return this.planes[3];
            }
        }

        /// <summary>
        /// Gets the top plane of the BoundingFrustum.
        /// </summary>
        public Plane Top
        {
            get
            {
                return this.planes[4];
            }
        }

        /// <summary>
        /// Gets the bottom plane of the BoundingFrustum.
        /// </summary>
        public Plane Bottom
        {
            get
            {
                return this.planes[5];
            }
        }

        /// <summary>
        /// Gets or sets the Matrix that describes this bounding frustum.
        /// </summary>
        public Matrix Matrix
        {
            get
            {
                return this.matrix;
            }
            set
            {
                this.SetMatrix(ref value);
            }
        }

        private BoundingFrustum()
        {
        }

        /// <summary>
        /// Creates a new instance of BoundingFrustum.
        /// </summary>
        /// <param name="value">Combined matrix that usually takes view × projection matrix.</param>
        public BoundingFrustum(Matrix value)
        {
            this.SetMatrix(ref value);
        }

        /// <summary>
        /// Determines whether two instances of BoundingFrustum are equal.
        /// </summary>
        /// <param name="a">The BoundingFrustum to the left of the equality operator.</param><param name="b">The BoundingFrustum to the right of the equality operator.</param>
        public static bool operator ==(BoundingFrustum a, BoundingFrustum b)
        {
            return object.Equals((object)a, (object)b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingFrustum are not equal.
        /// </summary>
        /// <param name="a">The BoundingFrustum to the left of the inequality operator.</param><param name="b">The BoundingFrustum to the right of the inequality operator.</param>
        public static bool operator !=(BoundingFrustum a, BoundingFrustum b)
        {
            return !object.Equals((object)a, (object)b);
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingFrustum. ALLOCATION!
        /// </summary>
        public Vector3[] GetCorners()
        {
            return (Vector3[])this.cornerArray.Clone();
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingFrustum.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3 points where the corners of the BoundingFrustum are written.</param>
        public void GetCorners(Vector3[] corners)
        {
            this.cornerArray.CopyTo((Array)corners, 0);
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3 points where the corners of the BoundingBox are written.</param>
        [Unsharper.UnsharperDisableReflection()]
        public unsafe void GetCornersUnsafe(Vector3* corners)
        {
            corners[0] = cornerArray[0];
            corners[1] = cornerArray[1];
            corners[2] = cornerArray[2];
            corners[3] = cornerArray[3];
            corners[4] = cornerArray[4];
            corners[5] = cornerArray[5];
            corners[6] = cornerArray[6];
            corners[7] = cornerArray[7];
        }

        /// <summary>
        /// Determines whether the specified BoundingFrustum is equal to the current BoundingFrustum.
        /// </summary>
        /// <param name="other">The BoundingFrustum to compare with the current BoundingFrustum.</param>
        public bool Equals(BoundingFrustum other)
        {
            if (other == (BoundingFrustum)null)
                return false;
            else
                return this.matrix == other.matrix;
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the BoundingFrustum.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingFrustum.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            BoundingFrustum boundingFrustum = obj as BoundingFrustum;
            if (boundingFrustum != (BoundingFrustum)null)
                flag = this.matrix == boundingFrustum.matrix;
            return flag;
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.matrix.GetHashCode();
        }

        /// <summary>
        /// Returns a String that represents the current BoundingFrustum.
        /// </summary>
        public override string ToString()
        {
            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, "{{Near:{0} Far:{1} Left:{2} Right:{3} Top:{4} Bottom:{5}}}", (object)this.Near.ToString(), (object)this.Far.ToString(), (object)this.Left.ToString(), (object)this.Right.ToString(), (object)this.Top.ToString(), (object)this.Bottom.ToString());
        }

        private void SetMatrix(ref Matrix value)
        {
            this.matrix = value;
            this.planes[2].Normal.X = -value.M14 - value.M11;
            this.planes[2].Normal.Y = -value.M24 - value.M21;
            this.planes[2].Normal.Z = -value.M34 - value.M31;
            this.planes[2].D = -value.M44 - value.M41;
            this.planes[3].Normal.X = -value.M14 + value.M11;
            this.planes[3].Normal.Y = -value.M24 + value.M21;
            this.planes[3].Normal.Z = -value.M34 + value.M31;
            this.planes[3].D = -value.M44 + value.M41;
            this.planes[4].Normal.X = -value.M14 + value.M12;
            this.planes[4].Normal.Y = -value.M24 + value.M22;
            this.planes[4].Normal.Z = -value.M34 + value.M32;
            this.planes[4].D = -value.M44 + value.M42;
            this.planes[5].Normal.X = -value.M14 - value.M12;
            this.planes[5].Normal.Y = -value.M24 - value.M22;
            this.planes[5].Normal.Z = -value.M34 - value.M32;
            this.planes[5].D = -value.M44 - value.M42;
            this.planes[0].Normal.X = -value.M13;
            this.planes[0].Normal.Y = -value.M23;
            this.planes[0].Normal.Z = -value.M33;
            this.planes[0].D = -value.M43;
            this.planes[1].Normal.X = -value.M14 + value.M13;
            this.planes[1].Normal.Y = -value.M24 + value.M23;
            this.planes[1].Normal.Z = -value.M34 + value.M33;
            this.planes[1].D = -value.M44 + value.M43;
            for (int index = 0; index < 6; ++index)
            {
                float num = this.planes[index].Normal.Length();
                this.planes[index].Normal /= num;
                this.planes[index].D /= num;
            }
            Ray intersectionLine1 = BoundingFrustum.ComputeIntersectionLine(ref this.planes[0], ref this.planes[2]);
            this.cornerArray[0] = BoundingFrustum.ComputeIntersection(ref this.planes[4], ref intersectionLine1);
            this.cornerArray[3] = BoundingFrustum.ComputeIntersection(ref this.planes[5], ref intersectionLine1);
            Ray intersectionLine2 = BoundingFrustum.ComputeIntersectionLine(ref this.planes[3], ref this.planes[0]);
            this.cornerArray[1] = BoundingFrustum.ComputeIntersection(ref this.planes[4], ref intersectionLine2);
            this.cornerArray[2] = BoundingFrustum.ComputeIntersection(ref this.planes[5], ref intersectionLine2);
            intersectionLine2 = BoundingFrustum.ComputeIntersectionLine(ref this.planes[2], ref this.planes[1]);
            this.cornerArray[4] = BoundingFrustum.ComputeIntersection(ref this.planes[4], ref intersectionLine2);
            this.cornerArray[7] = BoundingFrustum.ComputeIntersection(ref this.planes[5], ref intersectionLine2);
            intersectionLine2 = BoundingFrustum.ComputeIntersectionLine(ref this.planes[1], ref this.planes[3]);
            this.cornerArray[5] = BoundingFrustum.ComputeIntersection(ref this.planes[4], ref intersectionLine2);
            this.cornerArray[6] = BoundingFrustum.ComputeIntersection(ref this.planes[5], ref intersectionLine2);
        }

        private static Ray ComputeIntersectionLine(ref Plane p1, ref Plane p2)
        {
            Ray ray = new Ray();
            ray.Direction = Vector3.Cross(p1.Normal, p2.Normal);
            float num = ray.Direction.LengthSquared();
            ray.Position = Vector3.Cross(-p1.D * p2.Normal + p2.D * p1.Normal, ray.Direction) / num;
            return ray;
        }

        private static Vector3 ComputeIntersection(ref Plane plane, ref Ray ray)
        {
            float num = (-plane.D - Vector3.Dot(plane.Normal, ray.Position)) / Vector3.Dot(plane.Normal, ray.Direction);
            return ray.Position + ray.Direction * num;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection.</param>
        public bool Intersects(BoundingBox box)
        {
            bool result;
            this.Intersects(ref box, out result);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingFrustum and BoundingBox intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox box, out bool result)
        {
            if (this.gjk == null)
                this.gjk = new Gjk();
            this.gjk.Reset();
            Vector3 result1;
            Vector3.Subtract(ref this.cornerArray[0], ref box.Min, out result1);
            if ((double)result1.LengthSquared() < 9.99999974737875E-06)
                Vector3.Subtract(ref this.cornerArray[0], ref box.Max, out result1);
            float num1 = float.MaxValue;
            result = false;
            float num2;
            do
            {
                Vector3 v;
                v.X = -result1.X;
                v.Y = -result1.Y;
                v.Z = -result1.Z;
                Vector3 result2;
                this.SupportMapping(ref v, out result2);
                Vector3 result3;
                box.SupportMapping(ref result1, out result3);
                Vector3 result4;
                Vector3.Subtract(ref result2, ref result3, out result4);
                if ((double)result1.X * (double)result4.X + (double)result1.Y * (double)result4.Y + (double)result1.Z * (double)result4.Z > 0.0)
                    return;
                this.gjk.AddSupportPoint(ref result4);
                result1 = this.gjk.ClosestPoint;
                float num3 = num1;
                num1 = result1.LengthSquared();
                if ((double)num3 - (double)num1 <= 9.99999974737875E-06 * (double)num3)
                    return;
                num2 = 4E-05f * this.gjk.MaxLengthSquared;
            }
            while (!this.gjk.FullSimplex && (double)num1 >= (double)num2);
            result = true;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection.</param>
        public bool Intersects(BoundingFrustum frustum)
        {
            if (frustum == (BoundingFrustum)null)
                throw new ArgumentNullException("frustum");
            if (this.gjk == null)
                this.gjk = new Gjk();
            this.gjk.Reset();
            Vector3 result1;
            Vector3.Subtract(ref this.cornerArray[0], ref frustum.cornerArray[0], out result1);
            if ((double)result1.LengthSquared() < 9.99999974737875E-06)
                Vector3.Subtract(ref this.cornerArray[0], ref frustum.cornerArray[1], out result1);
            float num1 = float.MaxValue;
            float num2;
            do
            {
                Vector3 v;
                v.X = -result1.X;
                v.Y = -result1.Y;
                v.Z = -result1.Z;
                Vector3 result2;
                this.SupportMapping(ref v, out result2);
                Vector3 result3;
                frustum.SupportMapping(ref result1, out result3);
                Vector3 result4;
                Vector3.Subtract(ref result2, ref result3, out result4);
                if ((double)result1.X * (double)result4.X + (double)result1.Y * (double)result4.Y + (double)result1.Z * (double)result4.Z > 0.0)
                    return false;
                this.gjk.AddSupportPoint(ref result4);
                result1 = this.gjk.ClosestPoint;
                float num3 = num1;
                num1 = result1.LengthSquared();
                num2 = 4E-05f * this.gjk.MaxLengthSquared;
                if ((double)num3 - (double)num1 <= 9.99999974737875E-06 * (double)num3)
                    return false;
            }
            while (!this.gjk.FullSimplex && (double)num1 >= (double)num2);
            return true;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection.</param>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            int num = 0;
            for (int index = 0; index < 8; ++index)
            {
                float result;
                Vector3.Dot(ref this.cornerArray[index], ref plane.Normal, out result);
                if ((double)result + (double)plane.D > 0.0)
                    num |= 1;
                else
                    num |= 2;
                if (num == 3)
                    return PlaneIntersectionType.Intersecting;
            }
            return num != 1 ? PlaneIntersectionType.Back : PlaneIntersectionType.Front;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the BoundingFrustum intersects the Plane.</param>
        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            int num = 0;
            for (int index = 0; index < 8; ++index)
            {
                float result1;
                Vector3.Dot(ref this.cornerArray[index], ref plane.Normal, out result1);
                if ((double)result1 + (double)plane.D > 0.0)
                    num |= 1;
                else
                    num |= 2;
                if (num == 3)
                {
                    result = PlaneIntersectionType.Intersecting;
                    return;
                }
            }
            result = num == 1 ? PlaneIntersectionType.Front : PlaneIntersectionType.Back;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection.</param>
        public float? Intersects(Ray ray)
        {
            float? result;
            this.Intersects(ref ray, out result);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param><param name="result">[OutAttribute] Distance at which the ray intersects the BoundingFrustum or null if there is no intersection.</param>
        public void Intersects(ref Ray ray, out float? result)
        {
            ContainmentType result1;
            this.Contains(ref ray.Position, out result1);
            if (result1 == ContainmentType.Contains)
            {
                result = new float?(0.0f);
            }
            else
            {
                float num1 = float.MinValue;
                float num2 = float.MaxValue;
                result = new float?();
                foreach (Plane plane in this.planes)
                {
                    Vector3 vector2 = plane.Normal;
                    float result2;
                    Vector3.Dot(ref ray.Direction, ref vector2, out result2);
                    float result3;
                    Vector3.Dot(ref ray.Position, ref vector2, out result3);
                    result3 += plane.D;
                    if ((double)Math.Abs(result2) < 9.99999974737875E-06)
                    {
                        if ((double)result3 > 0.0)
                            return;
                    }
                    else
                    {
                        float num3 = -result3 / result2;
                        if ((double)result2 < 0.0)
                        {
                            if ((double)num3 > (double)num2)
                                return;
                            if ((double)num3 > (double)num1)
                                num1 = num3;
                        }
                        else
                        {
                            if ((double)num3 < (double)num1)
                                return;
                            if ((double)num3 < (double)num2)
                                num2 = num3;
                        }
                    }
                }
                float num4 = (double)num1 >= 0.0 ? num1 : num2;
                if ((double)num4 < 0.0)
                    return;
                result = new float?(num4);
            }
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection.</param>
        public bool Intersects(BoundingSphere sphere)
        {
            bool result;
            this.Intersects(ref sphere, out result);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingFrustum and BoundingSphere intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            if (this.gjk == null)
                this.gjk = new Gjk();
            this.gjk.Reset();
            Vector3 result1;
            Vector3.Subtract(ref this.cornerArray[0], ref sphere.Center, out result1);
            if ((double)result1.LengthSquared() < 9.99999974737875E-06)
                result1 = Vector3.UnitX;
            float num1 = float.MaxValue;
            result = false;
            float num2;
            do
            {
                Vector3 v;
                v.X = -result1.X;
                v.Y = -result1.Y;
                v.Z = -result1.Z;
                Vector3 result2;
                this.SupportMapping(ref v, out result2);
                Vector3 result3;
                sphere.SupportMapping(ref result1, out result3);
                Vector3 result4;
                Vector3.Subtract(ref result2, ref result3, out result4);
                if ((double)result1.X * (double)result4.X + (double)result1.Y * (double)result4.Y + (double)result1.Z * (double)result4.Z > 0.0)
                    return;
                this.gjk.AddSupportPoint(ref result4);
                result1 = this.gjk.ClosestPoint;
                float num3 = num1;
                num1 = result1.LengthSquared();
                if ((double)num3 - (double)num1 <= 9.99999974737875E-06 * (double)num3)
                    return;
                num2 = 4E-05f * this.gjk.MaxLengthSquared;
            }
            while (!this.gjk.FullSimplex && (double)num1 >= (double)num2);
            result = true;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum contains the specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(BoundingBox box)
        {
            bool flag = false;
            foreach (Plane plane in this.planes)
            {
                switch (box.Intersects(plane))
                {
                    case PlaneIntersectionType.Front:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        flag = true;
                        break;
                }
            }
            return !flag ? ContainmentType.Contains : ContainmentType.Intersects;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum contains the specified BoundingBox.
        /// </summary>
        /// <param name="box">The BoundingBox to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            bool flag = false;
            foreach (Plane plane in this.planes)
            {
                switch (box.Intersects(plane))
                {
                    case PlaneIntersectionType.Front:
                        result = ContainmentType.Disjoint;
                        return;
                    case PlaneIntersectionType.Intersecting:
                        flag = true;
                        break;
                }
            }
            result = flag ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum contains the specified BoundingFrustum.
        /// </summary>
        /// <param name="frustum">The BoundingFrustum to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(BoundingFrustum frustum)
        {
            if (frustum == (BoundingFrustum)null)
                throw new ArgumentNullException("frustum");
            ContainmentType containmentType = ContainmentType.Disjoint;
            if (this.Intersects(frustum))
            {
                containmentType = ContainmentType.Contains;
                for (int index = 0; index < this.cornerArray.Length; ++index)
                {
                    if (this.Contains(frustum.cornerArray[index]) == ContainmentType.Disjoint)
                    {
                        containmentType = ContainmentType.Intersects;
                        break;
                    }
                }
            }
            return containmentType;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum contains the specified point.
        /// </summary>
        /// <param name="point">The point to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(Vector3 point)
        {
            foreach (Plane plane in this.planes)
            {
                if ((double)((float)((double)plane.Normal.X * (double)point.X + (double)plane.Normal.Y * (double)point.Y + (double)plane.Normal.Z * (double)point.Z) + plane.D) > 9.99999974737875E-06)
                    return ContainmentType.Disjoint;
            }
            return ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum contains the specified point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            foreach (Plane plane in this.planes)
            {
                if ((double)((float)((double)plane.Normal.X * (double)point.X + (double)plane.Normal.Y * (double)point.Y + (double)plane.Normal.Z * (double)point.Z) + plane.D) > 9.99999974737875E-06)
                {
                    result = ContainmentType.Disjoint;
                    return;
                }
            }
            result = ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum contains the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(BoundingSphere sphere)
        {
            Vector3 vector3 = sphere.Center;
            float num1 = sphere.Radius;
            int num2 = 0;
            foreach (Plane plane in this.planes)
            {
                float num3 = (float)((double)plane.Normal.X * (double)vector3.X + (double)plane.Normal.Y * (double)vector3.Y + (double)plane.Normal.Z * (double)vector3.Z) + plane.D;
                if ((double)num3 > (double)num1)
                    return ContainmentType.Disjoint;
                if ((double)num3 < -(double)num1)
                    ++num2;
            }
            return num2 != 6 ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum contains the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            Vector3 vector3 = sphere.Center;
            float num1 = sphere.Radius;
            int num2 = 0;
            foreach (Plane plane in this.planes)
            {
                float num3 = (float)((double)plane.Normal.X * (double)vector3.X + (double)plane.Normal.Y * (double)vector3.Y + (double)plane.Normal.Z * (double)vector3.Z) + plane.D;
                if ((double)num3 > (double)num1)
                {
                    result = ContainmentType.Disjoint;
                    return;
                }
                else if ((double)num3 < -(double)num1)
                    ++num2;
            }
            result = num2 == 6 ? ContainmentType.Contains : ContainmentType.Intersects;
        }

        internal void SupportMapping(ref Vector3 v, out Vector3 result)
        {
            int index1 = 0;
            float result1;
            Vector3.Dot(ref this.cornerArray[0], ref v, out result1);
            for (int index2 = 1; index2 < this.cornerArray.Length; ++index2)
            {
                float result2;
                Vector3.Dot(ref this.cornerArray[index2], ref v, out result2);
                if ((double)result2 > (double)result1)
                {
                    index1 = index2;
                    result1 = result2;
                }
            }
            result = this.cornerArray[index1];
        }
    }
}
