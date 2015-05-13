using ProtoBuf;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BarbarianBot : MyObjectBuilder_HumanoidBot
    {
    }
}
