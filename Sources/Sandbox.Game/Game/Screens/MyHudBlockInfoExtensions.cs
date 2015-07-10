using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

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
                MyMultiBlockDefinition multiBlockDef = MyDefinitionManager.Static.GetMultiBlockDefinition(defId);
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

            if (merge) MergeSameComponents(blockInfo, definition);
        }

        public static void LoadDefinition(this MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition, DictionaryReader<MyDefinitionId, int> materials, bool merge = true)
        {
            InitBlockInfo(blockInfo, definition);

            foreach (var material in materials)
            {
                var componentDefinition = MyDefinitionManager.Static.GetComponentDefinition(material.Key);
                var info = new MyHudBlockInfo.ComponentInfo();
                info.DefinitionId = componentDefinition.Id;
                info.ComponentName = componentDefinition.DisplayNameText;
                info.Icon = componentDefinition.Icon;
                info.TotalCount = material.Value;
                blockInfo.Components.Add(info);
            }

            if (merge) MergeSameComponents(blockInfo, definition);
        }

        private static void AddComponentsForBlock(MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition)
        {
            for (int i = 0; i < definition.Components.Length; ++i)
            {
                var comp = definition.Components[i];
                var info = new MyHudBlockInfo.ComponentInfo();
                info.DefinitionId = comp.Definition.Id;
                info.ComponentName = comp.Definition.DisplayNameText;
                info.Icon = comp.Definition.Icon;
                info.TotalCount = comp.Count;
                blockInfo.Components.Add(info);
            }
        }

        private static void InitBlockInfo(MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition)
        {
            blockInfo.BlockName = definition.DisplayNameText;
            blockInfo.BlockIcon = definition.Icon;
            blockInfo.BlockIntegrity = 0;
            blockInfo.CriticalComponentIndex = -1;
            blockInfo.CriticalIntegrity = 0;
            blockInfo.OwnershipIntegrity = 0;
            blockInfo.MissingComponentIndex = -1;
            blockInfo.Components.Clear();
        }

        private static void MergeSameComponents(MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition)
        {
            for (int i = blockInfo.Components.Count - 1; i >= 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (blockInfo.Components[i].DefinitionId == blockInfo.Components[j].DefinitionId)
                    {
                        var info = blockInfo.Components[j];
                        info.TotalCount += blockInfo.Components[i].TotalCount;
                        blockInfo.Components[j] = info;
                        blockInfo.Components.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}