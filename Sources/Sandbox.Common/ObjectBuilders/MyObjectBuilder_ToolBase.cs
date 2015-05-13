using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Weapons
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolBase : MyObjectBuilder_DeviceBase
    {
    }
}
