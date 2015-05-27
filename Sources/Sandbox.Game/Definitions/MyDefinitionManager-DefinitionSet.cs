#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Collections.Generic;
using VRageMath;


#endregion

namespace Sandbox.Definitions
{
    public partial class MyDefinitionManager
    {
        class DefinitionDictionary<V> : Dictionary<MyDefinitionId, V>
        {
            public DefinitionDictionary(int capacity)
                : base(capacity, MyDefinitionId.Comparer)
            {
            }

            public void AddDefinitionSafe<T>(T definition, MyModContext context, string file)
                where T : MyDefinitionBase, V
            {
                if (definition.Id.TypeId != MyObjectBuilderType.Invalid)
                {
                    this[definition.Id] = definition;
                }
                else
                {
                    MyDefinitionErrors.Add(context, "Invalid definition id", ErrorSeverity.Error);
                }
            }
        }

        class DefinitionSet
        {
            static DefinitionDictionary<MyDefinitionBase> m_helperDict = new DefinitionDictionary<MyDefinitionBase>(100);

            public DefinitionSet()
            {
                Clear();
            }

            public void Clear()
            {
                m_cubeSizes = new float[typeof(MyCubeSize).GetEnumValues().Length];
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

                m_categoryClasses = new List<MyGuiBlockCategoryDefinition>(25);
                m_categories = new Dictionary<string, MyGuiBlockCategoryDefinition>(25);

                m_prefabs = new Dictionary<string, MyPrefabDefinition>();
                m_respawnShips = new Dictionary<string, MyRespawnShipDefinition>();

                m_sounds = new DefinitionDictionary<MyAudioDefinition>(10);
                m_environmentDef = new MyEnvironmentDefinition();
                m_behaviorDefinitions = new DefinitionDictionary<MyBehaviorDefinition>(10);
                m_voxelMapStorages = new Dictionary<string, MyVoxelMapStorageDefinition>(64);
                m_characterNames = new List<MyCharacterName>(32);

                m_battleDefinition = new MyBattleDefinition();
            }

            public void OverrideBy(DefinitionSet definitionSet)
            {
                for (int i = 0; i < definitionSet.m_cubeSizes.Length; i++)
                {
                    var cubeSize = definitionSet.m_cubeSizes[i];
                    if (cubeSize != 0)
                        m_cubeSizes[i] = cubeSize;
                }

                for (int i = 0; i < definitionSet.m_basePrefabNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(definitionSet.m_basePrefabNames[i]))
                        m_basePrefabNames[i] = definitionSet.m_basePrefabNames[i];
                }

                foreach (var definition in definitionSet.m_definitionsById)
                {
                    if (definition.Value.Enabled)
                        m_definitionsById[definition.Key] = definition.Value;
                    else
                        m_definitionsById.Remove(definition.Key);
                }

