using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRageMath;
using VRageMath.PackedVector;
using System.Diagnostics;

namespace System
{
    public static class SerializationExtensionsMath
    {
        public static void Serialize(this BitStream stream, ref Vector2 vec)
        {
            stream.Serialize(ref vec.X);
            stream.Serialize(ref vec.Y);
        }

        public static void Serialize(this BitStream stream, ref Vector3 vec)
        {
            stream.Serialize(ref vec.X);
            stream.Serialize(ref vec.Y);
            stream.Serialize(ref vec.Z);
        }

        public static void Serialize(this BitStream stream, ref Vector4 vec)
        {
            stream.Serialize(ref vec.X);
            stream.Serialize(ref vec.Y);
            stream.Serialize(ref vec.Z);
            stream.Serialize(ref vec.W);
        }

        public static Quaternion ReadQuaternion(this BitStream stream)
        {
            Quaternion q;
            q.X = stream.ReadFloat();
            q.Y = stream.ReadFloat();
            q.Z = stream.ReadFloat();
            q.W = stream.ReadFloat();
            return q;
        }

        public static void WriteQuaternion(this BitStream stream, Quaternion q)
        {
            stream.WriteFloat(q.X);
            stream.WriteFloat(q.Y);
            stream.WriteFloat(q.Z);
            stream.WriteFloat(q.W);
        }

        public static void Serialize(this BitStream stream, ref Quaternion quat)
        {
            stream.Serialize(ref quat.X);
            stream.Serialize(ref quat.Y);
            stream.Serialize(ref quat.Z);
            stream.Serialize(ref quat.W);
        }

        /// <summary>
        /// Serializes normalized quaternion into 52 bits
        /// </summary>
        public static Quaternion ReadQuaternionNorm(this BitStream stream)
        {
            Quaternion value;
            bool cwNeg = stream.ReadBool();
            bool cxNeg = stream.ReadBool();
            bool cyNeg = stream.ReadBool();
            bool czNeg = stream.ReadBool();
            ushort cx = stream.ReadUInt16();
            ushort cy = stream.ReadUInt16();
            ushort cz = stream.ReadUInt16();

            // Calculate w from x,y,z
            value.X = (float)(cx / 65535.0);
            value.Y = (float)(cy / 65535.0);
            value.Z = (float)(cz / 65535.0);
            if (cxNeg) value.X = -value.X;
            if (cyNeg) value.Y = -value.Y;
            if (czNeg) value.Z = -value.Z;
            float difference = 1.0f - value.X * value.X - value.Y * value.Y - value.Z * value.Z;
            if (difference < 0.0f)
                difference = 0.0f;
            value.W = (float)(Math.Sqrt(difference));
            if (cwNeg)
                value.W = -value.W;
            return value;
        }

        /// <summary>
        /// Serializes normalized quaternion into 52 bits
        /// </summary>
        public static void WriteQuaternionNorm(this BitStream stream, Quaternion value)
        {
            stream.WriteBool(value.W < 0.0f);
            stream.WriteBool(value.X < 0.0f);
            stream.WriteBool(value.Y < 0.0f);
            stream.WriteBool(value.Z < 0.0f);
            stream.WriteUInt16((ushort)(Math.Abs(value.X) * 65535.0));
            stream.WriteUInt16((ushort)(Math.Abs(value.Y) * 65535.0));
            stream.WriteUInt16((ushort)(Math.Abs(value.Z) * 65535.0));
        }

        /// <summary>
        /// Serializes normalized quaternion into 52 bits
        /// </summary>
        public static void SerializeNorm(this BitStream stream, ref Quaternion quat)
        {
            if (stream.Reading)
                quat = stream.ReadQuaternionNorm();
            else
                stream.WriteQuaternionNorm(quat);
        }

        /// <summary>
        /// 64 bits
        /// </summary>
        public static void Serialize(this BitStream stream, ref HalfVector4 vec)
        {
            stream.Serialize(ref vec.PackedValue);
        }

        /// <summary>
        /// 48 bits
        /// </summary>
        public static void Serialize(this BitStream stream, ref HalfVector3 vec)
        {
            stream.Serialize(ref vec.X);
            stream.Serialize(ref vec.Y);
            stream.Serialize(ref vec.Z);
        }

