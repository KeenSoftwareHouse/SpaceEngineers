using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalEvents : MyObjectBuilder_Base
    {
        [ProtoMember]
        public List<MyObjectBuilder_GlobalEventBase> Events = new List<MyObjectBuilder_GlobalEventBase>();
    }
}