                foreach (var voxelMaterial in definitionSet.m_voxelMaterialsByName)
                {
                    m_voxelMaterialsByName[voxelMaterial.Key] = voxelMaterial.Value;
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

                foreach (var blueprintsById in definitionSet.m_blueprintsById)
                {
                    if (blueprintsById.Value.Enabled)
                        m_blueprintsById[blueprintsById.Key] = blueprintsById.Value;
                    else
                        m_blueprintsById.Remove(blueprintsById.Key);
                }

                MergeDefinitionLists(m_spawnGroupDefinitions, definitionSet.m_spawnGroupDefinitions);

                foreach (var containerTypeDefinition in definitionSet.m_containerTypeDefinitions)
                {
                    if (containerTypeDefinition.Value.Enabled)
                        m_containerTypeDefinitions[containerTypeDefinition.Key] = containerTypeDefinition.Value;
                    else
                        m_containerTypeDefinitions.Remove(containerTypeDefinition.Key);
                }

                foreach (var handItem in definitionSet.m_handItemsById)
                {
                    if (handItem.Value.Enabled)
                        m_handItemsById[handItem.Key] = handItem.Value;
                    else
                        m_handItemsById.Remove(handItem.Key);
                }

                MergeDefinitionLists(m_scenarioDefinitions, definitionSet.m_scenarioDefinitions);

                foreach (var character in definitionSet.m_characters)
                {
                    if (character.Value.Enabled)
                        m_characters[character.Key] = character.Value;
                    else
                        m_characters.Remove(character.Key);
                }

                foreach (var classDef in definitionSet.m_blueprintClasses)
                {
                    if (classDef.Value.Enabled)
                        m_blueprintClasses[classDef.Key] = classDef.Value;
                    else
                        m_blueprintClasses.Remove(classDef.Key);
                }

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
                        categoryDefinition.ItemIds.AddRange(classEntry.ItemIds);
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

                foreach (var entry in definitionSet.m_blueprintsByResultId)
                {
                    if (entry.Value.Enabled)
                        m_blueprintsByResultId[entry.Key] = entry.Value;
                    else
                        m_blueprintsByResultId.Remove(entry.Key);
                }

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

                if (definitionSet.m_environmentDef != null)
                {
                    if (definitionSet.m_environmentDef.Enabled)
                        m_environmentDef.Merge(definitionSet.m_environmentDef);
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

                foreach (var weaponDef in definitionSet.m_weaponDefinitionsById)
                {
                    if (weaponDef.Value.Enabled)
                        m_weaponDefinitionsById[weaponDef.Key] = weaponDef.Value;
                    else
                        m_weaponDefinitionsById.Remove(weaponDef.Key);
                }

                foreach (var ammoDef in definitionSet.m_ammoDefinitionsById)
                {
                    if (ammoDef.Value.Enabled)
                        m_ammoDefinitionsById[ammoDef.Key] = ammoDef.Value;
                    else
                        m_ammoDefinitionsById.Remove(ammoDef.Key);
                }

                foreach (var behaviorDef in definitionSet.m_behaviorDefinitions)
                {
                    m_behaviorDefinitions[behaviorDef.Key] = behaviorDef.Value;
                }

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
            internal string[] m_basePrefabNames;

            internal DefinitionDictionary<MyCubeBlockDefinition>[] m_uniqueCubeBlocksBySize; //without variants
            internal DefinitionDictionary<MyDefinitionBase> m_definitionsById;
            internal DefinitionDictionary<MyBlueprintDefinitionBase> m_blueprintsById;
            internal DefinitionDictionary<MyHandItemDefinition> m_handItemsById;

            internal DefinitionDictionary<MyPhysicalItemDefinition> m_physicalItemsByHandItemId;
            internal DefinitionDictionary<MyHandItemDefinition> m_handItemsByPhysicalItemId;

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

            // Used only for loading the blueprint classes. When initialized, this should be null
            internal HashSet<BlueprintClassEntry> m_blueprintClassEntries;

            // Ditto for environment items entries
            internal HashSet<EnvironmentItemsEntry> m_environmentItemsEntries;

            internal DefinitionDictionary<MyBlueprintDefinitionBase> m_blueprintsByResultId;

            internal Dictionary<string, MyPrefabDefinition> m_prefabs;
            internal Dictionary<string, MyRespawnShipDefinition> m_respawnShips;

            /// <summary>
            /// Block pairs Small,Large
            /// </summary>
            internal Dictionary<string, MyCubeBlockDefinitionGroup> m_blockGroups;

            internal Dictionary<string, Vector2I> m_blockPositions;
            internal MyEnvironmentDefinition m_environmentDef = new MyEnvironmentDefinition();

            internal DefinitionDictionary<MyAudioDefinition> m_sounds;

            internal DefinitionDictionary<MyBehaviorDefinition> m_behaviorDefinitions;

            public Dictionary<string, MyVoxelMapStorageDefinition> m_voxelMapStorages;

            internal List<MyCharacterName> m_characterNames;

            internal MyBattleDefinition m_battleDefinition;
        }
    }
}
