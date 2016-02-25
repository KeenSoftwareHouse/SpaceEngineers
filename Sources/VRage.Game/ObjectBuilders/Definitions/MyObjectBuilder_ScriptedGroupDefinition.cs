using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScriptedGroupDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string Category;

        [ProtoMember]
        public string Script;
    }
}
