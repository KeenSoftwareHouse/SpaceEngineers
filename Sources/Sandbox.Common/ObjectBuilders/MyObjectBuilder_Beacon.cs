using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Beacon : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float BroadcastRadius;
    }
}
