using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.Game.Entities.Cube;
using VRage.Collections;
using System.Diagnostics;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Game.GameSystems
{
    partial class MyGridTerminalSystem : IMyGridTerminalSystem
    {
        void IMyGridTerminalSystem.GetBlocks(List<IMyTerminalBlock> blocks)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block.GetProgrammableBlockAccessibility == IngameScriptAccessibility.readWriteAccess)
                {
                    blocks.Add(block);
                }
            }
        }

        void IMyGridTerminalSystem.GetBlockGroups(List<IMyBlockGroup> blockGroups)
        {
            blockGroups.Clear();
            foreach (var group in BlockGroups)
            {
                blockGroups.Add(group);
            }
        }

        void IMyGridTerminalSystem.GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block is T)
                {
                    if (block.GetProgrammableBlockAccessibility < IngameScriptAccessibility.readWriteAccess || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block);
                }
            }
        }

        void IMyGridTerminalSystem.SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString().Contains(name,StringComparison.OrdinalIgnoreCase))
                {
                    if (block.GetProgrammableBlockAccessibility < IngameScriptAccessibility.readWriteAccess || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block);
                }
            }
        }

        IMyTerminalBlock IMyGridTerminalSystem.GetBlockWithName(string name)
        {
            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString() == name && block.GetProgrammableBlockAccessibility == IngameScriptAccessibility.readWriteAccess)
                {
                    return block;
                }
            }
            return null;
        }
    }
}
