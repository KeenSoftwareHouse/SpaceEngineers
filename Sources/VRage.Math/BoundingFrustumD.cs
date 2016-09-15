using System;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a frustum and helps determine whether forms intersect with it.
    /// </summary>
    [Serializable]
    public class BoundingFrustumD : IEquatable<BoundingFrustumD>
    {
        private PlaneD[] planes = new PlaneD[6];
        internal Vector3D[] cornerArray = new Vector3D[8];
        /// <summary>
        /// Specifies the total number of corners (8) in the BoundingFrustumD.
        /// </summary>
        public const int CornerCount = 8;
        private const int NearPlaneIndex = 0;
        private const int FarPlaneIndex = 1;
        private const int LeftPlaneIndex = 2;
        private const int RightPlaneIndex = 3;
        private const int TopPlaneIndex = 4;
        private const int BottomPlaneIndex = 5;
        private const int NumPlanes = 6;
        private MatrixD matrix;
        private GjkD gjk;

        public PlaneD this[int index]
        {
            get
            {
                return this.planes[index];
            }
        }

        /// <summary>
        /// Gets the near plane of the BoundingFrustumD.
        /// </summary>
        public PlaneD Near
        {
            get
            {
                return this.planes[0];
            }
        }

        /// <summary>
        /// Gets the far plane of the BoundingFrustumD.
        /// </summary>
        public PlaneD Far
        {
            get
            {
                return this.planes[1];
            }
        }

        /// <summary>
        /// Gets the left plane of the BoundingFrustumD.
        /// </summary>
        public PlaneD Left
        {
            get
            {
                return this.planes[2];
            }
        }

        /// <summary>
        /// Gets the right plane of the BoundingFrustumD.
        /// </summary>
        public PlaneD Right
        {
            get
            {
                return this.planes[3];
            }
        }

        /// <summary>
        /// Gets the top plane of the BoundingFrustumD.
        /// </summary>
        public PlaneD Top
        {
            get
            {
                return this.planes[4];
            }
        }

        /// <summary>
        /// Gets the bottom plane of the BoundingFrustumD.
        /// </summary>
        public PlaneD Bottom
        {
            get
            {
                return this.planes[5];
            }
        }

        /// <summary>
        /// Gets or sets the Matrix that describes this bounding frustum.
        /// </summary>
        public MatrixD Matrix
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

        public BoundingFrustumD()
        {
        }

        /// <summary>
        /// Creates a new instance of BoundingFrustumD.
        /// </summary>
        /// <param name="value">Combined matrix that usually takes view × projection matrix.</param>
        public BoundingFrustumD(MatrixD value)
        {
            this.SetMatrix(ref value);
        }

        /// <summary>
        /// Determines whether two instances of BoundingFrustumD are equal.
        /// </summary>
        /// <param name="a">The BoundingFrustumD to the left of the equality operator.</param><param name="b">The BoundingFrustumD to the right of the equality operator.</param>
        public static bool operator ==(BoundingFrustumD a, BoundingFrustumD b)
        {
            return object.Equals((object)a, (object)b);
        }

        /// <summary>
        /// Determines whether two instances of BoundingFrustumD are not equal.
        /// </summary>
        /// <param name="a">The BoundingFrustumD to the left of the inequality operator.</param><param name="b">The BoundingFrustumD to the right of the inequality operator.</param>
        public static bool operator !=(BoundingFrustumD a, BoundingFrustumD b)
        {
            return !object.Equals((object)a, (object)b);
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingFrustumD. ALLOCATION!
        /// </summary>
        public Vector3D[] GetCorners()
        {
            return (Vector3D[])this.cornerArray.Clone();
        }

        /// <summary>
        /// Gets an array of points that make up the corners of the BoundingFrustumD.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3D points where the corners of the BoundingFrustumD are written.</param>
        public void GetCorners(Vector3D[] corners)
        {
            this.cornerArray.CopyTo((Array)corners, 0);
        }

        /// <summary>
        /// Gets the array of points that make up the corners of the BoundingBox.
        /// </summary>
        /// <param name="corners">An existing array of at least 8 Vector3 points where the corners of the BoundingBox are written.</param>
        [Unsharper.UnsharperDisableReflection()]
        public unsafe void GetCornersUnsafe(Vector3D* corners)
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
        /// Determines whether the specified BoundingFrustumD is equal to the current BoundingFrustumD.
        /// </summary>
        /// <param name="other">The BoundingFrustumD to compare with the current BoundingFrustumD.</param>
        public bool Equals(BoundingFrustumD other)
        {
            if (other == (BoundingFrustumD)null)
                return false;
            else
                return this.matrix == other.matrix;
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the BoundingFrustumD.
        /// </summary>
        /// <param name="obj">The Object to compare with the current BoundingFrustumD.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            BoundingFrustumD BoundingFrustumD = obj as BoundingFrustumD;
            if (BoundingFrustumD != (BoundingFrustumD)null)
                flag = this.matrix == BoundingFrustumD.matrix;
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
        /// Returns a String that represents the current BoundingFrustumD.
        /// </summary>
        public override string ToString()
        {
            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, "{{Near:{0} Far:{1} Left:{2} Right:{3} Top:{4} Bottom:{5}}}", (object)this.Near.ToString(), (object)this.Far.ToString(), (object)this.Left.ToString(), (object)this.Right.ToString(), (object)this.Top.ToString(), (object)this.Bottom.ToString());
        }

        private void SetMatrix(ref MatrixD value)
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
                double num = this.planes[index].Normal.Length();
                this.planes[index].Normal = this.planes[index].Normal / num;
                this.planes[index].D = this.planes[index].D / num;
            }
            RayD intersectionLine1 = BoundingFrustumD.ComputeIntersectionLine(ref this.planes[0], ref this.planes[2]);
            this.cornerArray[0] = BoundingFrustumD.ComputeIntersection(ref this.planes[4], ref intersectionLine1);
            this.cornerArray[3] = BoundingFrustumD.ComputeIntersection(ref this.planes[5], ref intersectionLine1);
            RayD intersectionLine2 = BoundingFrustumD.ComputeIntersectionLine(ref this.planes[3], ref this.planes[0]);
            this.cornerArray[1] = BoundingFrustumD.ComputeIntersection(ref this.planes[4], ref intersectionLine2);
            this.cornerArray[2] = BoundingFrustumD.ComputeIntersection(ref this.planes[5], ref intersectionLine2);
            intersectionLine2 = BoundingFrustumD.ComputeIntersectionLine(ref this.planes[2], ref this.planes[1]);
            this.cornerArray[4] = BoundingFrustumD.ComputeIntersection(ref this.planes[4], ref intersectionLine2);
            this.cornerArray[7] = BoundingFrustumD.ComputeIntersection(ref this.planes[5], ref intersectionLine2);
            intersectionLine2 = BoundingFrustumD.ComputeIntersectionLine(ref this.planes[1], ref this.planes[3]);
            this.cornerArray[5] = BoundingFrustumD.ComputeIntersection(ref this.planes[4], ref intersectionLine2);
            this.cornerArray[6] = BoundingFrustumD.ComputeIntersection(ref this.planes[5], ref intersectionLine2);
        }

        private static RayD ComputeIntersectionLine(ref PlaneD p1, ref PlaneD p2)
        {
            RayD ray = new RayD();
            ray.Direction = Vector3D.Cross(p1.Normal, p2.Normal);
            double num = ray.Direction.LengthSquared();
            ray.Position = Vector3D.Cross(-p1.D * p2.Normal + p2.D * p1.Normal, ray.Direction) / num;
            return ray;
        }

        private static Vector3D ComputeIntersection(ref PlaneD plane, ref RayD ray)
        {
            double num = (-plane.D - Vector3D.Dot(plane.Normal, ray.Position)) / Vector3D.Dot(plane.Normal, ray.Direction);
            return ray.Position + ray.Direction * num;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD intersects the specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to check for intersection.</param>
        public bool Intersects(BoundingBoxD box)
        {
            bool result;
            this.Intersects(ref box, out result);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD intersects a BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingFrustumD and BoundingBoxD intersect; false otherwise.</param>
        public void Intersects(ref BoundingBoxD box, out bool result)
        {
            if (this.gjk == null)
                this.gjk = new GjkD();
            this.gjk.Reset();
            Vector3D result1;
            Vector3D.Subtract(ref this.cornerArray[0], ref box.Min, out result1);
            if ((double)result1.LengthSquared() < 9.99999974737875E-06)
                Vector3D.Subtract(ref this.cornerArray[0], ref box.Max, out result1);
            double num1 = double.MaxValue;
            result = false;
            double num2;
            do
            {
                Vector3D v;
                v.X = -result1.X;
                v.Y = -result1.Y;
                v.Z = -result1.Z;
                Vector3D result2;
                this.SupportMapping(ref v, out result2);
                Vector3D result3;
                box.SupportMapping(ref result1, out result3);
                Vector3D result4;
                Vector3D.Subtract(ref result2, ref result3, out result4);
                if ((double)result1.X * (double)result4.X + (double)result1.Y * (double)result4.Y + (double)result1.Z * (double)result4.Z > 0.0)
                    return;
                this.gjk.AddSupportPoint(ref result4);
                result1 = this.gjk.ClosestPoint;
                double num3 = num1;
                num1 = result1.LengthSquared();
                if ((double)num3 - (double)num1 <= 9.99999974737875E-06 * (double)num3)
                    return;
                num2 = 4E-05f * this.gjk.MaxLengthSquared;
            }
            while (!this.gjk.FullSimplex && (double)num1 >= (double)num2);
            result = true;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD intersects the specified BoundingFrustumD.
        /// </summary>
        /// <param name="frustum">The BoundingFrustumD to check for intersection.</param>
        public bool Intersects(BoundingFrustumD frustum)
        {
            if (frustum == (BoundingFrustumD)null)
                throw new ArgumentNullException("frustum");
            if (this.gjk == null)
                this.gjk = new GjkD();
            this.gjk.Reset();
            Vector3D result1;
            Vector3D.Subtract(ref this.cornerArray[0], ref frustum.cornerArray[0], out result1);
            if ((double)result1.LengthSquared() < 9.99999974737875E-06)
                Vector3D.Subtract(ref this.cornerArray[0], ref frustum.cornerArray[1], out result1);
            double num1 = double.MaxValue;
            double num2;
            do
            {
                Vector3D v;
                v.X = -result1.X;
                v.Y = -result1.Y;
                v.Z = -result1.Z;
                Vector3D result2;
                this.SupportMapping(ref v, out result2);
                Vector3D result3;
                frustum.SupportMapping(ref result1, out result3);
                Vector3D result4;
                Vector3D.Subtract(ref result2, ref result3, out result4);
                if ((double)result1.X * (double)result4.X + (double)result1.Y * (double)result4.Y + (double)result1.Z * (double)result4.Z > 0.0)
                    return false;
                this.gjk.AddSupportPoint(ref result4);
                result1 = this.gjk.ClosestPoint;
                double num3 = num1;
                num1 = result1.LengthSquared();
                num2 = 4E-05f * this.gjk.MaxLengthSquared;
                if ((double)num3 - (double)num1 <= 9.99999974737875E-06 * (double)num3)
                    return false;
            }
            while (!this.gjk.FullSimplex && (double)num1 >= (double)num2);
            return true;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD intersects the specified Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection.</param>
        public PlaneIntersectionType Intersects(PlaneD plane)
        {
            int num = 0;
            for (int index = 0; index < 8; ++index)
            {
                double result;
                Vector3D.Dot(ref this.cornerArray[index], ref plane.Normal, out result);
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
        /// Checks whether the current BoundingFrustumD intersects a Plane.
        /// </summary>
        /// <param name="plane">The Plane to check for intersection with.</param><param name="result">[OutAttribute] An enumeration indicating whether the BoundingFrustumD intersects the Plane.</param>
        public void Intersects(ref PlaneD plane, out PlaneIntersectionType result)
        {
            int num = 0;
            for (int index = 0; index < 8; ++index)
            {
                double result1;
                Vector3D.Dot(ref this.cornerArray[index], ref plane.Normal, out result1);
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
        /// Checks whether the current BoundingFrustumD intersects the specified Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection.</param>
        public double? Intersects(RayD ray)
        {
            double? result;
            this.Intersects(ref ray, out result);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD intersects a Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param><param name="result">[OutAttribute] Distance at which the ray intersects the BoundingFrustumD or null if there is no intersection.</param>
        public void Intersects(ref RayD ray, out double? result)
        {
            ContainmentType result1;
            this.Contains(ref ray.Position, out result1);
            if (result1 == ContainmentType.Contains)
            {
                result = new double?(0.0f);
            }
            else
            {
                double num1 = double.MinValue;
                double num2 = double.MaxValue;
                result = new double?();
                foreach (PlaneD plane in this.planes)
                {
                    Vector3D vector2 = plane.Normal;
                    double result2;
                    Vector3D.Dot(ref ray.Direction, ref vector2, out result2);
                    double result3;
                    Vector3D.Dot(ref ray.Position, ref vector2, out result3);
                    result3 += plane.D;
                    if ((double)Math.Abs(result2) < 9.99999974737875E-06)
                    {
                        if ((double)result3 > 0.0)
                            return;
                    }
                    else
                    {
                        double num3 = -result3 / result2;
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
                double num4 = (double)num1 >= 0.0 ? num1 : num2;
                if ((double)num4 < 0.0)
                    return;
                result = new double?(num4);
            }
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD intersects the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection.</param>
        public bool Intersects(BoundingSphereD sphere)
        {
            bool result;
            this.Intersects(ref sphere, out result);
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param><param name="result">[OutAttribute] true if the BoundingFrustumD and BoundingSphere intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphereD sphere, out bool result)
        {
            if (this.gjk == null)
                this.gjk = new GjkD();
            this.gjk.Reset();
            Vector3D result1;
            Vector3D.Subtract(ref this.cornerArray[0], ref sphere.Center, out result1);
            if ((double)result1.LengthSquared() < 9.99999974737875E-06)
                result1 = Vector3D.UnitX;
            double num1 = double.MaxValue;
            result = false;
            double num2;
            do
            {
                Vector3D v;
                v.X = -result1.X;
                v.Y = -result1.Y;
                v.Z = -result1.Z;
                Vector3D result2;
                this.SupportMapping(ref v, out result2);
                Vector3D result3;
                sphere.SupportMapping(ref result1, out result3);
                Vector3D result4;
                Vector3D.Subtract(ref result2, ref result3, out result4);
                if ((double)result1.X * (double)result4.X + (double)result1.Y * (double)result4.Y + (double)result1.Z * (double)result4.Z > 0.0)
                    return;
                this.gjk.AddSupportPoint(ref result4);
                result1 = this.gjk.ClosestPoint;
                double num3 = num1;
                num1 = result1.LengthSquared();
                if ((double)num3 - (double)num1 <= 9.99999974737875E-06 * (double)num3)
                    return;
                num2 = 4E-05f * this.gjk.MaxLengthSquared;
            }
            while (!this.gjk.FullSimplex && (double)num1 >= (double)num2);
            result = true;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD contains the specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to check against the current BoundingFrustumD.</param>
        public ContainmentType Contains(BoundingBoxD box)
        {
            bool flag = false;
            foreach (PlaneD plane in this.planes)
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
        /// Checks whether the current BoundingFrustumD contains the specified BoundingBoxD.
        /// </summary>
        /// <param name="box">The BoundingBoxD to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBoxD box, out ContainmentType result)
        {
            bool flag = false;
            foreach (PlaneD plane in this.planes)
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
        /// Checks whether the current BoundingFrustumD contains the specified BoundingFrustumD.
        /// </summary>
        /// <param name="frustum">The BoundingFrustumD to check against the current BoundingFrustumD.</param>
        public ContainmentType Contains(BoundingFrustumD frustum)
        {
            if (frustum == (BoundingFrustumD)null)
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
        /// Checks whether the current BoundingFrustumD contains the specified point.
        /// </summary>
        /// <param name="point">The point to check against the current BoundingFrustumD.</param>
        public ContainmentType Contains(Vector3D point)
        {
            foreach (PlaneD plane in this.planes)
            {
                if ((double)((double)((double)plane.Normal.X * (double)point.X + (double)plane.Normal.Y * (double)point.Y + (double)plane.Normal.Z * (double)point.Z) + plane.D) > 9.99999974737875E-06)
                    return ContainmentType.Disjoint;
            }
            return ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD contains the specified point.
        /// </summary>
        /// <param name="point">The point to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3D point, out ContainmentType result)
        {
            foreach (PlaneD plane in this.planes)
            {
                if ((double)((double)((double)plane.Normal.X * (double)point.X + (double)plane.Normal.Y * (double)point.Y + (double)plane.Normal.Z * (double)point.Z) + plane.D) > 9.99999974737875E-06)
                {
                    result = ContainmentType.Disjoint;
                    return;
                }
            }
            result = ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD contains the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to check against the current BoundingFrustumD.</param>
        public ContainmentType Contains(BoundingSphereD sphere)
        {
            Vector3D Vector3D = sphere.Center;
            double num1 = sphere.Radius;
            int num2 = 0;
            foreach (PlaneD plane in this.planes)
            {
                double num3 = (double)((double)plane.Normal.X * (double)Vector3D.X + (double)plane.Normal.Y * (double)Vector3D.Y + (double)plane.Normal.Z * (double)Vector3D.Z) + plane.D;
                if ((double)num3 > (double)num1)
                    return ContainmentType.Disjoint;
                if ((double)num3 < -(double)num1)
                    ++num2;
            }
            return num2 != 6 ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustumD contains the specified BoundingSphere.
        /// </summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param><param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphereD sphere, out ContainmentType result)
        {
            Vector3D Vector3D = sphere.Center;
            double num1 = sphere.Radius;
            int num2 = 0;
            foreach (PlaneD plane in this.planes)
            {
                double num3 = (double)((double)plane.Normal.X * (double)Vector3D.X + (double)plane.Normal.Y * (double)Vector3D.Y + (double)plane.Normal.Z * (double)Vector3D.Z) + plane.D;
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

        internal void SupportMapping(ref Vector3D v, out Vector3D result)
        {
            int index1 = 0;
            double result1;
            Vector3D.Dot(ref this.cornerArray[0], ref v, out result1);
            for (int index2 = 1; index2 < this.cornerArray.Length; ++index2)
            {
                double result2;
                Vector3D.Dot(ref this.cornerArray[index2], ref v, out result2);
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
