using VRage.Library.Collections;
using VRageMath;

namespace VRage.Network
{
    // Lossless 3*4B
    public class MySyncedVector3D : MySyncedBase<Vector3D>
    {
        public sealed override void Write(ref Vector3D value, BitStream s)
        {
            s.WriteDouble(value.X);
            s.WriteDouble(value.Y);
            s.WriteDouble(value.Z);
        }

        public sealed override void Read(out Vector3D value, BitStream s)
        {
            value.X = s.ReadDouble();
            value.Y = s.ReadDouble();
            value.Z = s.ReadDouble();
        }
    }
}
