using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    public class MyBBMemoryFloat : MyBBMemoryValue
    {
        [ProtoMember]
        public float FloatValue = 0;
    }
}
