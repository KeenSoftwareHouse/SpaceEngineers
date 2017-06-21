﻿#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.Definitions.Animation;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


#endregion

namespace Sandbox.Definitions
{
    public partial class MyDefinitionManager
    {
        internal class DefinitionDictionary<V> : Dictionary<MyDefinitionId, V>
            where V : MyDefinitionBase
        {
            public DefinitionDictionary(int capacity)
                : base(capacity, MyDefinitionId.Comparer)
            {
            }

            public void AddDefinitionSafe<T>(T definition, MyModContext context, string file)
                where T : V
            {
                if (definition.Id.TypeId != MyObjectBuilderType.Invalid)
                {
                    this[definition.Id] = definition;
                }
                else
                {
                    MyDefinitionErrors.Add(context, "Invalid definition id", TErrorSeverity.Error);
                }
            }

            public void Merge(DefinitionDictionary<V> other)
            {
                foreach (var definition in other)
                {
                    if (definition.Value.Enabled)
                        this[definition.Key] = definition.Value;
                    else
                        Remove(definition.Key);
                }
            }
        }

        internal class DefinitionSet : MyDefinitionSet
        {
            static DefinitionDictionary<MyDefinitionBase> m_helperDict = new DefinitionDictionary<MyDefinitionBase>(100);

            public DefinitionSet()
            {
                Clear();
            }

            public void Clear(bool unload = false)
            {
                base.Clear();

                m_cubeSizes = new float[typeof(MyCubeSize).GetEnumValues().Length];
                m_cubeSizesOriginal = new float[typeof(MyCubeSize).GetEnumValues().Length];
                m_basePrefabNames = new string[m_cubeSizes.Length * 4]; // Index computed 4 * enumInt + 2*static + creative.

                m_definitionsById = new DefinitionDictionary<MyDefinitionBase>(100);

                m_voxelMaterialsByName = new Dictionary<string, MyVoxelMaterialDefinition>(10);
                m_voxelMaterialsByIndex = new Dictionary<byte, MyVoxelMaterialDefinition>(10);
                m_voxelMaterialRareCount = 0;

                m_physicalItemDefinitions = new List<MyPhysicalItemDefinition>(10);

                m_weaponDefinitionsById = new DefinitionDictionary<MyWeaponDefinition>(10);
                m_ammoDefinitionsById = new DefinitionDictionary<MyAmmoDefinition>(10);

                m_blockPositions = new Dictionary<string, Vector2I>(10);
                m_uniqueCubeBlocksBySize = new DefinitionDictionary<MyCubeBlockDefinition>[m_cubeSizes.Length];
                for (int i = 0; i < m_cubeSizes.Length; ++i)
                {
                    m_uniqueCubeBlocksBySize[i] = new DefinitionDictionary<MyCubeBlockDefinition>(10);
                }

                m_blueprintsById = new DefinitionDictionary<MyBlueprintDefinitionBase>(10);

                m_spawnGroupDefinitions = new List<MySpawnGroupDefinition>(10);

                m_containerTypeDefinitions = new DefinitionDictionary<MyContainerTypeDefinition>(10);

                m_handItemsById = new DefinitionDictionary<MyHandItemDefinition>(10);
                m_physicalItemsByHandItemId = new DefinitionDictionary<MyPhysicalItemDefinition>(m_handItemsById.Count);
                m_handItemsByPhysicalItemId = new DefinitionDictionary<MyHandItemDefinition>(m_handItemsById.Count);

                m_scenarioDefinitions = new List<MyScenarioDefinition>(10);
                m_characters = new Dictionary<string, MyCharacterDefinition>();
                m_animationsBySkeletonType = new Dictionary<string, Dictionary<string, MyAnimationDefinition>>();

                m_blueprintClasses = new DefinitionDictionary<MyBlueprintClassDefinition>(10);
                m_blueprintClassEntries = new HashSet<BlueprintClassEntry>();
                m_blueprintsByResultId = new DefinitionDictionary<MyBlueprintDefinitionBase>(10);

                m_environmentItemsEntries = new HashSet<EnvironmentItemsEntry>();
                m_componentBlockEntries = new HashSet<MyComponentBlockEntry>();

                m_componentBlocks = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
                m_componentIdToBlock = new Dictionary<MyDefinitionId, MyCubeBlockDefinition>(MyDefinitionId.Comparer);

                m_categoryClasses = new List<MyGuiBlockCategoryDefinition>(25);
                m_categories = new Dictionary<string, MyGuiBlockCategoryDefinition>(25);

                m_prefabs = new Dictionary<string, MyPrefabDefinition>();
                m_respawnShips = new Dictionary<string, MyRespawnShipDefinition>();

                m_sounds = new DefinitionDictionary<MyAudioDefinition>(10);

                m_shipSounds = new DefinitionDictionary<MyShipSoundsDefinition>(10);                m_behaviorDefinitions = new DefinitionDictionary<MyBehaviorDefinition>(10);
                m_voxelMapStorages = new Dictionary<string, MyVoxelMapStorageDefinition>(64);
                m_characterNames = new List<MyCharacterName>(32);

                m_battleDefinition = new MyBattleDefinition();

                m_planetGeneratorDefinitions = new DefinitionDictionary<MyPlanetGeneratorDefinition>(5);

                m_componentGroups = new DefinitionDictionary<MyComponentGroupDefinition>(4);
                m_componentGroupMembers = new Dictionary<MyDefinitionId, MyTuple<int, MyComponentGroupDefinition>>();

                m_planetPrefabDefinitions = new DefinitionDictionary<MyPlanetPrefabDefinition>(5);

                m_groupedIds = new Dictionary<string, Dictionary<string, MyGroupedIds>>();

                m_scriptedGroupDefinitions = new DefinitionDictionary<MyScriptedGroupDefinition>(10);
                m_pirateAntennaDefinitions = new DefinitionDictionary<MyPirateAntennaDefinition>(4);

                m_componentSubstitutions = new Dictionary<MyDefinitionId, MyComponentSubstitutionDefinition>();

                m_destructionDefinition = new MyDestructionDefinition();

                m_mapMultiBlockDefToCubeBlockDef = new Dictionary<string, MyCubeBlockDefinition>();
                m_factionDefinitionsByTag.Clear();

                m_idToRope = new Dictionary<MyDefinitionId, MyRopeDefinition>(MyDefinitionId.Comparer);

                m_gridCreateDefinitions = new DefinitionDictionary<MyGridCreateToolDefinition>(3);

                m_entityComponentDefinitions = new DefinitionDictionary<MyComponentDefinitionBase>(10);
                m_entityContainers = new DefinitionDictionary<MyContainerDefinition>(10);
                if (unload)
                {
                    m_physicalMaterialsByName = new Dictionary<string, MyPhysicalMaterialDefinition>();
                }

                m_fontsById = new DefinitionDictionary<MyFontDefinition>(16);

                m_lootBagDefinition = null;
            }

