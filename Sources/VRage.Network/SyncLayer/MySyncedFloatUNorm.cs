using VRage.Library.Collections;
namespace VRage.Network
{
    // Summary:
    //     Float in range <0, +1>. Slightly lossy (2B instead of 4B). If you need precision, use MySyncedFloat.
    // TODO: Commented, missing some methods
    //public class MySyncedFloatUNorm : MySyncedBase<float>
    //{
    //    public sealed override void Write(ref float value, BitStream s)
    //    {
    //        System.Diagnostics.Debug.Assert(value >= 0.0f && value < +1.01f);
    //        s.WriteVariant((value + 1) / 2);
    //    }

    //    public sealed override bool Read(out float value, BitStream s)
    //    {
    //        bool success = s.ReadCompressed(out value);
    //        value = (value * 2) - 1;
    //        return success;
    //    }
    //}
}
