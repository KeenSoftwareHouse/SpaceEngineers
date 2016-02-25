using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;

namespace Medieval.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    [XmlType("Range")]
    public class MyObjectBuilder_WorldGeneratorPlayerStartingState_Range : MyObjectBuilder_WorldGeneratorPlayerStartingState
    {
        [ProtoMember]
        public SerializableVector3 MinPosition;

        [ProtoMember]
        public SerializableVector3 MaxPosition;
    }

    [MyObjectBuilderDefinition]
    [XmlType("GenerateTerrain")]
    public class MyObjectBuilder_WorldGeneratorOperation_GenerateTerrain : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string Name;

        [ProtoMember]
        public SerializableVector3 Size;
    }

    [MyObjectBuilderDefinition]
    [XmlType("WorldFromSave")]
    public class MyObjectBuilder_WorldGeneratorOperation_WorldFromSave : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string PrefabDirectory;
    }

    [MyObjectBuilderDefinition]
    [XmlType("BattleWorldFromSave")]
    public class MyObjectBuilder_WorldGeneratorOperation_BattleWorldFromSave : MyObjectBuilder_WorldGeneratorOperation_WorldFromSave
    {
    }

    [MyObjectBuilderDefinition]
    [XmlType("WorldFromMaps")]
    public class MyObjectBuilder_WorldGeneratorOperation_WorldFromMaps : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string Name;

        [ProtoMember]
        public SerializableVector3 Size;

        [ProtoMember]
        public string HeightMapFile;

        [ProtoMember]
        public string BiomeMapFile;

        [ProtoMember]
        public string TreeMapFile;

        [ProtoMember]
        public string TreeMaskFile;

    }

    [MyObjectBuilderDefinition]
    [XmlType("GenerateStatues")]
    public class MyObjectBuilder_WorldGeneratorOperation_GenerateStatues : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public int Count;
    }

    [MyObjectBuilderDefinition]
    [XmlType("TestTrees")]
    public class MyObjectBuilder_WorldGeneratorOperation_TestTrees : MyObjectBuilder_WorldGeneratorOperation
    {
    }
}
