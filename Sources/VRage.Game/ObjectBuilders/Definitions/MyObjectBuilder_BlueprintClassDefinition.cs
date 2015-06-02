using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlueprintClassDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("dds")]
        public string HighlightIcon;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string InputConstraintIcon;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string OutputConstraintIcon;
    }
}
