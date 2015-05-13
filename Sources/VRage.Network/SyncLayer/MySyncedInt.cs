namespace VRage.Network
{
    public class MySyncedInt : MySyncedBase<int>
    {
        public sealed override void Write(ref int value, BitStream s)
        {
            s.Write(value);
        }

        public sealed override bool Read(out int value, BitStream s)
        {
            return s.Read(out value);
        }
    }
}
