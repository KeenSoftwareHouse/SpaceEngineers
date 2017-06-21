// Disabled, some people have issues with this, but latest redist installed and SharpDX works
//#define NATIVE_SUPPORT

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1


namespace VRageMath
{
    /// <summary>
    /// Defines a matrix.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]    
    [StructLayout(LayoutKind.Explicit)]
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
    public struct Matrix : IEquatable<Matrix>
#else // XB1
    public struct Matrix : IEquatable<Matrix>, IMySetGetMemberDataHelper
#endif // XB1

    {
        private unsafe struct F16
        {
            // 16 float numbers, indexed as data[row,column]
            public fixed float data[16];
        }

        public static Matrix Identity = new Matrix(1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f);
        public static Matrix Zero = new Matrix(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);


#if !XB1

        /// <summary>
        /// Matrix values
        /// </summary>
        [FieldOffset(0)]
        private F16 M;
#endif
        /// <summary>
        /// Value at row 1 column 1 of the matrix.
        /// </summary>
        [FieldOffset(0)]
        [ProtoBuf.ProtoMember]
        public float M11;
        /// <summary>
        /// Value at row 1 column 2 of the matrix.
        /// </summary>
        [FieldOffset(4)]
        [ProtoBuf.ProtoMember]
        public float M12;
        /// <summary>
        /// Value at row 1 column 3 of the matrix.
        /// </summary>
        [FieldOffset(8)]
        [ProtoBuf.ProtoMember]
        public float M13;
        /// <summary>
        /// Value at row 1 column 4 of the matrix.
        /// </summary>
        [FieldOffset(12)]
        [ProtoBuf.ProtoMember]
        public float M14;
        /// <summary>
        /// Value at row 2 column 1 of the matrix.
        /// </summary>
        [FieldOffset(16)]
        [ProtoBuf.ProtoMember]
        public float M21;
        /// <summary>
        /// Value at row 2 column 2 of the matrix.
        /// </summary>
        [FieldOffset(20)]
        [ProtoBuf.ProtoMember]
        public float M22;
        /// <summary>
        /// Value at row 2 column 3 of the matrix.
        /// </summary>
        [FieldOffset(24)]
        [ProtoBuf.ProtoMember]
        public float M23;
        /// <summary>
        /// Value at row 2 column 4 of the matrix.
        /// </summary>
        [FieldOffset(28)]
        [ProtoBuf.ProtoMember]
        public float M24;
        /// <summary>
        /// Value at row 3 column 1 of the matrix.
        /// </summary>
        [FieldOffset(32)]
        [ProtoBuf.ProtoMember]
        public float M31;
        /// <summary>
        /// Value at row 3 column 2 of the matrix.
        /// </summary>
        [FieldOffset(36)]
        [ProtoBuf.ProtoMember]
        public float M32;
        /// <summary>
        /// Value at row 3 column 3 of the matrix.
        /// </summary>
        [FieldOffset(40)]
        [ProtoBuf.ProtoMember]
        public float M33;
        /// <summary>
        /// Value at row 3 column 4 of the matrix.
        /// </summary>
        [FieldOffset(44)]
        [ProtoBuf.ProtoMember]
        public float M34;
        /// <summary>
        /// Value at row 4 column 1 of the matrix.
        /// </summary>
        [FieldOffset(48)]
        [ProtoBuf.ProtoMember]
        public float M41;
        /// <summary>
        /// Value at row 4 column 2 of the matrix.
        /// </summary>
        [FieldOffset(52)]
        [ProtoBuf.ProtoMember]
        public float M42;
        /// <summary>
        /// Value at row 4 column 3 of the matrix.
        /// </summary>
        [FieldOffset(56)]
        [ProtoBuf.ProtoMember]
        public float M43;
        /// <summary>
        /// Value at row 4 column 4 of the matrix.
        /// </summary>
        [FieldOffset(60)]
        [ProtoBuf.ProtoMember]
        public float M44;

        /// <summary>
        /// Gets and sets the up vector of the Matrix.
        /// </summary>
        public Vector3 Up
        {
            get
            {
                Vector3 vector3;
                vector3.X = this.M21;
                vector3.Y = this.M22;
                vector3.Z = this.M23;
                return vector3;
            }
            set
            {
                this.M21 = value.X;
                this.M22 = value.Y;
                this.M23 = value.Z;
            }
        }

        /// <summary>
        /// Gets and sets the down vector of the Matrix.
        /// </summary>
        public Vector3 Down
        {
            get
            {
                Vector3 vector3;
                vector3.X = -this.M21;
                vector3.Y = -this.M22;
                vector3.Z = -this.M23;
                return vector3;
            }
            set
            {
                this.M21 = -value.X;
                this.M22 = -value.Y;
                this.M23 = -value.Z;
            }
        }

        /// <summary>
        /// Gets and sets the right vector of the Matrix.
        /// </summary>
        public Vector3 Right
        {
            get
            {
                Vector3 vector3;
                vector3.X = this.M11;
                vector3.Y = this.M12;
                vector3.Z = this.M13;
                return vector3;
            }
            set
            {
                this.M11 = value.X;
                this.M12 = value.Y;
                this.M13 = value.Z;
            }
        }

        public Vector3 Col0
        {
            get
            {
                Vector3 vector3;
                vector3.X = this.M11;
                vector3.Y = this.M21;
                vector3.Z = this.M31;
                return vector3;
            }
        }

        public Vector3 Col1
        {
            get
            {
                Vector3 vector3;
                vector3.X = this.M12;
                vector3.Y = this.M22;
                vector3.Z = this.M32;
                return vector3;
            }
        }

        public Vector3 Col2
        {
            get
            {
                Vector3 vector3;
                vector3.X = this.M13;
                vector3.Y = this.M23;
                vector3.Z = this.M33;
                return vector3;
            }
        }
        /// <summary>
        /// Gets and sets the left vector of the Matrix.
        /// </summary>
        public Vector3 Left
        {
            get
            {
                Vector3 vector3;
                vector3.X = -this.M11;
                vector3.Y = -this.M12;
                vector3.Z = -this.M13;
                return vector3;
            }
            set
            {
                this.M11 = -value.X;
                this.M12 = -value.Y;
                this.M13 = -value.Z;
            }
        }

        /// <summary>
        /// Gets and sets the forward vector of the Matrix.
        /// </summary>
        public Vector3 Forward
        {
            get
            {
                Vector3 vector3;
                vector3.X = -this.M31;
                vector3.Y = -this.M32;
                vector3.Z = -this.M33;
                return vector3;
            }
            set
            {
                this.M31 = -value.X;
                this.M32 = -value.Y;
                this.M33 = -value.Z;
            }
        }

        /// <summary>
        /// Gets and sets the backward vector of the Matrix.
        /// </summary>
        public Vector3 Backward
        {
            get
            {
                Vector3 vector3;
                vector3.X = this.M31;
                vector3.Y = this.M32;
                vector3.Z = this.M33;
                return vector3;
            }
            set
            {
                this.M31 = value.X;
                this.M32 = value.Y;
                this.M33 = value.Z;
            }
        }

        /// <summary>
        /// Gets the base vector of the matrix, corresponding to the given direction
        /// </summary>
        public Vector3 GetDirectionVector(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return Forward;
                case Base6Directions.Direction.Backward:
                    return Backward;
                case Base6Directions.Direction.Left:
                    return Left;
                case Base6Directions.Direction.Right:
                    return Right;
                case Base6Directions.Direction.Up:
                    return Up;
                case Base6Directions.Direction.Down:
                    return Down;
                default:
                    Debug.Fail("Unknown direction");
                    return Vector3.Zero;
            }
        }

