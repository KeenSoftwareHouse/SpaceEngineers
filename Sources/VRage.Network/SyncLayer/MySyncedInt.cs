namespace VRage.Network
{
    public class MySyncedInt : MySyncedBase<int>
    {
        public override sealed void Write(ref int value, BitStream s)
        {
            s.Write(value);
        }

        public override sealed bool Read(out int value, BitStream s)
        {
            return s.Read(out value);
        }
    }
}