﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_DebugSphere3))]
    class MyDebugSphere3 : MyFunctionalBlock
    {
        #region Properties
        private new MyDebugSphere3Definition BlockDefinition
        {
            get { return (MyDebugSphere3Definition)base.BlockDefinition; }
        }
        #endregion

    }
}
