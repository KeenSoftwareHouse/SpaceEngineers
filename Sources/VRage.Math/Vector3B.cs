using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VRageMath
{

    //  Sbyte version of Vector3, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector3B
    {
        [ProtoBuf.ProtoMember]
        public sbyte X;
        [ProtoBuf.ProtoMember]
        public sbyte Y;
        [ProtoBuf.ProtoMember]
        public sbyte Z;

        public static readonly Vector3B Zero = new Vector3B();
        public static Vector3B Up = new Vector3B(0, 1, 0);
        public static Vector3B Down = new Vector3B(0, -1, 0);
        public static Vector3B Right = new Vector3B(1, 0, 0);
        public static Vector3B Left = new Vector3B(-1, 0, 0);
        public static Vector3B Forward = new Vector3B(0, 0, -1);
        public static Vector3B Backward = new Vector3B(0, 0, 1);

        public Vector3B(sbyte x, sbyte y, sbyte z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3B(Vector3I vec)
        {
            Debug.Assert(vec.X >= sbyte.MinValue && vec.X <= sbyte.MaxValue
                && vec.Y >= sbyte.MinValue && vec.Y <= sbyte.MaxValue
                && vec.Z >= sbyte.MinValue && vec.Z <= sbyte.MaxValue, "This Vector3I does not fit Vector3B: " + vec.ToString());

            X = (sbyte)vec.X;
            Y = (sbyte)vec.Y;
            Z = (sbyte)vec.Z;
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z;
        }

        public override int GetHashCode()
        {
            return ((byte)Z << 16) | ((byte)Y << 8) | (byte)X;
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var rhs = obj as Vector3B?;
                if (rhs.HasValue)
                    return this == rhs.Value;
            }

            return false;
        }

        public static VRageMath.Vector3 operator *(VRageMath.Vector3 vector, Vector3B shortVector)
        {
            return shortVector * vector;
        }

        public static VRageMath.Vector3 operator *(Vector3B shortVector, VRageMath.Vector3 vector)
        {
            return new VRageMath.Vector3(shortVector.X * vector.X, shortVector.Y * vector.Y, shortVector.Z * vector.Z);
        }

        public static implicit operator Vector3I(Vector3B vec)
        {
            return new Vector3I(vec.X, vec.Y, vec.Z);
        }

        public static Vector3B Round(Vector3 vec)
        {
            return new Vector3B((sbyte)Math.Round(vec.X), (sbyte)Math.Round(vec.Y), (sbyte)Math.Round(vec.Z));
        }

        public static bool operator ==(Vector3B a, Vector3B b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vector3B a, Vector3B b)
        {
            return !(a == b);
        }

        public static Vector3B operator-(Vector3B me) {
            return new Vector3B((sbyte)-me.X, (sbyte)-me.Y, (sbyte)-me.Z);
        }

        /// <summary>
        /// Puts Vector3 into Vector3B, value -127 represents -range, 128 represents range
        /// </summary>
        public static Vector3B Fit(Vector3 vec, float range)
        {
            // Convert from (-range, range) to (-1,1).
            // Then convert from (-1,1) to (-128, 128).
            return Round(vec / range * 128);
        }
    }
}