        /// <summary>
        /// Sets the base vector of the matrix, corresponding to the given direction
        /// </summary>
        public void SetDirectionVector(Base6Directions.Direction direction, Vector3 newValue)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    Forward = newValue;
                    return;
                case Base6Directions.Direction.Backward:
                    Backward = newValue;
                    return;
                case Base6Directions.Direction.Left:
                    Left = newValue;
                    return;
                case Base6Directions.Direction.Right:
                    Right = newValue;
                    return;
                case Base6Directions.Direction.Up:
                    Up = newValue;
                    return;
                case Base6Directions.Direction.Down:
                    Down = newValue;
                    return;
                default:
                    Debug.Fail("Unknown direction");
                    return;
            }
        }

        public Base6Directions.Direction GetClosestDirection(Vector3 referenceVector)
        {
            return GetClosestDirection(ref referenceVector);
        }

        public Base6Directions.Direction GetClosestDirection(ref Vector3 referenceVector)
        {
            float dotX, dotY, dotZ;
            float aDotX, aDotY, aDotZ;
            dotX = Vector3.Dot(referenceVector, Right);
            dotY = Vector3.Dot(referenceVector, Up);
            dotZ = Vector3.Dot(referenceVector, Backward);
            aDotX = Math.Abs(dotX);
            aDotY = Math.Abs(dotY);
            aDotZ = Math.Abs(dotZ);
            if (aDotX > aDotY)
            {
                if (aDotX > aDotZ)
                {
                    if (dotX > 0)
                        return Base6Directions.Direction.Right;
                    else
                        return Base6Directions.Direction.Left;
                }
                else
                {
                    if (dotZ > 0)
                        return Base6Directions.Direction.Backward;
                    else
                        return Base6Directions.Direction.Forward;
                }
            }
            else
            {
                if (aDotY > aDotZ)
                {
                    if (dotY > 0)
                        return Base6Directions.Direction.Up;
                    else
                        return Base6Directions.Direction.Down;
                }
                else
                {
                    if (dotZ > 0)
                        return Base6Directions.Direction.Backward;
                    else
                        return Base6Directions.Direction.Forward;
                }
            }
        }


        public Vector3 Scale
        {
            get
            {
                return new Vector3(Right.Length(), Up.Length(), Forward.Length());
                //return new Vector3(Col0.Length(), Col1.Length(), Col2.Length());
            }
        }

        /// <summary>
        /// Same result as Matrix.CreateScale(scale) * matrix, but much faster
        /// </summary>
		[Unsharper.UnsharperDisableReflection()]
        public static void Rescale(ref Matrix matrix, float scale)
        {
            matrix.M11 *= scale;
            matrix.M12 *= scale;
            matrix.M13 *= scale;

            matrix.M21 *= scale;
            matrix.M22 *= scale;
            matrix.M23 *= scale;

            matrix.M31 *= scale;
            matrix.M32 *= scale;
            matrix.M33 *= scale;
        }

        /// <summary>
        /// Same result as Matrix.CreateScale(scale) * matrix, but much faster
        /// </summary>
		[Unsharper.UnsharperDisableReflection()]
        public static void Rescale(ref Matrix matrix, ref Vector3 scale)
        {
            matrix.M11 *= scale.X;
            matrix.M12 *= scale.X;
            matrix.M13 *= scale.X;

            matrix.M21 *= scale.Y;
            matrix.M22 *= scale.Y;
            matrix.M23 *= scale.Y;

            matrix.M31 *= scale.Z;
            matrix.M32 *= scale.Z;
            matrix.M33 *= scale.Z;
        }

		[Unsharper.UnsharperDisableReflection()]
        public static Matrix Rescale(Matrix matrix, float scale)
        {
            Rescale(ref matrix, scale);
            return matrix;
        }

		[Unsharper.UnsharperDisableReflection()]
        public static Matrix Rescale(Matrix matrix, Vector3 scale)
        {
            Rescale(ref matrix, ref scale);
            return matrix;
        }

        /// <summary>
        /// Gets and sets the translation vector of the Matrix.
        /// </summary>
        public Vector3 Translation
        {
            get
            {
                Vector3 vector3;
                vector3.X = this.M41;
                vector3.Y = this.M42;
                vector3.Z = this.M43;
                return vector3;
            }
            set
            {
                this.M41 = value.X;
                this.M42 = value.Y;
                this.M43 = value.Z;
            }
        }

        static Matrix()
        {
        }

        /// <summary>
        /// Initializes a new instance of Matrix.
        /// </summary>
        /// <param name="m11">Value to initialize m11 to.</param><param name="m12">Value to initialize m12 to.</param><param name="m13">Value to initialize m13 to.</param><param name="m14">Value to initialize m14 to.</param><param name="m21">Value to initialize m21 to.</param><param name="m22">Value to initialize m22 to.</param><param name="m23">Value to initialize m23 to.</param><param name="m24">Value to initialize m24 to.</param><param name="m31">Value to initialize m31 to.</param><param name="m32">Value to initialize m32 to.</param><param name="m33">Value to initialize m33 to.</param><param name="m34">Value to initialize m34 to.</param><param name="m41">Value to initialize m41 to.</param><param name="m42">Value to initialize m42 to.</param><param name="m43">Value to initialize m43 to.</param><param name="m44">Value to initialize m44 to.</param>
        public Matrix(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = m14;
            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = m24;
            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = m34;
            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;
            this.M44 = m44;
        }


        /// <summary>
        /// Initializes a new instance of Matrix with rotation data
        /// </summary>
        /// <param name="m11">Value to initialize m11 to.</param><param name="m12">Value to initialize m12 to.</param><param name="m13">Value to initialize m13 to.</param><param name="m14">Value to initialize m14 to.</param><param name="m21">Value to initialize m21 to.</param><param name="m22">Value to initialize m22 to.</param><param name="m23">Value to initialize m23 to.</param><param name="m24">Value to initialize m24 to.</param><param name="m31">Value to initialize m31 to.</param><param name="m32">Value to initialize m32 to.</param><param name="m33">Value to initialize m33 to.</param><param name="m34">Value to initialize m34 to.</param><param name="m41">Value to initialize m41 to.</param><param name="m42">Value to initialize m42 to.</param><param name="m43">Value to initialize m43 to.</param><param name="m44">Value to initialize m44 to.</param>
        public Matrix(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = 0;
            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = 0;
            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = 0;
            this.M41 = 0;
            this.M42 = 0;
            this.M43 = 0;
            this.M44 = 1;
        }

		public Matrix(MatrixD other)
		{
            this.M11 = (float)other.M11;
            this.M12 = (float)other.M12;
            this.M13 = (float)other.M13;
            this.M14 = (float)other.M14;
            this.M21 = (float)other.M21;
            this.M22 = (float)other.M22;
            this.M23 = (float)other.M23;
            this.M24 = (float)other.M24;
            this.M31 = (float)other.M31;
            this.M32 = (float)other.M32;
            this.M33 = (float)other.M33;
            this.M34 = (float)other.M34;
            this.M41 = (float)other.M41;
            this.M42 = (float)other.M42;
            this.M43 = (float)other.M43;
            this.M44 = (float)other.M44;
		}

        /// <summary>
        /// Negates individual elements of a matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param>
        public static Matrix operator -(Matrix matrix1)
        {
            Matrix matrix;
            matrix.M11 = -matrix1.M11;
            matrix.M12 = -matrix1.M12;
            matrix.M13 = -matrix1.M13;
            matrix.M14 = -matrix1.M14;
            matrix.M21 = -matrix1.M21;
            matrix.M22 = -matrix1.M22;
            matrix.M23 = -matrix1.M23;
            matrix.M24 = -matrix1.M24;
            matrix.M31 = -matrix1.M31;
            matrix.M32 = -matrix1.M32;
            matrix.M33 = -matrix1.M33;
            matrix.M34 = -matrix1.M34;
            matrix.M41 = -matrix1.M41;
            matrix.M42 = -matrix1.M42;
            matrix.M43 = -matrix1.M43;
            matrix.M44 = -matrix1.M44;
            return matrix;
        }

        /// <summary>
        /// Compares a matrix for equality with another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param>
        public static bool operator ==(Matrix matrix1, Matrix matrix2)
        {
            if ((double)matrix1.M11 == (double)matrix2.M11 && (double)matrix1.M22 == (double)matrix2.M22 && ((double)matrix1.M33 == (double)matrix2.M33 && (double)matrix1.M44 == (double)matrix2.M44) && ((double)matrix1.M12 == (double)matrix2.M12 && (double)matrix1.M13 == (double)matrix2.M13 && ((double)matrix1.M14 == (double)matrix2.M14 && (double)matrix1.M21 == (double)matrix2.M21)) && ((double)matrix1.M23 == (double)matrix2.M23 && (double)matrix1.M24 == (double)matrix2.M24 && ((double)matrix1.M31 == (double)matrix2.M31 && (double)matrix1.M32 == (double)matrix2.M32) && ((double)matrix1.M34 == (double)matrix2.M34 && (double)matrix1.M41 == (double)matrix2.M41 && (double)matrix1.M42 == (double)matrix2.M42)))
                return (double)matrix1.M43 == (double)matrix2.M43;
            else
                return false;
        }

        /// <summary>
        /// Tests a matrix for inequality with another matrix.
        /// </summary>
        /// <param name="matrix1">The matrix on the left of the equal sign.</param><param name="matrix2">The matrix on the right of the equal sign.</param>
        public static bool operator !=(Matrix matrix1, Matrix matrix2)
        {
            if ((double)matrix1.M11 == (double)matrix2.M11 && (double)matrix1.M12 == (double)matrix2.M12 && ((double)matrix1.M13 == (double)matrix2.M13 && (double)matrix1.M14 == (double)matrix2.M14) && ((double)matrix1.M21 == (double)matrix2.M21 && (double)matrix1.M22 == (double)matrix2.M22 && ((double)matrix1.M23 == (double)matrix2.M23 && (double)matrix1.M24 == (double)matrix2.M24)) && ((double)matrix1.M31 == (double)matrix2.M31 && (double)matrix1.M32 == (double)matrix2.M32 && ((double)matrix1.M33 == (double)matrix2.M33 && (double)matrix1.M34 == (double)matrix2.M34) && ((double)matrix1.M41 == (double)matrix2.M41 && (double)matrix1.M42 == (double)matrix2.M42 && (double)matrix1.M43 == (double)matrix2.M43)))
                return (double)matrix1.M44 != (double)matrix2.M44;
            else
                return true;
        }

        /// <summary>
        /// Adds a matrix to another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param>
        public static Matrix operator +(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 + matrix2.M11;
            matrix.M12 = matrix1.M12 + matrix2.M12;
            matrix.M13 = matrix1.M13 + matrix2.M13;
            matrix.M14 = matrix1.M14 + matrix2.M14;
            matrix.M21 = matrix1.M21 + matrix2.M21;
            matrix.M22 = matrix1.M22 + matrix2.M22;
            matrix.M23 = matrix1.M23 + matrix2.M23;
            matrix.M24 = matrix1.M24 + matrix2.M24;
            matrix.M31 = matrix1.M31 + matrix2.M31;
            matrix.M32 = matrix1.M32 + matrix2.M32;
            matrix.M33 = matrix1.M33 + matrix2.M33;
            matrix.M34 = matrix1.M34 + matrix2.M34;
            matrix.M41 = matrix1.M41 + matrix2.M41;
            matrix.M42 = matrix1.M42 + matrix2.M42;
            matrix.M43 = matrix1.M43 + matrix2.M43;
            matrix.M44 = matrix1.M44 + matrix2.M44;
            return matrix;
        }

        /// <summary>
        /// Subtracts matrices.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param>
        public static Matrix operator -(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 - matrix2.M11;
            matrix.M12 = matrix1.M12 - matrix2.M12;
            matrix.M13 = matrix1.M13 - matrix2.M13;
            matrix.M14 = matrix1.M14 - matrix2.M14;
            matrix.M21 = matrix1.M21 - matrix2.M21;
            matrix.M22 = matrix1.M22 - matrix2.M22;
            matrix.M23 = matrix1.M23 - matrix2.M23;
            matrix.M24 = matrix1.M24 - matrix2.M24;
            matrix.M31 = matrix1.M31 - matrix2.M31;
            matrix.M32 = matrix1.M32 - matrix2.M32;
            matrix.M33 = matrix1.M33 - matrix2.M33;
            matrix.M34 = matrix1.M34 - matrix2.M34;
            matrix.M41 = matrix1.M41 - matrix2.M41;
            matrix.M42 = matrix1.M42 - matrix2.M42;
            matrix.M43 = matrix1.M43 - matrix2.M43;
            matrix.M44 = matrix1.M44 - matrix2.M44;
            return matrix;
        }

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param>
        public static Matrix operator *(Matrix matrix1, Matrix matrix2)
        {
#if NATIVE_SUPPORT
            Matrix result;
            Multiply_Native(ref matrix1, ref matrix2, out result);
            return result;
#else
            Matrix matrix;
            matrix.M11 = (float)((double)matrix1.M11 * (double)matrix2.M11 + (double)matrix1.M12 * (double)matrix2.M21 + (double)matrix1.M13 * (double)matrix2.M31 + (double)matrix1.M14 * (double)matrix2.M41);
            matrix.M12 = (float)((double)matrix1.M11 * (double)matrix2.M12 + (double)matrix1.M12 * (double)matrix2.M22 + (double)matrix1.M13 * (double)matrix2.M32 + (double)matrix1.M14 * (double)matrix2.M42);
            matrix.M13 = (float)((double)matrix1.M11 * (double)matrix2.M13 + (double)matrix1.M12 * (double)matrix2.M23 + (double)matrix1.M13 * (double)matrix2.M33 + (double)matrix1.M14 * (double)matrix2.M43);
            matrix.M14 = (float)((double)matrix1.M11 * (double)matrix2.M14 + (double)matrix1.M12 * (double)matrix2.M24 + (double)matrix1.M13 * (double)matrix2.M34 + (double)matrix1.M14 * (double)matrix2.M44);
            matrix.M21 = (float)((double)matrix1.M21 * (double)matrix2.M11 + (double)matrix1.M22 * (double)matrix2.M21 + (double)matrix1.M23 * (double)matrix2.M31 + (double)matrix1.M24 * (double)matrix2.M41);
            matrix.M22 = (float)((double)matrix1.M21 * (double)matrix2.M12 + (double)matrix1.M22 * (double)matrix2.M22 + (double)matrix1.M23 * (double)matrix2.M32 + (double)matrix1.M24 * (double)matrix2.M42);
            matrix.M23 = (float)((double)matrix1.M21 * (double)matrix2.M13 + (double)matrix1.M22 * (double)matrix2.M23 + (double)matrix1.M23 * (double)matrix2.M33 + (double)matrix1.M24 * (double)matrix2.M43);
            matrix.M24 = (float)((double)matrix1.M21 * (double)matrix2.M14 + (double)matrix1.M22 * (double)matrix2.M24 + (double)matrix1.M23 * (double)matrix2.M34 + (double)matrix1.M24 * (double)matrix2.M44);
            matrix.M31 = (float)((double)matrix1.M31 * (double)matrix2.M11 + (double)matrix1.M32 * (double)matrix2.M21 + (double)matrix1.M33 * (double)matrix2.M31 + (double)matrix1.M34 * (double)matrix2.M41);
            matrix.M32 = (float)((double)matrix1.M31 * (double)matrix2.M12 + (double)matrix1.M32 * (double)matrix2.M22 + (double)matrix1.M33 * (double)matrix2.M32 + (double)matrix1.M34 * (double)matrix2.M42);
            matrix.M33 = (float)((double)matrix1.M31 * (double)matrix2.M13 + (double)matrix1.M32 * (double)matrix2.M23 + (double)matrix1.M33 * (double)matrix2.M33 + (double)matrix1.M34 * (double)matrix2.M43);
            matrix.M34 = (float)((double)matrix1.M31 * (double)matrix2.M14 + (double)matrix1.M32 * (double)matrix2.M24 + (double)matrix1.M33 * (double)matrix2.M34 + (double)matrix1.M34 * (double)matrix2.M44);
            matrix.M41 = (float)((double)matrix1.M41 * (double)matrix2.M11 + (double)matrix1.M42 * (double)matrix2.M21 + (double)matrix1.M43 * (double)matrix2.M31 + (double)matrix1.M44 * (double)matrix2.M41);
            matrix.M42 = (float)((double)matrix1.M41 * (double)matrix2.M12 + (double)matrix1.M42 * (double)matrix2.M22 + (double)matrix1.M43 * (double)matrix2.M32 + (double)matrix1.M44 * (double)matrix2.M42);
            matrix.M43 = (float)((double)matrix1.M41 * (double)matrix2.M13 + (double)matrix1.M42 * (double)matrix2.M23 + (double)matrix1.M43 * (double)matrix2.M33 + (double)matrix1.M44 * (double)matrix2.M43);
            matrix.M44 = (float)((double)matrix1.M41 * (double)matrix2.M14 + (double)matrix1.M42 * (double)matrix2.M24 + (double)matrix1.M43 * (double)matrix2.M34 + (double)matrix1.M44 * (double)matrix2.M44);
            return matrix;
#endif
        }

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="matrix">Source matrix.</param><param name="scaleFactor">Scalar value.</param>
        public static Matrix operator *(Matrix matrix, float scaleFactor)
        {
            float num = scaleFactor;
            Matrix matrix1;
            matrix1.M11 = matrix.M11 * num;
            matrix1.M12 = matrix.M12 * num;
            matrix1.M13 = matrix.M13 * num;
            matrix1.M14 = matrix.M14 * num;
            matrix1.M21 = matrix.M21 * num;
            matrix1.M22 = matrix.M22 * num;
            matrix1.M23 = matrix.M23 * num;
            matrix1.M24 = matrix.M24 * num;
            matrix1.M31 = matrix.M31 * num;
            matrix1.M32 = matrix.M32 * num;
            matrix1.M33 = matrix.M33 * num;
            matrix1.M34 = matrix.M34 * num;
            matrix1.M41 = matrix.M41 * num;
            matrix1.M42 = matrix.M42 * num;
            matrix1.M43 = matrix.M43 * num;
            matrix1.M44 = matrix.M44 * num;
            return matrix1;
        }

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="scaleFactor">Scalar value.</param><param name="matrix">Source matrix.</param>
        public static Matrix operator *(float scaleFactor, Matrix matrix)
        {
            float num = scaleFactor;
            Matrix matrix1;
            matrix1.M11 = matrix.M11 * num;
            matrix1.M12 = matrix.M12 * num;
            matrix1.M13 = matrix.M13 * num;
            matrix1.M14 = matrix.M14 * num;
            matrix1.M21 = matrix.M21 * num;
            matrix1.M22 = matrix.M22 * num;
            matrix1.M23 = matrix.M23 * num;
            matrix1.M24 = matrix.M24 * num;
            matrix1.M31 = matrix.M31 * num;
            matrix1.M32 = matrix.M32 * num;
            matrix1.M33 = matrix.M33 * num;
            matrix1.M34 = matrix.M34 * num;
            matrix1.M41 = matrix.M41 * num;
            matrix1.M42 = matrix.M42 * num;
            matrix1.M43 = matrix.M43 * num;
            matrix1.M44 = matrix.M44 * num;
            return matrix1;
        }

        /// <summary>
        /// Divides the components of a matrix by the corresponding components of another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">The divisor.</param>
        public static Matrix operator /(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 / matrix2.M11;
            matrix.M12 = matrix1.M12 / matrix2.M12;
            matrix.M13 = matrix1.M13 / matrix2.M13;
            matrix.M14 = matrix1.M14 / matrix2.M14;
            matrix.M21 = matrix1.M21 / matrix2.M21;
            matrix.M22 = matrix1.M22 / matrix2.M22;
            matrix.M23 = matrix1.M23 / matrix2.M23;
            matrix.M24 = matrix1.M24 / matrix2.M24;
            matrix.M31 = matrix1.M31 / matrix2.M31;
            matrix.M32 = matrix1.M32 / matrix2.M32;
            matrix.M33 = matrix1.M33 / matrix2.M33;
            matrix.M34 = matrix1.M34 / matrix2.M34;
            matrix.M41 = matrix1.M41 / matrix2.M41;
            matrix.M42 = matrix1.M42 / matrix2.M42;
            matrix.M43 = matrix1.M43 / matrix2.M43;
            matrix.M44 = matrix1.M44 / matrix2.M44;
            return matrix;
        }

        /// <summary>
        /// Divides the components of a matrix by a scalar.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="divider">The divisor.</param>
        public static Matrix operator /(Matrix matrix1, float divider)
        {
            float num = 1f / divider;
            Matrix matrix;
            matrix.M11 = matrix1.M11 * num;
            matrix.M12 = matrix1.M12 * num;
            matrix.M13 = matrix1.M13 * num;
            matrix.M14 = matrix1.M14 * num;
            matrix.M21 = matrix1.M21 * num;
            matrix.M22 = matrix1.M22 * num;
            matrix.M23 = matrix1.M23 * num;
            matrix.M24 = matrix1.M24 * num;
            matrix.M31 = matrix1.M31 * num;
            matrix.M32 = matrix1.M32 * num;
            matrix.M33 = matrix1.M33 * num;
            matrix.M34 = matrix1.M34 * num;
            matrix.M41 = matrix1.M41 * num;
            matrix.M42 = matrix1.M42 * num;
            matrix.M43 = matrix1.M43 * num;
            matrix.M44 = matrix1.M44 * num;
            return matrix;
        }

        /// <summary>
        /// Creates a spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param><param name="cameraPosition">Position of the camera.</param><param name="cameraUpVector">The up vector of the camera.</param><param name="cameraForwardVector">Optional forward vector of the camera.</param>
        public static Matrix CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3? cameraForwardVector)
        {
            Vector3 result1;
            result1.X = objectPosition.X - cameraPosition.X;
            result1.Y = objectPosition.Y - cameraPosition.Y;
            result1.Z = objectPosition.Z - cameraPosition.Z;
            float num = result1.LengthSquared();
            if ((double)num < 9.99999974737875E-05)
                result1 = cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward;
            else
                Vector3.Multiply(ref result1, 1f / (float)Math.Sqrt((double)num), out result1);
            Vector3 result2;
            Vector3.Cross(ref cameraUpVector, ref result1, out result2);
            result2.Normalize();
            Vector3 result3;
            Vector3.Cross(ref result1, ref result2, out result3);
            Matrix matrix;
            matrix.M11 = result2.X;
            matrix.M12 = result2.Y;
            matrix.M13 = result2.Z;
            matrix.M14 = 0.0f;
            matrix.M21 = result3.X;
            matrix.M22 = result3.Y;
            matrix.M23 = result3.Z;
            matrix.M24 = 0.0f;
            matrix.M31 = result1.X;
            matrix.M32 = result1.Y;
            matrix.M33 = result1.Z;
            matrix.M34 = 0.0f;
            matrix.M41 = objectPosition.X;
            matrix.M42 = objectPosition.Y;
            matrix.M43 = objectPosition.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param><param name="cameraPosition">Position of the camera.</param><param name="cameraUpVector">The up vector of the camera.</param><param name="cameraForwardVector">Optional forward vector of the camera.</param><param name="result">[OutAttribute] The created billboard matrix.</param>
        public static void CreateBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 cameraUpVector, Vector3? cameraForwardVector, out Matrix result)
        {
            Vector3 result1;
            result1.X = objectPosition.X - cameraPosition.X;
            result1.Y = objectPosition.Y - cameraPosition.Y;
            result1.Z = objectPosition.Z - cameraPosition.Z;
            float num = result1.LengthSquared();
            if ((double)num < 9.99999974737875E-05)
                result1 = cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward;
            else
                Vector3.Multiply(ref result1, 1f / (float)Math.Sqrt((double)num), out result1);
            Vector3 result2;
            Vector3.Cross(ref cameraUpVector, ref result1, out result2);
            result2.Normalize();
            Vector3 result3;
            Vector3.Cross(ref result1, ref result2, out result3);
            result.M11 = result2.X;
            result.M12 = result2.Y;
            result.M13 = result2.Z;
            result.M14 = 0.0f;
            result.M21 = result3.X;
            result.M22 = result3.Y;
            result.M23 = result3.Z;
            result.M24 = 0.0f;
            result.M31 = result1.X;
            result.M32 = result1.Y;
            result.M33 = result1.Z;
            result.M34 = 0.0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a cylindrical billboard that rotates around a specified axis.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param><param name="cameraPosition">Position of the camera.</param><param name="rotateAxis">Axis to rotate the billboard around.</param><param name="cameraForwardVector">Optional forward vector of the camera.</param><param name="objectForwardVector">Optional forward vector of the object.</param>
        public static Matrix CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis, Vector3? cameraForwardVector, Vector3? objectForwardVector)
        {
            Vector3 result1;
            result1.X = objectPosition.X - cameraPosition.X;
            result1.Y = objectPosition.Y - cameraPosition.Y;
            result1.Z = objectPosition.Z - cameraPosition.Z;
            float num = result1.LengthSquared();
            if ((double)num < 9.99999974737875E-05)
                result1 = cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward;
            else
                Vector3.Multiply(ref result1, 1f / (float)Math.Sqrt((double)num), out result1);
            Vector3 vector2 = rotateAxis;
            float result2;
            Vector3.Dot(ref rotateAxis, ref result1, out result2);
            Vector3 result3;
            Vector3 result4;
            if ((double)Math.Abs(result2) > 0.998254656791687)
            {
                if (objectForwardVector.HasValue)
                {
                    result3 = objectForwardVector.Value;
                    Vector3.Dot(ref rotateAxis, ref result3, out result2);
                    if ((double)Math.Abs(result2) > 0.998254656791687)
                        result3 = (double)Math.Abs((float)((double)rotateAxis.X * (double)Vector3.Forward.X + (double)rotateAxis.Y * (double)Vector3.Forward.Y + (double)rotateAxis.Z * (double)Vector3.Forward.Z)) > 0.998254656791687 ? Vector3.Right : Vector3.Forward;
                }
                else
                    result3 = (double)Math.Abs((float)((double)rotateAxis.X * (double)Vector3.Forward.X + (double)rotateAxis.Y * (double)Vector3.Forward.Y + (double)rotateAxis.Z * (double)Vector3.Forward.Z)) > 0.998254656791687 ? Vector3.Right : Vector3.Forward;
                Vector3.Cross(ref rotateAxis, ref result3, out result4);
                result4.Normalize();
                Vector3.Cross(ref result4, ref rotateAxis, out result3);
                result3.Normalize();
            }
            else
            {
                Vector3.Cross(ref rotateAxis, ref result1, out result4);
                result4.Normalize();
                Vector3.Cross(ref result4, ref vector2, out result3);
                result3.Normalize();
            }
            Matrix matrix;
            matrix.M11 = result4.X;
            matrix.M12 = result4.Y;
            matrix.M13 = result4.Z;
            matrix.M14 = 0.0f;
            matrix.M21 = vector2.X;
            matrix.M22 = vector2.Y;
            matrix.M23 = vector2.Z;
            matrix.M24 = 0.0f;
            matrix.M31 = result3.X;
            matrix.M32 = result3.Y;
            matrix.M33 = result3.Z;
            matrix.M34 = 0.0f;
            matrix.M41 = objectPosition.X;
            matrix.M42 = objectPosition.Y;
            matrix.M43 = objectPosition.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a cylindrical billboard that rotates around a specified axis.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param><param name="cameraPosition">Position of the camera.</param><param name="rotateAxis">Axis to rotate the billboard around.</param><param name="cameraForwardVector">Optional forward vector of the camera.</param><param name="objectForwardVector">Optional forward vector of the object.</param><param name="result">[OutAttribute] The created billboard matrix.</param>
        public static void CreateConstrainedBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 rotateAxis, Vector3? cameraForwardVector, Vector3? objectForwardVector, out Matrix result)
        {
            Vector3 result1;
            result1.X = objectPosition.X - cameraPosition.X;
            result1.Y = objectPosition.Y - cameraPosition.Y;
            result1.Z = objectPosition.Z - cameraPosition.Z;
            float num = result1.LengthSquared();
            if ((double)num < 9.99999974737875E-05)
                result1 = cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward;
            else
                Vector3.Multiply(ref result1, 1f / (float)Math.Sqrt((double)num), out result1);
            Vector3 vector2 = rotateAxis;
            float result2;
            Vector3.Dot(ref rotateAxis, ref result1, out result2);
            Vector3 result3;
            Vector3 result4;
            if ((double)Math.Abs(result2) > 0.998254656791687)
            {
                if (objectForwardVector.HasValue)
                {
                    result3 = objectForwardVector.Value;
                    Vector3.Dot(ref rotateAxis, ref result3, out result2);
                    if ((double)Math.Abs(result2) > 0.998254656791687)
                        result3 = (double)Math.Abs((float)((double)rotateAxis.X * (double)Vector3.Forward.X + (double)rotateAxis.Y * (double)Vector3.Forward.Y + (double)rotateAxis.Z * (double)Vector3.Forward.Z)) > 0.998254656791687 ? Vector3.Right : Vector3.Forward;
                }
                else
                    result3 = (double)Math.Abs((float)((double)rotateAxis.X * (double)Vector3.Forward.X + (double)rotateAxis.Y * (double)Vector3.Forward.Y + (double)rotateAxis.Z * (double)Vector3.Forward.Z)) > 0.998254656791687 ? Vector3.Right : Vector3.Forward;
                Vector3.Cross(ref rotateAxis, ref result3, out result4);
                result4.Normalize();
                Vector3.Cross(ref result4, ref rotateAxis, out result3);
                result3.Normalize();
            }
            else
            {
                Vector3.Cross(ref rotateAxis, ref result1, out result4);
                result4.Normalize();
                Vector3.Cross(ref result4, ref vector2, out result3);
                result3.Normalize();
            }
            result.M11 = result4.X;
            result.M12 = result4.Y;
            result.M13 = result4.Z;
            result.M14 = 0.0f;
            result.M21 = vector2.X;
            result.M22 = vector2.Y;
            result.M23 = vector2.Z;
            result.M24 = 0.0f;
            result.M31 = result3.X;
            result.M32 = result3.Y;
            result.M33 = result3.Z;
            result.M34 = 0.0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a translation Matrix.
        /// </summary>
        /// <param name="position">Amounts to translate by on the x, y, and z axes.</param>
        public static Matrix CreateTranslation(Vector3 position)
        {
            Matrix matrix;
            matrix.M11 = 1f;
            matrix.M12 = 0.0f;
            matrix.M13 = 0.0f;
            matrix.M14 = 0.0f;
            matrix.M21 = 0.0f;
            matrix.M22 = 1f;
            matrix.M23 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M31 = 0.0f;
            matrix.M32 = 0.0f;
            matrix.M33 = 1f;
            matrix.M34 = 0.0f;
            matrix.M41 = position.X;
            matrix.M42 = position.Y;
            matrix.M43 = position.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a translation Matrix.
        /// </summary>
        /// <param name="position">Amounts to translate by on the x, y, and z axes.</param><param name="result">[OutAttribute] The created translation Matrix.</param>
        public static void CreateTranslation(ref Vector3 position, out Matrix result)
        {
            result.M11 = 1f;
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = 1f;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = 1f;
            result.M34 = 0.0f;
            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a translation Matrix.
        /// </summary>
        /// <param name="xPosition">Value to translate by on the x-axis.</param><param name="yPosition">Value to translate by on the y-axis.</param><param name="zPosition">Value to translate by on the z-axis.</param>
        public static Matrix CreateTranslation(float xPosition, float yPosition, float zPosition)
        {
            Matrix matrix;
            matrix.M11 = 1f;
            matrix.M12 = 0.0f;
            matrix.M13 = 0.0f;
            matrix.M14 = 0.0f;
            matrix.M21 = 0.0f;
            matrix.M22 = 1f;
            matrix.M23 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M31 = 0.0f;
            matrix.M32 = 0.0f;
            matrix.M33 = 1f;
            matrix.M34 = 0.0f;
            matrix.M41 = xPosition;
            matrix.M42 = yPosition;
            matrix.M43 = zPosition;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a translation Matrix.
        /// </summary>
        /// <param name="xPosition">Value to translate by on the x-axis.</param><param name="yPosition">Value to translate by on the y-axis.</param><param name="zPosition">Value to translate by on the z-axis.</param><param name="result">[OutAttribute] The created translation Matrix.</param>
        public static void CreateTranslation(float xPosition, float yPosition, float zPosition, out Matrix result)
        {
            result.M11 = 1f;
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = 1f;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = 1f;
            result.M34 = 0.0f;
            result.M41 = xPosition;
            result.M42 = yPosition;
            result.M43 = zPosition;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a scaling Matrix.
        /// </summary>
        /// <param name="xScale">Value to scale by on the x-axis.</param><param name="yScale">Value to scale by on the y-axis.</param><param name="zScale">Value to scale by on the z-axis.</param>
        public static Matrix CreateScale(float xScale, float yScale, float zScale)
        {
            float num1 = xScale;
            float num2 = yScale;
            float num3 = zScale;
            Matrix matrix;
            matrix.M11 = num1;
            matrix.M12 = 0.0f;
            matrix.M13 = 0.0f;
            matrix.M14 = 0.0f;
            matrix.M21 = 0.0f;
            matrix.M22 = num2;
            matrix.M23 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M31 = 0.0f;
            matrix.M32 = 0.0f;
            matrix.M33 = num3;
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a scaling Matrix.
        /// </summary>
        /// <param name="xScale">Value to scale by on the x-axis.</param><param name="yScale">Value to scale by on the y-axis.</param><param name="zScale">Value to scale by on the z-axis.</param><param name="result">[OutAttribute] The created scaling Matrix.</param>
        public static void CreateScale(float xScale, float yScale, float zScale, out Matrix result)
        {
            float num1 = xScale;
            float num2 = yScale;
            float num3 = zScale;
            result.M11 = num1;
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = num2;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = num3;
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a scaling Matrix.
        /// </summary>
        /// <param name="scales">Amounts to scale by on the x, y, and z axes.</param>
        public static Matrix CreateScale(Vector3 scales)
        {
            float num1 = scales.X;
            float num2 = scales.Y;
            float num3 = scales.Z;
            Matrix matrix;
            matrix.M11 = num1;
            matrix.M12 = 0.0f;
            matrix.M13 = 0.0f;
            matrix.M14 = 0.0f;
            matrix.M21 = 0.0f;
            matrix.M22 = num2;
            matrix.M23 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M31 = 0.0f;
            matrix.M32 = 0.0f;
            matrix.M33 = num3;
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a scaling Matrix.
        /// </summary>
        /// <param name="scales">Amounts to scale by on the x, y, and z axes.</param><param name="result">[OutAttribute] The created scaling Matrix.</param>
        public static void CreateScale(ref Vector3 scales, out Matrix result)
        {
            float num1 = scales.X;
            float num2 = scales.Y;
            float num3 = scales.Z;
            result.M11 = num1;
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = num2;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = num3;
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a scaling Matrix.
        /// </summary>
        /// <param name="scale">Amount to scale by.</param>
        public static Matrix CreateScale(float scale)
        {
            float num = scale;
            Matrix matrix;
            matrix.M11 = num;
            matrix.M12 = 0.0f;
            matrix.M13 = 0.0f;
            matrix.M14 = 0.0f;
            matrix.M21 = 0.0f;
            matrix.M22 = num;
            matrix.M23 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M31 = 0.0f;
            matrix.M32 = 0.0f;
            matrix.M33 = num;
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a scaling Matrix.
        /// </summary>
        /// <param name="scale">Value to scale by.</param><param name="result">[OutAttribute] The created scaling Matrix.</param>
        public static void CreateScale(float scale, out Matrix result)
        {
            float num = scale;
            result.M11 = num;
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = num;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = num;
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Returns a matrix that can be used to rotate a set of vertices around the x-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the x-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix CreateRotationX(float radians)
        {
            float num1 = (float)Math.Cos((double)radians);
            float num2 = (float)Math.Sin((double)radians);
            Matrix matrix;
            matrix.M11 = 1f;
            matrix.M12 = 0.0f;
            matrix.M13 = 0.0f;
            matrix.M14 = 0.0f;
            matrix.M21 = 0.0f;
            matrix.M22 = num1;
            matrix.M23 = num2;
            matrix.M24 = 0.0f;
            matrix.M31 = 0.0f;
            matrix.M32 = -num2;
            matrix.M33 = num1;
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Populates data into a user-specified matrix that can be used to rotate a set of vertices around the x-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the x-axis. Note that you can use ToRadians to convert degrees to radians.</param><param name="result">[OutAttribute] The matrix in which to place the calculated data.</param>
        public static void CreateRotationX(float radians, out Matrix result)
        {
            float num1 = (float)Math.Cos((double)radians);
            float num2 = (float)Math.Sin((double)radians);
            result.M11 = 1f;
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = num1;
            result.M23 = num2;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = -num2;
            result.M33 = num1;
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Returns a matrix that can be used to rotate a set of vertices around the y-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the y-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix CreateRotationY(float radians)
        {
            float num1 = (float)Math.Cos((double)radians);
            float num2 = (float)Math.Sin((double)radians);
            Matrix matrix;
            matrix.M11 = num1;
            matrix.M12 = 0.0f;
            matrix.M13 = -num2;
            matrix.M14 = 0.0f;
            matrix.M21 = 0.0f;
            matrix.M22 = 1f;
            matrix.M23 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M31 = num2;
            matrix.M32 = 0.0f;
            matrix.M33 = num1;
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Populates data into a user-specified matrix that can be used to rotate a set of vertices around the y-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the y-axis. Note that you can use ToRadians to convert degrees to radians.</param><param name="result">[OutAttribute] The matrix in which to place the calculated data.</param>
        public static void CreateRotationY(float radians, out Matrix result)
        {
            float num1 = (float)Math.Cos((double)radians);
            float num2 = (float)Math.Sin((double)radians);
            result.M11 = num1;
            result.M12 = 0.0f;
            result.M13 = -num2;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = 1f;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = num2;
            result.M32 = 0.0f;
            result.M33 = num1;
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Returns a matrix that can be used to rotate a set of vertices around the z-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the z-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix CreateRotationZ(float radians)
        {
            float num1 = (float)Math.Cos((double)radians);
            float num2 = (float)Math.Sin((double)radians);
            Matrix matrix;
            matrix.M11 = num1;
            matrix.M12 = num2;
            matrix.M13 = 0.0f;
            matrix.M14 = 0.0f;
            matrix.M21 = -num2;
            matrix.M22 = num1;
            matrix.M23 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M31 = 0.0f;
            matrix.M32 = 0.0f;
            matrix.M33 = 1f;
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Populates data into a user-specified matrix that can be used to rotate a set of vertices around the z-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the z-axis. Note that you can use ToRadians to convert degrees to radians.</param><param name="result">[OutAttribute] The rotation matrix.</param>
        public static void CreateRotationZ(float radians, out Matrix result)
        {
            float num1 = (float)Math.Cos((double)radians);
            float num2 = (float)Math.Sin((double)radians);
            result.M11 = num1;
            result.M12 = num2;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = -num2;
            result.M22 = num1;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = 1f;
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a new Matrix that rotates around an arbitrary vector.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param><param name="angle">The angle to rotate around the vector.</param>
        public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
        {
            float num1 = axis.X;
            float num2 = axis.Y;
            float num3 = axis.Z;
            float num4 = (float)Math.Sin((double)angle);
            float num5 = (float)Math.Cos((double)angle);
            float num6 = num1 * num1;
            float num7 = num2 * num2;
            float num8 = num3 * num3;
            float num9 = num1 * num2;
            float num10 = num1 * num3;
            float num11 = num2 * num3;
            Matrix matrix;
            matrix.M11 = num6 + num5 * (1f - num6);
            matrix.M12 = (float)((double)num9 - (double)num5 * (double)num9 + (double)num4 * (double)num3);
            matrix.M13 = (float)((double)num10 - (double)num5 * (double)num10 - (double)num4 * (double)num2);
            matrix.M14 = 0.0f;
            matrix.M21 = (float)((double)num9 - (double)num5 * (double)num9 - (double)num4 * (double)num3);
            matrix.M22 = num7 + num5 * (1f - num7);
            matrix.M23 = (float)((double)num11 - (double)num5 * (double)num11 + (double)num4 * (double)num1);
            matrix.M24 = 0.0f;
            matrix.M31 = (float)((double)num10 - (double)num5 * (double)num10 + (double)num4 * (double)num2);
            matrix.M32 = (float)((double)num11 - (double)num5 * (double)num11 - (double)num4 * (double)num1);
            matrix.M33 = num8 + num5 * (1f - num8);
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a new Matrix that rotates around an arbitrary vector.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param><param name="angle">The angle to rotate around the vector.</param><param name="result">[OutAttribute] The created Matrix.</param>
        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix result)
        {
            float num1 = axis.X;
            float num2 = axis.Y;
            float num3 = axis.Z;
            float num4 = (float)Math.Sin((double)angle);
            float num5 = (float)Math.Cos((double)angle);
            float num6 = num1 * num1;
            float num7 = num2 * num2;
            float num8 = num3 * num3;
            float num9 = num1 * num2;
            float num10 = num1 * num3;
            float num11 = num2 * num3;
            result.M11 = num6 + num5 * (1f - num6);
            result.M12 = (float)((double)num9 - (double)num5 * (double)num9 + (double)num4 * (double)num3);
            result.M13 = (float)((double)num10 - (double)num5 * (double)num10 - (double)num4 * (double)num2);
            result.M14 = 0.0f;
            result.M21 = (float)((double)num9 - (double)num5 * (double)num9 - (double)num4 * (double)num3);
            result.M22 = num7 + num5 * (1f - num7);
            result.M23 = (float)((double)num11 - (double)num5 * (double)num11 + (double)num4 * (double)num1);
            result.M24 = 0.0f;
            result.M31 = (float)((double)num10 - (double)num5 * (double)num10 + (double)num4 * (double)num2);
            result.M32 = (float)((double)num11 - (double)num5 * (double)num11 - (double)num4 * (double)num1);
            result.M33 = num8 + num5 * (1f - num8);
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        public static void CreateRotationFromTwoVectors(ref Vector3 fromVector, ref Vector3 toVector, out Matrix resultMatrix)
        {
            Vector3 fromVectorNormalized = Vector3.Normalize(fromVector);
            Vector3 toVectorNormalized = Vector3.Normalize(toVector);

            Vector3 rotAxis;
            Vector3 thirdAxis;
            Vector3.Cross(ref fromVectorNormalized, ref toVectorNormalized, out rotAxis);
            rotAxis.Normalize();
            Vector3.Cross(ref fromVectorNormalized, ref rotAxis, out thirdAxis);
            Matrix fromMatrixTransposed = new Matrix(
                fromVectorNormalized.X, rotAxis.X, thirdAxis.X, 0,
                fromVectorNormalized.Y, rotAxis.Y, thirdAxis.Y, 0,
                fromVectorNormalized.Z, rotAxis.Z, thirdAxis.Z, 0,
                0, 0, 0, 1);
            
            Vector3.Cross(ref toVectorNormalized, ref rotAxis, out thirdAxis);
            Matrix toMatrix = new Matrix(
                toVectorNormalized.X, toVectorNormalized.Y, toVectorNormalized.Z, 0,
                rotAxis.X, rotAxis.Y, rotAxis.Z, 0,
                thirdAxis.X, thirdAxis.Y, thirdAxis.Z, 0,
                0, 0, 0, 1);

            resultMatrix = fromMatrixTransposed * toMatrix;
        }

        /// <summary>
        /// Builds a perspective projection matrix based on a field of view and returns by value.
        /// </summary>
        /// <param name="fieldOfView">Field of view in the y direction, in radians.</param><param name="aspectRatio">Aspect ratio, defined as view space width divided by height. To match the aspect ratio of the viewport, the property AspectRatio.</param><param name="nearPlaneDistance">Distance to the near view plane.</param><param name="farPlaneDistance">Distance to the far view plane.</param>
        public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
        {
            if ((double)fieldOfView <= 0.0 || (double)fieldOfView >= 3.14159274101257)
                throw new ArgumentOutOfRangeException("fieldOfView", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "OutRangeFieldOfView", new object[1]
                {
                    (object) "fieldOfView"
                }));
            else if ((double)nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
                {
                    (object) "nearPlaneDistance"
                }));
            else if ((double)farPlaneDistance <= 0.0)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
                {
                    (object) "farPlaneDistance"
                }));
            }
            else
            {
                //if ((double)nearPlaneDistance >= (double)farPlaneDistance)
                //    throw new ArgumentOutOfRangeException("nearPlaneDistance", "OppositePlanes");
                float num1 = 1f / (float)Math.Tan((double)fieldOfView * 0.5);
                float num2 = num1 / aspectRatio;
                Matrix matrix;
                matrix.M11 = num2;
                matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
                matrix.M22 = num1;
                matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
                matrix.M31 = matrix.M32 = 0.0f;
                matrix.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
                matrix.M34 = -1f;
                matrix.M41 = matrix.M42 = matrix.M44 = 0.0f;
                matrix.M43 = (float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
                return matrix;
            }
        }
        public static Matrix CreatePerspectiveFovRhComplementary(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
        {
            if ((double)fieldOfView <= 0.0 || (double)fieldOfView >= 3.14159274101257)
                throw new ArgumentOutOfRangeException("fieldOfView", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "OutRangeFieldOfView", new object[1]
                {
                    (object) "fieldOfView"
                }));
            else if ((double)nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
                {
                    (object) "nearPlaneDistance"
                }));
            else if ((double)farPlaneDistance <= 0.0)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
                {
                    (object) "farPlaneDistance"
                }));
            }
            else
            {
                //if ((double)nearPlaneDistance >= (double)farPlaneDistance)
                //    throw new ArgumentOutOfRangeException("nearPlaneDistance", "OppositePlanes");
                float num1 = 1f / (float)Math.Tan((double)fieldOfView * 0.5);
                float num2 = num1 / aspectRatio;
                Matrix matrix;
                matrix.M11 = num2;
                matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
                matrix.M22 = num1;
                matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
                matrix.M31 = matrix.M32 = 0.0f;
                matrix.M33 = -farPlaneDistance / (nearPlaneDistance - farPlaneDistance) - 1;
                matrix.M34 = -1f;
                matrix.M41 = matrix.M42 = matrix.M44 = 0.0f;
                matrix.M43 = -(float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
                return matrix;
            }
        }

        public static Matrix CreatePerspectiveFovRhInfinite(float fieldOfView, float aspectRatio, float nearPlaneDistance)
        {
            //const float EPS = -2.4e-4f;

            float num1 = 1f / (float)Math.Tan((double)fieldOfView * 0.5);
            float num2 = num1 / aspectRatio;
            Matrix matrix;
            matrix.M11 = num2;
            matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
            matrix.M22 = num1;
            matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
            matrix.M31 = matrix.M32 = 0.0f;
            matrix.M33 = -1f;
            matrix.M34 = -1f;
            matrix.M41 = matrix.M42 = matrix.M44 = 0.0f;
            matrix.M43 = -nearPlaneDistance;
            return matrix;
        }

        public static Matrix CreatePerspectiveFovRhInfiniteComplementary(float fieldOfView, float aspectRatio, float nearPlaneDistance)
        {
            //const float EPS = -2.4e-4f;

            float num1 = 1f / (float)Math.Tan((double)fieldOfView * 0.5);
            float num2 = num1 / aspectRatio;
            Matrix matrix;
            matrix.M11 = num2;
            matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
            matrix.M22 = num1;
            matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
            matrix.M31 = matrix.M32 = 0.0f;
            matrix.M33 = 0;
            matrix.M34 = -1f;
            matrix.M41 = matrix.M42 = matrix.M44 = 0.0f;
            matrix.M43 = nearPlaneDistance;

            return matrix;
        }

        public static Matrix CreatePerspectiveFovRhInverse(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
        {
            float num1 = (float)Math.Tan((double)fieldOfView * 0.5);
            float num2 = aspectRatio * num1;
            Matrix matrix;
            matrix.M11 = num2;
            matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
            matrix.M22 = num1;
            matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
            matrix.M31 = matrix.M32 = matrix.M33 = 0.0f;
            matrix.M34 = (nearPlaneDistance - farPlaneDistance) / (nearPlaneDistance * farPlaneDistance);
            matrix.M41 = matrix.M42 = matrix.M44 = 0.0f;
            matrix.M43 = -1;
            matrix.M44 = 1 / nearPlaneDistance;
            return matrix;
        }

        public static Matrix CreatePerspectiveFovRhInfiniteInverse(float fieldOfView, float aspectRatio, float nearPlaneDistance)
        {
            float num1 = 1f / (float)Math.Tan((double)fieldOfView * 0.5);
            float num2 = num1 / aspectRatio;
            Matrix matrix;
            matrix.M11 = num2;
            matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
            matrix.M22 = num1;
            matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
            matrix.M31 = matrix.M32 = 0.0f;
            matrix.M33 = 0;
            matrix.M34 = -1f / nearPlaneDistance;
            matrix.M41 = matrix.M42 = 0.0f;
            matrix.M43 = -1f;
            matrix.M44 = 1f / nearPlaneDistance;
            return matrix;
        }

        public static Matrix CreatePerspectiveFovRhInfiniteComplementaryInverse(float fieldOfView, float aspectRatio, float nearPlaneDistance)
        {
            float num1 = 1f / (float)Math.Tan((double)fieldOfView * 0.5);
            float num2 = num1 / aspectRatio;
            Matrix matrix;
            matrix.M11 = num2;
            matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
            matrix.M22 = num1;
            matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
            matrix.M31 = matrix.M32 = 0.0f;
            matrix.M33 = 0;
            matrix.M34 = 1f / nearPlaneDistance;
            matrix.M41 = matrix.M42 = 0.0f;
            matrix.M43 = -1f;
            matrix.M44 = 0;
            return matrix;
        }

        public static Matrix CreateFromPerspectiveFieldOfView(ref Matrix proj, float nearPlaneDistance, float farPlaneDistance)
        {
            Matrix matrix = proj;
            matrix.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            matrix.M43 = (float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
            return matrix;
        }

        /// <summary>
        /// Builds a perspective projection matrix based on a field of view and returns by reference.
        /// </summary>
        /// <param name="fieldOfView">Field of view in the y direction, in radians.</param><param name="aspectRatio">Aspect ratio, defined as view space width divided by height. To match the aspect ratio of the viewport, the property AspectRatio.</param><param name="nearPlaneDistance">Distance to the near view plane.</param><param name="farPlaneDistance">Distance to the far view plane.</param><param name="result">[OutAttribute] The perspective projection matrix.</param>
        public static void CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
        {
            if ((double)fieldOfView <= 0.0 || (double)fieldOfView >= 3.14159274101257)
                throw new ArgumentOutOfRangeException("fieldOfView", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "OutRangeFieldOfView", new object[1]
        {
          (object) "fieldOfView"
        }));
            else if ((double)nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "nearPlaneDistance"
        }));
            else if ((double)farPlaneDistance <= 0.0)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "farPlaneDistance"
        }));
            }
            else
            {
                if ((double)nearPlaneDistance >= (double)farPlaneDistance)
                    throw new ArgumentOutOfRangeException("nearPlaneDistance", "OppositePlanes");
                float num1 = 1f / (float)Math.Tan((double)fieldOfView * 0.5);
                float num2 = num1 / aspectRatio;
                result.M11 = num2;
                result.M12 = result.M13 = result.M14 = 0.0f;
                result.M22 = num1;
                result.M21 = result.M23 = result.M24 = 0.0f;
                result.M31 = result.M32 = 0.0f;
                result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
                result.M34 = -1f;
                result.M41 = result.M42 = result.M44 = 0.0f;
                result.M43 = (float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
            }
        }

        /// <summary>
        /// Builds a perspective projection matrix and returns the result by value.
        /// </summary>
        /// <param name="width">Width of the view volume at the near view plane.</param><param name="height">Height of the view volume at the near view plane.</param><param name="nearPlaneDistance">Distance to the near view plane.</param><param name="farPlaneDistance">Distance to the far view plane.</param>
        public static Matrix CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
        {
            if ((double)nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "nearPlaneDistance"
        }));
            else if ((double)farPlaneDistance <= 0.0)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "farPlaneDistance"
        }));
            }
            else
            {
                if ((double)nearPlaneDistance >= (double)farPlaneDistance)
                    throw new ArgumentOutOfRangeException("nearPlaneDistance", "OppositePlanes");
                Matrix matrix;
                matrix.M11 = 2f * nearPlaneDistance / width;
                matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
                matrix.M22 = 2f * nearPlaneDistance / height;
                matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
                matrix.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
                matrix.M31 = matrix.M32 = 0.0f;
                matrix.M34 = -1f;
                matrix.M41 = matrix.M42 = matrix.M44 = 0.0f;
                matrix.M43 = (float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
                return matrix;
            }
        }

        /// <summary>
        /// Builds a perspective projection matrix and returns the result by reference.
        /// </summary>
        /// <param name="width">Width of the view volume at the near view plane.</param><param name="height">Height of the view volume at the near view plane.</param><param name="nearPlaneDistance">Distance to the near view plane.</param><param name="farPlaneDistance">Distance to the far view plane.</param><param name="result">[OutAttribute] The projection matrix.</param>
        public static void CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
        {
            if ((double)nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "nearPlaneDistance"
        }));
            else if ((double)farPlaneDistance <= 0.0)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "farPlaneDistance"
        }));
            }
            else
            {
                if ((double)nearPlaneDistance >= (double)farPlaneDistance)
                    throw new ArgumentOutOfRangeException("nearPlaneDistance", "OppositePlanes");
                result.M11 = 2f * nearPlaneDistance / width;
                result.M12 = result.M13 = result.M14 = 0.0f;
                result.M22 = 2f * nearPlaneDistance / height;
                result.M21 = result.M23 = result.M24 = 0.0f;
                result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
                result.M31 = result.M32 = 0.0f;
                result.M34 = -1f;
                result.M41 = result.M42 = result.M44 = 0.0f;
                result.M43 = (float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
            }
        }

        /// <summary>
        /// Builds a customized, perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the view volume at the near view plane.</param><param name="right">Maximum x-value of the view volume at the near view plane.</param><param name="bottom">Minimum y-value of the view volume at the near view plane.</param><param name="top">Maximum y-value of the view volume at the near view plane.</param><param name="nearPlaneDistance">Distance to the near view plane.</param><param name="farPlaneDistance">Distance to of the far view plane.</param>
        public static Matrix CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
        {
            if ((double)nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "nearPlaneDistance"
        }));
            else if ((double)farPlaneDistance <= 0.0)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "farPlaneDistance"
        }));
            }
            else
            {
                if ((double)nearPlaneDistance >= (double)farPlaneDistance)
                    throw new ArgumentOutOfRangeException("nearPlaneDistance", "OppositePlanes");
                Matrix matrix;
                matrix.M11 = (float)(2.0 * (double)nearPlaneDistance / ((double)right - (double)left));
                matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
                matrix.M22 = (float)(2.0 * (double)nearPlaneDistance / ((double)top - (double)bottom));
                matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
                matrix.M31 = (float)(((double)left + (double)right) / ((double)right - (double)left));
                matrix.M32 = (float)(((double)top + (double)bottom) / ((double)top - (double)bottom));
                matrix.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
                matrix.M34 = -1f;
                matrix.M43 = (float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
                matrix.M41 = matrix.M42 = matrix.M44 = 0.0f;
                return matrix;
            }
        }

        /// <summary>
        /// Builds a customized, perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the view volume at the near view plane.</param><param name="right">Maximum x-value of the view volume at the near view plane.</param><param name="bottom">Minimum y-value of the view volume at the near view plane.</param><param name="top">Maximum y-value of the view volume at the near view plane.</param><param name="nearPlaneDistance">Distance to the near view plane.</param><param name="farPlaneDistance">Distance to of the far view plane.</param><param name="result">[OutAttribute] The created projection matrix.</param>
        public static void CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
        {
            if ((double)nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "nearPlaneDistance"
        }));
            else if ((double)farPlaneDistance <= 0.0)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format((IFormatProvider)CultureInfo.CurrentCulture, "NegativePlaneDistance", new object[1]
        {
          (object) "farPlaneDistance"
        }));
            }
            else
            {
                if ((double)nearPlaneDistance >= (double)farPlaneDistance)
                    throw new ArgumentOutOfRangeException("nearPlaneDistance", "OppositePlanes");
                result.M11 = (float)(2.0 * (double)nearPlaneDistance / ((double)right - (double)left));
                result.M12 = result.M13 = result.M14 = 0.0f;
                result.M22 = (float)(2.0 * (double)nearPlaneDistance / ((double)top - (double)bottom));
                result.M21 = result.M23 = result.M24 = 0.0f;
                result.M31 = (float)(((double)left + (double)right) / ((double)right - (double)left));
                result.M32 = (float)(((double)top + (double)bottom) / ((double)top - (double)bottom));
                result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
                result.M34 = -1f;
                result.M43 = (float)((double)nearPlaneDistance * (double)farPlaneDistance / ((double)nearPlaneDistance - (double)farPlaneDistance));
                result.M41 = result.M42 = result.M44 = 0.0f;
            }
        }

        /// <summary>
        /// Builds an orthogonal projection matrix.
        /// </summary>
        /// <param name="width">Width of the view volume.</param><param name="height">Height of the view volume.</param><param name="zNearPlane">Minimum z-value of the view volume.</param><param name="zFarPlane">Maximum z-value of the view volume.</param>
        public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
        {
            Matrix matrix;
            matrix.M11 = 2f / width;
            matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
            matrix.M22 = 2f / height;
            matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
            matrix.M33 = (float)(1.0 / ((double)zNearPlane - (double)zFarPlane));
            matrix.M31 = matrix.M32 = matrix.M34 = 0.0f;
            matrix.M41 = matrix.M42 = 0.0f;
            matrix.M43 = zNearPlane / (zNearPlane - zFarPlane);
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Builds an orthogonal projection matrix.
        /// </summary>
        /// <param name="width">Width of the view volume.</param><param name="height">Height of the view volume.</param><param name="zNearPlane">Minimum z-value of the view volume.</param><param name="zFarPlane">Maximum z-value of the view volume.</param><param name="result">[OutAttribute] The projection matrix.</param>
        public static void CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane, out Matrix result)
        {
            result.M11 = 2f / width;
            result.M12 = result.M13 = result.M14 = 0.0f;
            result.M22 = 2f / height;
            result.M21 = result.M23 = result.M24 = 0.0f;
            result.M33 = (float)(1.0 / ((double)zNearPlane - (double)zFarPlane));
            result.M31 = result.M32 = result.M34 = 0.0f;
            result.M41 = result.M42 = 0.0f;
            result.M43 = zNearPlane / (zNearPlane - zFarPlane);
            result.M44 = 1f;
        }

        /// <summary>
        /// Builds a customized, orthogonal projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the view volume.</param><param name="right">Maximum x-value of the view volume.</param><param name="bottom">Minimum y-value of the view volume.</param><param name="top">Maximum y-value of the view volume.</param><param name="zNearPlane">Minimum z-value of the view volume.</param><param name="zFarPlane">Maximum z-value of the view volume.</param>
        public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
        {
            Matrix matrix;
            matrix.M11 = (float)(2.0 / ((double)right - (double)left));
            matrix.M12 = matrix.M13 = matrix.M14 = 0.0f;
            matrix.M22 = (float)(2.0 / ((double)top - (double)bottom));
            matrix.M21 = matrix.M23 = matrix.M24 = 0.0f;
            matrix.M33 = (float)(1.0 / ((double)zNearPlane - (double)zFarPlane));
            matrix.M31 = matrix.M32 = matrix.M34 = 0.0f;
            matrix.M41 = (float)(((double)left + (double)right) / ((double)left - (double)right));
            matrix.M42 = (float)(((double)top + (double)bottom) / ((double)bottom - (double)top));
            matrix.M43 = zNearPlane / (zNearPlane - zFarPlane);
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Builds a customized, orthogonal projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the view volume.</param><param name="right">Maximum x-value of the view volume.</param><param name="bottom">Minimum y-value of the view volume.</param><param name="top">Maximum y-value of the view volume.</param><param name="zNearPlane">Minimum z-value of the view volume.</param><param name="zFarPlane">Maximum z-value of the view volume.</param><param name="result">[OutAttribute] The projection matrix.</param>
        public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane, out Matrix result)
        {
            result.M11 = (float)(2.0 / ((double)right - (double)left));
            result.M12 = result.M13 = result.M14 = 0.0f;
            result.M22 = (float)(2.0 / ((double)top - (double)bottom));
            result.M21 = result.M23 = result.M24 = 0.0f;
            result.M33 = (float)(1.0 / ((double)zNearPlane - (double)zFarPlane));
            result.M31 = result.M32 = result.M34 = 0.0f;
            result.M41 = (float)(((double)left + (double)right) / ((double)left - (double)right));
            result.M42 = (float)(((double)top + (double)bottom) / ((double)bottom - (double)top));
            result.M43 = zNearPlane / (zNearPlane - zFarPlane);
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a view matrix.
        /// </summary>
        /// <param name="cameraPosition">The position of the camera.</param><param name="cameraTarget">The target towards which the camera is pointing.</param><param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Vector3 vector3_1 = Vector3.Normalize(cameraPosition - cameraTarget);
            Vector3 vector3_2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector3_1));
            Vector3 vector1 = Vector3.Cross(vector3_1, vector3_2);
            Matrix matrix;
            matrix.M11 = vector3_2.X;
            matrix.M12 = vector1.X;
            matrix.M13 = vector3_1.X;
            matrix.M14 = 0.0f;
            matrix.M21 = vector3_2.Y;
            matrix.M22 = vector1.Y;
            matrix.M23 = vector3_1.Y;
            matrix.M24 = 0.0f;
            matrix.M31 = vector3_2.Z;
            matrix.M32 = vector1.Z;
            matrix.M33 = vector3_1.Z;
            matrix.M34 = 0.0f;
            matrix.M41 = -Vector3.Dot(vector3_2, cameraPosition);
            matrix.M42 = -Vector3.Dot(vector1, cameraPosition);
            matrix.M43 = -Vector3.Dot(vector3_1, cameraPosition);
            matrix.M44 = 1f;
            return matrix;
        }

        public static Matrix CreateLookAtInverse(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Vector3 vector3_1 = Vector3.Normalize(cameraPosition - cameraTarget);
            Vector3 vector3_2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector3_1));
            Vector3 vector1 = Vector3.Cross(vector3_1, vector3_2);
            Matrix matrix;
            matrix.M11 = vector3_2.X;
            matrix.M12 = vector3_2.Y;
            matrix.M13 = vector3_2.Z;
            matrix.M14 = 0.0f;
            matrix.M21 = vector1.X;
            matrix.M22 = vector1.Y;
            matrix.M23 = vector1.Z;
            matrix.M24 = 0.0f;
            matrix.M31 = vector3_1.X;
            matrix.M32 = vector3_1.Y;
            matrix.M33 = vector3_1.Z;
            matrix.M34 = 0.0f;
            matrix.M41 = cameraPosition.X;
            matrix.M42 = cameraPosition.Y;
            matrix.M43 = cameraPosition.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a view matrix.
        /// </summary>
        /// <param name="cameraPosition">The position of the camera.</param><param name="cameraTarget">The target towards which the camera is pointing.</param><param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param><param name="result">[OutAttribute] The created view matrix.</param>
        public static void CreateLookAt(ref Vector3 cameraPosition, ref Vector3 cameraTarget, ref Vector3 cameraUpVector, out Matrix result)
        {
            Vector3 vector3_1 = Vector3.Normalize(cameraPosition - cameraTarget);
            Vector3 vector3_2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector3_1));
            Vector3 vector1 = Vector3.Cross(vector3_1, vector3_2);
            result.M11 = vector3_2.X;
            result.M12 = vector1.X;
            result.M13 = vector3_1.X;
            result.M14 = 0.0f;
            result.M21 = vector3_2.Y;
            result.M22 = vector1.Y;
            result.M23 = vector3_1.Y;
            result.M24 = 0.0f;
            result.M31 = vector3_2.Z;
            result.M32 = vector1.Z;
            result.M33 = vector3_1.Z;
            result.M34 = 0.0f;
            result.M41 = -Vector3.Dot(vector3_2, cameraPosition);
            result.M42 = -Vector3.Dot(vector1, cameraPosition);
            result.M43 = -Vector3.Dot(vector3_1, cameraPosition);
            result.M44 = 1f;
        }

        public static Matrix CreateWorld(Vector3 position)
        {
            return Matrix.CreateWorld(position, Vector3.Forward, Vector3.Up);
        }

        /// <summary>
        /// Creates a world matrix with the specified parameters.
        /// </summary>
        /// <param name="position">Position of the object. This value is used in translation operations.</param><param name="forward">Forward direction of the object.</param><param name="up">Upward direction of the object; usually [0, 1, 0].</param>
        public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
        {
            Vector3 vector3_1 = Vector3.Normalize(-forward);
            Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector3_1));
            Vector3 vector3_2 = Vector3.Cross(vector3_1, vector2);
            Matrix matrix;
            matrix.M11 = vector2.X;
            matrix.M12 = vector2.Y;
            matrix.M13 = vector2.Z;
            matrix.M14 = 0.0f;
            matrix.M21 = vector3_2.X;
            matrix.M22 = vector3_2.Y;
            matrix.M23 = vector3_2.Z;
            matrix.M24 = 0.0f;
            matrix.M31 = vector3_1.X;
            matrix.M32 = vector3_1.Y;
            matrix.M33 = vector3_1.Z;
            matrix.M34 = 0.0f;
            matrix.M41 = position.X;
            matrix.M42 = position.Y;
            matrix.M43 = position.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a world matrix with the specified parameters.
        /// </summary>
        /// <param name="position">Position of the object. This value is used in translation operations.</param><param name="forward">Forward direction of the object.</param><param name="up">Upward direction of the object; usually [0, 1, 0].</param><param name="result">[OutAttribute] The created world matrix.</param>
        public static void CreateWorld(ref Vector3 position, ref Vector3 forward, ref Vector3 up, out Matrix result)
        {
            Vector3 vector3_1 = Vector3.Normalize(-forward);
            Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector3_1));
            Vector3 vector3_2 = Vector3.Cross(vector3_1, vector2);
            result.M11 = vector2.X;
            result.M12 = vector2.Y;
            result.M13 = vector2.Z;
            result.M14 = 0.0f;
            result.M21 = vector3_2.X;
            result.M22 = vector3_2.Y;
            result.M23 = vector3_2.Z;
            result.M24 = 0.0f;
            result.M31 = vector3_1.X;
            result.M32 = vector3_1.Y;
            result.M33 = vector3_1.Z;
            result.M34 = 0.0f;
            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a rotation Matrix from a Quaternion.
        /// </summary>
        /// <param name="quaternion">Quaternion to create the Matrix from.</param>
        public static Matrix CreateFromQuaternion(Quaternion quaternion)
        {
            float num1 = quaternion.X * quaternion.X;
            float num2 = quaternion.Y * quaternion.Y;
            float num3 = quaternion.Z * quaternion.Z;
            float num4 = quaternion.X * quaternion.Y;
            float num5 = quaternion.Z * quaternion.W;
            float num6 = quaternion.Z * quaternion.X;
            float num7 = quaternion.Y * quaternion.W;
            float num8 = quaternion.Y * quaternion.Z;
            float num9 = quaternion.X * quaternion.W;
            Matrix matrix;
            matrix.M11 = (float)(1.0 - 2.0 * ((double)num2 + (double)num3));
            matrix.M12 = (float)(2.0 * ((double)num4 + (double)num5));
            matrix.M13 = (float)(2.0 * ((double)num6 - (double)num7));
            matrix.M14 = 0.0f;
            matrix.M21 = (float)(2.0 * ((double)num4 - (double)num5));
            matrix.M22 = (float)(1.0 - 2.0 * ((double)num3 + (double)num1));
            matrix.M23 = (float)(2.0 * ((double)num8 + (double)num9));
            matrix.M24 = 0.0f;
            matrix.M31 = (float)(2.0 * ((double)num6 + (double)num7));
            matrix.M32 = (float)(2.0 * ((double)num8 - (double)num9));
            matrix.M33 = (float)(1.0 - 2.0 * ((double)num2 + (double)num1));
            matrix.M34 = 0.0f;
            matrix.M41 = 0.0f;
            matrix.M42 = 0.0f;
            matrix.M43 = 0.0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Creates a rotation Matrix from a Quaternion.
        /// </summary>
        /// <param name="quaternion">Quaternion to create the Matrix from.</param><param name="result">[OutAttribute] The created Matrix.</param>
        public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix result)
        {
            float num1 = quaternion.X * quaternion.X;
            float num2 = quaternion.Y * quaternion.Y;
            float num3 = quaternion.Z * quaternion.Z;
            float num4 = quaternion.X * quaternion.Y;
            float num5 = quaternion.Z * quaternion.W;
            float num6 = quaternion.Z * quaternion.X;
            float num7 = quaternion.Y * quaternion.W;
            float num8 = quaternion.Y * quaternion.Z;
            float num9 = quaternion.X * quaternion.W;
            result.M11 = (float)(1.0 - 2.0 * ((double)num2 + (double)num3));
            result.M12 = (float)(2.0 * ((double)num4 + (double)num5));
            result.M13 = (float)(2.0 * ((double)num6 - (double)num7));
            result.M14 = 0.0f;
            result.M21 = (float)(2.0 * ((double)num4 - (double)num5));
            result.M22 = (float)(1.0 - 2.0 * ((double)num3 + (double)num1));
            result.M23 = (float)(2.0 * ((double)num8 + (double)num9));
            result.M24 = 0.0f;
            result.M31 = (float)(2.0 * ((double)num6 + (double)num7));
            result.M32 = (float)(2.0 * ((double)num8 - (double)num9));
            result.M33 = (float)(1.0 - 2.0 * ((double)num2 + (double)num1));
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a new rotation matrix from a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Angle of rotation, in radians, around the y-axis.</param><param name="pitch">Angle of rotation, in radians, around the x-axis.</param><param name="roll">Angle of rotation, in radians, around the z-axis.</param>
        public static Matrix CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion result1;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out result1);
            Matrix result2;
            Matrix.CreateFromQuaternion(ref result1, out result2);
            return result2;
        }

        /// <summary>
        /// Fills in a rotation matrix from a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Angle of rotation, in radians, around the y-axis.</param><param name="pitch">Angle of rotation, in radians, around the x-axis.</param><param name="roll">Angle of rotation, in radians, around the z-axis.</param><param name="result">[OutAttribute] An existing matrix filled in to represent the specified yaw, pitch, and roll.</param>
        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Matrix result)
        {
            Quaternion result1;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out result1);
            Matrix.CreateFromQuaternion(ref result1, out result);
        }

        public static Matrix CreateFromTransformScale(Quaternion orientation, Vector3 position, Vector3 scale)
        {
            Matrix result = Matrix.CreateFromQuaternion(orientation);
            result.Translation = position;
            Matrix.Rescale(ref result, ref scale);
            return result;
        }

        /// <summary>
        /// Creates a Matrix that flattens geometry into a specified Plane as if casting a shadow from a specified light source.
        /// </summary>
        /// <param name="lightDirection">A Vector3 specifying the direction from which the light that will cast the shadow is coming.</param><param name="plane">The Plane onto which the new matrix should flatten geometry so as to cast a shadow.</param>
        public static Matrix CreateShadow(Vector3 lightDirection, Plane plane)
        {
            Plane result;
            Plane.Normalize(ref plane, out result);
            float num1 = (float)((double)result.Normal.X * (double)lightDirection.X + (double)result.Normal.Y * (double)lightDirection.Y + (double)result.Normal.Z * (double)lightDirection.Z);
            float num2 = -result.Normal.X;
            float num3 = -result.Normal.Y;
            float num4 = -result.Normal.Z;
            float num5 = -result.D;
            Matrix matrix;
            matrix.M11 = num2 * lightDirection.X + num1;
            matrix.M21 = num3 * lightDirection.X;
            matrix.M31 = num4 * lightDirection.X;
            matrix.M41 = num5 * lightDirection.X;
            matrix.M12 = num2 * lightDirection.Y;
            matrix.M22 = num3 * lightDirection.Y + num1;
            matrix.M32 = num4 * lightDirection.Y;
            matrix.M42 = num5 * lightDirection.Y;
            matrix.M13 = num2 * lightDirection.Z;
            matrix.M23 = num3 * lightDirection.Z;
            matrix.M33 = num4 * lightDirection.Z + num1;
            matrix.M43 = num5 * lightDirection.Z;
            matrix.M14 = 0.0f;
            matrix.M24 = 0.0f;
            matrix.M34 = 0.0f;
            matrix.M44 = num1;
            return matrix;
        }

        /// <summary>
        /// Fills in a Matrix to flatten geometry into a specified Plane as if casting a shadow from a specified light source.
        /// </summary>
        /// <param name="lightDirection">A Vector3 specifying the direction from which the light that will cast the shadow is coming.</param><param name="plane">The Plane onto which the new matrix should flatten geometry so as to cast a shadow.</param><param name="result">[OutAttribute] A Matrix that can be used to flatten geometry onto the specified plane from the specified direction.</param>
        public static void CreateShadow(ref Vector3 lightDirection, ref Plane plane, out Matrix result)
        {
            Plane result1;
            Plane.Normalize(ref plane, out result1);
            float num1 = (float)((double)result1.Normal.X * (double)lightDirection.X + (double)result1.Normal.Y * (double)lightDirection.Y + (double)result1.Normal.Z * (double)lightDirection.Z);
            float num2 = -result1.Normal.X;
            float num3 = -result1.Normal.Y;
            float num4 = -result1.Normal.Z;
            float num5 = -result1.D;
            result.M11 = num2 * lightDirection.X + num1;
            result.M21 = num3 * lightDirection.X;
            result.M31 = num4 * lightDirection.X;
            result.M41 = num5 * lightDirection.X;
            result.M12 = num2 * lightDirection.Y;
            result.M22 = num3 * lightDirection.Y + num1;
            result.M32 = num4 * lightDirection.Y;
            result.M42 = num5 * lightDirection.Y;
            result.M13 = num2 * lightDirection.Z;
            result.M23 = num3 * lightDirection.Z;
            result.M33 = num4 * lightDirection.Z + num1;
            result.M43 = num5 * lightDirection.Z;
            result.M14 = 0.0f;
            result.M24 = 0.0f;
            result.M34 = 0.0f;
            result.M44 = num1;
        }

        /// <summary>
        /// Creates a Matrix that reflects the coordinate system about a specified Plane.
        /// </summary>
        /// <param name="value">The Plane about which to create a reflection.</param>
        public static Matrix CreateReflection(Plane value)
        {
            value.Normalize();
            float num1 = value.Normal.X;
            float num2 = value.Normal.Y;
            float num3 = value.Normal.Z;
            float num4 = -2f * num1;
            float num5 = -2f * num2;
            float num6 = -2f * num3;
            Matrix matrix;
            matrix.M11 = (float)((double)num4 * (double)num1 + 1.0);
            matrix.M12 = num5 * num1;
            matrix.M13 = num6 * num1;
            matrix.M14 = 0.0f;
            matrix.M21 = num4 * num2;
            matrix.M22 = (float)((double)num5 * (double)num2 + 1.0);
            matrix.M23 = num6 * num2;
            matrix.M24 = 0.0f;
            matrix.M31 = num4 * num3;
            matrix.M32 = num5 * num3;
            matrix.M33 = (float)((double)num6 * (double)num3 + 1.0);
            matrix.M34 = 0.0f;
            matrix.M41 = num4 * value.D;
            matrix.M42 = num5 * value.D;
            matrix.M43 = num6 * value.D;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>
        /// Fills in an existing Matrix so that it reflects the coordinate system about a specified Plane.
        /// </summary>
        /// <param name="value">The Plane about which to create a reflection.</param><param name="result">[OutAttribute] A Matrix that creates the reflection.</param>
        public static void CreateReflection(ref Plane value, out Matrix result)
        {
            Plane result1;
            Plane.Normalize(ref value, out result1);
            value.Normalize();
            float num1 = result1.Normal.X;
            float num2 = result1.Normal.Y;
            float num3 = result1.Normal.Z;
            float num4 = -2f * num1;
            float num5 = -2f * num2;
            float num6 = -2f * num3;
            result.M11 = (float)((double)num4 * (double)num1 + 1.0);
            result.M12 = num5 * num1;
            result.M13 = num6 * num1;
            result.M14 = 0.0f;
            result.M21 = num4 * num2;
            result.M22 = (float)((double)num5 * (double)num2 + 1.0);
            result.M23 = num6 * num2;
            result.M24 = 0.0f;
            result.M31 = num4 * num3;
            result.M32 = num5 * num3;
            result.M33 = (float)((double)num6 * (double)num3 + 1.0);
            result.M34 = 0.0f;
            result.M41 = num4 * result1.D;
            result.M42 = num5 * result1.D;
            result.M43 = num6 * result1.D;
            result.M44 = 1f;
        }
        /*
/// <summary>
/// Extracts the scalar, translation, and rotation components from a 3D scale/rotate/translate (SRT) Matrix.  Reference page contains code sample.
/// </summary>
/// <param name="scale">[OutAttribute] The scalar component of the transform matrix, expressed as a Vector3.</param><param name="rotation">[OutAttribute] The rotation component of the transform matrix, expressed as a Quaternion.</param><param name="translation">[OutAttribute] The translation component of the transform matrix, expressed as a Vector3.</param>
public unsafe bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
{
bool flag = true;
fixed (float* numPtr = &scale.X)
{
Matrix.VectorBasis vectorBasis;
Vector3** vector3Ptr1 = (Vector3**) &vectorBasis;
Matrix identity = Matrix.Identity;
Matrix.CanonicalBasis canonicalBasis = new Matrix.CanonicalBasis();
Vector3* vector3Ptr2 = &canonicalBasis.Row0;
canonicalBasis.Row0 = new Vector3(1f, 0.0f, 0.0f);
canonicalBasis.Row1 = new Vector3(0.0f, 1f, 0.0f);
canonicalBasis.Row2 = new Vector3(0.0f, 0.0f, 1f);
translation.X = this.M41;
translation.Y = this.M42;
translation.Z = this.M43;
*vector3Ptr1 = (Vector3*) &identity.M11;
vector3Ptr1[1] = (Vector3*) &identity.M21;
vector3Ptr1[2] = (Vector3*) &identity.M31;
**vector3Ptr1 = new Vector3(this.M11, this.M12, this.M13);
*vector3Ptr1[1] = new Vector3(this.M21, this.M22, this.M23);
*vector3Ptr1[2] = new Vector3(this.M31, this.M32, this.M33);
scale.X = (*vector3Ptr1)->Length();
scale.Y = vector3Ptr1[1]->Length();
scale.Z = vector3Ptr1[2]->Length();
float num1 = *numPtr;
float num2 = numPtr[1];
float num3 = numPtr[2];
uint index1;
uint index2;
uint index3;
if ((double) num1 < (double) num2)
{
if ((double) num2 < (double) num3)
{
  index1 = 2U;
  index2 = 1U;
  index3 = 0U;
}
else
{
  index1 = 1U;
  if ((double) num1 < (double) num3)
  {
    index2 = 2U;
    index3 = 0U;
  }
  else
  {
    index2 = 0U;
    index3 = 2U;
  }
}
}
else if ((double) num1 < (double) num3)
{
index1 = 2U;
index2 = 0U;
index3 = 1U;
}
else
{
index1 = 0U;
if ((double) num2 < (double) num3)
{
  index2 = 2U;
  index3 = 1U;
}
else
{
  index2 = 1U;
  index3 = 2U;
}
}
if ((double) numPtr[index1] < 9.99999974737875E-05)
*(Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))) = *(Vector3*) ((IntPtr) vector3Ptr2 + (IntPtr) ((long) index1 * (long) sizeof (Vector3)));
((Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))))->Normalize();
if ((double) numPtr[index2] < 9.99999974737875E-05)
{
float num4 = Math.Abs(((Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))))->X);
float num5 = Math.Abs(((Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))))->Y);
float num6 = Math.Abs(((Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))))->Z);
uint num7 = (double) num4 >= (double) num5 ? ((double) num4 >= (double) num6 ? ((double) num5 >= (double) num6 ? 2U : 1U) : 1U) : ((double) num5 >= (double) num6 ? ((double) num4 >= (double) num6 ? 2U : 0U) : 0U);
// ISSUE: cast to a reference type
// ISSUE: cast to a reference type
// ISSUE: cast to a reference type
Vector3.Cross((Vector3&) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index2 * (long) sizeof (Vector3*))), (Vector3&) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))), (Vector3&) ((IntPtr) vector3Ptr2 + (IntPtr) ((long) num7 * (long) sizeof (Vector3))));
}
((Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index2 * (long) sizeof (Vector3*))))->Normalize();
if ((double) numPtr[index3] < 9.99999974737875E-05)
{
// ISSUE: cast to a reference type
// ISSUE: cast to a reference type
// ISSUE: cast to a reference type
Vector3.Cross((Vector3&) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index3 * (long) sizeof (Vector3*))), (Vector3&) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))), (Vector3&) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index2 * (long) sizeof (Vector3*))));
}
((Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index3 * (long) sizeof (Vector3*))))->Normalize();
float num8 = identity.Determinant();
if ((double) num8 < 0.0)
{
numPtr[index1] = -numPtr[index1];
*(Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*))) = -*(Vector3*) *(IntPtr*) ((IntPtr) vector3Ptr1 + (IntPtr) ((long) index1 * (long) sizeof (Vector3*)));
num8 = -num8;
}
float num9 = num8 - 1f;
if (9.99999974737875E-05 < (double) (num9 * num9))
{
rotation = Quaternion.Identity;
flag = false;
}
else
Quaternion.CreateFromRotationMatrix(ref identity, out rotation);
}
return flag;
}
*/
        /// <summary>
        /// Transforms a Matrix by applying a Quaternion rotation.
        /// </summary>
        /// <param name="value">The Matrix to transform.</param><param name="rotation">The rotation to apply, expressed as a Quaternion.</param>
        public static Matrix Transform(Matrix value, Quaternion rotation)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num1;
            float num5 = rotation.W * num2;
            float num6 = rotation.W * num3;
            float num7 = rotation.X * num1;
            float num8 = rotation.X * num2;
            float num9 = rotation.X * num3;
            float num10 = rotation.Y * num2;
            float num11 = rotation.Y * num3;
            float num12 = rotation.Z * num3;
            float num13 = 1f - num10 - num12;
            float num14 = num8 - num6;
            float num15 = num9 + num5;
            float num16 = num8 + num6;
            float num17 = 1f - num7 - num12;
            float num18 = num11 - num4;
            float num19 = num9 - num5;
            float num20 = num11 + num4;
            float num21 = 1f - num7 - num10;
            Matrix matrix;
            matrix.M11 = (float)((double)value.M11 * (double)num13 + (double)value.M12 * (double)num14 + (double)value.M13 * (double)num15);
            matrix.M12 = (float)((double)value.M11 * (double)num16 + (double)value.M12 * (double)num17 + (double)value.M13 * (double)num18);
            matrix.M13 = (float)((double)value.M11 * (double)num19 + (double)value.M12 * (double)num20 + (double)value.M13 * (double)num21);
            matrix.M14 = value.M14;
            matrix.M21 = (float)((double)value.M21 * (double)num13 + (double)value.M22 * (double)num14 + (double)value.M23 * (double)num15);
            matrix.M22 = (float)((double)value.M21 * (double)num16 + (double)value.M22 * (double)num17 + (double)value.M23 * (double)num18);
            matrix.M23 = (float)((double)value.M21 * (double)num19 + (double)value.M22 * (double)num20 + (double)value.M23 * (double)num21);
            matrix.M24 = value.M24;
            matrix.M31 = (float)((double)value.M31 * (double)num13 + (double)value.M32 * (double)num14 + (double)value.M33 * (double)num15);
            matrix.M32 = (float)((double)value.M31 * (double)num16 + (double)value.M32 * (double)num17 + (double)value.M33 * (double)num18);
            matrix.M33 = (float)((double)value.M31 * (double)num19 + (double)value.M32 * (double)num20 + (double)value.M33 * (double)num21);
            matrix.M34 = value.M34;
            matrix.M41 = (float)((double)value.M41 * (double)num13 + (double)value.M42 * (double)num14 + (double)value.M43 * (double)num15);
            matrix.M42 = (float)((double)value.M41 * (double)num16 + (double)value.M42 * (double)num17 + (double)value.M43 * (double)num18);
            matrix.M43 = (float)((double)value.M41 * (double)num19 + (double)value.M42 * (double)num20 + (double)value.M43 * (double)num21);
            matrix.M44 = value.M44;
            return matrix;
        }

        /// <summary>
        /// Transforms a Matrix by applying a Quaternion rotation.
        /// </summary>
        /// <param name="value">The Matrix to transform.</param><param name="rotation">The rotation to apply, expressed as a Quaternion.</param><param name="result">[OutAttribute] An existing Matrix filled in with the result of the transform.</param>
        public static void Transform(ref Matrix value, ref Quaternion rotation, out Matrix result)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num1;
            float num5 = rotation.W * num2;
            float num6 = rotation.W * num3;
            float num7 = rotation.X * num1;
            float num8 = rotation.X * num2;
            float num9 = rotation.X * num3;
            float num10 = rotation.Y * num2;
            float num11 = rotation.Y * num3;
            float num12 = rotation.Z * num3;
            float num13 = 1f - num10 - num12;
            float num14 = num8 - num6;
            float num15 = num9 + num5;
            float num16 = num8 + num6;
            float num17 = 1f - num7 - num12;
            float num18 = num11 - num4;
            float num19 = num9 - num5;
            float num20 = num11 + num4;
            float num21 = 1f - num7 - num10;
            float num22 = (float)((double)value.M11 * (double)num13 + (double)value.M12 * (double)num14 + (double)value.M13 * (double)num15);
            float num23 = (float)((double)value.M11 * (double)num16 + (double)value.M12 * (double)num17 + (double)value.M13 * (double)num18);
            float num24 = (float)((double)value.M11 * (double)num19 + (double)value.M12 * (double)num20 + (double)value.M13 * (double)num21);
            float num25 = value.M14;
            float num26 = (float)((double)value.M21 * (double)num13 + (double)value.M22 * (double)num14 + (double)value.M23 * (double)num15);
            float num27 = (float)((double)value.M21 * (double)num16 + (double)value.M22 * (double)num17 + (double)value.M23 * (double)num18);
            float num28 = (float)((double)value.M21 * (double)num19 + (double)value.M22 * (double)num20 + (double)value.M23 * (double)num21);
            float num29 = value.M24;
            float num30 = (float)((double)value.M31 * (double)num13 + (double)value.M32 * (double)num14 + (double)value.M33 * (double)num15);
            float num31 = (float)((double)value.M31 * (double)num16 + (double)value.M32 * (double)num17 + (double)value.M33 * (double)num18);
            float num32 = (float)((double)value.M31 * (double)num19 + (double)value.M32 * (double)num20 + (double)value.M33 * (double)num21);
            float num33 = value.M34;
            float num34 = (float)((double)value.M41 * (double)num13 + (double)value.M42 * (double)num14 + (double)value.M43 * (double)num15);
            float num35 = (float)((double)value.M41 * (double)num16 + (double)value.M42 * (double)num17 + (double)value.M43 * (double)num18);
            float num36 = (float)((double)value.M41 * (double)num19 + (double)value.M42 * (double)num20 + (double)value.M43 * (double)num21);
            float num37 = value.M44;
            result.M11 = num22;
            result.M12 = num23;
            result.M13 = num24;
            result.M14 = num25;
            result.M21 = num26;
            result.M22 = num27;
            result.M23 = num28;
            result.M24 = num29;
            result.M31 = num30;
            result.M32 = num31;
            result.M33 = num32;
            result.M34 = num33;
            result.M41 = num34;
            result.M42 = num35;
            result.M43 = num36;
            result.M44 = num37;
        }

        public Vector4 GetRow(int row)
        {
            unsafe
            {
				fixed(float * data = &M11)
                {
                    float* basePos = data + row * 4;
                    return new Vector4(*basePos, *(basePos + 1), *(basePos + 2), *(basePos + 3));
                }
            }
        }

        public void SetRow(int row, Vector4 value)
        {
            unsafe
            {
				fixed (float* data = &M11)
                {
                    float* basePos = data + row * 4;
                    *(basePos + 0) = value.X;
                    *(basePos + 1) = value.Y;
                    *(basePos + 2) = value.Z;
                    *(basePos + 3) = value.W;
                }
            }
        }

        public float this[int row, int column]
        {
            get
            {
                unsafe
                {
					fixed (float* data = &M11)
                    {
                        return data[row * 4 + column];
                    }
                }
            }
            set
            {
                unsafe
                {
					fixed (float* data = &M11)
                    {
                        data[row * 4 + column] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a string representation of the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return "{ " + string.Format((IFormatProvider)currentCulture, "{{M11:{0} M12:{1} M13:{2} M14:{3}}} ", (object)this.M11.ToString((IFormatProvider)currentCulture), (object)this.M12.ToString((IFormatProvider)currentCulture), (object)this.M13.ToString((IFormatProvider)currentCulture), (object)this.M14.ToString((IFormatProvider)currentCulture)) + string.Format((IFormatProvider)currentCulture, "{{M21:{0} M22:{1} M23:{2} M24:{3}}} ", (object)this.M21.ToString((IFormatProvider)currentCulture), (object)this.M22.ToString((IFormatProvider)currentCulture), (object)this.M23.ToString((IFormatProvider)currentCulture), (object)this.M24.ToString((IFormatProvider)currentCulture)) + string.Format((IFormatProvider)currentCulture, "{{M31:{0} M32:{1} M33:{2} M34:{3}}} ", (object)this.M31.ToString((IFormatProvider)currentCulture), (object)this.M32.ToString((IFormatProvider)currentCulture), (object)this.M33.ToString((IFormatProvider)currentCulture), (object)this.M34.ToString((IFormatProvider)currentCulture)) + string.Format((IFormatProvider)currentCulture, "{{M41:{0} M42:{1} M43:{2} M44:{3}}} ", (object)this.M41.ToString((IFormatProvider)currentCulture), (object)this.M42.ToString((IFormatProvider)currentCulture), (object)this.M43.ToString((IFormatProvider)currentCulture), (object)this.M44.ToString((IFormatProvider)currentCulture)) + "}";
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the Matrix.
        /// </summary>
        /// <param name="other">The Object to compare with the current Matrix.</param>
        public bool Equals(Matrix other)
        {
            if ((double)this.M11 == (double)other.M11 && (double)this.M22 == (double)other.M22 && ((double)this.M33 == (double)other.M33 && (double)this.M44 == (double)other.M44) && ((double)this.M12 == (double)other.M12 && (double)this.M13 == (double)other.M13 && ((double)this.M14 == (double)other.M14 && (double)this.M21 == (double)other.M21)) && ((double)this.M23 == (double)other.M23 && (double)this.M24 == (double)other.M24 && ((double)this.M31 == (double)other.M31 && (double)this.M32 == (double)other.M32) && ((double)this.M34 == (double)other.M34 && (double)this.M41 == (double)other.M41 && (double)this.M42 == (double)other.M42)))
                return (double)this.M43 == (double)other.M43;
            else
                return false;
        }

        /// <summary>
        /// Compares just position, forward and up
        /// </summary>
        public bool EqualsFast(ref Matrix other, float epsilon = 0.0001f)
        {
            float m21 = M21 - other.M21;
            float m22 = M22 - other.M22;
            float m23 = M23 - other.M23;
            float m31 = M31 - other.M31;
            float m32 = M32 - other.M32;
            float m33 = M33 - other.M33;
            float m41 = M41 - other.M41;
            float m42 = M42 - other.M42;
            float m43 = M43 - other.M43;
            float epSq = epsilon * epsilon;

            return ((m21 * m21 + m22 * m22 + m23 * m23) < epSq)
                & ((m31 * m31 + m32 * m32 + m33 * m33) < epSq)
                & ((m41 * m41 + m42 * m42 + m43 * m43) < epSq);
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Matrix)
                flag = this.Equals((Matrix)obj);
            return flag;
        }

        /// <summary>
        /// Gets the hash code of this object.
        /// </summary>
        public override int GetHashCode()
        {
            return this.M11.GetHashCode() + this.M12.GetHashCode() + this.M13.GetHashCode() + this.M14.GetHashCode() + this.M21.GetHashCode() + this.M22.GetHashCode() + this.M23.GetHashCode() + this.M24.GetHashCode() + this.M31.GetHashCode() + this.M32.GetHashCode() + this.M33.GetHashCode() + this.M34.GetHashCode() + this.M41.GetHashCode() + this.M42.GetHashCode() + this.M43.GetHashCode() + this.M44.GetHashCode();
        }

        /// <summary>
        /// Transposes the rows and columns of a matrix.
        /// </summary>
        /// <param name="matrix">Source matrix.</param>
        public static Matrix Transpose(Matrix matrix)
        {
            Matrix matrix1;
            matrix1.M11 = matrix.M11;
            matrix1.M12 = matrix.M21;
            matrix1.M13 = matrix.M31;
            matrix1.M14 = matrix.M41;
            matrix1.M21 = matrix.M12;
            matrix1.M22 = matrix.M22;
            matrix1.M23 = matrix.M32;
            matrix1.M24 = matrix.M42;
            matrix1.M31 = matrix.M13;
            matrix1.M32 = matrix.M23;
            matrix1.M33 = matrix.M33;
            matrix1.M34 = matrix.M43;
            matrix1.M41 = matrix.M14;
            matrix1.M42 = matrix.M24;
            matrix1.M43 = matrix.M34;
            matrix1.M44 = matrix.M44;
            return matrix1;
        }

        /// <summary>
        /// Transposes the rows and columns of a matrix.
        /// </summary>
        /// <param name="matrix">Source matrix.</param><param name="result">[OutAttribute] Transposed matrix.</param>
        public static void Transpose(ref Matrix matrix, out Matrix result)
        {
            float num1 = matrix.M11;
            float num2 = matrix.M12;
            float num3 = matrix.M13;
            float num4 = matrix.M14;
            float num5 = matrix.M21;
            float num6 = matrix.M22;
            float num7 = matrix.M23;
            float num8 = matrix.M24;
            float num9 = matrix.M31;
            float num10 = matrix.M32;
            float num11 = matrix.M33;
            float num12 = matrix.M34;
            float num13 = matrix.M41;
            float num14 = matrix.M42;
            float num15 = matrix.M43;
            float num16 = matrix.M44;
            result.M11 = num1;
            result.M12 = num5;
            result.M13 = num9;
            result.M14 = num13;
            result.M21 = num2;
            result.M22 = num6;
            result.M23 = num10;
            result.M24 = num14;
            result.M31 = num3;
            result.M32 = num7;
            result.M33 = num11;
            result.M34 = num15;
            result.M41 = num4;
            result.M42 = num8;
            result.M43 = num12;
            result.M44 = num16;
        }

        /// <summary>
        /// Transposes the rows and columns of a matrix that is assumed to be rotation only in place.
        /// </summary>
        /// <param name="matrix">Source matrix.</param>
        public void TransposeRotationInPlace()
        {
            float temp;

            temp = M12;
            M12 = M21;
            M21 = temp;

            temp = M13;
            M13 = M31;
            M31 = temp;

            temp = M23;
            M23 = M32;
            M32 = temp;
        }
        
        /// <summary>
        /// Calculates the determinant of the matrix.
        /// </summary>
        public float Determinant()
        {
            float num1 = this.M11;
            float num2 = this.M12;
            float num3 = this.M13;
            float num4 = this.M14;
            float num5 = this.M21;
            float num6 = this.M22;
            float num7 = this.M23;
            float num8 = this.M24;
            float num9 = this.M31;
            float num10 = this.M32;
            float num11 = this.M33;
            float num12 = this.M34;
            float num13 = this.M41;
            float num14 = this.M42;
            float num15 = this.M43;
            float num16 = this.M44;
            float num17 = (float)((double)num11 * (double)num16 - (double)num12 * (double)num15);
            float num18 = (float)((double)num10 * (double)num16 - (double)num12 * (double)num14);
            float num19 = (float)((double)num10 * (double)num15 - (double)num11 * (double)num14);
            float num20 = (float)((double)num9 * (double)num16 - (double)num12 * (double)num13);
            float num21 = (float)((double)num9 * (double)num15 - (double)num11 * (double)num13);
            float num22 = (float)((double)num9 * (double)num14 - (double)num10 * (double)num13);
            return (float)((double)num1 * ((double)num6 * (double)num17 - (double)num7 * (double)num18 + (double)num8 * (double)num19) - (double)num2 * ((double)num5 * (double)num17 - (double)num7 * (double)num20 + (double)num8 * (double)num21) + (double)num3 * ((double)num5 * (double)num18 - (double)num6 * (double)num20 + (double)num8 * (double)num22) - (double)num4 * ((double)num5 * (double)num19 - (double)num6 * (double)num21 + (double)num7 * (double)num22));
        }

        /// <summary>
        /// Calculates the inverse of a matrix.
        /// </summary>
        /// <param name="matrix">Source matrix.</param>
        public static Matrix Invert(Matrix matrix)
        {
            return Invert(ref matrix);
        }

        public static Matrix Invert(ref Matrix matrix)
        {
            float num1 = matrix.M11;
            float num2 = matrix.M12;
            float num3 = matrix.M13;
            float num4 = matrix.M14;
            float num5 = matrix.M21;
            float num6 = matrix.M22;
            float num7 = matrix.M23;
            float num8 = matrix.M24;
            float num9 = matrix.M31;
            float num10 = matrix.M32;
            float num11 = matrix.M33;
            float num12 = matrix.M34;
            float num13 = matrix.M41;
            float num14 = matrix.M42;
            float num15 = matrix.M43;
            float num16 = matrix.M44;
            float num17 = (float)((double)num11 * (double)num16 - (double)num12 * (double)num15);
            float num18 = (float)((double)num10 * (double)num16 - (double)num12 * (double)num14);
            float num19 = (float)((double)num10 * (double)num15 - (double)num11 * (double)num14);
            float num20 = (float)((double)num9 * (double)num16 - (double)num12 * (double)num13);
            float num21 = (float)((double)num9 * (double)num15 - (double)num11 * (double)num13);
            float num22 = (float)((double)num9 * (double)num14 - (double)num10 * (double)num13);
            float num23 = (float)((double)num6 * (double)num17 - (double)num7 * (double)num18 + (double)num8 * (double)num19);
            float num24 = (float)-((double)num5 * (double)num17 - (double)num7 * (double)num20 + (double)num8 * (double)num21);
            float num25 = (float)((double)num5 * (double)num18 - (double)num6 * (double)num20 + (double)num8 * (double)num22);
            float num26 = (float)-((double)num5 * (double)num19 - (double)num6 * (double)num21 + (double)num7 * (double)num22);
            float num27 = (float)(1.0 / ((double)num1 * (double)num23 + (double)num2 * (double)num24 + (double)num3 * (double)num25 + (double)num4 * (double)num26));
            Matrix matrix1;
            matrix1.M11 = num23 * num27;
            matrix1.M21 = num24 * num27;
            matrix1.M31 = num25 * num27;
            matrix1.M41 = num26 * num27;
            matrix1.M12 = (float)-((double)num2 * (double)num17 - (double)num3 * (double)num18 + (double)num4 * (double)num19) * num27;
            matrix1.M22 = (float)((double)num1 * (double)num17 - (double)num3 * (double)num20 + (double)num4 * (double)num21) * num27;
            matrix1.M32 = (float)-((double)num1 * (double)num18 - (double)num2 * (double)num20 + (double)num4 * (double)num22) * num27;
            matrix1.M42 = (float)((double)num1 * (double)num19 - (double)num2 * (double)num21 + (double)num3 * (double)num22) * num27;
            float num28 = (float)((double)num7 * (double)num16 - (double)num8 * (double)num15);
            float num29 = (float)((double)num6 * (double)num16 - (double)num8 * (double)num14);
            float num30 = (float)((double)num6 * (double)num15 - (double)num7 * (double)num14);
            float num31 = (float)((double)num5 * (double)num16 - (double)num8 * (double)num13);
            float num32 = (float)((double)num5 * (double)num15 - (double)num7 * (double)num13);
            float num33 = (float)((double)num5 * (double)num14 - (double)num6 * (double)num13);
            matrix1.M13 = (float)((double)num2 * (double)num28 - (double)num3 * (double)num29 + (double)num4 * (double)num30) * num27;
            matrix1.M23 = (float)-((double)num1 * (double)num28 - (double)num3 * (double)num31 + (double)num4 * (double)num32) * num27;
            matrix1.M33 = (float)((double)num1 * (double)num29 - (double)num2 * (double)num31 + (double)num4 * (double)num33) * num27;
            matrix1.M43 = (float)-((double)num1 * (double)num30 - (double)num2 * (double)num32 + (double)num3 * (double)num33) * num27;
            float num34 = (float)((double)num7 * (double)num12 - (double)num8 * (double)num11);
            float num35 = (float)((double)num6 * (double)num12 - (double)num8 * (double)num10);
            float num36 = (float)((double)num6 * (double)num11 - (double)num7 * (double)num10);
            float num37 = (float)((double)num5 * (double)num12 - (double)num8 * (double)num9);
            float num38 = (float)((double)num5 * (double)num11 - (double)num7 * (double)num9);
            float num39 = (float)((double)num5 * (double)num10 - (double)num6 * (double)num9);
            matrix1.M14 = (float)-((double)num2 * (double)num34 - (double)num3 * (double)num35 + (double)num4 * (double)num36) * num27;
            matrix1.M24 = (float)((double)num1 * (double)num34 - (double)num3 * (double)num37 + (double)num4 * (double)num38) * num27;
            matrix1.M34 = (float)-((double)num1 * (double)num35 - (double)num2 * (double)num37 + (double)num4 * (double)num39) * num27;
            matrix1.M44 = (float)((double)num1 * (double)num36 - (double)num2 * (double)num38 + (double)num3 * (double)num39) * num27;
            return matrix1;
        }

        /// <summary>
        /// Calculates the inverse of a matrix.
        /// </summary>
        /// <param name="matrix">The source matrix.</param><param name="result">[OutAttribute] The inverse of matrix. The same matrix can be used for both arguments.</param>
        public static void Invert(ref Matrix matrix, out Matrix result)
        {
            float num1 = matrix.M11;
            float num2 = matrix.M12;
            float num3 = matrix.M13;
            float num4 = matrix.M14;
            float num5 = matrix.M21;
            float num6 = matrix.M22;
            float num7 = matrix.M23;
            float num8 = matrix.M24;
            float num9 = matrix.M31;
            float num10 = matrix.M32;
            float num11 = matrix.M33;
            float num12 = matrix.M34;
            float num13 = matrix.M41;
            float num14 = matrix.M42;
            float num15 = matrix.M43;
            float num16 = matrix.M44;
            float num17 = (float)((double)num11 * (double)num16 - (double)num12 * (double)num15);
            float num18 = (float)((double)num10 * (double)num16 - (double)num12 * (double)num14);
            float num19 = (float)((double)num10 * (double)num15 - (double)num11 * (double)num14);
            float num20 = (float)((double)num9 * (double)num16 - (double)num12 * (double)num13);
            float num21 = (float)((double)num9 * (double)num15 - (double)num11 * (double)num13);
            float num22 = (float)((double)num9 * (double)num14 - (double)num10 * (double)num13);
            float num23 = (float)((double)num6 * (double)num17 - (double)num7 * (double)num18 + (double)num8 * (double)num19);
            float num24 = (float)-((double)num5 * (double)num17 - (double)num7 * (double)num20 + (double)num8 * (double)num21);
            float num25 = (float)((double)num5 * (double)num18 - (double)num6 * (double)num20 + (double)num8 * (double)num22);
            float num26 = (float)-((double)num5 * (double)num19 - (double)num6 * (double)num21 + (double)num7 * (double)num22);
            float num27 = (float)(1.0 / ((double)num1 * (double)num23 + (double)num2 * (double)num24 + (double)num3 * (double)num25 + (double)num4 * (double)num26));
            result.M11 = num23 * num27;
            result.M21 = num24 * num27;
            result.M31 = num25 * num27;
            result.M41 = num26 * num27;
            result.M12 = (float)-((double)num2 * (double)num17 - (double)num3 * (double)num18 + (double)num4 * (double)num19) * num27;
            result.M22 = (float)((double)num1 * (double)num17 - (double)num3 * (double)num20 + (double)num4 * (double)num21) * num27;
            result.M32 = (float)-((double)num1 * (double)num18 - (double)num2 * (double)num20 + (double)num4 * (double)num22) * num27;
            result.M42 = (float)((double)num1 * (double)num19 - (double)num2 * (double)num21 + (double)num3 * (double)num22) * num27;
            float num28 = (float)((double)num7 * (double)num16 - (double)num8 * (double)num15);
            float num29 = (float)((double)num6 * (double)num16 - (double)num8 * (double)num14);
            float num30 = (float)((double)num6 * (double)num15 - (double)num7 * (double)num14);
            float num31 = (float)((double)num5 * (double)num16 - (double)num8 * (double)num13);
            float num32 = (float)((double)num5 * (double)num15 - (double)num7 * (double)num13);
            float num33 = (float)((double)num5 * (double)num14 - (double)num6 * (double)num13);
            result.M13 = (float)((double)num2 * (double)num28 - (double)num3 * (double)num29 + (double)num4 * (double)num30) * num27;
            result.M23 = (float)-((double)num1 * (double)num28 - (double)num3 * (double)num31 + (double)num4 * (double)num32) * num27;
            result.M33 = (float)((double)num1 * (double)num29 - (double)num2 * (double)num31 + (double)num4 * (double)num33) * num27;
            result.M43 = (float)-((double)num1 * (double)num30 - (double)num2 * (double)num32 + (double)num3 * (double)num33) * num27;
            float num34 = (float)((double)num7 * (double)num12 - (double)num8 * (double)num11);
            float num35 = (float)((double)num6 * (double)num12 - (double)num8 * (double)num10);
            float num36 = (float)((double)num6 * (double)num11 - (double)num7 * (double)num10);
            float num37 = (float)((double)num5 * (double)num12 - (double)num8 * (double)num9);
            float num38 = (float)((double)num5 * (double)num11 - (double)num7 * (double)num9);
            float num39 = (float)((double)num5 * (double)num10 - (double)num6 * (double)num9);
            result.M14 = (float)-((double)num2 * (double)num34 - (double)num3 * (double)num35 + (double)num4 * (double)num36) * num27;
            result.M24 = (float)((double)num1 * (double)num34 - (double)num3 * (double)num37 + (double)num4 * (double)num38) * num27;
            result.M34 = (float)-((double)num1 * (double)num35 - (double)num2 * (double)num37 + (double)num4 * (double)num39) * num27;
            result.M44 = (float)((double)num1 * (double)num36 - (double)num2 * (double)num38 + (double)num3 * (double)num39) * num27;
        }

        /// <summary>
        /// Linearly interpolates between the corresponding values of two matrices.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param><param name="amount">Interpolation value.</param>
        public static Matrix Lerp(Matrix matrix1, Matrix matrix2, float amount)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
            matrix.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
            matrix.M13 = matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount;
            matrix.M14 = matrix1.M14 + (matrix2.M14 - matrix1.M14) * amount;
            matrix.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
            matrix.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
            matrix.M23 = matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount;
            matrix.M24 = matrix1.M24 + (matrix2.M24 - matrix1.M24) * amount;
            matrix.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
            matrix.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
            matrix.M33 = matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount;
            matrix.M34 = matrix1.M34 + (matrix2.M34 - matrix1.M34) * amount;
            matrix.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
            matrix.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
            matrix.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
            matrix.M44 = matrix1.M44 + (matrix2.M44 - matrix1.M44) * amount;
            return matrix;
        }

        /// <summary>
        /// Linearly interpolates between the corresponding values of two matrices.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param><param name="amount">Interpolation value.</param><param name="result">[OutAttribute] Resulting matrix.</param>
        public static void Lerp(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result)
        {
            result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
            result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
            result.M13 = matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount;
            result.M14 = matrix1.M14 + (matrix2.M14 - matrix1.M14) * amount;
            result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
            result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
            result.M23 = matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount;
            result.M24 = matrix1.M24 + (matrix2.M24 - matrix1.M24) * amount;
            result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
            result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
            result.M33 = matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount;
            result.M34 = matrix1.M34 + (matrix2.M34 - matrix1.M34) * amount;
            result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
            result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
            result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
            result.M44 = matrix1.M44 + (matrix2.M44 - matrix1.M44) * amount;
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation.
        /// </summary>
        public static void Slerp(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result)
        {
            Quaternion a, b, c;
            Quaternion.CreateFromRotationMatrix(ref matrix1, out a);
            Quaternion.CreateFromRotationMatrix(ref matrix2, out b);

            Quaternion.Slerp(ref a, ref b, amount, out c);
            Matrix.CreateFromQuaternion(ref c, out result);

            // Interpolate position
            result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
            result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
            result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation and scale.
        /// </summary>
        public static void SlerpScale(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result)
        {
            var scale1 = matrix1.Scale;
            var scale2 = matrix2.Scale;

            if (scale1.LengthSquared() < 0.000001f || scale2.LengthSquared() < 0.000001f)
            {
                result = Matrix.Zero;
                return;
            }

            var matrix1Normalized = Matrix.Normalize(matrix1);
            var matrix2Normalized = Matrix.Normalize(matrix2);


            Quaternion a, b, c;
            Quaternion.CreateFromRotationMatrix(ref matrix1Normalized, out a);
            Quaternion.CreateFromRotationMatrix(ref matrix2Normalized, out b);

            Quaternion.Slerp(ref a, ref b, amount, out c);
            Matrix.CreateFromQuaternion(ref c, out result);

            var scale = Vector3.Lerp(scale1, scale2, amount);
            Matrix.Rescale(ref result, ref scale);

            // Interpolate position
            result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
            result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
            result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation.
        /// </summary>
        public static void Slerp(Matrix matrix1, Matrix matrix2, float amount, out Matrix result)
        {
            Slerp(ref matrix1, ref matrix2, amount, out result);
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation.
        /// </summary>
        public static Matrix Slerp(Matrix matrix1, Matrix matrix2, float amount)
        {
            Matrix result;
            Slerp(ref matrix1, ref matrix2, amount, out result);
            return result;
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation and scale.
        /// </summary>
        public static void SlerpScale(Matrix matrix1, Matrix matrix2, float amount, out Matrix result)
        {
            SlerpScale(ref matrix1, ref matrix2, amount, out result);
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation and scale.
        /// </summary>
        public static Matrix SlerpScale(Matrix matrix1, Matrix matrix2, float amount)
        {
            Matrix result;
            SlerpScale(ref matrix1, ref matrix2, amount, out result);
            return result;
        }


        /// <summary>
        /// Negates individual elements of a matrix.
        /// </summary>
        /// <param name="matrix">Source matrix.</param>
        public static Matrix Negate(Matrix matrix)
        {
            Matrix matrix1;
            matrix1.M11 = -matrix.M11;
            matrix1.M12 = -matrix.M12;
            matrix1.M13 = -matrix.M13;
            matrix1.M14 = -matrix.M14;
            matrix1.M21 = -matrix.M21;
            matrix1.M22 = -matrix.M22;
            matrix1.M23 = -matrix.M23;
            matrix1.M24 = -matrix.M24;
            matrix1.M31 = -matrix.M31;
            matrix1.M32 = -matrix.M32;
            matrix1.M33 = -matrix.M33;
            matrix1.M34 = -matrix.M34;
            matrix1.M41 = -matrix.M41;
            matrix1.M42 = -matrix.M42;
            matrix1.M43 = -matrix.M43;
            matrix1.M44 = -matrix.M44;
            return matrix1;
        }

        /// <summary>
        /// Negates individual elements of a matrix.
        /// </summary>
        /// <param name="matrix">Source matrix.</param><param name="result">[OutAttribute] Negated matrix.</param>
        public static void Negate(ref Matrix matrix, out Matrix result)
        {
            result.M11 = -matrix.M11;
            result.M12 = -matrix.M12;
            result.M13 = -matrix.M13;
            result.M14 = -matrix.M14;
            result.M21 = -matrix.M21;
            result.M22 = -matrix.M22;
            result.M23 = -matrix.M23;
            result.M24 = -matrix.M24;
            result.M31 = -matrix.M31;
            result.M32 = -matrix.M32;
            result.M33 = -matrix.M33;
            result.M34 = -matrix.M34;
            result.M41 = -matrix.M41;
            result.M42 = -matrix.M42;
            result.M43 = -matrix.M43;
            result.M44 = -matrix.M44;
        }

        /// <summary>
        /// Adds a matrix to another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param>
        public static Matrix Add(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 + matrix2.M11;
            matrix.M12 = matrix1.M12 + matrix2.M12;
            matrix.M13 = matrix1.M13 + matrix2.M13;
            matrix.M14 = matrix1.M14 + matrix2.M14;
            matrix.M21 = matrix1.M21 + matrix2.M21;
            matrix.M22 = matrix1.M22 + matrix2.M22;
            matrix.M23 = matrix1.M23 + matrix2.M23;
            matrix.M24 = matrix1.M24 + matrix2.M24;
            matrix.M31 = matrix1.M31 + matrix2.M31;
            matrix.M32 = matrix1.M32 + matrix2.M32;
            matrix.M33 = matrix1.M33 + matrix2.M33;
            matrix.M34 = matrix1.M34 + matrix2.M34;
            matrix.M41 = matrix1.M41 + matrix2.M41;
            matrix.M42 = matrix1.M42 + matrix2.M42;
            matrix.M43 = matrix1.M43 + matrix2.M43;
            matrix.M44 = matrix1.M44 + matrix2.M44;
            return matrix;
        }

        /// <summary>
        /// Adds a matrix to another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param><param name="result">[OutAttribute] Resulting matrix.</param>
        public static void Add(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            result.M11 = matrix1.M11 + matrix2.M11;
            result.M12 = matrix1.M12 + matrix2.M12;
            result.M13 = matrix1.M13 + matrix2.M13;
            result.M14 = matrix1.M14 + matrix2.M14;
            result.M21 = matrix1.M21 + matrix2.M21;
            result.M22 = matrix1.M22 + matrix2.M22;
            result.M23 = matrix1.M23 + matrix2.M23;
            result.M24 = matrix1.M24 + matrix2.M24;
            result.M31 = matrix1.M31 + matrix2.M31;
            result.M32 = matrix1.M32 + matrix2.M32;
            result.M33 = matrix1.M33 + matrix2.M33;
            result.M34 = matrix1.M34 + matrix2.M34;
            result.M41 = matrix1.M41 + matrix2.M41;
            result.M42 = matrix1.M42 + matrix2.M42;
            result.M43 = matrix1.M43 + matrix2.M43;
            result.M44 = matrix1.M44 + matrix2.M44;
        }

        /// <summary>
        /// Subtracts matrices.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param>
        public static Matrix Subtract(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 - matrix2.M11;
            matrix.M12 = matrix1.M12 - matrix2.M12;
            matrix.M13 = matrix1.M13 - matrix2.M13;
            matrix.M14 = matrix1.M14 - matrix2.M14;
            matrix.M21 = matrix1.M21 - matrix2.M21;
            matrix.M22 = matrix1.M22 - matrix2.M22;
            matrix.M23 = matrix1.M23 - matrix2.M23;
            matrix.M24 = matrix1.M24 - matrix2.M24;
            matrix.M31 = matrix1.M31 - matrix2.M31;
            matrix.M32 = matrix1.M32 - matrix2.M32;
            matrix.M33 = matrix1.M33 - matrix2.M33;
            matrix.M34 = matrix1.M34 - matrix2.M34;
            matrix.M41 = matrix1.M41 - matrix2.M41;
            matrix.M42 = matrix1.M42 - matrix2.M42;
            matrix.M43 = matrix1.M43 - matrix2.M43;
            matrix.M44 = matrix1.M44 - matrix2.M44;
            return matrix;
        }

        /// <summary>
        /// Subtracts matrices.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param><param name="result">[OutAttribute] Result of the subtraction.</param>
        public static void Subtract(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            result.M11 = matrix1.M11 - matrix2.M11;
            result.M12 = matrix1.M12 - matrix2.M12;
            result.M13 = matrix1.M13 - matrix2.M13;
            result.M14 = matrix1.M14 - matrix2.M14;
            result.M21 = matrix1.M21 - matrix2.M21;
            result.M22 = matrix1.M22 - matrix2.M22;
            result.M23 = matrix1.M23 - matrix2.M23;
            result.M24 = matrix1.M24 - matrix2.M24;
            result.M31 = matrix1.M31 - matrix2.M31;
            result.M32 = matrix1.M32 - matrix2.M32;
            result.M33 = matrix1.M33 - matrix2.M33;
            result.M34 = matrix1.M34 - matrix2.M34;
            result.M41 = matrix1.M41 - matrix2.M41;
            result.M42 = matrix1.M42 - matrix2.M42;
            result.M43 = matrix1.M43 - matrix2.M43;
            result.M44 = matrix1.M44 - matrix2.M44;
        }

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param>
        public static Matrix Multiply(Matrix matrix1, Matrix matrix2)
        {
#if NATIVE_SUPPORT
            Matrix result;
            Multiply_Native(ref matrix1, ref matrix2, out result);
            return result;
#else
            Matrix matrix;
            matrix.M11 = (float)((double)matrix1.M11 * (double)matrix2.M11 + (double)matrix1.M12 * (double)matrix2.M21 + (double)matrix1.M13 * (double)matrix2.M31 + (double)matrix1.M14 * (double)matrix2.M41);
            matrix.M12 = (float)((double)matrix1.M11 * (double)matrix2.M12 + (double)matrix1.M12 * (double)matrix2.M22 + (double)matrix1.M13 * (double)matrix2.M32 + (double)matrix1.M14 * (double)matrix2.M42);
            matrix.M13 = (float)((double)matrix1.M11 * (double)matrix2.M13 + (double)matrix1.M12 * (double)matrix2.M23 + (double)matrix1.M13 * (double)matrix2.M33 + (double)matrix1.M14 * (double)matrix2.M43);
            matrix.M14 = (float)((double)matrix1.M11 * (double)matrix2.M14 + (double)matrix1.M12 * (double)matrix2.M24 + (double)matrix1.M13 * (double)matrix2.M34 + (double)matrix1.M14 * (double)matrix2.M44);
            matrix.M21 = (float)((double)matrix1.M21 * (double)matrix2.M11 + (double)matrix1.M22 * (double)matrix2.M21 + (double)matrix1.M23 * (double)matrix2.M31 + (double)matrix1.M24 * (double)matrix2.M41);
            matrix.M22 = (float)((double)matrix1.M21 * (double)matrix2.M12 + (double)matrix1.M22 * (double)matrix2.M22 + (double)matrix1.M23 * (double)matrix2.M32 + (double)matrix1.M24 * (double)matrix2.M42);
            matrix.M23 = (float)((double)matrix1.M21 * (double)matrix2.M13 + (double)matrix1.M22 * (double)matrix2.M23 + (double)matrix1.M23 * (double)matrix2.M33 + (double)matrix1.M24 * (double)matrix2.M43);
            matrix.M24 = (float)((double)matrix1.M21 * (double)matrix2.M14 + (double)matrix1.M22 * (double)matrix2.M24 + (double)matrix1.M23 * (double)matrix2.M34 + (double)matrix1.M24 * (double)matrix2.M44);
            matrix.M31 = (float)((double)matrix1.M31 * (double)matrix2.M11 + (double)matrix1.M32 * (double)matrix2.M21 + (double)matrix1.M33 * (double)matrix2.M31 + (double)matrix1.M34 * (double)matrix2.M41);
            matrix.M32 = (float)((double)matrix1.M31 * (double)matrix2.M12 + (double)matrix1.M32 * (double)matrix2.M22 + (double)matrix1.M33 * (double)matrix2.M32 + (double)matrix1.M34 * (double)matrix2.M42);
            matrix.M33 = (float)((double)matrix1.M31 * (double)matrix2.M13 + (double)matrix1.M32 * (double)matrix2.M23 + (double)matrix1.M33 * (double)matrix2.M33 + (double)matrix1.M34 * (double)matrix2.M43);
            matrix.M34 = (float)((double)matrix1.M31 * (double)matrix2.M14 + (double)matrix1.M32 * (double)matrix2.M24 + (double)matrix1.M33 * (double)matrix2.M34 + (double)matrix1.M34 * (double)matrix2.M44);
            matrix.M41 = (float)((double)matrix1.M41 * (double)matrix2.M11 + (double)matrix1.M42 * (double)matrix2.M21 + (double)matrix1.M43 * (double)matrix2.M31 + (double)matrix1.M44 * (double)matrix2.M41);
            matrix.M42 = (float)((double)matrix1.M41 * (double)matrix2.M12 + (double)matrix1.M42 * (double)matrix2.M22 + (double)matrix1.M43 * (double)matrix2.M32 + (double)matrix1.M44 * (double)matrix2.M42);
            matrix.M43 = (float)((double)matrix1.M41 * (double)matrix2.M13 + (double)matrix1.M42 * (double)matrix2.M23 + (double)matrix1.M43 * (double)matrix2.M33 + (double)matrix1.M44 * (double)matrix2.M43);
            matrix.M44 = (float)((double)matrix1.M41 * (double)matrix2.M14 + (double)matrix1.M42 * (double)matrix2.M24 + (double)matrix1.M43 * (double)matrix2.M34 + (double)matrix1.M44 * (double)matrix2.M44);
            return matrix;
#endif
        }

        // Matrix multiplication.  The result represents the transformation M2
        // followed by the transformation M1.  (Out = M1 * M2)
        //D3DXMATRIX* WINAPI D3DXMatrixMultiply
        //  ( D3DXMATRIX *pOut, CONST D3DXMATRIX *pM1, CONST D3DXMATRIX *pM2 );

        /// <summary>Native Interop Function</summary>
#if NATIVE_SUPPORT
        [DllImport("d3dx9_43.dll", EntryPoint = "D3DXMatrixMultiply", CallingConvention = CallingConvention.StdCall, SetLastError = false, PreserveSig = true), SuppressUnmanagedCodeSecurityAttribute]
        private unsafe extern static Matrix* D3DXMatrixMultiply_([Out] Matrix* pOut, [In] Matrix* pM1, [In] Matrix* pM2);

        public static void Multiply_Native(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            unsafe
            {
                fixed (Matrix* resultRef_ = &result)
                fixed (Matrix* m1Ref_ = &matrix1)
                fixed (Matrix* m2Ref_ = &matrix2)

                    D3DXMatrixMultiply_(resultRef_, m1Ref_, m2Ref_);
            }
        }
#endif
        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">Source matrix.</param><param name="result">[OutAttribute] Result of the multiplication.</param>
        public static void Multiply(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
#if NATIVE_SUPPORT
            Multiply_Native(ref matrix1, ref matrix2, out result);
#else
            float num1 = (float)((double)matrix1.M11 * (double)matrix2.M11 + (double)matrix1.M12 * (double)matrix2.M21 + (double)matrix1.M13 * (double)matrix2.M31 + (double)matrix1.M14 * (double)matrix2.M41);
            float num2 = (float)((double)matrix1.M11 * (double)matrix2.M12 + (double)matrix1.M12 * (double)matrix2.M22 + (double)matrix1.M13 * (double)matrix2.M32 + (double)matrix1.M14 * (double)matrix2.M42);
            float num3 = (float)((double)matrix1.M11 * (double)matrix2.M13 + (double)matrix1.M12 * (double)matrix2.M23 + (double)matrix1.M13 * (double)matrix2.M33 + (double)matrix1.M14 * (double)matrix2.M43);
            float num4 = (float)((double)matrix1.M11 * (double)matrix2.M14 + (double)matrix1.M12 * (double)matrix2.M24 + (double)matrix1.M13 * (double)matrix2.M34 + (double)matrix1.M14 * (double)matrix2.M44);
            float num5 = (float)((double)matrix1.M21 * (double)matrix2.M11 + (double)matrix1.M22 * (double)matrix2.M21 + (double)matrix1.M23 * (double)matrix2.M31 + (double)matrix1.M24 * (double)matrix2.M41);
            float num6 = (float)((double)matrix1.M21 * (double)matrix2.M12 + (double)matrix1.M22 * (double)matrix2.M22 + (double)matrix1.M23 * (double)matrix2.M32 + (double)matrix1.M24 * (double)matrix2.M42);
            float num7 = (float)((double)matrix1.M21 * (double)matrix2.M13 + (double)matrix1.M22 * (double)matrix2.M23 + (double)matrix1.M23 * (double)matrix2.M33 + (double)matrix1.M24 * (double)matrix2.M43);
            float num8 = (float)((double)matrix1.M21 * (double)matrix2.M14 + (double)matrix1.M22 * (double)matrix2.M24 + (double)matrix1.M23 * (double)matrix2.M34 + (double)matrix1.M24 * (double)matrix2.M44);
            float num9 = (float)((double)matrix1.M31 * (double)matrix2.M11 + (double)matrix1.M32 * (double)matrix2.M21 + (double)matrix1.M33 * (double)matrix2.M31 + (double)matrix1.M34 * (double)matrix2.M41);
            float num10 = (float)((double)matrix1.M31 * (double)matrix2.M12 + (double)matrix1.M32 * (double)matrix2.M22 + (double)matrix1.M33 * (double)matrix2.M32 + (double)matrix1.M34 * (double)matrix2.M42);
            float num11 = (float)((double)matrix1.M31 * (double)matrix2.M13 + (double)matrix1.M32 * (double)matrix2.M23 + (double)matrix1.M33 * (double)matrix2.M33 + (double)matrix1.M34 * (double)matrix2.M43);
            float num12 = (float)((double)matrix1.M31 * (double)matrix2.M14 + (double)matrix1.M32 * (double)matrix2.M24 + (double)matrix1.M33 * (double)matrix2.M34 + (double)matrix1.M34 * (double)matrix2.M44);
            float num13 = (float)((double)matrix1.M41 * (double)matrix2.M11 + (double)matrix1.M42 * (double)matrix2.M21 + (double)matrix1.M43 * (double)matrix2.M31 + (double)matrix1.M44 * (double)matrix2.M41);
            float num14 = (float)((double)matrix1.M41 * (double)matrix2.M12 + (double)matrix1.M42 * (double)matrix2.M22 + (double)matrix1.M43 * (double)matrix2.M32 + (double)matrix1.M44 * (double)matrix2.M42);
            float num15 = (float)((double)matrix1.M41 * (double)matrix2.M13 + (double)matrix1.M42 * (double)matrix2.M23 + (double)matrix1.M43 * (double)matrix2.M33 + (double)matrix1.M44 * (double)matrix2.M43);
            float num16 = (float)((double)matrix1.M41 * (double)matrix2.M14 + (double)matrix1.M42 * (double)matrix2.M24 + (double)matrix1.M43 * (double)matrix2.M34 + (double)matrix1.M44 * (double)matrix2.M44);
            result.M11 = num1;
            result.M12 = num2;
            result.M13 = num3;
            result.M14 = num4;
            result.M21 = num5;
            result.M22 = num6;
            result.M23 = num7;
            result.M24 = num8;
            result.M31 = num9;
            result.M32 = num10;
            result.M33 = num11;
            result.M34 = num12;
            result.M41 = num13;
            result.M42 = num14;
            result.M43 = num15;
            result.M44 = num16;
#endif
        }

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="scaleFactor">Scalar value.</param>
        public static Matrix Multiply(Matrix matrix1, float scaleFactor)
        {
            float num = scaleFactor;
            Matrix matrix;
            matrix.M11 = matrix1.M11 * num;
            matrix.M12 = matrix1.M12 * num;
            matrix.M13 = matrix1.M13 * num;
            matrix.M14 = matrix1.M14 * num;
            matrix.M21 = matrix1.M21 * num;
            matrix.M22 = matrix1.M22 * num;
            matrix.M23 = matrix1.M23 * num;
            matrix.M24 = matrix1.M24 * num;
            matrix.M31 = matrix1.M31 * num;
            matrix.M32 = matrix1.M32 * num;
            matrix.M33 = matrix1.M33 * num;
            matrix.M34 = matrix1.M34 * num;
            matrix.M41 = matrix1.M41 * num;
            matrix.M42 = matrix1.M42 * num;
            matrix.M43 = matrix1.M43 * num;
            matrix.M44 = matrix1.M44 * num;
            return matrix;
        }

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="scaleFactor">Scalar value.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Matrix matrix1, float scaleFactor, out Matrix result)
        {
            float num = scaleFactor;
            result.M11 = matrix1.M11 * num;
            result.M12 = matrix1.M12 * num;
            result.M13 = matrix1.M13 * num;
            result.M14 = matrix1.M14 * num;
            result.M21 = matrix1.M21 * num;
            result.M22 = matrix1.M22 * num;
            result.M23 = matrix1.M23 * num;
            result.M24 = matrix1.M24 * num;
            result.M31 = matrix1.M31 * num;
            result.M32 = matrix1.M32 * num;
            result.M33 = matrix1.M33 * num;
            result.M34 = matrix1.M34 * num;
            result.M41 = matrix1.M41 * num;
            result.M42 = matrix1.M42 * num;
            result.M43 = matrix1.M43 * num;
            result.M44 = matrix1.M44 * num;
        }

        /// <summary>
        /// Divides the components of a matrix by the corresponding components of another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">The divisor.</param>
        public static Matrix Divide(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 / matrix2.M11;
            matrix.M12 = matrix1.M12 / matrix2.M12;
            matrix.M13 = matrix1.M13 / matrix2.M13;
            matrix.M14 = matrix1.M14 / matrix2.M14;
            matrix.M21 = matrix1.M21 / matrix2.M21;
            matrix.M22 = matrix1.M22 / matrix2.M22;
            matrix.M23 = matrix1.M23 / matrix2.M23;
            matrix.M24 = matrix1.M24 / matrix2.M24;
            matrix.M31 = matrix1.M31 / matrix2.M31;
            matrix.M32 = matrix1.M32 / matrix2.M32;
            matrix.M33 = matrix1.M33 / matrix2.M33;
            matrix.M34 = matrix1.M34 / matrix2.M34;
            matrix.M41 = matrix1.M41 / matrix2.M41;
            matrix.M42 = matrix1.M42 / matrix2.M42;
            matrix.M43 = matrix1.M43 / matrix2.M43;
            matrix.M44 = matrix1.M44 / matrix2.M44;
            return matrix;
        }

        /// <summary>
        /// Divides the components of a matrix by the corresponding components of another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="matrix2">The divisor.</param><param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            result.M11 = matrix1.M11 / matrix2.M11;
            result.M12 = matrix1.M12 / matrix2.M12;
            result.M13 = matrix1.M13 / matrix2.M13;
            result.M14 = matrix1.M14 / matrix2.M14;
            result.M21 = matrix1.M21 / matrix2.M21;
            result.M22 = matrix1.M22 / matrix2.M22;
            result.M23 = matrix1.M23 / matrix2.M23;
            result.M24 = matrix1.M24 / matrix2.M24;
            result.M31 = matrix1.M31 / matrix2.M31;
            result.M32 = matrix1.M32 / matrix2.M32;
            result.M33 = matrix1.M33 / matrix2.M33;
            result.M34 = matrix1.M34 / matrix2.M34;
            result.M41 = matrix1.M41 / matrix2.M41;
            result.M42 = matrix1.M42 / matrix2.M42;
            result.M43 = matrix1.M43 / matrix2.M43;
            result.M44 = matrix1.M44 / matrix2.M44;
        }

        /// <summary>
        /// Divides the components of a matrix by a scalar.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="divider">The divisor.</param>
        public static Matrix Divide(Matrix matrix1, float divider)
        {
            float num = 1f / divider;
            Matrix matrix;
            matrix.M11 = matrix1.M11 * num;
            matrix.M12 = matrix1.M12 * num;
            matrix.M13 = matrix1.M13 * num;
            matrix.M14 = matrix1.M14 * num;
            matrix.M21 = matrix1.M21 * num;
            matrix.M22 = matrix1.M22 * num;
            matrix.M23 = matrix1.M23 * num;
            matrix.M24 = matrix1.M24 * num;
            matrix.M31 = matrix1.M31 * num;
            matrix.M32 = matrix1.M32 * num;
            matrix.M33 = matrix1.M33 * num;
            matrix.M34 = matrix1.M34 * num;
            matrix.M41 = matrix1.M41 * num;
            matrix.M42 = matrix1.M42 * num;
            matrix.M43 = matrix1.M43 * num;
            matrix.M44 = matrix1.M44 * num;
            return matrix;
        }

        /// <summary>
        /// Divides the components of a matrix by a scalar.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param><param name="divider">The divisor.</param><param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Matrix matrix1, float divider, out Matrix result)
        {
            float num = 1f / divider;
            result.M11 = matrix1.M11 * num;
            result.M12 = matrix1.M12 * num;
            result.M13 = matrix1.M13 * num;
            result.M14 = matrix1.M14 * num;
            result.M21 = matrix1.M21 * num;
            result.M22 = matrix1.M22 * num;
            result.M23 = matrix1.M23 * num;
            result.M24 = matrix1.M24 * num;
            result.M31 = matrix1.M31 * num;
            result.M32 = matrix1.M32 * num;
            result.M33 = matrix1.M33 * num;
            result.M34 = matrix1.M34 * num;
            result.M41 = matrix1.M41 * num;
            result.M42 = matrix1.M42 * num;
            result.M43 = matrix1.M43 * num;
            result.M44 = matrix1.M44 * num;
        }

        /// <summary>
        /// Gets the orientation.
        /// </summary>
        /// <returns></returns>
        public Matrix GetOrientation()
        {
            Matrix orientation = Matrix.Identity;
            orientation.Forward = Forward;
            orientation.Up = Up;
            orientation.Right = Right;
            return orientation;
        }

        [Conditional("DEBUG")]
        public void AssertIsValid()
        {
            Debug.Assert(IsValid());
        }

        public bool IsValid()
        {
            return (M11 + M12 + M13 + M14 + M21 + M22 + M23 + M24 + M31 + M32 + M33 + M34 + M41 + M42 + M43 + M44).IsValid();
        }

        public bool IsNan()
        {
            return float.IsNaN(M11 + M12 + M13 + M14 + M21 + M22 + M23 + M24 + M31 + M32 + M33 + M34 + M41 + M42 + M43 + M44);
        }

        public bool IsRotation()
        {
            float epsilon = 0.01f;

            if (!HasNoTranslationOrPerspective()) return false;

            if (Math.Abs(Right.Dot(Up)) > epsilon) return false;
            if (Math.Abs(Right.Dot(Backward)) > epsilon) return false;
            if (Math.Abs(Up.Dot(Backward)) > epsilon) return false;
            if (Math.Abs(Right.LengthSquared() - 1) > epsilon) return false;
            if (Math.Abs(Up.LengthSquared() - 1) > epsilon) return false;
            if (Math.Abs(Backward.LengthSquared() - 1) > epsilon) return false;

            return true;
        }

        /// <summary>
        /// Returns true if this matrix represents invertible (you can call Invert on it) linear (it does not contain translation or perspective transformation) transformation.
        /// Such matrix consist solely of rotations, shearing, mirroring and scaling. It can be orthogonalized to create an orthogonal rotation matrix.
        /// </summary>
        public bool HasNoTranslationOrPerspective()
        {
            float epsilon = 0.0001f;

            float shouldBeZero = M41 + M42 + M43 + M34 + M24 + M14;
            if (shouldBeZero > epsilon) return false;
            if (Math.Abs(M44 - 1.0f) > epsilon) return false;

            return true;
        }

        //Creates rotation matrix from direction (dir must be normalized)
        public static Matrix CreateFromDir(Vector3 dir)
        {
            Vector3 right = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 up;
            float d = dir.Z;

            if (d > -0.99999 && d < 0.99999)
            { // to avoid problems with normalize in special cases		
                right = right - dir * d;
                right = Vector3.Normalize(right);
                up = Vector3.Cross(dir, right);
            }
            else
            { //dir lies with z axis
                right = new Vector3(dir.Z, 0, -dir.X);
                up = new Vector3(0, 1, 0);
            };

            Matrix m = Matrix.Identity;
            m.Right = right;
            m.Up = up;
            m.Forward = dir;

            return m;
        }

        public static Matrix CreateFromDir(Vector3 dir, Vector3 suggestedUp)
        {
            Vector3 right = Vector3.Cross(dir, suggestedUp);
            Vector3 newUp = Vector3.Cross(right, dir);

            return Matrix.CreateWorld(Vector3.Zero, dir, newUp);
        }

        public static Matrix Normalize(Matrix matrix)
        {
            Matrix m = matrix;
            m.Right = Vector3.Normalize(m.Right);
            m.Up = Vector3.Normalize(m.Up);
            m.Forward = Vector3.Normalize(m.Forward);
            Debug.Assert(!m.IsNan());
            return m;
        }

        public static Matrix Orthogonalize(Matrix rotationMatrix)
        {
            Debug.Assert(rotationMatrix.HasNoTranslationOrPerspective());

            Matrix m = rotationMatrix;
            m.Right = Vector3.Normalize(m.Right);
            m.Up = Vector3.Normalize(m.Up - m.Right * m.Up.Dot(m.Right));
            m.Backward = Vector3.Normalize(m.Backward - m.Right * m.Backward.Dot(m.Right) - m.Up * m.Backward.Dot(m.Up));

            Debug.Assert(m.IsRotation());

            return m;
        }

        public static Matrix Round(ref Matrix matrix)
        {
            Matrix m = matrix;
            m.Right = Vector3I.Round(m.Right);
            m.Up = Vector3I.Round(m.Up);
            m.Forward = Vector3I.Round(m.Forward);
            return m;
        }

        // Rotates the first matrix so that it's coordinate axes are parallel or perpendicular to their
        // counterparts in the second matrix. Furthemore, the rotated matrix is the closest (measured by
        // rotation difference) to the first axis among such matrices.
        public static Matrix AlignRotationToAxes(ref Matrix toAlign, ref Matrix axisDefinitionMatrix)
        {
            Debug.Assert(toAlign.IsRotation());
            Debug.Assert(axisDefinitionMatrix.IsRotation());

            Matrix retval = Matrix.Identity;

            bool right = false;
            bool up = false;
            bool backward = false;

            // Rotate the right vector
            float dotRight = toAlign.Right.Dot(axisDefinitionMatrix.Right);
            float dotUp = toAlign.Right.Dot(axisDefinitionMatrix.Up);
            float dotBackward = toAlign.Right.Dot(axisDefinitionMatrix.Backward);
            if (Math.Abs(dotRight) > Math.Abs(dotUp))
            {
                if (Math.Abs(dotRight) > Math.Abs(dotBackward))
                {
                    retval.Right = dotRight > 0 ? axisDefinitionMatrix.Right : axisDefinitionMatrix.Left;
                    right = true;
                }
                else
                {
                    retval.Right = dotBackward > 0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                    backward = true;
                }
            }
            else
            {
                if (Math.Abs(dotUp) > Math.Abs(dotBackward))
                {
                    retval.Right = dotUp > 0 ? axisDefinitionMatrix.Up : axisDefinitionMatrix.Down;
                    up = true;
                }
                else
                {
                    retval.Right = dotBackward > 0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                    backward = true;
                }
            }

            // Rotate the up vector
            dotRight = toAlign.Up.Dot(axisDefinitionMatrix.Right);
            dotUp = toAlign.Up.Dot(axisDefinitionMatrix.Up);
            dotBackward = toAlign.Up.Dot(axisDefinitionMatrix.Backward);
            if (up || (Math.Abs(dotRight) > Math.Abs(dotUp) && !right))
            {
                if (Math.Abs(dotRight) > Math.Abs(dotBackward) || backward)
                {
                    retval.Up = dotRight > 0 ? axisDefinitionMatrix.Right : axisDefinitionMatrix.Left;
                    right = true;
                }
                else
                {
                    retval.Up = dotBackward > 0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                    backward = true;
                }
            }
            else
            {
                if (Math.Abs(dotUp) > Math.Abs(dotBackward) || backward)
                {
                    retval.Up = dotUp > 0 ? axisDefinitionMatrix.Up : axisDefinitionMatrix.Down;
                    up = true;
                }
                else
                {
                    retval.Up = dotBackward > 0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                    backward = true;
                }
            }

            // The backward vector will be rotated to the remaining axis
            if (right == false)
            {
                dotRight = toAlign.Backward.Dot(axisDefinitionMatrix.Right);
                retval.Backward = dotRight > 0 ? axisDefinitionMatrix.Right : axisDefinitionMatrix.Left;
            }
            else if (up == false)
            {
                dotUp = toAlign.Backward.Dot(axisDefinitionMatrix.Up);
                retval.Backward = dotUp > 0 ? axisDefinitionMatrix.Up : axisDefinitionMatrix.Down;
            }
            else
            {
                Debug.Assert(backward == false);
                dotBackward = toAlign.Backward.Dot(axisDefinitionMatrix.Backward);
                retval.Backward = dotBackward > 0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
            }

            Debug.Assert(retval.IsRotation());
            return retval;
        }

        public static bool GetEulerAnglesXYZ(ref Matrix mat, out Vector3 xyz)
        {
            //	// rot =  cy*cz          -cy*sz           sy
            //	//        cz*sx*sy+cx*sz  cx*cz-sx*sy*sz -cy*sx
            //	//       -cx*cz*sy+sx*sz  cz*sx+cx*sy*sz  cx*cy
            //

            float matElem0 = mat.GetRow(0).X;// MathUtil.GetMatrixElem(ref mat, 0);
            float matElem1 = mat.GetRow(0).Y;// MathUtil.GetMatrixElem(ref mat, 1);
            float matElem2 = mat.GetRow(0).Z;// MathUtil.GetMatrixElem(ref mat, 2);
            float matElem3 = mat.GetRow(1).X;// MathUtil.GetMatrixElem(ref mat, 3);
            float matElem4 = mat.GetRow(1).Y;// MathUtil.GetMatrixElem(ref mat, 4);
            float matElem5 = mat.GetRow(1).Z;// MathUtil.GetMatrixElem(ref mat, 5);
            float matElem6 = mat.GetRow(2).X;// MathUtil.GetMatrixElem(ref mat, 6);
            float matElem7 = mat.GetRow(2).Y;// MathUtil.GetMatrixElem(ref mat, 7);
            float matElem8 = mat.GetRow(2).Z;// MathUtil.GetMatrixElem(ref mat, 8);

            float fi = matElem2;
            if (fi < 1.0f)
            {
                if (fi > -1.0f)
                {
                    xyz = new Vector3(
                        (float)Math.Atan2(-matElem5, matElem8),
                        (float)Math.Asin(matElem2),
                        (float)Math.Atan2(-matElem1, matElem0));
                    return true;
                }
                else
                {
                    // WARNING.  Not unique.  XA - ZA = -atan2(r10,r11)
                    xyz = new Vector3(
                        (float)-Math.Atan2(matElem3, matElem4),
                        -MathHelper.PiOver2,
                        0f);
                    return false;
                }
            }
            else
            {
                // WARNING.  Not unique.  XAngle + ZAngle = atan2(r10,r11)
                xyz = new Vector3(
                    (float)Math.Atan2(matElem3, matElem4),
                    -MathHelper.PiOver2,
                    0.0f);
            }
            return false;
        }

        //public static Matrix SwapYZCoordinates(Matrix m)
        //{ 
        //    Matrix m2 = m; 
        //    Vector3 right = m.Right;
        //    Vector3 up = m.Up;
        //    Vector3 forward = m.Forward;

        //    m2.Right = new Vector3(right.X, right.Z, -right.Y);
        //    m2.Up = new Vector3(forward.X, forward.Z, -forward.Y);
        //    m2.Forward = new Vector3(-up.X, -up.Z, up.Y);
        //    Vector3 t2 = m.Translation;
        //    m2.Translation = Vector3.SwapYZCoordinates(t2);
        //    return m2;
        //}

        public static Matrix SwapYZCoordinates(Matrix m)
        {
            return m * Matrix.CreateRotationX(MathHelper.ToRadians(-90));
        }

        public bool IsMirrored()
        {
            return Determinant() < 0f;
        }

        //public static explicit operator MatrixD(Matrix m)
        //{
        //    return new MatrixD(
        //        (double)m.M11, (double)m.M12, (double)m.M13, (double)m.M14,
        //        (double)m.M21, (double)m.M22, (double)m.M23, (double)m.M24,
        //        (double)m.M31, (double)m.M32, (double)m.M33, (double)m.M34,
        //        (double)m.M41, (double)m.M42, (double)m.M43, (double)m.M44);
        //}

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public object GetMemberData(MemberInfo m)
        {
            if (m.Name.Length > 1)
            {
                if (m.Name[0] != 'M')
                {
                    if (m.Name == "Up")
                        return Up;
                    if (m.Name == "Down")
                        return Down;
                    if (m.Name == "Right")
                        return Right;
                    if (m.Name == "Left")
                        return Left;
                    if (m.Name == "Forward")
                        return Forward;
                    if (m.Name == "Backward")
                        return Backward;
                    if (m.Name == "Translation")
                        return Translation;
                }
                else
                {
                    if (m.Name.Length > 2)
                    {
                        if (m.Name[1] == '1')
                        {
                            if (m.Name[2] == '1')
                                return M11;
                            if (m.Name[2] == '2')
                                return M12;
                            if (m.Name[2] == '3')
                                return M13;
                            if (m.Name[2] == '4')
                                return M14;
                        }
                        if (m.Name[1] == '2')
                        {
                            if (m.Name[2] == '1')
                                return M21;
                            if (m.Name[2] == '2')
                                return M22;
                            if (m.Name[2] == '3')
                                return M23;
                            if (m.Name[2] == '4')
                                return M24;
                        }
                        if (m.Name[1] == '3')
                        {
                            if (m.Name[2] == '1')
                                return M31;
                            if (m.Name[2] == '2')
                                return M32;
                            if (m.Name[2] == '3')
                                return M33;
                            if (m.Name[2] == '4')
                                return M34;
                        }
                        if (m.Name[1] == '4')
                        {
                            if (m.Name[2] == '1')
                                return M41;
                            if (m.Name[2] == '2')
                                return M42;
                            if (m.Name[2] == '3')
                                return M43;
                            if (m.Name[2] == '4')
                                return M44;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
            return null;
        }
#endif // XB1
    }

}
