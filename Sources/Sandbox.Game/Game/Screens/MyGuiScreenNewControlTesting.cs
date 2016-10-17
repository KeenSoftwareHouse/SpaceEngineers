using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using Sandbox.Gui.DirectoryBrowser;
using VRage.FileSystem;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenNewControlTesting : MyGuiScreenBase
    {
        public override string GetFriendlyName()
        {
            return "TESTING!";
        }

        public MyGuiScreenNewControlTesting() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.9f, 0.97f))
        {
            var dirBrowser = new MyGuiControlSaveBrowser()
            {
                Size = Size.Value - new Vector2(0.1f),
                Position = - Size.Value / 2 + new Vector2(0.05f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                VisibleRowsCount = 20,
                HeaderVisible = true
            };
            Controls.Add(dirBrowser);
        }
    }
}
