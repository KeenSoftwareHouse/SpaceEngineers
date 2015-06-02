using ProtoBuf;
using VRage.ObjectBuilders;

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
