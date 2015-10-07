using ProtoBuf;
using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    public class MyBBMemoryBool : MyBBMemoryValue
    {
        [ProtoMember]
        public bool BoolValue = false;
    }
}
