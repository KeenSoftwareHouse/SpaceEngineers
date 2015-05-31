using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemWeaponGroup : MyObjectBuilder_ToolbarItem
    {
        [ProtoMember]
        public string GroupName { get; set; }
        [ProtoMember]
        public string DisplayNameText { get; set; }
        [ProtoMember]
        public string Icon { get; set; }
    }
}