        public static void Serialize(this BitStream stream, ref Vector3D vec)
        {
            stream.Serialize(ref vec.X);
            stream.Serialize(ref vec.Y);
            stream.Serialize(ref vec.Z);
        }

        public static void Serialize(this BitStream stream, ref Vector4D vec)
        {
            stream.Serialize(ref vec.X);
            stream.Serialize(ref vec.Y);
            stream.Serialize(ref vec.Z);
            stream.Serialize(ref vec.W);
        }

        public static void Serialize(this BitStream stream, ref Vector3I vec)
        {
            stream.Serialize(ref vec.X);
            stream.Serialize(ref vec.Y);
            stream.Serialize(ref vec.Z);
        }

        public static void SerializeVariant(this BitStream stream, ref Vector3I vec)
        {
            stream.SerializeVariant(ref vec.X);
            stream.SerializeVariant(ref vec.Y);
            stream.SerializeVariant(ref vec.Z);
        }

        /// <summary>
        /// 64 bits
        /// </summary>
        public static void Write(this BitStream stream, HalfVector4 vec)
        {
            stream.WriteUInt64(vec.PackedValue);
        }

        /// <summary>
        /// 48 bits
        /// </summary>
        public static void Write(this BitStream stream, HalfVector3 vec)
        {
            stream.WriteUInt16(vec.X);
            stream.WriteUInt16(vec.Y);
            stream.WriteUInt16(vec.Z);
        }

        public static void Write(this BitStream stream, Vector3 vec)
        {
            stream.WriteFloat(vec.X);
            stream.WriteFloat(vec.Y);
            stream.WriteFloat(vec.Z);
        }

        public static void Write(this BitStream stream, Vector3D vec)
        {
            stream.WriteDouble(vec.X);
            stream.WriteDouble(vec.Y);
            stream.WriteDouble(vec.Z);
        }

        public static void Write(this BitStream stream, Vector4 vec)
        {
            stream.WriteFloat(vec.X);
            stream.WriteFloat(vec.Y);
            stream.WriteFloat(vec.Z);
            stream.WriteFloat(vec.W);
        }

        public static void Write(this BitStream stream, Vector4D vec)
        {
            stream.WriteDouble(vec.X);
            stream.WriteDouble(vec.Y);
            stream.WriteDouble(vec.Z);
            stream.WriteDouble(vec.W);
        }

        public static void Write(this BitStream stream, Vector3I vec)
        {
            stream.WriteInt32(vec.X);
            stream.WriteInt32(vec.Y);
            stream.WriteInt32(vec.Z);
        }

        /// <summary>
        /// Writes Vector3 with -1, 1 range (uniform-spacing) with specified bit precision.
        /// </summary>
        public static void WriteNormalizedSignedVector3(this BitStream stream, Vector3 vec, int bitCount)
        {
            vec = Vector3.Clamp(vec, Vector3.MinusOne, Vector3.One);
            stream.WriteNormalizedSignedFloat(vec.X, bitCount);
            stream.WriteNormalizedSignedFloat(vec.Y, bitCount);
            stream.WriteNormalizedSignedFloat(vec.Z, bitCount);
        }

        public static void WriteVariant(this BitStream stream, Vector3I vec)
        {
            stream.WriteVariantSigned(vec.X);
            stream.WriteVariantSigned(vec.Y);
            stream.WriteVariantSigned(vec.Z);
        }

        /// <summary>
        /// 64 bits
        /// </summary>
        public static HalfVector4 ReadHalfVector4(this BitStream stream)
        {
            HalfVector4 result;
            result.PackedValue = stream.ReadUInt64();
            return result;
        }

        /// <summary>
        /// 48 bits
        /// </summary>
        public static HalfVector3 ReadHalfVector3(this BitStream stream)
        {
            HalfVector3 result;
            result.X = stream.ReadUInt16();
            result.Y = stream.ReadUInt16();
            result.Z = stream.ReadUInt16();
            return result;
        }

        /// <summary>
        /// Reads Vector3 with -1, 1 range (uniform-spacing) with specified bit precision.
        /// </summary>
        public static Vector3 ReadNormalizedSignedVector3(this BitStream stream, int bitCount)
        {
            Vector3 vec;
            vec.X = stream.ReadNormalizedSignedFloat(bitCount);
            vec.Y = stream.ReadNormalizedSignedFloat(bitCount);
            vec.Z = stream.ReadNormalizedSignedFloat(bitCount);
            return vec;
        }

