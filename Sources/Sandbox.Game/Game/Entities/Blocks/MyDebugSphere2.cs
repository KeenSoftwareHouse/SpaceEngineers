using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_DebugSphere2))]
    class MyDebugSphere2 : MyFunctionalBlock
    {
        #region Properties
        private new MyDebugSphere2Definition BlockDefinition
        {
            get { return (MyDebugSphere2Definition)base.BlockDefinition; }
        }
        #endregion

    }
}
