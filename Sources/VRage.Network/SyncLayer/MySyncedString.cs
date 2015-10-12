using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    public class MySyncedString : MySyncedBase<string>
    {
        public override void Write(ref string value, BitStream s)
        {
            s.WritePrefixLengthString(value, 0, value.Length, Encoding.UTF8);
        }

        public override void Read(out string value, BitStream s)
        {
            value = s.ReadPrefixLengthString(Encoding.UTF8);
        }
    }
}
