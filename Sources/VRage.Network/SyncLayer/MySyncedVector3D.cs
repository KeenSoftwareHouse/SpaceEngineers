using VRageMath;

namespace VRage.Network
{
    // Lossless 3*4B
    public class MySyncedVector3D : MySyncedBase<Vector3D>
    {
        public override sealed void Write(ref Vector3D value, BitStream s)
        {
            s.Write(value.X);
            s.Write(value.Y);
            s.Write(value.Z);
        }

        public override sealed bool Read(out Vector3D value, BitStream s)
        {
            var success = s.Read(out value.X);
            success &= s.Read(out value.Y);
            success &= s.Read(out value.Z);

            return success;
        }
    }
}