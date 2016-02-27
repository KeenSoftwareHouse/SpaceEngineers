using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.ModAPI;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ToolbarItemTerminalGroup : MyObjectBuilder_ToolbarItemTerminal
    {
        //Old save compatibility
        public long GridEntityId;

        [ProtoMember]
        public long BlockEntityId;
        [ProtoMember]
        public string GroupName;
        
        public override void Remap(IMyRemapHelper remapHelper)
        {
            if (BlockEntityId != 0)
                BlockEntityId = remapHelper.RemapEntityId(BlockEntityId);
        }
    }
}
