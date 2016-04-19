using System;
using System.Text;
using VRage.Game;
using VRage.Game.ObjectBuilders.Gui;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlImage))]
    public class MyGuiControlImage : MyGuiControlBase
    {
        public string[] Textures { get; set; }

        public MyGuiControlImage()
            : this(null)
        {
        }

        public MyGuiControlImage(
            Vector2? position = null,
            Vector2? size = null,
            Vector4? backgroundColor = null,
            string backgroundTexture = null,
            string[] textures = null,
            String toolTip = null,
            MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
            : base(position: position,
                    size: size,
                    colorMask: backgroundColor,
                    toolTip: toolTip,
                    backgroundTexture: new MyGuiCompositeTexture() { Center = new MyGuiSizedTexture() { Texture = backgroundTexture } },
                    isActiveControl: false,
                    originAlign: originAlign,
                    highlightType: MyGuiControlHighlightType.NEVER)
        {
            Visible = true;
            Textures = textures != null ? textures : new string[] { "" };
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            DrawBackground(backgroundTransitionAlpha);

            if (Textures != null)
                for (int i = 0; i < Textures.Length; i++)
                    MyGuiManager.DrawSpriteBatch(
                               texture: Textures[i],
                               normalizedCoord: GetPositionAbsoluteTopLeft(),
                               normalizedSize: Size,
                               color: ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                               drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                               waitTillLoaded: false);

            DrawElements(transitionAlpha, backgroundTransitionAlpha);
            DrawBorder(transitionAlpha);
        }
    }
}
