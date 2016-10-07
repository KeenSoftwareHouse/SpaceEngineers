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
    class MyDebugRenderComponentGravityGenerator: MyDebugRenderComponent
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
