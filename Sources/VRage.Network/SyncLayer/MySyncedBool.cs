using System.Diagnostics;

namespace VRage.Network
{
    public class MySyncedBool : MySyncedBase<bool>
    {
        public override sealed void Write(ref bool value, BitStream s)
        {
            s.Write(value);
        }

        public override sealed bool Read(out bool value, BitStream s)
        {
            return s.Read(out value);
        }

        public override sealed void Serialize(BitStream bs, int clientIndex)
        {
            lock (this)
            {
                bs.Write(m_value);
            }
        }

        public override sealed void Deserialize(BitStream bs)
        {
            lock (this)
            {
                bool success = Read(out m_value, bs);
                Debug.Assert(success, "Failed to read synced value");
            }
        }

        public override sealed void SerializeDefault(BitStream bs, int clientIndex = -1)
        {
            Serialize(bs, clientIndex);
        }

        public override sealed void DeserializeDefault(BitStream bs)
        {
            Deserialize(bs);
        }
    }
}