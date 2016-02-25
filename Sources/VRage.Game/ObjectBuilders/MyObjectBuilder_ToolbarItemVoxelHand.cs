using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemVoxelHand : MyObjectBuilder_ToolbarItemDefinition
    {
        public SerializableDefinitionId defId
        {
            get { return base.DefinitionId; }
            set { base.DefinitionId = value; }
        }
        public bool ShouldSerializedefId() { return false; }
    }
}