            public void OverrideBy(DefinitionSet definitionSet)
            {
                base.OverrideBy(definitionSet);

                foreach (var def in definitionSet.m_gridCreateDefinitions)
                {
                    m_gridCreateDefinitions[def.Key] = def.Value;
                }

                for (int i = 0; i < definitionSet.m_cubeSizes.Length; i++)
                {
                    var cubeSize = definitionSet.m_cubeSizes[i];
                    if (cubeSize != 0)
                    {
                        m_cubeSizes[i] = cubeSize;
                        m_cubeSizesOriginal[i] = definitionSet.m_cubeSizesOriginal[i];
                    }
                }

                for (int i = 0; i < definitionSet.m_basePrefabNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(definitionSet.m_basePrefabNames[i]))
                        m_basePrefabNames[i] = definitionSet.m_basePrefabNames[i];
                }

                m_definitionsById.Merge(definitionSet.m_definitionsById);

                foreach (var voxelMaterial in definitionSet.m_voxelMaterialsByName)
                {
                    m_voxelMaterialsByName[voxelMaterial.Key] = voxelMaterial.Value;
                }

                foreach (var physicalMaterial in definitionSet.m_physicalMaterialsByName)
                {
                    m_physicalMaterialsByName[physicalMaterial.Key] = physicalMaterial.Value;
                }

                MergeDefinitionLists(m_physicalItemDefinitions, definitionSet.m_physicalItemDefinitions);

                foreach (var blockPosition in definitionSet.m_blockPositions)
                {
                    m_blockPositions[blockPosition.Key] = blockPosition.Value;
                }

                for (int i = 0; i < definitionSet.m_uniqueCubeBlocksBySize.Length; i++)
                {
                    var uniqueCubeBlocksBySize = definitionSet.m_uniqueCubeBlocksBySize[i];
                    foreach (var uniqueCubeBlocks in uniqueCubeBlocksBySize)
                    {
                        m_uniqueCubeBlocksBySize[i][uniqueCubeBlocks.Key] = uniqueCubeBlocks.Value;
                    }
                }

