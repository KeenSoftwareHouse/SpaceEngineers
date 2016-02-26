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
    using Sandbox.ModAPI.Ingame;

    [MyCubeBlockType(typeof(MyObjectBuilder_MotorAdvancedStator))]
    class MyMotorAdvancedStator : MyMotorStator, IMyMotorAdvancedStator
    {
        public override bool Attach(MyMotorRotor rotor, bool updateGroup = true)
        {
            var ret = base.Attach(rotor, updateGroup);

            if (ret)
            {
                if (m_rotorBlock is MyMotorAdvancedRotor)
                {
                    m_conveyorEndpoint.Attach((m_rotorBlock as MyMotorAdvancedRotor).ConveyorEndpoint as MyAttachableConveyorEndpoint);
                }
            }

            return ret;
        }
        public override bool Detach(bool updateGroup = true)
        {
            if (m_rotorBlock != null)
            {
                if (m_rotorBlock is MyMotorAdvancedRotor)
                {
                    m_conveyorEndpoint.Detach((m_rotorBlock as MyMotorAdvancedRotor).ConveyorEndpoint as MyAttachableConveyorEndpoint);
                }
            }
            var ret = base.Detach(updateGroup);
            return ret;
        }

        public MyMotorAdvancedStator()
        {
            m_canBeDetached = false;
        }
    }
}
