using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.Definitions;

namespace VRage.Game
{
    [XmlRoot("Definitions")]
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Definitions : MyObjectBuilder_Base
    {
        [XmlElement(MyDefinitionXmlSerializer.DEFINITION_ELEMENT_NAME, Type = typeof(MyDefinitionXmlSerializer))]
        public MyObjectBuilder_DefinitionBase[] Definitions;

        [XmlArrayItem("GridCreator")]
        [ProtoMember]
        public MyObjectBuilder_GridCreateToolDefinition[] GridCreators;

        [XmlArrayItem("AmmoMagazine", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_AmmoMagazineDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_AmmoMagazineDefinition[] AmmoMagazines;

        [XmlArrayItem("Blueprint", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_BlueprintDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_BlueprintDefinition[] Blueprints;

        [XmlArrayItem("Component", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ComponentDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ComponentDefinition[] Components;

        [XmlArrayItem("ContainerType", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ContainerTypeDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ContainerTypeDefinition[] ContainerTypes;

        [XmlArrayItem("Definition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CubeBlockDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_CubeBlockDefinition[] CubeBlocks;

        [XmlArrayItem("BlockPosition")]
        [ProtoMember]
        public MyBlockPosition[] BlockPositions;

        [ProtoMember]
        [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_Configuration>))]
        public MyObjectBuilder_Configuration Configuration;

        [ProtoMember]
        [XmlElement("Environment", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EnvironmentDefinition>))]
        public MyObjectBuilder_EnvironmentDefinition[] Environments;

        [XmlArrayItem("GlobalEvent", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_GlobalEventDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_GlobalEventDefinition[] GlobalEvents;

        [XmlArrayItem("HandItem", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_HandItemDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_HandItemDefinition[] HandItems;

        [XmlArrayItem("PhysicalItem", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalItemDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PhysicalItemDefinition[] PhysicalItems;

        [XmlArrayItem("SpawnGroup", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_SpawnGroupDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_SpawnGroupDefinition[] SpawnGroups;

        [XmlArrayItem("TransparentMaterial", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_TransparentMaterialDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_TransparentMaterialDefinition[] TransparentMaterials;

        [XmlArrayItem("VoxelMaterial", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_VoxelMaterialDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_VoxelMaterialDefinition[] VoxelMaterials;

        [XmlArrayItem("Character", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CharacterDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_CharacterDefinition[] Characters;

        [XmlArrayItem("Animation", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_AnimationDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_AnimationDefinition[] Animations;

        [XmlArrayItem("Debris", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_DebrisDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_DebrisDefinition[] Debris;

        [XmlArrayItem("Edges", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EdgesDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_EdgesDefinition[] Edges;

        [XmlArrayItem("Faction", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_FactionDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_FactionDefinition[] Factions;

        [XmlArrayItem("Prefab", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PrefabDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PrefabDefinition[] Prefabs;

        [XmlArrayItem("Class")]
        [ProtoMember]
        public MyObjectBuilder_BlueprintClassDefinition[] BlueprintClasses;

        [XmlArrayItem("Entry")]
        [ProtoMember]
        public BlueprintClassEntry[] BlueprintClassEntries;

        [XmlArrayItem("EnvironmentItem", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EnvironmentItemDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_EnvironmentItemDefinition[] EnvironmentItems;

        [XmlArrayItem("Template", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CompoundBlockTemplateDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_CompoundBlockTemplateDefinition[] CompoundBlockTemplates;

        [XmlArrayItem("Ship", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_RespawnShipDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_RespawnShipDefinition[] RespawnShips;

        [XmlArrayItem("Category", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_GuiBlockCategoryDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_GuiBlockCategoryDefinition[] CategoryClasses;

        [XmlArrayItem("ShipBlueprint", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ShipBlueprintDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ShipBlueprintDefinition[] ShipBlueprints;

        [XmlArrayItem("Weapon", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_WeaponDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_WeaponDefinition[] Weapons;

        [XmlArrayItem("Ammo", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_AmmoDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_AmmoDefinition[] Ammos;

        [XmlArrayItem("Sound", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_AudioDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_AudioDefinition[] Sounds;

        [XmlArrayItem("VoxelHand", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_VoxelHandDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_VoxelHandDefinition[] VoxelHands;

        [XmlArrayItem("MultiBlock", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_MultiBlockDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_MultiBlockDefinition[] MultiBlocks;

        [XmlArrayItem("PrefabThrower", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PrefabThrowerDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PrefabThrowerDefinition[] PrefabThrowers;

        [XmlArrayItem("SoundCategory", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_SoundCategoryDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_SoundCategoryDefinition[] SoundCategories;

        [XmlArrayItem("ShipSoundGroup", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ShipSoundsDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ShipSoundsDefinition[] ShipSoundGroups;

        [ProtoMember]
        [XmlArrayItem("DroneBehavior", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_DroneBehaviorDefinition>))]
        public MyObjectBuilder_DroneBehaviorDefinition[] DroneBehaviors;

        [XmlElement("ShipSoundSystem", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ShipSoundSystemDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ShipSoundSystemDefinition ShipSoundSystem;

        [XmlArrayItem("ParticleEffect", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ParticleEffect>))]
        [ProtoMember]
        public MyObjectBuilder_ParticleEffect[] ParticleEffects;

        [XmlArrayItem("AIBehavior", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_BehaviorTreeDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_BehaviorTreeDefinition[] AIBehaviors;

        [XmlArrayItem("VoxelMapStorage", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_VoxelMapStorageDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_VoxelMapStorageDefinition[] VoxelMapStorages;

        [XmlArrayItem("LCDTextureDefinition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_LCDTextureDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_LCDTextureDefinition[] LCDTextures;

        [XmlArrayItem("Bot", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_BotDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_BotDefinition[] Bots;

        [XmlArrayItem("Rope", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_RopeDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_RopeDefinition[] RopeTypes;

        [XmlArrayItem("PhysicalMaterial", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalMaterialDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PhysicalMaterialDefinition[] PhysicalMaterials;

        [XmlArrayItem("AiCommand", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_AiCommandDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_AiCommandDefinition[] AiCommands;

        [XmlArrayItem("NavDef", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_BlockNavigationDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_BlockNavigationDefinition[] BlockNavigationDefinitions;

        [XmlArrayItem("Cutting", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CuttingDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_CuttingDefinition[] Cuttings;

        [XmlArrayItem("Properties", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_MaterialPropertiesDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_MaterialPropertiesDefinition[] MaterialProperties;

        [XmlArrayItem("ControllerSchema", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ControllerSchemaDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ControllerSchemaDefinition[] ControllerSchemas;

        [XmlArrayItem("SoundCurve", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CurveDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_CurveDefinition[] CurveDefinitions;

        [XmlArrayItem("Effect", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_AudioEffectDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_AudioEffectDefinition[] AudioEffects;

        [XmlArrayItem("Definition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EnvironmentItemsDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_EnvironmentItemsDefinition[] EnvironmentItemsDefinitions;

        [XmlArrayItem("Entry")]
        [ProtoMember]
        public EnvironmentItemsEntry[] EnvironmentItemsEntries;

        [XmlArrayItem("Definition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_AreaMarkerDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_AreaMarkerDefinition[] AreaMarkerDefinitions;

        [XmlArrayItem("Entry")]
        [ProtoMember]
        public MyCharacterName[] CharacterNames;

        [ProtoMember]
        [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_BattleDefinition>))]
        public MyObjectBuilder_BattleDefinition Battle;

        [ProtoMember]
        public MyObjectBuilder_DecalGlobalsDefinition DecalGlobals;

        [XmlArrayItem("Decal")]
        [ProtoMember]
        public MyObjectBuilder_DecalDefinition[] Decals;

        [XmlArrayItem("PlanetGeneratorDefinition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PlanetGeneratorDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PlanetGeneratorDefinition[] PlanetGeneratorDefinitions;

        [XmlArrayItem("Definition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_FloraElementDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_FloraElementDefinition[] FloraElements;

        [XmlArrayItem("Stat")]
		[ProtoMember]
		public MyObjectBuilder_EntityStatDefinition[] StatDefinitions;

        [XmlArrayItem("Gas")]
		[ProtoMember]
		public MyObjectBuilder_GasProperties[] GasProperties;

        [XmlArrayItem("DistributionGroup")]
		[ProtoMember]
		public MyObjectBuilder_ResourceDistributionGroup[] ResourceDistributionGroups;

        [XmlArrayItem("Group", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ComponentGroupDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ComponentGroupDefinition[] ComponentGroups;

        [XmlArrayItem("Substitution")]
        [ProtoMember]
        public MyObjectBuilder_ComponentSubstitutionDefinition[] ComponentSubstitutions;

        [XmlArrayItem("Block")]
        [ProtoMember]
        public MyComponentBlockEntry[] ComponentBlocks;

        [XmlArrayItem("PlanetPrefab", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PlanetPrefabDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PlanetPrefabDefinition[] PlanetPrefabs;

        [XmlArrayItem("Group")]
        [ProtoMember]
        public MyGroupedIds[] EnvironmentGroups;

        [XmlArrayItem("Group", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ScriptedGroupDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ScriptedGroupDefinition[] ScriptedGroups;

        [XmlArrayItem("Map")]
        [ProtoMember]
        public MyMappedId[] ScriptedGroupsMap;

        [XmlArrayItem("Antenna", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PirateAntennaDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PirateAntennaDefinition[] PirateAntennas;

        [ProtoMember]
        public MyObjectBuilder_DestructionDefinition Destruction;

        [XmlArrayItem("EntityComponent", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ComponentDefinitionBase>))]
        [ProtoMember]
        public MyObjectBuilder_ComponentDefinitionBase[] EntityComponents;

        [XmlArrayItem("Container", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ContainerDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_ContainerDefinition[] EntityContainers;

        [ProtoMember]
        [XmlArrayItem("ShadowTextureSet")]
        public MyObjectBuilder_ShadowTextureSetDefinition[] ShadowTextureSets;

        [XmlArrayItem("Font", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_FontDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_FontDefinition[] Fonts;
    }
}
