using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Engine.Utils;
using VRageRender;
using VRageMath;

using VRage.Utils;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentTerminal:MyDebugRenderComponent
    {
        MyTerminalBlock m_terminal;
        public MyDebugRenderComponentTerminal(MyTerminalBlock terminal):base(terminal)
        {
            m_terminal = terminal;
        }
        #region overrides
        public override void DebugDraw()
        {
            base.DebugDraw();

            if (MyDebugDrawSettings.DEBUG_DRAW_BLOCK_NAMES && m_terminal.CustomName != null && MySession.Static.ControlledEntity != null)
            {
                var character = (MySession.Static.ControlledEntity as Sandbox.Game.Entities.Character.MyCharacter);
                Vector3D disp = character == null ? Vector3D.Zero : character.WorldMatrix.Up;
                Vector3D pos = m_terminal.PositionComp.WorldMatrix.Translation + disp * m_terminal.CubeGrid.GridSize * 0.4f;
                Vector3D viewerPos = MySession.Static.ControlledEntity.Entity.WorldMatrix.Translation;
                var dist = (pos - viewerPos).Length();
                if (dist > 35.0f) return;

                Color c = Color.LightSteelBlue;
                c.A = dist < 15.0f ? (byte)255 : (byte)((15.0f - dist) * 12.75f);

                var size = Math.Min(8.0f / dist, 1.0f);

                MyRenderProxy.DebugDrawText3D(pos, "<- " + m_terminal.CustomName.ToString(), c, (float)size, false, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            }
        }
        #endregion
    }
}
