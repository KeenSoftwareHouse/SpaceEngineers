using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Weapons;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderCompomentDrawDrillBase : MyDebugRenderComponent
    {
        MyDrillBase m_drillBase = null;
        public MyDebugRenderCompomentDrawDrillBase(MyDrillBase drillBase):base(null)
        {
            m_drillBase = drillBase;
        }

        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_DRILLS)
            {
                m_drillBase.DebugDraw();
            }
        }
    }
}
