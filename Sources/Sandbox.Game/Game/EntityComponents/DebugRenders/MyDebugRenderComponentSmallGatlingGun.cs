using Sandbox.Game.Weapons;
using Sandbox.Game.EntityComponents;

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

        public override void DebugDraw()
        {
            m_gatlingGun.ConveyorEndpoint.DebugDraw();
	        var sinkComp = m_gatlingGun.Components.Get<MyResourceSinkComponent>();
			if(sinkComp != null)
				sinkComp.DebugDraw(m_gatlingGun.PositionComp.WorldMatrix);
        }
    }
}
