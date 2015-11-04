using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    public class MySyncedBool : MySyncedBase<bool>
    {
        public sealed override void Write(ref bool value, BitStream s)
        {
            s.WriteBool(value);
        }

        public sealed override void Read(out bool value, BitStream s)
        {
            value = s.ReadBool();
        }

        public sealed override void Serialize(BitStream bs, int clientIndex)
        {
            lock (this)
            {
                bs.WriteBool(m_value);
            }
        }

        public sealed override void Deserialize(BitStream bs)
        {
            lock (this)
            {
                Read(out m_value, bs);
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
