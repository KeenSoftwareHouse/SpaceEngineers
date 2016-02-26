using Sandbox.Common;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenDebugInput : MyGuiScreenDebugBase
    {
        static StringBuilder m_debugText = new StringBuilder(1000);

        public MyGuiScreenDebugInput()
            : base(new Vector2(0.5f, 0.5f), new Vector2(), null, true)
        {
            m_isTopMostScreen = true;
            m_drawEvenWithoutFocus = true;
            CanHaveFocus = false;
        }

        public override string GetFriendlyName()
        {
            return "DebugInputScreen";
        }

        public Vector2 GetScreenLeftTopPosition()
        {
            float deltaPixels = 25 * MyGuiManager.GetSafeScreenScale();
            Rectangle fullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            return MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixels, deltaPixels));
        }

        public void SetTexts()
        {
            m_debugText.Clear();

            MyInput.Static.GetActualJoystickState(m_debugText);
        }

        public override bool Draw()
        {
            if (!base.Draw()) return false;

            SetTexts();
            float textScale = MyGuiConstants.DEBUG_STATISTICS_TEXT_SCALE;

            Vector2 origin = GetScreenLeftTopPosition();

            MyGuiManager.DrawString(MyFontEnum.White, m_debugText, origin, textScale,
                    Color.Yellow, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            return true;
        }
    }
}
