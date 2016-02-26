using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ComponentGroupDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct Component
        {
            [ProtoMember]
            [XmlAttribute]
            public string SubtypeId;

            [ProtoMember]
            [XmlAttribute]
            public int Amount;
        }

        [XmlArrayItem("Component")]
        [ProtoMember]
        public Component[] Components;
    }
}
