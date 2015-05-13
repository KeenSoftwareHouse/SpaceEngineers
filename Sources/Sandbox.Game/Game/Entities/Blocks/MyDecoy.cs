using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Decoy))]
    class MyDecoy : MyFunctionalBlock
    {
    }
}
