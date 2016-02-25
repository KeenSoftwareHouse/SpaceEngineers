using VRage.ObjectBuilders;
using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;


namespace VRage.Game
{
    // MZ: Move to medieval? Currently referenced by MyObjectBuilder_Definitions, cannot move.
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CuttingDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class MyCuttingPrefab
        {
            [ProtoMember, DefaultValue(null)]
            public string Prefab = null;

            [ProtoMember, DefaultValue(1)]
            public int SpawnCount = 1;

            [ProtoMember, DefaultValue(null)]
            public SerializableDefinitionId? PhysicalItemId;  // If prefab field is null, this is used to spawn product of cutting as floating object
        }

        [ProtoMember]
        public SerializableDefinitionId EntityId;

        [ProtoMember]
        public SerializableDefinitionId ScrapWoodBranchesId;

        [ProtoMember]
        public SerializableDefinitionId ScrapWoodId;

		[ProtoMember]
		public int ScrapWoodAmountMin = 5;

		[ProtoMember]
		public int ScrapWoodAmountMax = 7;

		[ProtoMember]
		public int CraftingScrapWoodAmountMin = 1;

		[ProtoMember]
		public int CraftingScrapWoodAmountMax = 3;

        [XmlArrayItem("CuttingPrefab")]
        [ProtoMember, DefaultValue(null)]
        public MyCuttingPrefab[] CuttingPrefabs = null;

        [ProtoMember]
        public bool DestroySourceAfterCrafting = true;

        [ProtoMember]
        public float CraftingTimeInSeconds = 0.5f;
    }
}
