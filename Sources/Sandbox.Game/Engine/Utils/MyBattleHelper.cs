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

namespace Sandbox.Engine.Utils
{
    public static class MyBattleHelper
    {
        private static List<MySlimBlock> m_tmpBlocks = new List<MySlimBlock>();

        public static ulong GetBattlePoints(MyCubeGrid grid)
        {
            ulong points = 0;

            foreach (var block in grid.GetBlocks())
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                if (compoundBlock != null)
                {
                    m_tmpBlocks.Clear();
                    foreach (var blockInCompound in compoundBlock.GetBlocks(m_tmpBlocks))
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

            // Get points from container items
            IMyInventoryOwner inventoryOwner = slimBlock.FatBlock as IMyInventoryOwner;
            if (inventoryOwner != null)
            {
                var inventory = inventoryOwner.GetInventory(0);
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
                    if (item.Content is MyObjectBuilder_BlockItem)
                    {
                        MyObjectBuilder_BlockItem blockItem = item.Content as MyObjectBuilder_BlockItem;
                        pts += GetBattlePoints(blockItem.BlockDefId);
                    }
                }
            }

            return pts;
        }

        public static ulong GetBattlePoints(MyDefinitionId defId)
        {
            MyCubeBlockDefinition definition = MyDefinitionManager.Static.GetCubeBlockDefinition(defId);
            if (definition == null)
            {
                Debug.Fail("No cube block definition found to get battle points");
                return 0;
            }

            if (definition.IsGeneratedBlock)
                return 0;

            Debug.Assert(definition.Points > 0);
            return (ulong)(definition.Points > 0 ? definition.Points : 1);
        }



    }
}
