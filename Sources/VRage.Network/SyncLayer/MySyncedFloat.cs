using VRage.Library.Collections;
namespace VRage.Network
{
    public class MySyncedFloat : MySyncedBase<float>
    {
        public sealed override void Write(ref float value, BitStream s)
        {
            s.WriteFloat(value);
        }

        public sealed override void Read(out float value, BitStream s)
        {
            value = s.ReadFloat();
        }
    }
}
