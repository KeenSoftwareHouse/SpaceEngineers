using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;

namespace Sandbox.Game.World
{
    public class MySessionCompatHelper
    {
        public virtual void FixSessionObjectBuilders(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
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
        }

        public virtual void AfterEntitiesLoad(int saveVersion)
        { }
    }
}
