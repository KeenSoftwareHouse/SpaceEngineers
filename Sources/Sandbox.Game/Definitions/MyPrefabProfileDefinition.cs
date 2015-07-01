using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities.Cube;

namespace Sandbox.Definitions
{    
    public class MyPrefabProfileDefinition
    {
        public MyPrefabProfileDefinition()
        {
            BlocksTypes = new SortedDictionary<string,int>();
        }

        public string Name { get; set; }
        public string GridSize { get; set; }
        public int BlocksCount { get; set; }
        public SortedDictionary<string, int> BlocksTypes;
    }
}
