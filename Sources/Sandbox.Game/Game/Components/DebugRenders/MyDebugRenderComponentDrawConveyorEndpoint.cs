using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems.Conveyors;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentDrawConveyorEndpoint : MyDebugRenderComponent
    {
        private IMyConveyorEndpoint ConveyorEndpoint { get; set; }
        public MyDebugRenderComponentDrawConveyorEndpoint(IMyConveyorEndpoint endpoint) : base(null)
        {
            ConveyorEndpoint = endpoint;
        }
        public override bool DebugDraw()
        {
            ConveyorEndpoint.DebugDraw();
            return true;
        }
    }
}