                m_blueprintsById.Merge(definitionSet.m_blueprintsById);
                MergeDefinitionLists(m_spawnGroupDefinitions, definitionSet.m_spawnGroupDefinitions);
                m_containerTypeDefinitions.Merge(definitionSet.m_containerTypeDefinitions);
                m_handItemsById.Merge(definitionSet.m_handItemsById);
                MergeDefinitionLists(m_scenarioDefinitions, definitionSet.m_scenarioDefinitions);

                foreach (var character in definitionSet.m_characters)
                {
                    if (character.Value.Enabled)
                        m_characters[character.Key] = character.Value;
                    else
                        m_characters.Remove(character.Key);
                }

                m_blueprintClasses.Merge(definitionSet.m_blueprintClasses);

                foreach (var classEntry in definitionSet.m_categoryClasses)
                {
                    m_categoryClasses.Add(classEntry);

                    string categoryName = classEntry.Name;

                    MyGuiBlockCategoryDefinition categoryDefinition = null;
                    if (false == m_categories.TryGetValue(categoryName, out categoryDefinition))
                    {
                        m_categories.Add(categoryName, classEntry);
                    }
                    else
                    {
                        categoryDefinition.ItemIds.UnionWith(classEntry.ItemIds);
                    }
                }

                foreach (var classEntry in definitionSet.m_blueprintClassEntries)
                {
                    if (m_blueprintClassEntries.Contains(classEntry))
                    {
                        if (classEntry.Enabled == false)
                            m_blueprintClassEntries.Remove(classEntry);
                    }
                    else
                    {
                        if (classEntry.Enabled == true)
                            m_blueprintClassEntries.Add(classEntry);
                    }
                }

                m_blueprintsByResultId.Merge(definitionSet.m_blueprintsByResultId);

                foreach (var classEntry in definitionSet.m_environmentItemsEntries)
                {
                    if (m_environmentItemsEntries.Contains(classEntry))
                    {
                        if (classEntry.Enabled == false)
                            m_environmentItemsEntries.Remove(classEntry);
                    }
                    else
                    {
                        if (classEntry.Enabled == true)
                            m_environmentItemsEntries.Add(classEntry);
                    }
                }

                foreach (var blockEntry in definitionSet.m_componentBlockEntries)
                {
                    if (m_componentBlockEntries.Contains(blockEntry))
                    {
                        if (blockEntry.Enabled == false)
                            m_componentBlockEntries.Remove(blockEntry);
                    }
                    else
                    {
                        if (blockEntry.Enabled == true)
                            m_componentBlockEntries.Add(blockEntry);
                    }
                }

                foreach (var prefab in definitionSet.m_prefabs)
                {
                    if (prefab.Value.Enabled)
                        m_prefabs[prefab.Key] = prefab.Value;
                    else
                        m_prefabs.Remove(prefab.Key);
                }

                foreach (var respawnShip in definitionSet.m_respawnShips)
                {
                    if (respawnShip.Value.Enabled)
                        m_respawnShips[respawnShip.Key] = respawnShip.Value;
                    else
                        m_respawnShips.Remove(respawnShip.Key);
                }

                foreach (var animationSet in definitionSet.m_animationsBySkeletonType)
                {
                    foreach (var animation in animationSet.Value)
                    {
                        if (animation.Value.Enabled)
                        {
                            if (!m_animationsBySkeletonType.ContainsKey(animationSet.Key))
                                m_animationsBySkeletonType[animationSet.Key] = new Dictionary<string, MyAnimationDefinition>();

                            m_animationsBySkeletonType[animationSet.Key][animation.Value.Id.SubtypeName] = animation.Value;
                        }
                        else
                            m_animationsBySkeletonType[animationSet.Key].Remove(animation.Value.Id.SubtypeName);
                    }
                }

                foreach (var soundDef in definitionSet.m_sounds)
                {
                    // Enabled attribute is handled differently with sounds to prevent confusion between removed sound and missing sound
                    m_sounds[soundDef.Key] = soundDef.Value;
                }

                m_weaponDefinitionsById.Merge(definitionSet.m_weaponDefinitionsById);
                m_ammoDefinitionsById.Merge(definitionSet.m_ammoDefinitionsById);
                m_behaviorDefinitions.Merge(definitionSet.m_behaviorDefinitions);

                foreach (var voxelMapStorageDef in definitionSet.m_voxelMapStorages)
                {
                    m_voxelMapStorages[voxelMapStorageDef.Key] = voxelMapStorageDef.Value;
                }

                foreach (var nameEntry in definitionSet.m_characterNames)
                {
                    m_characterNames.Add(nameEntry);
                }

