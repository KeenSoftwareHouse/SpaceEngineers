using Sandbox.Definitions;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Gui
{
    public static class HudBlockInfoExtensions
    {
        public static void LoadDefinition(this MyHudBlockInfo blockInfo, MyCubeBlockDefinition definition, bool merge = true)
        {
            blockInfo.BlockName = definition.DisplayNameText;
            blockInfo.BlockIcon = definition.Icon;
            blockInfo.BlockIntegrity = 0;
            blockInfo.CriticalComponentIndex = -1;
            blockInfo.CriticalIntegrity = 0;
            blockInfo.OwnershipIntegrity = 0;
            blockInfo.MissingComponentIndex = -1;

            blockInfo.Components.Clear();
            for (int i = 0; i < definition.Components.Length; ++i)
            {
                var comp = definition.Components[i];
                var info = new MyHudBlockInfo.ComponentInfo();
                info.ComponentName = comp.Definition.DisplayNameText;
                info.Icon = comp.Definition.Icon;
                info.TotalCount = comp.Count;
                blockInfo.Components.Add(info);
            }

            // Merge same components
            if (merge)
            {
                for (int i = definition.Components.Length - 1; i >= 0; i--)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (definition.Components[i].Definition == definition.Components[j].Definition)
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
}