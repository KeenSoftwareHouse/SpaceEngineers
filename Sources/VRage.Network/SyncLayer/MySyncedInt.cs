using VRage.Library.Collections;
namespace VRage.Network
{
    public class MySyncedInt : MySyncedBase<int>
    {
        public sealed override void Write(ref int value, BitStream s)
        {
            s.WriteInt32(value);
        }

        public sealed override void Read(out int value, BitStream s)
        {
            value = s.ReadInt32();
        }
    }
}
