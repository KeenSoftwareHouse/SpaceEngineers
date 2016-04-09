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
                if (block.IsAccessibleForProgrammableBlock)
                {
                    blocks.Add(block);
                }
            }
        }

        List<IMyTerminalBlock> IMyGridTerminalSystem.GetBlocks()
        {
            var blocks = new List<IMyTerminalBlock>();

            foreach (var block in m_blocks)
            {
                if (block.IsAccessibleForProgrammableBlock)
                {
                    blocks.Add(block);
                }
            }

            return blocks;
        }

        void IMyGridTerminalSystem.GetBlockGroups(List<IMyBlockGroup> blockGroups)
        {
            blockGroups.Clear();
            foreach (var group in BlockGroups)
            {
                blockGroups.Add(group);
            }
        }

        List<IMyBlockGroup> IMyGridTerminalSystem.GetBlockGroups()
        {
            var blockGroups = new List<IMyBlockGroup>();
            foreach (var group in BlockGroups)
            {
                blockGroups.Add(group);
            }
            return blockGroups;
        }

        void IMyGridTerminalSystem.GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block is T)
                {
                    if (block.IsAccessibleForProgrammableBlock == false || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block);
                }
            }
        }

        void IMyGridTerminalSystem.GetBlocksOfType<T>(List<T> blocks, Func<IMyTerminalBlock, bool> collect)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block is T)
                {
                    if (block.IsAccessibleForProgrammableBlock == false || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block as T);
                }
            }
        }

        List<T> IMyGridTerminalSystem.GetBlocksOfType<T>(Func<IMyTerminalBlock, bool> collect)
        {
            var blocks = new List<T>();

            foreach (var block in m_blocks)
            {
                if (block is T)
                {
                    if (block.IsAccessibleForProgrammableBlock == false || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block as T);
                }
            }

            return blocks;
        }

        void IMyGridTerminalSystem.SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
        {
            blocks.Clear();
            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString().Contains(name,StringComparison.OrdinalIgnoreCase))
                {
                    if (block.IsAccessibleForProgrammableBlock == false || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block);
                }
            }
        }

        List<IMyTerminalBlock> IMyGridTerminalSystem.SearchBlocksOfName(string name, Func<IMyTerminalBlock, bool> collect)
        {
            var blocks = new List<IMyTerminalBlock>();

            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString().Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (block.IsAccessibleForProgrammableBlock == false || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block);
                }
            }

            return blocks;
        }

        List<T> IMyGridTerminalSystem.SearchBlocksOfTypeWithName<T>(string name, Func<IMyTerminalBlock, bool> collect)
        {
            var blocks = new List<T>();

            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString().Contains(name, StringComparison.OrdinalIgnoreCase) && block is T)
                {
                    if (block.IsAccessibleForProgrammableBlock == false || (collect != null && collect(block) == false))
                    {
                        continue;
                    }
                    blocks.Add(block as T);
                }
            }

            return blocks;
        }

        IMyTerminalBlock IMyGridTerminalSystem.GetBlockWithName(string name)
        {
            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString() == name&& block.IsAccessibleForProgrammableBlock )
                {
                    return block;
                }
            }
            return null;
        }

        T IMyGridTerminalSystem.GetBlockOfTypeWithName<T>(string name)
        {
            foreach (var block in m_blocks)
            {
                if (block.CustomName.ToString() == name && block.IsAccessibleForProgrammableBlock && block is T)
                {
                    return block as T;
                }
            }
            return null;
        }

        IMyBlockGroup IMyGridTerminalSystem.GetBlockGroupWithName(string name)
        {
            foreach (var group in BlockGroups)
            {
                if (group.Name.ToString() == name)
                {
                    //Check if every block in group IsAccessibleForProgrammableBlock
                    var IsAccessibleForProgrammableBlock = true;
                    foreach (var block in group.Blocks)
                    {
                        if (!block.IsAccessibleForProgrammableBlock)
                        {
                            IsAccessibleForProgrammableBlock = false;
                            break;
                        }
                    }

                    if (IsAccessibleForProgrammableBlock)
                    {
                        return group;
                    }
                }
            }
            return null;
        }
    }
}
