using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    /// <summary>
    /// Bit field with up to 64 bits.
    /// </summary>
    public struct SmallBitField
    {
        public const int BitCount = sizeof(ulong) * 8;
        public const ulong BitsEmpty = 0;
        public const ulong BitsFull = ulong.MaxValue;

        public static readonly SmallBitField Empty = new SmallBitField(false);
        public static readonly SmallBitField Full = new SmallBitField(true);

        public ulong Bits;

        public SmallBitField(bool value)
        {
            Bits = value ? BitsFull : BitsEmpty;
        }

        public void Reset(bool value)
        {
            Bits = value ? BitsFull : BitsEmpty;
        }

        public bool this[int index]
        {
            get
            {
                return ((Bits >> index) & 1) != 0;
            }
            set
            {
                if (value)
                {
                    Bits |= 1u << index;
                }
                else
                {
                    Bits &= ~(1u << index);
                }
            }
        }
    }

}
