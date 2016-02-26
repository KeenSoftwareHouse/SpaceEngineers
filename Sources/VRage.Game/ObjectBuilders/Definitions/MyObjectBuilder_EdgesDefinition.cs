using ProtoBuf;
using VRage.Data;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    public class MyEdgesModelSet
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Vertical;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string VerticalDiagonal;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Horisontal;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string HorisontalDiagonal;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EdgesDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyEdgesModelSet Small;

        [ProtoMember]
        public MyEdgesModelSet Large;
    }
}