                if (definitionSet.m_battleDefinition != null)
                {
                    if (definitionSet.m_battleDefinition.Enabled)
                        m_battleDefinition.Merge(definitionSet.m_battleDefinition);
                }

                foreach (var entry in definitionSet.m_componentSubstitutions)
                {
                    m_componentSubstitutions[entry.Key] = entry.Value;
                }

                m_componentGroups.Merge(definitionSet.m_componentGroups);
                foreach (var entry in definitionSet.m_planetGeneratorDefinitions)
                {
                    if (entry.Value.Enabled)
                        m_planetGeneratorDefinitions[entry.Key] = entry.Value;
                    else
                        m_planetGeneratorDefinitions.Remove(entry.Key);
                }

                foreach (var entry in definitionSet.m_planetPrefabDefinitions)
                {
                    if (entry.Value.Enabled)
                        m_planetPrefabDefinitions[entry.Key] = entry.Value;
                    else
                        m_planetPrefabDefinitions.Remove(entry.Key);
                }

                foreach (var entry in definitionSet.m_groupedIds)
                {
                    if (m_groupedIds.ContainsKey(entry.Key))
                    {
                        var localGroup = m_groupedIds[entry.Key];
                        foreach (var key in entry.Value)
                            localGroup[key.Key] = key.Value;
                    }
                    else
                    {
                        m_groupedIds[entry.Key] = entry.Value;
                    }
                }

                m_scriptedGroupDefinitions.Merge(definitionSet.m_scriptedGroupDefinitions);
                m_pirateAntennaDefinitions.Merge(definitionSet.m_pirateAntennaDefinitions);

                if (definitionSet.m_destructionDefinition != null)
                {
                    if (definitionSet.m_destructionDefinition.Enabled)
                        m_destructionDefinition.Merge(definitionSet.m_destructionDefinition);
                }

                foreach (var entry in definitionSet.m_mapMultiBlockDefToCubeBlockDef)
                {
                    if (m_mapMultiBlockDefToCubeBlockDef.ContainsKey(entry.Key))
                    {
                        m_mapMultiBlockDefToCubeBlockDef.Remove(entry.Key);
                    }

                    m_mapMultiBlockDefToCubeBlockDef.Add(entry.Key, entry.Value);
                }

                foreach (var entry in definitionSet.m_idToRope)
                {
                    m_idToRope[entry.Key] = entry.Value;
                }

                m_entityComponentDefinitions.Merge(definitionSet.m_entityComponentDefinitions);
                m_entityContainers.Merge(definitionSet.m_entityContainers);

                m_lootBagDefinition = definitionSet.m_lootBagDefinition;

                foreach (var font in definitionSet.m_fontsById)
                {
                    m_fontsById[font.Key] = font.Value;
                }
            }

            static void MergeDefinitionLists<T>(List<T> output, List<T> input) where T : MyDefinitionBase
            {
                m_helperDict.Clear();
                foreach (MyDefinitionBase definition in output)
                {
                    m_helperDict[definition.Id] = definition;
                }
                foreach (var definition in input)
                {
                    if (definition.Enabled)
                        m_helperDict[definition.Id] = definition;
                    else
                        m_helperDict.Remove(definition.Id);
                }
                output.Clear();
                foreach (var definition in m_helperDict.Values)
                {
                    output.Add((T)definition);
                }
                m_helperDict.Clear();
            }

            internal float[] m_cubeSizes;
            internal float[] m_cubeSizesOriginal;
            internal string[] m_basePrefabNames;

            internal DefinitionDictionary<MyCubeBlockDefinition>[] m_uniqueCubeBlocksBySize; //without variants
            internal DefinitionDictionary<MyDefinitionBase> m_definitionsById;
            internal DefinitionDictionary<MyBlueprintDefinitionBase> m_blueprintsById;
            internal DefinitionDictionary<MyHandItemDefinition> m_handItemsById;
            internal DefinitionDictionary<MyFontDefinition> m_fontsById;

            internal DefinitionDictionary<MyPhysicalItemDefinition> m_physicalItemsByHandItemId;
            internal DefinitionDictionary<MyHandItemDefinition> m_handItemsByPhysicalItemId;
            internal Dictionary<string, MyPhysicalMaterialDefinition> m_physicalMaterialsByName = new Dictionary<string,MyPhysicalMaterialDefinition>();

            internal Dictionary<string, MyVoxelMaterialDefinition> m_voxelMaterialsByName;
            internal Dictionary<byte, MyVoxelMaterialDefinition> m_voxelMaterialsByIndex;
            internal int m_voxelMaterialRareCount;

