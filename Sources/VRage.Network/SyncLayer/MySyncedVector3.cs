using VRage.Library.Collections;
using VRageMath;

namespace VRage.Network
{
    // Lossless 3*4B
    public class MySyncedVector3 : MySyncedBase<Vector3>
    {
        public sealed override void Write(ref Vector3 value, BitStream s)
        {
            s.WriteFloat(value.X);
            s.WriteFloat(value.Y);
            s.WriteFloat(value.Z);
        }

        public sealed override void Read(out Vector3 value, BitStream s)
        {
            value.X = s.ReadFloat();
            value.Y = s.ReadFloat();
            value.Z = s.ReadFloat();
        }
    }
}
