using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Configuration : MyObjectBuilder_Base
    {
        [ProtoContract]
        public struct CubeSizeSettings
        {
            [ProtoMember, XmlAttribute]
            public float Large;

            [ProtoMember, XmlAttribute]
            public float Small;

            [ProtoMember, XmlAttribute]
            public float SmallOriginal;
        }

        [ProtoContract]
        public struct BaseBlockSettings
        {
            [ProtoMember, XmlAttribute]
            public string SmallStatic;

            [ProtoMember, XmlAttribute]
            public string LargeStatic;

            [ProtoMember, XmlAttribute]
            public string SmallDynamic;

            [ProtoMember, XmlAttribute]
            public string LargeDynamic;
        }

        [ProtoContract]
        public class LootBagDefinition
        {
            [ProtoMember]
            public SerializableDefinitionId ContainerDefinition;

            // Radius for searching lootbag nearby player.
            [ProtoMember, XmlAttribute]
            public float SearchRadius = 3;
        }

        [ProtoMember]
        public CubeSizeSettings CubeSizes;

        [ProtoMember]
        public BaseBlockSettings BaseBlockPrefabs;

        [ProtoMember]
        public BaseBlockSettings BaseBlockPrefabsSurvival;

        // Definition of loot bag - spawned when an inventory item cannot be spawn in world (no free place found).
        [ProtoMember]
        public LootBagDefinition LootBag;

    }
}
