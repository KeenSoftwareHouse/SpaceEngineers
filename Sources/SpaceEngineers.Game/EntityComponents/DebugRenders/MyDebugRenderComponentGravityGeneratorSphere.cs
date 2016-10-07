using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;

namespace SpaceEngineers.Game.EntityComponents.DebugRenders
{
    public class MyDebugRenderComponentGravityGeneratorSphere: MyDebugRenderComponent
    {
        MyGravityGeneratorSphere m_gravityGenerator = null;

        public MyDebugRenderComponentGravityGeneratorSphere(MyGravityGeneratorSphere gravityGenerator):base(gravityGenerator)
        {
            m_gravityGenerator = gravityGenerator;
        }
        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_MISCELLANEOUS && m_gravityGenerator.IsWorking)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(m_gravityGenerator.PositionComp.WorldMatrix.Translation, m_gravityGenerator.Radius, Color.CadetBlue, 1, false);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_MISCELLANEOUS)
            {
                VRageRender.MyRenderProxy.DebugDrawAxis(m_gravityGenerator.PositionComp.WorldMatrix, 2, false);
            }
        }
    }
}
