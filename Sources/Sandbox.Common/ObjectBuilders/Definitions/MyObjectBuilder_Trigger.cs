using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Trigger : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public bool IsTrue;
        [ProtoMember(2)]
        public string Message;
    }
}
