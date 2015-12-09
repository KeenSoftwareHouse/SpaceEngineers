using System;
using VRage.Library.Collections;
using VRageMath;

namespace VRage.Network
{
    // Lossless 3*4B
    public class MySyncedQuaternionNorm : MySyncedBase<Quaternion>
    {
        public sealed override void Write(ref Quaternion value, BitStream s)
        {
            s.Serialize(ref value);
        }

        public sealed override void Read(out Quaternion value, BitStream s)
        {
            value = default(Quaternion);
            s.Serialize(ref value);
        }
    }
}
