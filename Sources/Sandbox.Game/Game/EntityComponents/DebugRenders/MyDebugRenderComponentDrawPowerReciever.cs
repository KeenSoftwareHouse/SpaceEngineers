using Sandbox.Game.EntityComponents;
using VRage.ModAPI;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponentDrawPowerReciever : MyDebugRenderComponent
    {
	    private readonly MyResourceSinkComponent m_sink = null;
        private IMyEntity m_entity =null;

		public MyDebugRenderComponentDrawPowerReciever(MyResourceSinkComponent sink, IMyEntity entity)
			: base(null)
		{
			m_sink = sink;
			m_entity = entity;
		}

        public override void DebugDraw()
        {
			m_sink.DebugDraw(m_entity.PositionComp.WorldMatrix);
        }
    }
}
