using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMaterialChangesDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlArrayItem("Group")]
        public MyVoxelMapGroup[] Groups;

        [ProtoMember]
        [XmlArrayItem("Modifier")]
        public MyVoxelMapModifier[] Modifiers;
    }

    [ProtoContract]
    public class MyVoxelMapGroupItem
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "SubtypeId")]
        public string SubtypeId;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Chance")]
        public float Chance = 0f;
    }

    [ProtoContract]
    public class MyVoxelMapGroup
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "GroupId")]
        public string GroupId;

        [ProtoMember]
        public float ChanceTotal = 0f;

        [ProtoMember]
        [XmlArrayItem("Item")]
        public MyVoxelMapGroupItem[] Items;
    }

    [ProtoContract]
    public class MyVoxelMapModifier
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "ModifierId")]
        public string ModifierId;

        [ProtoMember]
        [XmlArrayItem("Option")]
        public MyVoxelMapModifierOption[] Options;

        [ProtoMember]
        public float ChanceTotal = 0f;
    }

    [ProtoContract]
    public class MyVoxelMapModifierOption
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Chance")]
        public float Chance;

        [ProtoMember]
        [XmlArrayItem("Change")]
        public MyVoxelMapModifierChange[] Changes;
    }

    [ProtoContract]
    public class MyVoxelMapModifierChange
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "From")]
        public string From;

        [ProtoMember]
        public byte FromIndex = 0;

        [ProtoMember]
        [XmlAttribute(AttributeName = "To")]
        public string To;

        [ProtoMember]
        public byte ToIndex = 0;
    }

    public class MyVoxelMaterialChangesDefinition
    {
        public MyVoxelMapGroup[] Groups;
        public MyVoxelMapModifier[] Modifiers;
    }
}