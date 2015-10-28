﻿using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;
using VRage.Components;

namespace World
{
	class MySpaceSessionCompatHelper : MySessionCompatHelper
	{
		public override void FixSessionObjectBuilders(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
		{
			if (sector.AppVersion == 0)
			{
				HashSet<String> previouslyColored = new HashSet<String>();
				previouslyColored.Add("LargeBlockArmorBlock");
				previouslyColored.Add("LargeBlockArmorSlope");
				previouslyColored.Add("LargeBlockArmorCorner");
				previouslyColored.Add("LargeBlockArmorCornerInv");
				previouslyColored.Add("LargeRoundArmor_Slope");
				previouslyColored.Add("LargeRoundArmor_Corner");
				previouslyColored.Add("LargeRoundArmor_CornerInv");
				previouslyColored.Add("LargeHeavyBlockArmorBlock");
				previouslyColored.Add("LargeHeavyBlockArmorSlope");
				previouslyColored.Add("LargeHeavyBlockArmorCorner");
				previouslyColored.Add("LargeHeavyBlockArmorCornerInv");
				previouslyColored.Add("SmallBlockArmorBlock");
				previouslyColored.Add("SmallBlockArmorSlope");
				previouslyColored.Add("SmallBlockArmorCorner");
				previouslyColored.Add("SmallBlockArmorCornerInv");
				previouslyColored.Add("SmallHeavyBlockArmorBlock");
				previouslyColored.Add("SmallHeavyBlockArmorSlope");
				previouslyColored.Add("SmallHeavyBlockArmorCorner");
				previouslyColored.Add("SmallHeavyBlockArmorCornerInv");
				previouslyColored.Add("LargeBlockInteriorWall");

				foreach (var obj in sector.SectorObjects)
				{
					var grid = obj as MyObjectBuilder_CubeGrid;
					if (grid == null)
						continue;

					foreach (var block in grid.CubeBlocks)
					{
						if (block.TypeId != typeof(MyObjectBuilder_CubeBlock) || !previouslyColored.Contains(block.SubtypeName))
						{
							block.ColorMaskHSV = MyRenderComponentBase.OldGrayToHSV;
						}
					}
				}
			}

			if (sector.AppVersion <= 01100001)
			{
				CheckOxygenContainers(sector);
			}
		}

		#region 01100001 Oxygen 

		private void CheckOxygenContainers(MyObjectBuilder_Sector sector)
		{
			foreach (var objectBuilder in sector.SectorObjects)
			{
				var gridBuilder = objectBuilder as MyObjectBuilder_CubeGrid;
				if (gridBuilder != null)
				{
					foreach (var cubeBuilder in gridBuilder.CubeBlocks)
					{
						var oxygenTankBuilder = cubeBuilder as MyObjectBuilder_OxygenTank;
						if (oxygenTankBuilder != null)
						{
							CheckOxygenInventory(oxygenTankBuilder.Inventory);
							continue;
						}

						var oxygenGeneratorBuilder = cubeBuilder as MyObjectBuilder_OxygenGenerator;
						if (oxygenGeneratorBuilder != null)
						{
							CheckOxygenInventory(oxygenGeneratorBuilder.Inventory);
							continue;
						}
					}
				}

				var floatingObjectBuilder = objectBuilder as MyObjectBuilder_FloatingObject;
				if (floatingObjectBuilder != null)
				{
					var oxygenContainer = floatingObjectBuilder.Item.PhysicalContent as MyObjectBuilder_OxygenContainerObject;
					if (oxygenContainer == null)
						continue;
					
					FixOxygenContainer(oxygenContainer);
				}
			}
		}

		private void CheckOxygenInventory(MyObjectBuilder_Inventory inventory)
		{
			if (inventory == null)
				return;

			foreach (var inventoryItem in inventory.Items)
			{
				var oxygenContainer = inventoryItem.PhysicalContent as MyObjectBuilder_OxygenContainerObject;
				if (oxygenContainer == null)
					continue;

				FixOxygenContainer(oxygenContainer);
			}
		}

		private void FixOxygenContainer(MyObjectBuilder_OxygenContainerObject oxygenContainer)
		{
			oxygenContainer.GasLevel = oxygenContainer.OxygenLevel;
		}

		#endregion
	}
}
