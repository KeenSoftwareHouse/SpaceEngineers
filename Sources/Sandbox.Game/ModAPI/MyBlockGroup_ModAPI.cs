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
    partial class MyBlockGroup : IMyBlockGroup
    {
        List<IMyTerminalBlock> IMyBlockGroup.Blocks
        {
            get
            {
                List<IMyTerminalBlock> ret = new List<IMyTerminalBlock>();
                foreach (var block in Blocks)
                {
                    if (block.GetProgrammableBlockAccessibility == IngameScriptAccessibility.readWriteAccess)
                    {
                        ret.Add(block);
                    }
                }
                return ret;
            }
        }
        String IMyBlockGroup.Name
        {
            get { return Name.ToString(); }
        }
    }
}
