using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public class MySyncedString : MySyncedBase<string>
    {
        public override void Write(ref string value, BitStream s)
        {
            s.Write(value);
        }

        public override bool Read(out string value, BitStream s)
        {
            return s.Read(out value);
        }
    }
}
