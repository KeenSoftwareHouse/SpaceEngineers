using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Data;
using VRage.Game;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FontDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember, ModdableContentFile(".xml")]
	    public string Path;
        [ProtoMember]
        public bool Default = false;
    }
}
