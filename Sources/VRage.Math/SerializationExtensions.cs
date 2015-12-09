using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRageMath;
using VRageMath.PackedVector;

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

        /// <summary>
        /// Serializes Vector3 in range from 0 to 1, each component to byte precision
        /// </summary>
        public static void SerializeNormalizedUByte(this BitStream stream, ref Vector3 vec)
        {
            if (stream.Writing)
            {
                stream.WriteByte((byte)(vec.X / 255));
                stream.WriteByte((byte)(vec.Y / 255));
                stream.WriteByte((byte)(vec.Z / 255));
            }
            else
            {
                vec.X = stream.ReadByte() * 255.0f;
                vec.Y = stream.ReadByte() * 255.0f;
                vec.Z = stream.ReadByte() * 255.0f;
            }
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

        public static void SerializeList(this BitStream stream, ref List<Vector3D> list)
        {
            stream.SerializeList(ref list, delegate(BitStream bs, ref Vector3D vec) { bs.Serialize(ref vec); }); // Does not allocated, anonymous function cached by compiler
        }
    }
}
