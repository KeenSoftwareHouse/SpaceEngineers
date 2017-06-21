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
        public class StyleDefinition
        {
            public MyGuiCompositeTexture BackgroundTexture;
            public MyGuiBorderThickness Padding;
        }

        public string[] Textures { get; private set; }
        private MyGuiBorderThickness m_padding;
        private StyleDefinition m_styleDefinition;

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
            SetTextures(textures);
        }

        public void SetPadding(MyGuiBorderThickness padding)
        {
            m_padding = padding;
        }

        public void SetTextures(string[] textures = null)
        {
            Textures = textures ?? new string[] { "" };
        }

        public void SetTexture(string texture = null)
        {
            Textures = texture != null ? new[] { texture } : new[] { "" };
        }

        public bool IsAnyTextureValid()
        {
            for (int i = 0; i < Textures.Length; i++)
            {
                if (!String.IsNullOrEmpty(Textures[i]))
                    return true;
            }
            return false;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            DrawBackground(backgroundTransitionAlpha);
            if (Textures != null)
                for (int i = 0; i < Textures.Length; i++)
                    MyGuiManager.DrawSpriteBatch(
                               texture: Textures[i],
                               normalizedCoord: GetPositionAbsoluteTopLeft() + m_padding.TopLeftOffset / MyGuiConstants.GUI_OPTIMAL_SIZE,
                               normalizedSize: Size - m_padding.SizeChange / MyGuiConstants.GUI_OPTIMAL_SIZE,
                               color: ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                               drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                               waitTillLoaded: false);

            DrawElements(transitionAlpha, backgroundTransitionAlpha);
            DrawBorder(transitionAlpha);
        }

        private void RefreshInternals()
        {
            if (m_styleDefinition != null)
            {
                BackgroundTexture = m_styleDefinition.BackgroundTexture;
                m_padding = m_styleDefinition.Padding;
            }
        }

        public void ApplyStyle(StyleDefinition style)
        {
            m_styleDefinition = style;
            RefreshInternals();
        }
    }
}
