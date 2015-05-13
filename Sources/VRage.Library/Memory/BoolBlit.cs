using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public struct BoolBlit
    {
        byte m_value;

        internal BoolBlit(byte value)
        {
            m_value = value;
        }

        public static implicit operator bool(BoolBlit b)
        {
            return b.m_value != 0;
        }

        public static implicit operator BoolBlit(bool b)
        {
            return new BoolBlit(b ? (byte)0xff : (byte)0x0);
        }

        public override string ToString()
        {
            return ((bool)this).ToString();
        }
    }
}
