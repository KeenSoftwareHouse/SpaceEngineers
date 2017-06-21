using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentGravityGeneratorSphere: MyDebugRenderComponent
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
