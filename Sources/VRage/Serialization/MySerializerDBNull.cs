using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerDBNull : MySerializer<DBNull>
    {
        public override void Clone(ref DBNull value)
        {
        }

        public override bool Equals(ref DBNull a, ref DBNull b)
        {
            return true;
        }

        public override void Read(Library.Collections.BitStream stream, out DBNull value, MySerializeInfo info)
        {
            value = DBNull.Value;
        }

        public override void Write(Library.Collections.BitStream stream, ref DBNull value, MySerializeInfo info)
        {
        }
    }
}
