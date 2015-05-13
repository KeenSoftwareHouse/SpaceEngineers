using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Game.AI;
using ProtoBuf;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncCockpit : MySyncShipController 
    {
        public MySyncCockpit(MyCockpit cockpit):
            base(cockpit)
        {

        }
    }
}
