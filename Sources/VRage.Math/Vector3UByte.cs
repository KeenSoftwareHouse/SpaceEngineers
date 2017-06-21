using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageMath
{
    [ProtoBuf.ProtoContract]
    public struct Vector3UByte
    {
        public class EqualityComparer : IEqualityComparer<Vector3UByte>, IComparer<Vector3UByte>
        {
            public bool Equals(Vector3UByte x, Vector3UByte y)
            {
                return x.X == y.X & x.Y == y.Y & x.Z == y.Z;
            }

            public int GetHashCode(Vector3UByte obj)
            {
                return (((obj.X * 397) ^ obj.Y) * 397) ^ obj.Z;
            }

            public int Compare(Vector3UByte a, Vector3UByte b)
            {
                int x = a.X - b.X;
                int y = a.Y - b.Y;
                int z = a.Z - b.Z;
                return x != 0 ? x : (y != 0 ? y : z);
            }
        }

        public static readonly EqualityComparer Comparer = new EqualityComparer();

        public static Vector3UByte Zero = new Vector3UByte(0, 0, 0);

        [ProtoBuf.ProtoMember]
        public byte X;
        [ProtoBuf.ProtoMember]
        public byte Y;
        [ProtoBuf.ProtoMember]
        public byte Z;

        public Vector3UByte(byte x, byte y, byte z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3UByte(Vector3I vec)
        {
            Debug.Assert(vec.X >= byte.MinValue && vec.X <= byte.MaxValue
                && vec.Y >= byte.MinValue && vec.Y <= byte.MaxValue
                && vec.Z >= byte.MinValue && vec.Z <= byte.MaxValue, "This Vector3I does not fit Vector3UByte: " + vec.ToString());

            X = (byte)vec.X;
            Y = (byte)vec.Y;
            Z = (byte)vec.Z;
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z;
        }

        public override int GetHashCode()
        {
            return (Z << 16) | (Y << 8) | X;
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var rhs = obj as Vector3UByte?;
                if (rhs.HasValue)
                    return this == rhs.Value;
            }

            return false;
        }

        public static bool operator ==(Vector3UByte a, Vector3UByte b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vector3UByte a, Vector3UByte b)
        {
            return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
        }

        public static Vector3UByte Round(Vector3 vec)
        {
            return new Vector3UByte((byte)Math.Round(vec.X), (byte)Math.Round(vec.Y), (byte)Math.Round(vec.Z));
        }

        public static Vector3UByte Floor(Vector3 vec)
        {
            return new Vector3UByte((byte)Math.Floor(vec.X), (byte)Math.Floor(vec.Y), (byte)Math.Floor(vec.Z));
        }

        public static implicit operator Vector3I(Vector3UByte vec)
        {
            return new Vector3I(vec.X, vec.Y, vec.Z);
        }

        public int LengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        /// <summary>
        /// Returns true when all components are 127
        /// </summary>
        public static bool IsMiddle(Vector3UByte vec)
        {
            return vec.X == 127 && vec.Y == 127 && vec.Z == 127;
        }

        private static Vector3 m_clampBoundary = new Vector3(255f);
        /// <summary>
        /// Normalizes Vector3 into Vector4UByte, scales vector from (-range, range) to (0, 255).
        /// Unsafe for values "range >= any_vec_value / 257";
        /// </summary>
        public static Vector3UByte Normalize(Vector3 vec, float range)
        {
            // Scale from (-range, range) to (-1,1):  vec / range 
            // Scale to (-0.5f, 0.5f): vec / range / 2
            // Scale to (0, 1): (vec / range / 2 + new Vector3(0.5f)
            // Finally scale to (0, 255) -- Clamp
            var v = (vec / range / 2 + new Vector3(0.5f)) * 255f;
            Vector3.Clamp(ref v, ref Vector3.Zero, ref m_clampBoundary, out v);
            return new Vector3UByte((byte)v.X, (byte)v.Y, (byte)v.Z);
        }

        /// <summary>
        /// Unpacks Vector3 from Vector3UByte, scales vector from (0, 255) to (-range, range)
        /// </summary>
        public static Vector3 Denormalize(Vector3UByte vec, float range)
        {
            float epsilon = 0.5f / 255.0f;
            return (new Vector3(vec.X, vec.Y, vec.Z) / 255.0f - new Vector3(0.5f - epsilon)) * 2 * range;
        }
    }
}
