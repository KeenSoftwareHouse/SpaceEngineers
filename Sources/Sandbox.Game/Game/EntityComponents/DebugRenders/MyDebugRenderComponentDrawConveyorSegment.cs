using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems.Conveyors;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentDrawConveyorSegment : MyDebugRenderComponent
    {
        private MyConveyorSegment m_conveyorSegment = null;
        public MyDebugRenderComponentDrawConveyorSegment(MyConveyorSegment conveyorSegment): base(null)
        {
            m_conveyorSegment = conveyorSegment;
        }

        public override void DebugDraw()
        {
            m_conveyorSegment.DebugDraw();
        }
    }
}
