using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]

    public class MyObjectBuilder_FireLightBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember(1)]
        public bool Enabled = true;
    }
}
