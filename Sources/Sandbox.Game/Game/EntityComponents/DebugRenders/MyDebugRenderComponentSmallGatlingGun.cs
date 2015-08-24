using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Weapons;
using VRageMath;
using Sandbox.Common.Components;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentSmallGatlingGun : MyDebugRenderComponent
    {
        MySmallGatlingGun m_gatlingGun = null;
        public MyDebugRenderComponentSmallGatlingGun(MySmallGatlingGun gatlingGun)
            : base(gatlingGun)
        {
            m_gatlingGun = gatlingGun;
        }

        public override bool DebugDraw()
        {
            m_gatlingGun.ConveyorEndpoint.DebugDraw();
            m_gatlingGun.PowerReceiver.DebugDraw(m_gatlingGun.PositionComp.WorldMatrix);
            return true;
        }
    }
}