        public static Vector3 ReadVector3(this BitStream stream)
        {
            Vector3 vec;
            vec.X = stream.ReadFloat();
            vec.Y = stream.ReadFloat();
            vec.Z = stream.ReadFloat();
            return vec;
        }

        public static Vector3D ReadVector3D(this BitStream stream)
        {
            Vector3D vec;
            vec.X = stream.ReadDouble();
            vec.Y = stream.ReadDouble();
            vec.Z = stream.ReadDouble();
            return vec;
        }

        public static Vector4 ReadVector4(this BitStream stream)
        {
            Vector4 vec;
            vec.X = stream.ReadFloat();
            vec.Y = stream.ReadFloat();
            vec.Z = stream.ReadFloat();
            vec.W = stream.ReadFloat();
            return vec;
        }

        public static Vector4D ReadVector4D(this BitStream stream)
        {
            Vector4D vec;
            vec.X = stream.ReadDouble();
            vec.Y = stream.ReadDouble();
            vec.Z = stream.ReadDouble();
            vec.W = stream.ReadDouble();
            return vec;
        }

        public static Vector3I ReadVector3I(this BitStream stream)
        {
            Vector3I vec;
            vec.X = stream.ReadInt32();
            vec.Y = stream.ReadInt32();
            vec.Z = stream.ReadInt32();
            return vec;
        }

        public static Vector3I ReadVector3IVariant(this BitStream stream)
        {
            Vector3I vec;
            vec.X = stream.ReadInt32Variant();
            vec.Y = stream.ReadInt32Variant();
            vec.Z = stream.ReadInt32Variant();
            return vec;
        }

        /// <summary>
        /// Serializes only position and orientation, 12 + 6.5 = 18.5 bytes
        /// </summary>
        public static void SerializePositionOrientation(this BitStream stream, ref Matrix m)
        {
            if (stream.Writing)
            {
                Vector3 pos = m.Translation;
                Quaternion rot;
                Quaternion.CreateFromRotationMatrix(ref m, out rot);
                stream.Serialize(ref pos);
                stream.SerializeNorm(ref rot);
            }
            else
            {
                Vector3 pos = default(Vector3);
                Quaternion rot = default(Quaternion);
                stream.Serialize(ref pos);
                stream.SerializeNorm(ref rot);
                Matrix.CreateFromQuaternion(ref rot, out m);
                m.Translation = pos;
            }
        }

        /// <summary>
        /// Serializes only position and orientation, 24 + 6.5 = 30.5 bytes
        /// </summary>
        public static void SerializePositionOrientation(this BitStream stream, ref MatrixD m)
        {
            if (stream.Writing)
            {
                Vector3D pos = m.Translation;
                Quaternion rot;
                Quaternion.CreateFromRotationMatrix(ref m, out rot);
                stream.Serialize(ref pos);
                stream.SerializeNorm(ref rot);
            }
            else
            {
                Vector3D pos = default(Vector3D);
                Quaternion rot = default(Quaternion);
                stream.Serialize(ref pos);
                stream.SerializeNorm(ref rot);
                MatrixD.CreateFromQuaternion(ref rot, out m);
                m.Translation = pos;
            }
        }

        public static unsafe void Serialize(this BitStream stream, ref MyBlockOrientation orientation)
        {
            var copy = orientation;
            stream.SerializeMemory(&copy, sizeof(MyBlockOrientation) * 8);
            orientation = copy;
        }

#if! XB1
        public static void SerializeList(this BitStream stream, ref List<Vector3D> list)
        {
            stream.SerializeList(ref list, delegate(BitStream bs, ref Vector3D vec) { bs.Serialize(ref vec); }); // Does not allocated, anonymous function cached by compiler
        }
#endif
        public static void Serialize(this BitStream stream, ref BoundingBox bb)
        {
            stream.Serialize(ref bb.Min);
            stream.Serialize(ref bb.Max);
        }

        public static void Serialize(this BitStream stream, ref BoundingBoxD bb)
        {
            stream.Serialize(ref bb.Min);
            stream.Serialize(ref bb.Max);
        }
    }
}
