using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemTerminalGroup : MyObjectBuilder_ToolbarItemTerminal
    {
        //Old save compatibility
        public long GridEntityId;

        [ProtoMember(1)]
        public long BlockEntityId;
        [ProtoMember(2)]
        public string GroupName;
        
        public override void Remap(IMyRemapHelper remapHelper)
        {
            if (BlockEntityId != 0)
                BlockEntityId = remapHelper.RemapEntityId(BlockEntityId);
        }
    }
}
