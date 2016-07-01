using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.Game.Entities.Cube;
using VRage.Collections;
using System.Diagnostics;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace Sandbox.Game.GameSystems
{
    partial class MyGridTerminalSystem : ModAPI.IMyGridTerminalSystem
    {
        void IMyGridTerminalSystem.GetBlocks(List<IMyTerminalBlock> blocks)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block.IsAccessibleForProgrammableBlock)
                {
                    blocks.Add(block);
                }
            }
        }

        void IMyGridTerminalSystem.GetBlockGroups(List<IMyBlockGroup> blockGroups, Func<IMyBlockGroup, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blockGroups != null)
            {
                blockGroups.Clear();
            }
            for (var index = 0; index < BlockGroups.Count; index++)
            {
                var blockGroup = BlockGroups[index];
                if (collect != null && !collect(blockGroup))
                    continue;
                if (blockGroups != null)
                {
                    blockGroups.Add(blockGroup);
                }
            }
        }

        void IMyGridTerminalSystem.GetBlocksOfType<T>(List<T> blocks, Func<T, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            foreach (var block in m_blocks)
            {
                var typedBlock = block as T;
                if (typedBlock == null || !block.IsAccessibleForProgrammableBlock || (collect != null && !collect(typedBlock)))
                {
                    continue;
                }
                if (blocks != null)
                {
                    blocks.Add(typedBlock);
                }
            }
        }

        void IMyGridTerminalSystem.GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            foreach (var block in m_blocks)
            {
                var typedBlock = block as T;
                if (typedBlock == null || !block.IsAccessibleForProgrammableBlock || (collect != null && !collect(block)))
                {
                    continue;
                }
                if (blocks != null)
                {
                    blocks.Add(block);
                }
            }
        }

        void IMyGridTerminalSystem.SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            foreach (var block in m_blocks)
            {
                if (!block.CustomName.ToString().Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!block.IsAccessibleForProgrammableBlock || (collect != null && !collect(block)))
                {
                    continue;
                }
                if (blocks != null)
                {
                    blocks.Add(block);
                }
            }
        }

        IMyTerminalBlock IMyGridTerminalSystem.GetBlockWithName(string name)
        {
            foreach (var block in m_blocks)
            {
                if (block.CustomName.CompareTo(name) == 0 && block.IsAccessibleForProgrammableBlock)
                {
                    return block;
                }
            }
            return null;
        }

        IMyBlockGroup IMyGridTerminalSystem.GetBlockGroupWithName(string name)
        {
            for (var i = 0; i < BlockGroups.Count; i++)
            {
                var group = BlockGroups[i];
                if (group.Name.CompareTo(name) != 0)
                {
                    continue;
                }
                return group;
            }
            return null;
        }

        IMyTerminalBlock IMyGridTerminalSystem.GetBlockWithId(long id)
        {
            MyTerminalBlock block;
            if (m_blockTable.TryGetValue(id, out block) && block.IsAccessibleForProgrammableBlock)
            {
                return block;
            }
            return null;
        }

        #region ModAPI
        void ModAPI.IMyGridTerminalSystem.GetBlocks(List<ModAPI.IMyTerminalBlock> blocks)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                blocks.Add(block);
            }
        }

        void ModAPI.IMyGridTerminalSystem.GetBlockGroups(List<ModAPI.IMyBlockGroup> blockGroups)
        {
            blockGroups.Clear();
            foreach (var group in BlockGroups)
            {
                blockGroups.Add(group);
            }
        }

        void ModAPI.IMyGridTerminalSystem.GetBlocksOfType<T>(List<ModAPI.IMyTerminalBlock> blocks, Func<ModAPI.IMyTerminalBlock, bool> collect)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block is T)
                {
                    if (collect != null && collect(block) == false)
                    {
                        continue;
                    }
                    blocks.Add(block);
                }
            }
        }

        void ModAPI.IMyGridTerminalSystem.SearchBlocksOfName(string name, List<ModAPI.IMyTerminalBlock> blocks, Func<ModAPI.IMyTerminalBlock, bool> collect)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString().Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (collect != null && collect(block) == false)
                    {
                        continue;
                    }
                    blocks.Add(block);
                }
            }
        }

        ModAPI.IMyTerminalBlock ModAPI.IMyGridTerminalSystem.GetBlockWithName(string name)
        {
            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString() == name)
                {
                    return block;
                }
            }
            return null;
        }

        ModAPI.IMyBlockGroup ModAPI.IMyGridTerminalSystem.GetBlockGroupWithName(string name)
        {
            foreach (var group in BlockGroups)
            {
                if (group.Name.ToString() == name)
                {
                    return group;
                }
            }
            return null;
        }
        #endregion
    }
}