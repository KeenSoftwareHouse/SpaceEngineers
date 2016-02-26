
using Sandbox.Definitions;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenDebugErrors : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugErrors";
        }

        public MyGuiScreenDebugErrors() :
            base(new Vector2(0.5f, 0.5f), null, null, true)
        {
            EnabledBackgroundFade = true;
            m_backgroundTexture = null;

            var screenRect = MyGuiManager.GetSafeFullscreenRectangle();
            float aspectRatio = (float)screenRect.Width / (float)screenRect.Height;
            Size = new Vector2(aspectRatio * 3.0f / 4.0f, 1.0f);

            CanHideOthers = true;
            m_isTopScreen = true;
            m_canShareInput = false;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            AddCaption(MyCommonTexts.ScreenDebugOfficial_ErrorLogCaption, captionOffset: new Vector2(0.0f, MyGuiConstants.SCREEN_CAPTION_DELTA_Y * -0.5f));

            m_currentPosition.Y += MyGuiConstants.SCREEN_CAPTION_DELTA_Y;

            var text = AddMultilineText(size: Size - new Vector2(0.0f, MyGuiConstants.SCREEN_CAPTION_DELTA_Y), offset: Size * -0.5f, textScale: 0.7f);

            if (MyDefinitionErrors.GetErrors().Count() == 0)
            {
                text.AppendText(MyTexts.Get(MyCommonTexts.ScreenDebugOfficial_NoErrorText));
            }
            foreach (var error in MyDefinitionErrors.GetErrors())
            {
                text.AppendText(error.ToString(), text.Font, text.TextScaleWithLanguage, error.GetSeverityColor().ToVector4());
                text.AppendLine();
                text.AppendLine(); // Extra newline to separate different errors
            }
        }
    }
}
