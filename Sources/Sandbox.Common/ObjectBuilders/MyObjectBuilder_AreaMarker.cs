using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AreaMarker : MyObjectBuilder_EntityBase
    {
    }
}
