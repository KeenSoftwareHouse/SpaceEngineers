namespace VRage.Network
{
    public class MySyncedFloat : MySyncedBase<float>
    {
        public override sealed void Write(ref float value, BitStream s)
        {
            s.Write(value);
        }

        public override sealed bool Read(out float value, BitStream s)
        {
            return s.Read(out value);
        }
    }
}