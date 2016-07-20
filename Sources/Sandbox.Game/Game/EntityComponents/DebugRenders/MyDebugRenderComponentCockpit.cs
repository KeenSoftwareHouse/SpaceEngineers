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
    class MyDebugRenderComponentCockpit: MyDebugRenderComponent
    {
        MyCockpit m_cockpit = null;

        public MyDebugRenderComponentCockpit(MyCockpit cockpit)
            : base(cockpit)
        {
            m_cockpit = cockpit;
        }

        public override void DebugDraw()
        {
            if (!MyDebugDrawSettings.DEBUG_DRAW_COCKPIT)
                return;

            if (m_cockpit.AiPilot != null)
                m_cockpit.AiPilot.DebugDraw();

            VRageRender.MyRenderProxy.DebugDrawText3D(m_cockpit.PositionComp.WorldMatrix.Translation, m_cockpit.IsShooting() ? "PEW!" : "", Color.Red, 2.0f, false);

            if (m_cockpit.Pilot == null)
                return;

            foreach (Vector3I testPosI in m_cockpit.NeighbourPositions)
            {
                Vector3D translation;
                if (m_cockpit.IsNeighbourPositionFree(testPosI, out translation))
                    VRageRender.MyRenderProxy.DebugDrawSphere(translation, 0.3f, Color.Green, 1, false);
                else
                    VRageRender.MyRenderProxy.DebugDrawSphere(translation, 0.3f, Color.Red, 1, false);
            }
        }
    }
}
