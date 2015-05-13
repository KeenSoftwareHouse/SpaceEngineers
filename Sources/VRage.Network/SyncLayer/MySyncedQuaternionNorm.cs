using VRageMath;

namespace VRage.Network
{
    // Lossless 3*4B
    public class MySyncedQuaternionNorm : MySyncedBase<Quaternion>
    {
        public sealed override void Write(ref Quaternion value, BitStream s)
        {
            s.WriteNormQuat(value.W, value.X, value.Y, value.Z);
        }

        public sealed override bool Read(out Quaternion value, BitStream s)
        {
            return s.ReadNormQuat(out value.W, out value.X, out value.Y, out value.Z);
        }
    }
}
