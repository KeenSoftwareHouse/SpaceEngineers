using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;

namespace SpaceEngineers.Game.EntityComponents.DebugRenders
{
    public class MyDebugRenderComponentGravityGenerator: MyDebugRenderComponent
    {
        MyGravityGenerator m_gravityGenerator = null;
        public MyDebugRenderComponentGravityGenerator(MyGravityGenerator gravityGenerator)
            : base(gravityGenerator)
        {
            m_gravityGenerator = gravityGenerator;
        }

        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_MISCELLANEOUS && m_gravityGenerator.IsWorking)
                VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(m_gravityGenerator.FieldSize) * m_gravityGenerator.PositionComp.WorldMatrix, Color.CadetBlue, 1, true, false);

            if (MyDebugDrawSettings.DEBUG_DRAW_MISCELLANEOUS)
            {
                VRageRender.MyRenderProxy.DebugDrawAxis(m_gravityGenerator.PositionComp.WorldMatrix, 2, false);
            }
        }
    }
}
