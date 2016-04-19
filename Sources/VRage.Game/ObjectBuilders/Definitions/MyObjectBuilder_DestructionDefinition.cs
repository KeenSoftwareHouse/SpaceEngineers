using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DestructionDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember, DefaultValue(100f)]
        public float DestructionDamage = 100f;

        [ProtoMember, DefaultValue(new string[] {"Textures\\GUI\\Icons\\Fake.dds"})]
        [XmlElement("Icon")]
        new public string[] Icons = new string[] {"Textures\\GUI\\Icons\\Fake.dds"};

        // Integrity ratio of converted fracture block part (original block and fracture component). Set when fracture block has full itegrity only.
        [ProtoMember, DefaultValue(0.75f)]
        public float ConvertedFractureIntegrityRatio = 0.75f;

        [ProtoContract]
        public struct MyOBFracturedPieceDefinition
        {
            public SerializableDefinitionId Id;
            public int Age; // [s]
        }

        [XmlArrayItem("FracturedPiece")]
        [ProtoMember]
        public MyOBFracturedPieceDefinition[] FracturedPieceDefinitions;
    }
}
