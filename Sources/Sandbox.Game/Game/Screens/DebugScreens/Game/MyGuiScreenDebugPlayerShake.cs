using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Player Shake")]
    class MyGuiScreenDebugPlayerShake : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugPlayerShake()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption("Player Head Shake", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;


            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugPlayerShake";
        }
    }

}
