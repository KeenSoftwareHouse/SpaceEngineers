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
    [MyCubeBlockType(typeof(MyObjectBuilder_DebugSphere1))]
    class MyDebugSphere1 : MyFunctionalBlock
    {
        #region Properties
        private new MyDebugSphere1Definition BlockDefinition
        {
            get { return (MyDebugSphere1Definition)base.BlockDefinition; }
        }
        #endregion

    }
}
