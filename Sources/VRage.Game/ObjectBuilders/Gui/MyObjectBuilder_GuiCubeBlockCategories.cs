#region Using

using ProtoBuf;
using VRage.ObjectBuilders;

#endregion

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiBlockCategoryDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public string[] ItemIds;

        [ProtoMember]
        public bool IsShipCategory = false;

        [ProtoMember]
        public bool IsBlockCategory = true;

        [ProtoMember]
        public bool SearchBlocks = true;

        [ProtoMember]
        public bool ShowAnimations = false;
    }
}
