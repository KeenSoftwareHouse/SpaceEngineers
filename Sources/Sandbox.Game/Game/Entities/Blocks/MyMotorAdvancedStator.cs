using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Debugging;
using Sandbox.Game.GameSystems.Electricity;

using VRage.Utils;
using VRage.Trace;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.GameSystems.Conveyors;

namespace Sandbox.Game.Entities.Cube
{
    using Sandbox.Engine.Models;
    using VRage.Groups;
    using Sandbox.Game.Screens.Terminal.Controls;
    using Sandbox.ModAPI;
    using Sandbox.Game.Entities.Blocks;

    [MyCubeBlockType(typeof(MyObjectBuilder_MotorAdvancedStator))]
    public class MyMotorAdvancedStator : MyMotorStator, IMyMotorAdvancedStator
    {
        protected override bool Attach(MyAttachableTopBlockBase rotor, bool updateGroup = true)
        {
            if (rotor is MyMotorRotor)
            {
                var ret = base.Attach(rotor, updateGroup);

                if (ret &&updateGroup)
                {
                    if (TopBlock is MyMotorAdvancedRotor)
                    {
                        m_conveyorEndpoint.Attach((TopBlock as MyMotorAdvancedRotor).ConveyorEndpoint as MyAttachableConveyorEndpoint);
                    }
                }

                return ret;
            }
            return false;
        }
        protected override void Detach(bool updateGroup = true)
        {
            if (TopBlock != null && updateGroup)
            {
                var topBlock = TopBlock;
                if (topBlock is MyMotorAdvancedRotor)
                {
                    m_conveyorEndpoint.Detach((topBlock as MyMotorAdvancedRotor).ConveyorEndpoint as MyAttachableConveyorEndpoint);
                }
            }
            base.Detach(updateGroup);
        }

        public MyMotorAdvancedStator()
        {
            m_canBeDetached = true;
        }
    }
}
