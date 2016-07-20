using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;

namespace Sandbox.Game.Gui
{
    public static class HudBlockInfoExtensions
    {
        public static void LoadDefinition(this MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition, bool merge = true)
        {
            InitBlockInfo(blockInfo, definition);

            if (definition.MultiBlock != null)
            {
                MyDefinitionId defId = new MyDefinitionId(typeof(MyObjectBuilder_MultiBlockDefinition), definition.MultiBlock);
                MyMultiBlockDefinition multiBlockDef = MyDefinitionManager.Static.TryGetMultiBlockDefinition(defId);
                if (multiBlockDef != null)
                {
                    foreach (var blockPart in multiBlockDef.BlockDefinitions)
                    {
                        MyCubeBlockDefinition cubeBlockDef = null;
                        MyDefinitionManager.Static.TryGetDefinition(blockPart.Id, out cubeBlockDef);
                        if (cubeBlockDef != null)
                        {
                            AddComponentsForBlock(blockInfo, cubeBlockDef);
                        }
                    }
                }
            }
            else
            {
                AddComponentsForBlock(blockInfo, definition);
            }

            if (merge) MergeSameComponents(blockInfo);
        }

        public static void LoadDefinition(this MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition, DictionaryReader<MyDefinitionId, int> materials, bool merge = true)
        {
            InitBlockInfo(blockInfo, definition);

            foreach (var material in materials)
            {
                var def = MyDefinitionManager.Static.GetDefinition(material.Key);
                var info = new MyHudBlockInfo.ComponentInfo();
                if (def == null)
                {
                    MyPhysicalItemDefinition physicalDefinition = null;
                    if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(material.Key, out physicalDefinition))
                        continue;
                    info.ComponentName = physicalDefinition.DisplayNameText;
                    info.Icons = physicalDefinition.Icons;
                    info.DefinitionId = physicalDefinition.Id;
                    info.TotalCount = 1;
                }
                else
                {
                    info.DefinitionId = def.Id;
                    info.ComponentName = def.DisplayNameText;
                    info.Icons = def.Icons;
                    info.TotalCount = material.Value;
                }
                blockInfo.Components.Add(info);
            }

            if (merge) MergeSameComponents(blockInfo);
        }

        public static void AddComponentsForBlock(this MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition)
        {
            for (int i = 0; i < definition.Components.Length; ++i)
            {
                var comp = definition.Components[i];
                var info = new MyHudBlockInfo.ComponentInfo();
                info.DefinitionId = comp.Definition.Id;
                info.ComponentName = comp.Definition.DisplayNameText;
                info.Icons = comp.Definition.Icons;
                info.TotalCount = comp.Count;
                blockInfo.Components.Add(info);
            }
        }

        public static void InitBlockInfo(this MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition)
        {
            blockInfo.BlockName = definition.DisplayNameText;
            blockInfo.BlockIcons = definition.Icons;
            blockInfo.BlockIntegrity = 0;
            blockInfo.CriticalComponentIndex = definition.CriticalGroup;
            blockInfo.CriticalIntegrity = definition.CriticalIntegrityRatio;
            blockInfo.OwnershipIntegrity = definition.OwnershipIntegrityRatio;
            blockInfo.MissingComponentIndex = -1;
            blockInfo.Components.Clear();
        }

        public static void MergeSameComponents(this MyHudBlockInfo blockInfo)
        {
            for (int i = blockInfo.Components.Count - 1; i >= 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (blockInfo.Components[i].DefinitionId == blockInfo.Components[j].DefinitionId)
                    {
                        var info = blockInfo.Components[j];
                        info.TotalCount += blockInfo.Components[i].TotalCount;
                        info.MountedCount += blockInfo.Components[i].MountedCount;
                        info.StockpileCount += blockInfo.Components[i].StockpileCount;
                        blockInfo.Components[j] = info;
                        blockInfo.Components.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}