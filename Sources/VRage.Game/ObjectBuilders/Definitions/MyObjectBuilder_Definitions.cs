using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.Audio;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [XmlRoot("Definitions")]
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Definitions : MyObjectBuilder_Base
    {
        [XmlArrayItem("AmmoMagazine")]
        [ProtoMember]
        public MyObjectBuilder_AmmoMagazineDefinition[] AmmoMagazines;

        [XmlArrayItem("Blueprint")]
        [ProtoMember]
        public MyObjectBuilder_BlueprintDefinition[] Blueprints;

        [XmlArrayItem("Component")]
        [ProtoMember]
        public MyObjectBuilder_ComponentDefinition[] Components;

        [XmlArrayItem("ContainerType")]
        [ProtoMember]
        public MyObjectBuilder_ContainerTypeDefinition[] ContainerTypes;

        [XmlArrayItem("Definition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CubeBlockDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_CubeBlockDefinition[] CubeBlocks;

        [XmlArrayItem("BlockPosition")]
        [ProtoMember]
        public MyBlockPosition[] BlockPositions;

        [ProtoMember]
        public MyObjectBuilder_Configuration Configuration;

        [ProtoMember]
        public MyObjectBuilder_EnvironmentDefinition Environment;

        [XmlArrayItem("GlobalEvent")]
        [ProtoMember]
        public MyObjectBuilder_GlobalEventDefinition[] GlobalEvents;

        [XmlArrayItem("HandItem")]
        [ProtoMember]
        public MyObjectBuilder_HandItemDefinition[] HandItems;

        [XmlArrayItem("PhysicalItem", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalItemDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_PhysicalItemDefinition[] PhysicalItems;

        [XmlArrayItem("SpawnGroup")]
        [ProtoMember]
        public MyObjectBuilder_SpawnGroupDefinition[] SpawnGroups;

        [XmlArrayItem("TransparentMaterial")]
        [ProtoMember]
        public MyObjectBuilder_TransparentMaterialDefinition[] TransparentMaterials;

        [XmlArrayItem("VoxelMaterial", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_VoxelMaterialDefinition>))]
        [ProtoMember]
        public MyObjectBuilder_VoxelMaterialDefinition[] VoxelMaterials;

        [XmlArrayItem("Character")]
        [ProtoMember]
        public MyObjectBuilder_CharacterDefinition[] Characters;

        [XmlArrayItem("Animation")]
        [ProtoMember]
        public MyObjectBuilder_AnimationDefinition[] Animations;

        [XmlArrayItem("Debris")]
        [ProtoMember]
        public MyObjectBuilder_DebrisDefinition[] Debris;

        [XmlArrayItem("Edges")]
        [ProtoMember]
        public MyObjectBuilder_EdgesDefinition[] Edges;

        [XmlArrayItem("Prefab")]
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

        [XmlArrayItem("Category")]
        [ProtoMember]
        public MyObjectBuilder_GuiBlockCategoryDefinition[] CategoryClasses;

        [XmlArrayItem("ShipBlueprint")]
        [ProtoMember]
        public MyObjectBuilder_ShipBlueprintDefinition[] ShipBlueprints;

        [XmlArrayItem("Weapon")]
        [ProtoMember]
        public MyObjectBuilder_WeaponDefinition[] Weapons;

        [XmlArrayItem("Ammo")]
        [ProtoMember]
        public MyObjectBuilder_AmmoDefinition[] Ammos;

        [XmlArrayItem("Sound")]
        [ProtoMember]
        public MyObjectBuilder_AudioDefinition[] Sounds;

        [XmlArrayItem("VoxelHand")]
        [ProtoMember]
        public MyObjectBuilder_VoxelHandDefinition[] VoxelHands;

        [XmlArrayItem("MultiBlock")]
        [ProtoMember]
        public MyObjectBuilder_MultiBlockDefinition[] MultiBlocks;

        [XmlArrayItem("PrefabThrower")]
        [ProtoMember]
        public MyObjectBuilder_PrefabThrowerDefinition[] PrefabThrowers;

        [XmlArrayItem("SoundCategory")]
        [ProtoMember]
        public MyObjectBuilder_SoundCategoryDefinition[] SoundCategories;

        [XmlArrayItem("AIBehavior")]
        [ProtoMember]
        public MyObjectBuilder_BehaviorTreeDefinition[] AIBehaviors;

        [XmlArrayItem("VoxelMapStorage")]
        [ProtoMember]
        public MyObjectBuilder_VoxelMapStorageDefinition[] VoxelMapStorages;

        [XmlArrayItem("LCDTextureDefinition")]
        [ProtoMember]
        public MyObjectBuilder_LCDTextureDefinition[] LCDTextures;

        [XmlArrayItem("Bot")]
        [ProtoMember]
        public MyObjectBuilder_BotDefinition[] Bots;

        [XmlArrayItem("Rope")]
        [ProtoMember]
        public MyObjectBuilder_RopeDefinition[] RopeTypes;

        [XmlArrayItem("PhysicalMaterial")]
        [ProtoMember]
        public MyObjectBuilder_PhysicalMaterialDefinition[] PhysicalMaterials;

        [XmlArrayItem("AiCommand")]
        [ProtoMember]
        public MyObjectBuilder_AiCommandDefinition[] AiCommands;

        [XmlArrayItem("NavDef")]
        [ProtoMember]
        public MyObjectBuilder_BlockNavigationDefinition[] BlockNavigationDefinitions;

        [XmlArrayItem("Cutting")]
        [ProtoMember]
        public MyObjectBuilder_CuttingDefinition[] Cuttings;

        [XmlArrayItem("Sounds")]
        [ProtoMember]
        public MyObjectBuilder_MaterialSoundsDefinition[] MaterialSounds;

        [XmlArrayItem("ControllerSchema")]
        [ProtoMember]
        public MyObjectBuilder_ControllerSchemaDefinition[] ControllerSchemas;

        [XmlArrayItem("SoundCurve")]
        [ProtoMember]
        public MyObjectBuilder_CurveDefinition[] CurveDefinitions;

        [XmlArrayItem("Effect")]
        [ProtoMember]
        public MyObjectBuilder_AudioEffectDefinition[] AudioEffects;

        [XmlArrayItem("Definition")]
        [ProtoMember]
        public MyObjectBuilder_EnvironmentItemsDefinition[] EnvironmentItemsDefinitions;

        [XmlArrayItem("Entry")]
        [ProtoMember]
        public EnvironmentItemsEntry[] EnvironmentItemsEntries;

        [XmlArrayItem("Definition")]
        [ProtoMember]
        public MyObjectBuilder_AreaMarkerDefinition[] AreaMarkerDefinitions;

        [XmlArrayItem("Entry")]
        [ProtoMember]
        public MyCharacterName[] CharacterNames;

        [ProtoMember]
        public MyObjectBuilder_BattleDefinition Battle;

        [XmlArrayItem("Decal")]
        [ProtoMember]
        public MyObjectBuilder_DecalDefinition[] Decals;

        [XmlArrayItem("PlanetDefinition")]
        [ProtoMember]
        public MyObjectBuilder_PlanetDefinition[] PlanetDefinitions;

        [XmlArrayItem("Definition")]
        [ProtoMember]
        public MyObjectBuilder_FloraElementDefinition[] FloraElements;

		[XmlArrayItem("StatGroup")]
		[ProtoMember]
		public MyObjectBuilder_StatsDefinition[] StatGroupDefinitions;

		[XmlArrayItem("Stat")]
		[ProtoMember]
		public MyObjectBuilder_EntityStatDefinition[] StatDefinitions;

        [XmlArrayItem("Group")]
        [ProtoMember]
        public MyObjectBuilder_ComponentGroupDefinition[] ComponentGroups;

        [XmlArrayItem("Block")]
        [ProtoMember]
        public MyComponentBlockEntry[] ComponentBlocks;
    }
}
