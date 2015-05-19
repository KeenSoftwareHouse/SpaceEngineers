using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using System.Xml.Serialization;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TorsionSpringBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
        public float Angle;

        [ProtoMember]
        public bool State;
    }
}
