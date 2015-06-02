using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public class MySyncedBool : MySyncedBase<bool>
    {
        public sealed override void Write(ref bool value, BitStream s)
        {
            s.Write(value);
        }

        public sealed override bool Read(out bool value, BitStream s)
        {
            return s.Read(out value);
        }

        public sealed override void Serialize(BitStream bs, int clientIndex)
        {
            lock (this)
            {
                bs.Write(m_value);
            }
        }

        public sealed override void Deserialize(BitStream bs)
        {
            lock (this)
            {
                bool success = Read(out m_value, bs);
                Debug.Assert(success, "Failed to read synced value");
            }
        }

        public sealed override void SerializeDefault(BitStream bs, int clientIndex = -1)
        {
            Serialize(bs, clientIndex);
        }

        public sealed override void DeserializeDefault(BitStream bs)
        {
            Deserialize(bs);
        }
    }
}
