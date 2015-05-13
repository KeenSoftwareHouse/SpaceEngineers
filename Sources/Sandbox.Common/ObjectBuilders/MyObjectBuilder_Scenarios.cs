using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Medieval.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    [XmlType("Range")]
    public class MyObjectBuilder_WorldGeneratorPlayerStartingState_Range : MyObjectBuilder_WorldGeneratorPlayerStartingState
    {
        [ProtoMember(1)]
        public SerializableVector3 MinPosition;

        [ProtoMember(2)]
        public SerializableVector3 MaxPosition;
    }

    [MyObjectBuilderDefinition]
    [XmlType("GenerateTerrain")]
    public class MyObjectBuilder_WorldGeneratorOperation_GenerateTerrain : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember(1), XmlAttribute]
        public string Name;

        [ProtoMember(2)]
        public SerializableVector3 Size;
    }

    [MyObjectBuilderDefinition]
    [XmlType("WorldFromSave")]
    public class MyObjectBuilder_WorldGeneratorOperation_WorldFromSave : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember(1), XmlAttribute]
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
        [ProtoMember(1), XmlAttribute]
        public string Name;

        [ProtoMember(2)]
        public SerializableVector3 Size;

        [ProtoMember(3)]
        public string HeightMapFile;

        [ProtoMember(4)]
        public string BiomeMapFile;

        [ProtoMember(5)]
        public string TreeMapFile;

        [ProtoMember(6)]
        public string TreeMaskFile;

    }

    [MyObjectBuilderDefinition]
    [XmlType("GenerateStatues")]
    public class MyObjectBuilder_WorldGeneratorOperation_GenerateStatues : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember(1), XmlAttribute]
        public int Count;
    }

    [MyObjectBuilderDefinition]
    [XmlType("TestTrees")]
    public class MyObjectBuilder_WorldGeneratorOperation_TestTrees : MyObjectBuilder_WorldGeneratorOperation
    {
    }
}
