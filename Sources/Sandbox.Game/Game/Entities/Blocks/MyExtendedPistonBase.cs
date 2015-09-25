﻿using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRage.Import;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ExtendedPistonBase))]
    public class MyExtendedPistonBase : MyPistonBase
    {
        new public MyExtendedPistonBaseDefinition BlockDefinition 
        {
            get { return (MyExtendedPistonBaseDefinition)base.BlockDefinition; } 
        }

        protected override MyPistonBaseDefinition PistonDefinition
        {
            get { return (MyExtendedPistonBaseDefinition)BlockDefinition; }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_ExtendedPistonBase)base.GetObjectBuilderCubeBlock(copy);
            return ob;
        }
    }
}
