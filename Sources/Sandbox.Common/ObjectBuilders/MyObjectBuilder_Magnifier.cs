using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medieval.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Magnifier : MyObjectBuilder_EntityBase
    {
    }
}
