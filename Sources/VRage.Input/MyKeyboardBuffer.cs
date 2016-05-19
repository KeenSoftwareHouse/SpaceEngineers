#if !XB1
namespace VRage.Input
{
    public struct MyKeyboardBuffer
    {
        unsafe fixed byte m_data[32]; // 8b * 32 = 256b

        public unsafe void SetBit(byte bit, bool value)
        {
            if (bit == 0) return; // Zero key is reserved for system use

            int bitOffset = bit % 8;
            byte mask = (byte)(1 << bitOffset);
            fixed (byte* data = m_data)
            {
                if (value)
                    *(data + bit / 8) |= mask;
                else
                    *(data + bit / 8) &= (byte)~mask;
            }
        }

        public unsafe bool AnyBitSet()
        {
            fixed (byte* data = m_data)
            {
                // When cast to 64b long, only 4 compares are necessary (256b/64b = 4)
                long* bigData = (long*)data;
                return bigData[0] + bigData[1] + bigData[2] + bigData[3] != 0;
            }
        }

        public unsafe bool GetBit(byte bit)
        {
            int bitOffset = (bit % 8);
            byte mask = (byte)(1 << bitOffset);
            fixed (byte* data = m_data)
            {
                return ((*(data + bit / 8)) & mask) != 0;
            }
        }
    }
}

#endif