using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemTerminalBlock : MyObjectBuilder_ToolbarItemTerminal
    {
        [ProtoMember]
        public long BlockEntityId;
        
        public override void Remap(IMyRemapHelper remapHelper)
        {
            BlockEntityId = remapHelper.RemapEntityId(BlockEntityId);
        }
    }
}
