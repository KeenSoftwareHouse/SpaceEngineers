using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public class MySerializerBitReaderWriter : MySerializer<BitReaderWriter>
    {
        public override void Clone(ref BitReaderWriter value)
        {
            throw new NotSupportedException();
        }

        public override bool Equals(ref BitReaderWriter a, ref BitReaderWriter b)
        {
            throw new NotSupportedException();
        }

        public override void Read(BitStream stream, out BitReaderWriter value, MySerializeInfo info)
        {
            value = BitReaderWriter.ReadFrom(stream);
        }

        public override void Write(BitStream stream, ref BitReaderWriter value, MySerializeInfo info)
        {
            value.Write(stream);
        }
    }
}
