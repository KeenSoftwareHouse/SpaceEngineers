using VRage.Library.Collections;
namespace VRage.Network
{
    // Summary:
    //     Float in range <-1, +1>. Slightly lossy (2B instead of 4B). If you need precision, use MySyncedFloat.
    // TODO: Commented missing methods
    //public class MySyncedFloatSNorm : MySyncedBase<float>
    //{
    //    // TODO:SK different compression/packing
    //    public sealed override void Write(ref float value, BitStream s)
    //    {
    //        System.Diagnostics.Debug.Assert(value > -1.01f && value < +1.01f);
    //        s.WriteCompressed(value);
    //    }

    //    public sealed override void Read(out float value, BitStream s)
    //    {
    //        return s.ReadCompressed(out value);
    //    }
    //}
}
