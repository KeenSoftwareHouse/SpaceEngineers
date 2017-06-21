using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyColoredText
    {        
        #region Properties
        public StringBuilder Text { get; private set; }
        public Color NormalColor { get; private set; }
        public Color HighlightColor { get; private set; }
        public string Font { get; private set; }
        public Vector2 Offset { get; private set; }
        public float ScaleWithLanguage { get; private set; }
        public Vector2 Size { get; private set; }

        private float m_scale;
        public float Scale
        {
            get { return m_scale; }
            private set
            {
                m_scale = value;
                ScaleWithLanguage = value * MyGuiManager.LanguageTextScale;
                Size = MyGuiManager.MeasureString(Font, Text, ScaleWithLanguage);
            }
        }
        #endregion

        #region Ctors
        public MyColoredText(
            String text,
            Color? normalColor    = null,
            Color? highlightColor = null,
            string font = MyFontEnum.White,
            float textScale       = MyGuiConstants.COLORED_TEXT_DEFAULT_TEXT_SCALE,
            Vector2? offset       = null)
        {
            Text           = new StringBuilder(text.Length).Append(text);
            NormalColor    = normalColor ?? MyGuiConstants.COLORED_TEXT_DEFAULT_COLOR;
            HighlightColor = highlightColor ?? MyGuiConstants.COLORED_TEXT_DEFAULT_HIGHLIGHT_COLOR;
            Font           = font;
            Scale          = textScale;
            Offset         = offset ?? Vector2.Zero;
        }
        #endregion

        #region Methods
        public void Draw(Vector2 normalizedPosition, MyGuiDrawAlignEnum drawAlign, float backgroundAlphaFade, bool isHighlight, float colorMultiplicator = 1f)
        {
            Color drawColor = isHighlight ? HighlightColor : NormalColor;
            Vector4 vctColor = drawColor.ToVector4();
            vctColor.W *= backgroundAlphaFade;
            vctColor *= colorMultiplicator;

            MyGuiManager.DrawString(Font, Text, normalizedPosition + Offset, ScaleWithLanguage, new Color(vctColor), drawAlign);
        }

        public void Draw(Vector2 normalizedPosition, MyGuiDrawAlignEnum drawAlign, float backgroundAlphaFade, float colorMultiplicator = 1f)
        {
            Draw(normalizedPosition, drawAlign, backgroundAlphaFade, false, colorMultiplicator);
        }

        #endregion
    }
}
