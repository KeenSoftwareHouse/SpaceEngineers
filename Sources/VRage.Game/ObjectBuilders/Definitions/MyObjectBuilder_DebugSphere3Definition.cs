using System;
using System.Collections.Generic;
using System.Linq;
using VRage.ObjectBuilders;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DebugSphere3Definition : MyObjectBuilder_CubeBlockDefinition
    {
    }
}
