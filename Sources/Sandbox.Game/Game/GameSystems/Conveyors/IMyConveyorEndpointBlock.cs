using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems.Conveyors
{
    interface IMyConveyorEndpointBlock
    {
        IMyConveyorEndpoint ConveyorEndpoint { get; }
        void InitializeConveyorEndpoint();
    }
}