            internal List<MyPhysicalItemDefinition> m_physicalItemDefinitions;

            internal DefinitionDictionary<MyWeaponDefinition> m_weaponDefinitionsById;
            internal DefinitionDictionary<MyAmmoDefinition> m_ammoDefinitionsById;

            internal List<MySpawnGroupDefinition> m_spawnGroupDefinitions;
            internal DefinitionDictionary<MyContainerTypeDefinition> m_containerTypeDefinitions;

            internal List<MyScenarioDefinition> m_scenarioDefinitions;

            internal Dictionary<string, MyCharacterDefinition> m_characters;

            internal Dictionary<string, Dictionary<string, MyAnimationDefinition>> m_animationsBySkeletonType;

            internal DefinitionDictionary<MyBlueprintClassDefinition> m_blueprintClasses;

            internal List<MyGuiBlockCategoryDefinition> m_categoryClasses;
            internal Dictionary<string, MyGuiBlockCategoryDefinition> m_categories;

            // The following hashsets are used only for loading. When initialized, they should be cleared
            internal HashSet<BlueprintClassEntry> m_blueprintClassEntries;
            internal HashSet<EnvironmentItemsEntry> m_environmentItemsEntries;
            internal HashSet<MyComponentBlockEntry> m_componentBlockEntries;

            public HashSet<MyDefinitionId> m_componentBlocks;
            public Dictionary<MyDefinitionId, MyCubeBlockDefinition> m_componentIdToBlock;

            internal DefinitionDictionary<MyBlueprintDefinitionBase> m_blueprintsByResultId;

            internal Dictionary<string, MyPrefabDefinition> m_prefabs;
            internal Dictionary<string, MyRespawnShipDefinition> m_respawnShips;

            /// <summary>
            /// Block pairs Small,Large
            /// </summary>
            internal Dictionary<string, MyCubeBlockDefinitionGroup> m_blockGroups;

            internal Dictionary<string, Vector2I> m_blockPositions;

            internal DefinitionDictionary<MyAudioDefinition> m_sounds;
            internal DefinitionDictionary<MyShipSoundsDefinition> m_shipSounds;
            internal MyShipSoundSystemDefinition m_shipSoundSystem = new MyShipSoundSystemDefinition();

            internal DefinitionDictionary<MyBehaviorDefinition> m_behaviorDefinitions;

            public Dictionary<string, MyVoxelMapStorageDefinition> m_voxelMapStorages;

            public readonly Dictionary<int, List<MyDefinitionId>> m_channelEnvironmentItemsDefs = new Dictionary<int, List<MyDefinitionId>>();

            internal List<MyCharacterName> m_characterNames;

            internal MyBattleDefinition m_battleDefinition;

            internal DefinitionDictionary<MyPlanetGeneratorDefinition> m_planetGeneratorDefinitions;

            internal DefinitionDictionary<MyComponentGroupDefinition> m_componentGroups;
            internal Dictionary<MyDefinitionId, MyTuple<int, MyComponentGroupDefinition>> m_componentGroupMembers;

            internal Dictionary<MyDefinitionId, MyComponentSubstitutionDefinition> m_componentSubstitutions;

            internal DefinitionDictionary<MyPlanetPrefabDefinition> m_planetPrefabDefinitions;

            internal Dictionary<string, Dictionary<string, MyGroupedIds>> m_groupedIds;

            internal DefinitionDictionary<MyScriptedGroupDefinition> m_scriptedGroupDefinitions;

            internal DefinitionDictionary<MyPirateAntennaDefinition> m_pirateAntennaDefinitions;

            internal MyDestructionDefinition m_destructionDefinition;

            internal Dictionary<string, MyCubeBlockDefinition> m_mapMultiBlockDefToCubeBlockDef = new Dictionary<string, MyCubeBlockDefinition>();

            internal Dictionary<string, MyFactionDefinition> m_factionDefinitionsByTag = new Dictionary<string, MyFactionDefinition>();

            internal Dictionary<MyDefinitionId, MyRopeDefinition> m_idToRope = new Dictionary<MyDefinitionId, MyRopeDefinition>(MyDefinitionId.Comparer);

            internal DefinitionDictionary<MyGridCreateToolDefinition> m_gridCreateDefinitions;

            internal DefinitionDictionary<MyComponentDefinitionBase> m_entityComponentDefinitions;

            internal DefinitionDictionary<MyContainerDefinition> m_entityContainers;

            internal MyLootBagDefinition m_lootBagDefinition;
        }
    }
}
