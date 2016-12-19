using System;
using VRage.Library.Collections;
using VRageMath;

namespace VRage.BitCompression
{
    public static class CompressedQuaternion
    {
        const float MIN_QF_LENGTH = -1.0f / 1.414214f;
        const float MAX_QF_LENGTH = +1.0f / 1.414214f;
        const int QF_BITS = 9;
        const int QF_VALUE = (1 << QF_BITS) - 1;
        const float QF_SCALE = (float)QF_VALUE;
        const float QF_SCALE_INV = 1 / QF_SCALE; 

        public static void Write(BitStream stream, Quaternion value)
        {
            value.Normalize();
            var largest = value.FindLargestIndex();
            var lc = value.GetComponent(largest);
            if (lc < 0)
                value = -value;
            stream.WriteInt32(largest, 2);
            for (int i = 0; i < 4; i++)
            {
                if (i != largest)
                {
                    float c = value.GetComponent(i);
                    float v = (c - MIN_QF_LENGTH) / (MAX_QF_LENGTH - MIN_QF_LENGTH);
                    uint vi = (uint)Math.Floor(v * QF_SCALE + 0.5f);
                    stream.WriteUInt32(vi, QF_BITS);
                }
            }
        }

        public static Quaternion Read(BitStream stream)
        {
            Quaternion value = Quaternion.Identity;
            var largest = stream.ReadInt32(2);
            float qLen = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i != largest)
                {
                    var vi = stream.ReadInt32(QF_BITS);
                    float v = vi * QF_SCALE_INV;
                    float c = v * (MAX_QF_LENGTH - MIN_QF_LENGTH) + MIN_QF_LENGTH;
                    value.SetComponent(i, c);
                    System.Diagnostics.Debug.Assert(c.IsValid());
                    qLen += c * c;
                }
            }
            value.SetComponent(largest, (float)Math.Sqrt(1 - qLen));
            value.Normalize();
            return value;
        }

        public static bool CompressedQuaternionUnitTest()
        {
            var stream = new VRage.Library.Collections.BitStream();
            stream.ResetWrite();
            Quaternion q = Quaternion.Identity;
            stream.WriteQuaternionNormCompressed(q);
            stream.ResetRead();
            var q2 = stream.ReadQuaternionNormCompressed();
            bool fail = !q.Equals(q2, 1 / 511.0f);

            stream.ResetWrite();
            q = Quaternion.CreateFromAxisAngle(Vector3.Forward, (float)Math.PI / 3.0f);
            stream.WriteQuaternionNormCompressed(q);
            stream.ResetRead();
            q2 = stream.ReadQuaternionNormCompressed();
            fail |= !q.Equals(q2, 1 / 511.0f);

            stream.ResetWrite();
            var v = new Vector3(1, -1, 3);
            v.Normalize();
            q = Quaternion.CreateFromAxisAngle(v, (float)Math.PI / 3.0f);
            stream.WriteQuaternionNormCompressed(q);
            stream.ResetRead();
            q2 = stream.ReadQuaternionNormCompressed();
            fail |= !q.Equals(q2, 1 / 511.0f);
            return fail;
        }
    }
}