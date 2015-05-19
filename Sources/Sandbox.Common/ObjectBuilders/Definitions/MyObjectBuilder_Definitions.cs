using ProtoBuf;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.Audio;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [XmlRoot("Definitions")]
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Definitions : MyObjectBuilder_Base
    {
        [XmlArrayItem("AmmoMagazine")]
        [ProtoMember(1)]
        public MyObjectBuilder_AmmoMagazineDefinition[] AmmoMagazines;

        [XmlArrayItem("Blueprint")]
        [ProtoMember(2)]
        public MyObjectBuilder_BlueprintDefinition[] Blueprints;

        [XmlArrayItem("Component")]
        [ProtoMember(3)]
        public MyObjectBuilder_ComponentDefinition[] Components;

        [XmlArrayItem("ContainerType")]
        [ProtoMember(4)]
        public MyObjectBuilder_ContainerTypeDefinition[] ContainerTypes;

        [XmlArrayItem("Definition", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CubeBlockDefinition>))]
        [ProtoMember(5)]
        public MyObjectBuilder_CubeBlockDefinition[] CubeBlocks;

        [XmlArrayItem("BlockPosition")]
        [ProtoMember(6)]
        public MyBlockPosition[] BlockPositions;

        [ProtoMember(7)]
        public MyObjectBuilder_Configuration Configuration;

        [ProtoMember(8)]
        public MyObjectBuilder_EnvironmentDefinition Environment;

        [XmlArrayItem("GlobalEvent")]
        [ProtoMember(9)]
        public MyObjectBuilder_GlobalEventDefinition[] GlobalEvents;

        [XmlArrayItem("HandItem")]
        [ProtoMember(10)]
        public MyObjectBuilder_HandItemDefinition[] HandItems;

        [XmlArrayItem("PhysicalItem", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalItemDefinition>))]
        [ProtoMember(11)]
        public MyObjectBuilder_PhysicalItemDefinition[] PhysicalItems;

        [XmlArrayItem("SpawnGroup")]
        [ProtoMember(12)]
        public MyObjectBuilder_SpawnGroupDefinition[] SpawnGroups;

        [XmlArrayItem("TransparentMaterial")]
        [ProtoMember(13)]
        public MyObjectBuilder_TransparentMaterialDefinition[] TransparentMaterials;

        [XmlArrayItem("VoxelMaterial", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_VoxelMaterialDefinition>))]
        [ProtoMember(14)]
        public MyObjectBuilder_VoxelMaterialDefinition[] VoxelMaterials;

        [XmlArrayItem("Character")]
        [ProtoMember(15)]
        public MyObjectBuilder_CharacterDefinition[] Characters;

        [XmlArrayItem("Animation")]
        [ProtoMember(16)]
        public MyObjectBuilder_AnimationDefinition[] Animations;

        [XmlArrayItem("Debris")]
        [ProtoMember(17)]
        public MyObjectBuilder_DebrisDefinition[] Debris;

        [XmlArrayItem("Edges")]
        [ProtoMember(18)]
        public MyObjectBuilder_EdgesDefinition[] Edges;

        [XmlArrayItem("Prefab")]
        [ProtoMember(19)]
        public MyObjectBuilder_PrefabDefinition[] Prefabs;

        [XmlArrayItem("Class")]
        [ProtoMember(20)]
        public MyObjectBuilder_BlueprintClassDefinition[] BlueprintClasses;

        [XmlArrayItem("Entry")]
        [ProtoMember(21)]
        public BlueprintClassEntry[] BlueprintClassEntries;

        [XmlArrayItem("EnvironmentItem", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EnvironmentItemDefinition>))]
        [ProtoMember(22)]
        public MyObjectBuilder_EnvironmentItemDefinition[] EnvironmentItems;

        [XmlArrayItem("Template", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CompoundBlockTemplateDefinition>))]
        [ProtoMember(23)]
        public MyObjectBuilder_CompoundBlockTemplateDefinition[] CompoundBlockTemplates;

        [XmlArrayItem("Ship", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_RespawnShipDefinition>))]
        [ProtoMember(24)]
        public MyObjectBuilder_RespawnShipDefinition[] RespawnShips;

        [XmlArrayItem("Category")]
        [ProtoMember(25)]
        public MyObjectBuilder_GuiBlockCategoryDefinition[] CategoryClasses;

        [XmlArrayItem("ShipBlueprint")]
        [ProtoMember(26)]
        public MyObjectBuilder_ShipBlueprintDefinition[] ShipBlueprints;

        [XmlArrayItem("Weapon")]
        [ProtoMember(27)]
        public MyObjectBuilder_WeaponDefinition[] Weapons;

        [XmlArrayItem("Ammo")]
        [ProtoMember(28)]
        public MyObjectBuilder_AmmoDefinition[] Ammos;

        [XmlArrayItem("Sound")]
        [ProtoMember(29)]
        public MyObjectBuilder_AudioDefinition[] Sounds;

        [XmlArrayItem("VoxelHand")]
        [ProtoMember(30)]
        public MyObjectBuilder_VoxelHandDefinition[] VoxelHands;

        [XmlArrayItem("MultiBlock")]
        [ProtoMember(31)]
        public MyObjectBuilder_MultiBlockDefinition[] MultiBlocks;

        [XmlArrayItem("PrefabThrower")]
        [ProtoMember(32)]
        public MyObjectBuilder_PrefabThrowerDefinition[] PrefabThrowers;

        [XmlArrayItem("SoundCategory")]
        [ProtoMember(33)]
        public MyObjectBuilder_SoundCategoryDefinition[] SoundCategories;

        [XmlArrayItem("AIBehavior")]
        [ProtoMember(34)]
        public MyObjectBuilder_BehaviorTreeDefinition[] AIBehaviors;

        [XmlArrayItem("VoxelMapStorage")]
        [ProtoMember(35)]
        public MyObjectBuilder_VoxelMapStorageDefinition[] VoxelMapStorages;

        [XmlArrayItem("LCDTextureDefinition")]
        [ProtoMember(36)]
        public MyObjectBuilder_LCDTextureDefinition[] LCDTextures;
        
        [XmlArrayItem("Bot")]
        [ProtoMember(37)]
        public MyObjectBuilder_BotDefinition[] Bots;

        [XmlArrayItem("Rope")]
        [ProtoMember(38)]
        public MyObjectBuilder_RopeDefinition[] RopeTypes;

        [XmlArrayItem("PhysicalMaterial")]
        [ProtoMember(39)]
        public MyObjectBuilder_PhysicalMaterialDefinition[] PhysicalMaterials;

        [XmlArrayItem("AiCommand")]
        [ProtoMember(40)]
        public MyObjectBuilder_AiCommandDefinition[] AiCommands;

        [XmlArrayItem("NavDef")]
        [ProtoMember(41)]
        public MyObjectBuilder_BlockNavigationDefinition[] BlockNavigationDefinitions;

        [XmlArrayItem("Cutting")]
        [ProtoMember(42)]
        public MyObjectBuilder_CuttingDefinition[] Cuttings;

        [XmlArrayItem("Sounds")]
        [ProtoMember(43)]
        public MyObjectBuilder_MaterialSoundsDefinition[] MaterialSounds;

        [XmlArrayItem("ControllerSchema")]
        [ProtoMember(44)]
        public MyObjectBuilder_ControllerSchemaDefinition[] ControllerSchemas;

        [XmlArrayItem("SoundCurve")]
        [ProtoMember(45)]
        public MyObjectBuilder_CurveDefinition[] CurveDefinitions;

        [XmlArrayItem("Effect")]
        [ProtoMember(46)]
        public MyObjectBuilder_AudioEffectDefinition[] AudioEffects;

        [XmlArrayItem("Definition")]
        [ProtoMember(47)]
        public MyObjectBuilder_EnvironmentItemsDefinition[] EnvironmentItemsDefinitions;

        [XmlArrayItem("Entry")]
        [ProtoMember(48)]
        public EnvironmentItemsEntry[] EnvironmentItemsEntries;
        
        [XmlArrayItem("LCDFontDefinition")]
        [ProtoMember(49)]
        public MyObjectBuilder_LCDFontDefinition[] LCDFonts;
    }
}
