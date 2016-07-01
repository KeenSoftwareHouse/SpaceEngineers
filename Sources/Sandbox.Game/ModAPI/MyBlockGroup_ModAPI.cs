using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Game.GameSystems
{
    partial class MyBlockGroup : ModAPI.IMyBlockGroup
    {
        void IMyBlockGroup.GetBlocks(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            for (var index = 0; index < Blocks.Count; index++)
            {
                var block = Blocks[index];
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

        void IMyBlockGroup.GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            for (var index = 0; index < Blocks.Count; index++)
            {
                var block = Blocks[index];
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

        void IMyBlockGroup.GetBlocksOfType<T>(List<T> blocks, Func<T, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            for (var index = 0; index < Blocks.Count; index++)
            {
                var block = Blocks[index];
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

        string IMyBlockGroup.Name
        {
            get { return Name.ToString(); }
        }

        #region ModAPI
        void ModAPI.IMyBlockGroup.GetBlocks(List<ModAPI.IMyTerminalBlock> blocks, Func<ModAPI.IMyTerminalBlock, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            for (var index = 0; index < Blocks.Count; index++)
            {
                var block = Blocks[index];
                if (collect != null && !collect(block))
                {
                    continue;
                }
                if (blocks != null)
                {
                    blocks.Add(block);
                }
            }
        }

        void ModAPI.IMyBlockGroup.GetBlocksOfType<T>(List<ModAPI.IMyTerminalBlock> blocks, Func<ModAPI.IMyTerminalBlock, bool> collect)
        {
            // Allow a pure collect search by allowing a null block list
            if (blocks != null)
            {
                blocks.Clear();
            }
            for (var index = 0; index < Blocks.Count; index++)
            {
                var block = Blocks[index];
                var typedBlock = block as T;
                if (typedBlock == null || (collect != null && !collect(block)))
                {
                    continue;
                }
                if (blocks != null)
                {
                    blocks.Add(block);
                }
            }
        }
        #endregion
    }
}