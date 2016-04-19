using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRage.Library.Utils;

namespace Sandbox.Engine.Utils
{
    public static class MyBattleHelper
    {
        public const int MAX_BATTLE_PLAYERS = 12;

        public static ulong GetBattlePoints(MyCubeGrid grid)
        {
            ulong points = 0;

            foreach (var block in grid.GetBlocks())
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                if (compoundBlock != null)
                {
                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                        points += GetBattlePoints(blockInCompound);
                }
                else
                {
                    points += GetBattlePoints(block);
                }
            }

            return points;
        }

        public static ulong GetBattlePoints(MySlimBlock slimBlock)
        {
            Debug.Assert(slimBlock.BlockDefinition.Points > 0);
            ulong pts = (ulong)(slimBlock.BlockDefinition.Points > 0 ? slimBlock.BlockDefinition.Points : 1);

            if (slimBlock.BlockDefinition.IsGeneratedBlock)
                pts = 0;

            if (slimBlock.FatBlock != null)
            {
                var inventory = slimBlock.FatBlock.GetInventory(0);
                if (inventory != null)
                {
                    foreach (var item in inventory.GetItems())
                    {
                        if (item.Content is MyObjectBuilder_BlockItem)
                        {
                            MyObjectBuilder_BlockItem blockItem = item.Content as MyObjectBuilder_BlockItem;
                            pts += GetBattlePoints(blockItem.BlockDefId);
                        }
                    }
                }
            }

            return pts;
        }

        public static ulong GetBattlePoints(MyObjectBuilder_CubeGrid[] grids)
        {
            ulong pts = 0;
            foreach (var grid in grids)
                pts += GetBattlePoints(grid);

            return pts;
        }

        public static ulong GetBattlePoints(MyObjectBuilder_CubeGrid grid)
        {
            ulong pts = 0;
            foreach (var block in grid.CubeBlocks)
            {
                MyObjectBuilder_CompoundCubeBlock compoundBlock = block as MyObjectBuilder_CompoundCubeBlock;
                if (compoundBlock != null)
                {
                    foreach (var blockInCompound in compoundBlock.Blocks)
                        pts += GetBattlePoints(blockInCompound);
                }
                else
                {
                    pts += GetBattlePoints(block);
                }
            }

            return pts;
        }

        public static ulong GetBattlePoints(MyObjectBuilder_CubeBlock block)
        {
            ulong pts = GetBattlePoints(block.GetId());

            MyObjectBuilder_CargoContainer cargoContainer = block as MyObjectBuilder_CargoContainer;
            if (cargoContainer != null && cargoContainer.Inventory != null)
            {
                foreach (var item in cargoContainer.Inventory.Items)
                {
                    if (item.PhysicalContent is MyObjectBuilder_BlockItem)
                    {
                        MyObjectBuilder_BlockItem blockItem = item.PhysicalContent as MyObjectBuilder_BlockItem;
                        pts += GetBattlePoints(blockItem.BlockDefId);
                    }
                }
            }

            return pts;
        }

        public static ulong GetBattlePoints(MyDefinitionId defId)
        {
            MyCubeBlockDefinition definition;
            if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out definition))
                return 0;

            if (definition.IsGeneratedBlock)
                return 0;

            Debug.Assert(definition.Points > 0);
            return (ulong)(definition.Points > 0 ? definition.Points : 1);
        }

        public static void FillDefaultBattleServerSettings(MyObjectBuilder_SessionSettings settings, bool dedicated)
        {
            settings.GameMode = MyGameModeEnum.Survival;
            settings.Battle = true;
            settings.OnlineMode = MyOnlineModeEnum.PUBLIC;
            settings.MaxPlayers = dedicated ? (short)MAX_BATTLE_PLAYERS : (short)12;
            settings.PermanentDeath = false;
            settings.AutoSave = false;
            settings.EnableStructuralSimulation = true;
        }

    }
}
