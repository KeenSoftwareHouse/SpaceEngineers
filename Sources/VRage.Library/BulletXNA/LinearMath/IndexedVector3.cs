using System;
using System.Diagnostics;

namespace BulletXNA.LinearMath
{
    public struct IndexedVector3
    {
        public IndexedVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public IndexedVector3(float x)
        {
            X = x;
            Y = x;
            Z = x;
        }

        public IndexedVector3(IndexedVector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public static IndexedVector3 operator +(IndexedVector3 value1, IndexedVector3 value2)
        {
            IndexedVector3 vector;
            vector.X = value1.X + value2.X;
            vector.Y = value1.Y + value2.Y;
            vector.Z = value1.Z + value2.Z;
            return vector;
        }

        public static IndexedVector3 operator -(IndexedVector3 value1, IndexedVector3 value2)
        {
            IndexedVector3 vector;
            vector.X = value1.X - value2.X;
            vector.Y = value1.Y - value2.Y;
            vector.Z = value1.Z - value2.Z;
            return vector;
        }

        public static IndexedVector3 operator *(IndexedVector3 value, float scaleFactor)
        {
            IndexedVector3 vector;
            vector.X = value.X * scaleFactor;
            vector.Y = value.Y * scaleFactor;
            vector.Z = value.Z * scaleFactor;
            return vector;
        }

        public static IndexedVector3 operator *(float scaleFactor, IndexedVector3 value)
        {
            IndexedVector3 vector;
            vector.X = value.X * scaleFactor;
            vector.Y = value.Y * scaleFactor;
            vector.Z = value.Z * scaleFactor;
            return vector;
        }

        public static IndexedVector3 operator -(IndexedVector3 value)
        {
            IndexedVector3 vector;
            vector.X = -value.X;
            vector.Y = -value.Y;
            vector.Z = -value.Z;
            return vector;
        }

        public static IndexedVector3 operator *(IndexedVector3 value1, IndexedVector3 value2)
        {
            IndexedVector3 vector;
            vector.X = value1.X * value2.X;
            vector.Y = value1.Y * value2.Y;
            vector.Z = value1.Z * value2.Z;
            return vector;
        }

        public static IndexedVector3 operator /(IndexedVector3 value1, IndexedVector3 value2)
        {
            IndexedVector3 vector;
            vector.X = value1.X / value2.X;
            vector.Y = value1.Y / value2.Y;
            vector.Z = value1.Z / value2.Z;
            return vector;
        }

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case (0): return X;
                    case (1): return Y;
                    case (2): return Z;
                    default:
                        {
                            Debug.Assert(false);
                            return 0.0f;
                        }
                }
            }
            set
            {
                switch (i)
                {
                    case (0): X = value; break;
                    case (1): Y = value; break;
                    case (2): Z = value; break;
                    default:
                        {
                            Debug.Assert(false);
                            break;
                        }
                }
            }
        }

        public bool Equals(IndexedVector3 other)
        {
            if (this.X == other.X && this.Y == other.Y)
                return this.Z == other.Z;
            else
                return false;
        }

        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is IndexedVector3)
                flag = this.Equals((IndexedVector3)obj);
            return flag;
        }

        public static IndexedVector3 Zero
        {
            get
            {
                return IndexedVector3._zero;
            }
        }

        public float Dot(ref IndexedVector3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        public float Dot(IndexedVector3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Z.GetHashCode();
                return result;
            }
        }

        private static IndexedVector3 _zero = new IndexedVector3();
        private static IndexedVector3 _one = new IndexedVector3(1);
        private static IndexedVector3 _up = new IndexedVector3(0, 1, 0);


        public float X;
        public float Y;
        public float Z;
    }
}
