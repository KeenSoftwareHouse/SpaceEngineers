using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender;
using VRageMath;
using Sandbox.Graphics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Weapons;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentEngineerTool : MyDebugRenderComponent
    {
        MyEngineerToolBase m_tool = null;
        public MyDebugRenderComponentEngineerTool(MyEngineerToolBase tool)
            : base(tool)
        {
            m_tool = tool;
        }
        public override void DebugDraw()
        {
           // if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC)
              //  m_tool.Sensor.DebugDraw();
            // Debug cube
            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC && m_tool.GetTargetGrid() != null)
            {
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0, 0), m_tool.TargetCube.ToString(), Color.White, 1.0f);
            }
            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC)
                VRageRender.MyRenderProxy.DebugDrawSphere(m_tool.GunBase.GetMuzzleWorldPosition(), 0.01f, Color.Green, 1.0f, false);
        }

    }
}
