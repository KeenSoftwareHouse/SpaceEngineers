using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public class MyVoxelMapModifierOption
    {
        [XmlAttribute(AttributeName = "Chance")]
        public float Chance;

        [XmlElement("Change")]
        public MyVoxelMapModifierChange[] Changes;
    }

    public struct MyVoxelMapModifierChange
    {
        [XmlAttribute(AttributeName = "From")]
        public string From;

        [XmlAttribute(AttributeName = "To")]
        public string To;
    }

    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMaterialModifierDefinition : MyObjectBuilder_DefinitionBase
    {
        [XmlArrayItem("Option")]
        public MyVoxelMapModifierOption[] Options;
    }
}