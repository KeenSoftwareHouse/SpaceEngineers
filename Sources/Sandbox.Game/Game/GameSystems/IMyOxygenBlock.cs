using Sandbox.Game.GameSystems.Conveyors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems
{
    interface IMyOxygenBlock : IMyConveyorEndpointBlock
    {
        bool IsWorking();
    }
}
