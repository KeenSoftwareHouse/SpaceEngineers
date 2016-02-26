using System;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlPanel))]
    public class MyGuiControlPanel : MyGuiControlBase
    {
        public MyGuiControlPanel()
            : this(null)
        {
        }

        public MyGuiControlPanel(
            Vector2? position = null,
            Vector2? size = null,
            Vector4? backgroundColor = null,
            string texture = null,
            String toolTip = null,
            MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
            : base(position: position,
                    size: size,
                    colorMask: backgroundColor,
                    toolTip: toolTip,
                    backgroundTexture: new MyGuiCompositeTexture() { Center = new MyGuiSizedTexture() { Texture = texture } },
                    isActiveControl: false,
                    originAlign: originAlign,
                    highlightType: MyGuiControlHighlightType.NEVER)
        {
            Visible = true;
        }
    }
}
