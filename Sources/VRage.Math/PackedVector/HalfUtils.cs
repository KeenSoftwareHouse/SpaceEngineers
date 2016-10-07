namespace VRageMath.PackedVector
{
    public static class HalfUtils
    {
        private const int cFracBits = 10;
        private const int cExpBits = 5;
        private const int cSignBit = 15;
        private const uint cSignMask = 32768U;
        private const uint cFracMask = 1023U;
        private const int cExpBias = 15;
        private const uint cRoundBit = 4096U;
        private const uint eMax = 16U;
        private const int eMin = -14;
        private const uint wMaxNormal = 1207955455U;
        private const uint wMinNormal = 947912704U;
        private const uint BiasDiffo = 3355443200U;
        private const int cFracBitsDiff = 13;

        public static unsafe ushort Pack(float value)
        {
            unchecked
            {
                uint num1 = *(uint*)&value;
                uint num2 = (num1 & (uint)int.MinValue) >> 16;
                uint num3 = num1 & (uint)int.MaxValue;
                ushort num4;
                if (num3 > 1207955455U)
                    num4 = (ushort)(num2 | (uint)short.MaxValue);
                else if (num3 < 947912704U)
                {
                    uint num5 = (uint)((int)num3 & 8388607 | 8388608);
                    int num6 = 113 - (int)(num3 >> 23);
                    uint num7 = num6 > 31 ? 0U : num5 >> num6;
                    num4 = (ushort)(num2 | (uint)((int)num7 + 4095 + ((int)(num7 >> 13) & 1)) >> 13);
                }
                else
                    num4 = (ushort)(num2 | (uint)((int)num3 - 939524096 + 4095 + ((int)(num3 >> 13) & 1)) >> 13);
                return num4;
            }
        }

        public static unsafe float Unpack(ushort value)
        {
            uint num1;
            if (((int)value & -33792) == 0)
            {
                if (((int)value & 1023) != 0)
                {
                    uint num2 = 4294967282U;
                    uint num3 = (uint)value & 1023U;
                    while (((int)num3 & 1024) == 0)
                    {
                        --num2;
                        num3 <<= 1;
                    }
                    uint num4 = num3 & 4294966271U;
                    num1 = (uint)(((int)value & 32768) << 16 | (int)num2 + (int)sbyte.MaxValue << 23 | (int)num4 << 13);
                }
                else
                    num1 = (uint)(((int)value & 32768) << 16);
            }
            else
                num1 = (uint)(((int)value & 32768) << 16 | ((int)value >> 10 & 31) - 15 + (int)sbyte.MaxValue << 23 | ((int)value & 1023) << 13);
            return *(float*)&num1;
        }
    }
}
