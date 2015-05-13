using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Utils
{
    class MyQuantizer
    {
        private int m_quantizationBits; // number of bits kept
        private int m_throwawayBits;    // number of bits thrown away
        private int m_minValue; // minimum value that can be represented (apart from 0);

        // Values quantized to (8 - QUANTIZATION_BITS) with correct smearing of significant bits.
        // Example: 3 significant bits
        //   000xxxxx -> 0000000   001xxxxx -> 0010010   010xxxxx -> 0100100   011xxxxx -> 0110110
        //   100xxxxx -> 1001001   101xxxxx -> 1011011   110xxxxx -> 1101101   111xxxxx -> 1111111
        // It's important to return 255 for max value and 0 for min value.
        byte[] m_smearBits;
        uint[] m_bitmask;

        public MyQuantizer(int quantizationBits)
        {
            m_quantizationBits = quantizationBits;
            m_throwawayBits = 8 - m_quantizationBits;
            m_smearBits = new byte[1 << m_quantizationBits];
            for (uint i = 0; i < 1 << m_quantizationBits; i++)
            {
                uint value = i << m_throwawayBits;

                // smear bits
                value = value + (value >> m_quantizationBits);
                if (m_quantizationBits < 4)
                {
                    value = value + (value >> m_quantizationBits * 2);
                    if (m_quantizationBits < 2)
                        value = value + (value >> m_quantizationBits * 4);
                }

                m_smearBits[i] = (byte)value;
            }
            m_bitmask = new uint[]
                {
                    ~((255u >> m_throwawayBits) << 0), ~((255u >> m_throwawayBits) << 1), ~((255u >> m_throwawayBits) << 2), ~((255u >> m_throwawayBits) << 3),
                    ~((255u >> m_throwawayBits) << 4), ~((255u >> m_throwawayBits) << 5), ~((255u >> m_throwawayBits) << 6), ~((255u >> m_throwawayBits) << 7),
                };
            m_minValue = 1 << m_throwawayBits;
        }

        public byte QuantizeValue(byte val)
        {
            return m_smearBits[val >> m_throwawayBits];
        }

        public void SetAllFromUnpacked(byte[] dstPacked, int dstSize, byte[] srcUnpacked)
        {
            unchecked
            {
                // for QUANTIZATION_BITS == 8 we can just do System.Buffer.BlockCopy
                Array.Clear(dstPacked, 0, dstPacked.Length);
                for (int bitadr = 0, adr = 0; bitadr < dstSize * m_quantizationBits; bitadr += m_quantizationBits, adr++)
                {
                    int byteadr = bitadr >> 3;
                    uint c = ((uint)srcUnpacked[adr] >> m_throwawayBits) << (bitadr & 7);
                    dstPacked[byteadr] |= (byte)c;
                    dstPacked[byteadr + 1] |= (byte)(c >> 8);  // this needs to be done only for QUANTIZATION_BITS == 1,2,4,8
                }
            }
        }

        public void WriteVal(byte[] packed, int idx, byte val)
        {
            unchecked
            {
                // for QUANTIZATION_BITS == 8: packed[idx] = content;
                int bitadr = idx * m_quantizationBits;
                int bit = bitadr & 7;
                int byteadr = bitadr >> 3;
                uint c = ((uint)val >> m_throwawayBits) << bit;
                packed[byteadr] = (byte)(packed[byteadr] & m_bitmask[bit] | c);
                packed[byteadr + 1] = (byte)(packed[byteadr + 1] & m_bitmask[bit] >> 8 | c >> 8);   // this needs to be done only for QUANTIZATION_BITS == 1,2,4,8
            }
        }

        public byte ReadVal(byte[] packed, int idx)
        {
            unchecked
            {
                // for QUANTIZATION_BITS == 8: return packed[idx];
                int bitadr = idx * m_quantizationBits;
                int byteadr = bitadr >> 3;
                uint value = packed[byteadr] + ((uint)packed[byteadr + 1] << 8);  // QUANTIZATION_BITS == 1,2,4,8: value = (uint)m_packed[bitadr >> 3];
                return m_smearBits[(value >> (bitadr & 7)) & (255 >> m_throwawayBits)];
            }
        }

        public int ComputeRequiredPackedSize(int unpackedSize)
        {
            return (unpackedSize * m_quantizationBits + 7) / 8 + 1;
        }

        public int GetMinimumQuantizableValue()
        {
            return m_minValue;
        }
    }
}
