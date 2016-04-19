using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;

namespace Sandbox.Game.GameSystems
{
    public partial class MyBlockGroup
    {
        public StringBuilder Name = new StringBuilder();
        internal List<MyTerminalBlock> Blocks = new List<MyTerminalBlock>();
        private MyCubeGrid m_grid;

        internal MyBlockGroup(MyCubeGrid grid)
        {
            m_grid = grid;
        }

        internal void Init(MyObjectBuilder_BlockGroup builder)
        {
            Debug.Assert(m_grid != null);
            Name.Clear().Append(builder.Name);
            foreach (var blockPosition in builder.Blocks)
            {
                var slimBlock = m_grid.GetCubeBlock(blockPosition);
                if (slimBlock != null)
                {
                    MyTerminalBlock block = slimBlock.FatBlock as MyTerminalBlock;
                    if (block != null)
                    {
                        Blocks.Add(block);
                        continue;
                    }
                }
                //Can happen when grid is split
                //Debug.Fail("Block in group not found!");
            }
        }

        internal MyObjectBuilder_BlockGroup GetObjectBuilder()
        {
            MyObjectBuilder_BlockGroup ob = new MyObjectBuilder_BlockGroup();
            ob.Name = Name.ToString();
            foreach (var block in Blocks)
                ob.Blocks.Add(block.Position);
            return ob;
        }

        public MyCubeGrid CubeGrid { get { return m_grid; } set { m_grid = value; } }
    }
}
