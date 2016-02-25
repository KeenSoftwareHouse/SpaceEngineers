using VRage.ObjectBuilders;
using VRageMath;
using ProtoBuf;
using VRage.ModAPI;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
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
