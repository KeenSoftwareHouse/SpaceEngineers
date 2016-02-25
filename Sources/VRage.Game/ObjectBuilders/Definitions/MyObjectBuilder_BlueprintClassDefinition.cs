using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Data;
using System.ComponentModel;

namespace VRage.Game
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

        [ProtoMember, DefaultValue(null)]
        public string ProgressBarSoundCue = null;        
    }
}
