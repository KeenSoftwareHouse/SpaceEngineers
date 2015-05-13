using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_WheelBlock : MyObjectBuilder_CubeBlock
    {
    }
}
