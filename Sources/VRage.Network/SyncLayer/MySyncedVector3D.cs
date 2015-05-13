using VRageMath;

namespace VRage.Network
{
    // Lossless 3*4B
    public class MySyncedVector3D : MySyncedBase<Vector3D>
    {
        public sealed override void Write(ref Vector3D value, BitStream s)
        {
            s.Write(value.X);
            s.Write(value.Y);
            s.Write(value.Z);
        }

        public sealed override bool Read(out Vector3D value, BitStream s)
        {
            bool success;

            success = s.Read(out value.X);
            success &= s.Read(out value.Y);
            success &= s.Read(out value.Z);

            return success;
        }
    }
}
