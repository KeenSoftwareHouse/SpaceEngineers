using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MultiBlockDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class MyOBMultiBlockPartDefinition
        {
            [ProtoMember]
            public SerializableDefinitionId Id;

            [ProtoMember]
            public SerializableVector3I Position;

            [ProtoMember]
            public SerializableBlockOrientation Orientation;
        }

        [XmlArrayItem("BlockDefinition")]
        [ProtoMember, DefaultValue(null)]
        public MyOBMultiBlockPartDefinition[] BlockDefinitions = null;
    }
}
