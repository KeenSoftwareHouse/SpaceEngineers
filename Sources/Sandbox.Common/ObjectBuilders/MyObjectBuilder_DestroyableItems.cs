using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [MyEnvironmentItems(typeof(MyObjectBuilder_DestroyableItem))]
    public class MyObjectBuilder_DestroyableItems : MyObjectBuilder_EnvironmentItems
    {
    }
}
