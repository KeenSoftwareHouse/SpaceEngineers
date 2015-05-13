namespace VRage.Network
{
    public class MySyncedFloat : MySyncedBase<float>
    {
        public sealed override void Write(ref float value, BitStream s)
        {
            s.Write(value);
        }

        public sealed override bool Read(out float value, BitStream s)
        {
            return s.Read(out value);
        }
    }
}
