﻿namespace VRage.Network
{
    // Summary:
    //     Float in range <0, +1>. Slightly lossy (2B instead of 4B). If you need precision, use MySyncedFloat.
    public class MySyncedFloatUNorm : MySyncedBase<float>
    {
        public override sealed void Write(ref float value, BitStream s)
        {
            System.Diagnostics.Debug.Assert(value >= 0.0f && value < +1.01f);
            s.WriteCompressed((value + 1)/2);
        }

        public override sealed bool Read(out float value, BitStream s)
        {
            bool success = s.ReadCompressed(out value);
            value = (value*2) - 1;
            return success;
        }
    }
}