using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Gui;
using System;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

//  Label is defined by string builder or by text enum. Only one of them at a time. It's good to use enum whenever
//  possible, as it easily supports changing languages. Use string builder only if the text isn't and can't be defined
//  in text resources.
//
//  If enum version is used, then text won't be stored in string builder until you use UpdateParams

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlLabel))]
    public class MyGuiControlLabel : MyGuiControlBase
    {
        private MyFontEnum m_font;
        /// <summary>
        /// Font used for drawing. Setting null will switch to default font (ie. this never returns null).
        /// </summary>
        public MyFontEnum Font
        {
            get { return m_font; }
            set { m_font = value; }
        }

        private string m_text;
        public string Text
        {
            get { return m_text; }
            set
            {
                if (m_text != value)
                {
                    m_text = value;
                    UpdateFormatParams(null);
                }
            }
        }

        private MyStringId m_textEnum;
        public MyStringId TextEnum
        {
            get { return m_textEnum; }
            set
            {
                if (m_textEnum != value || m_text != null)
                {
                    m_textEnum = value;
                    m_text = null;
                    UpdateFormatParams(null);
                }
            }
        }

        private float m_textScale;
        public float TextScale
        {
            get { return m_textScale; }
            set
            {
                if (m_textScale != value)
                {
                    m_textScale = value;
                    TextScaleWithLanguage = value * MyGuiManager.LanguageTextScale;
                    RecalculateSize();
                }
            }
        }

        private float m_textScaleWithLanguage;
        public float TextScaleWithLanguage
        {
            get { return m_textScaleWithLanguage; }
            private set { m_textScaleWithLanguage = value; }
        }

        public StringBuilder TextToDraw;
        private StringBuilder TextForDraw
        {
            get { return TextToDraw ?? MyTexts.Get(m_textEnum); }
        }

        /// <summary>
        /// Automatically shorten text by using triple dot character
        /// </summary>
        public bool AutoEllipsis = false;

        public MyGuiControlLabel() :
            this(null)
        {
        }

        public MyGuiControlLabel(
            Vector2? position = null,
            Vector2? size = null,
            String text = null,
            Vector4? colorMask = null,
            float textScale = MyGuiConstants.DEFAULT_TEXT_SCALE,
            MyFontEnum font = MyFontEnum.Blue,
            MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
            : base(position: position,
                   size: size,
                   colorMask: colorMask,
                   isActiveControl: false)
        {
            Name = "Label";
            Font = font;
            if (text != null)
            {
                //  Create COPY of the text (Don't just point to one string builder!!! This was my original mistake!)
                m_text = text;
                TextToDraw = new StringBuilder(text);
            }
            OriginAlign = originAlign;
            TextScale = textScale;
        }



        //public MyGuiControlLabel(
        //    Vector2? position = null,
        //    Vector2? size = null,
        //    StringBuilder text = null,
        //    Vector4? colorMask = null,
        //    float textScale = MyGuiConstants.DEFAULT_TEXT_SCALE,
        //    MyFontEnum font = MyFontEnum.Blue,
        //    MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
        //    : base(position: position,
        //           size: size,
        //           colorMask: colorMask,
        //           isActiveControl: false)
        //{
        //    Name = "Label";
        //    Font = font;
        //    if (text != null)
        //    {
        //        //  Create COPY of the text (Don't just point to one string builder!!! This was my original mistake!)
        //        m_text = text.ToString();
        //        TextToDraw = new StringBuilder(text.ToString());
        //    }
        //    OriginAlign = originAlign;
        //    TextScale = textScale;
        //}

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            var labelObjectBuilder = (MyObjectBuilder_GuiControlLabel)objectBuilder;
            m_textEnum = MyStringId.GetOrCompute(labelObjectBuilder.TextEnum);
            TextScale = labelObjectBuilder.TextScale;
            m_text = String.IsNullOrWhiteSpace(labelObjectBuilder.Text) ? null : labelObjectBuilder.Text;
            Font = labelObjectBuilder.Font;
            TextToDraw = new StringBuilder();
            UpdateFormatParams(null);
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var objectBuilder = (MyObjectBuilder_GuiControlLabel)base.GetObjectBuilder();

            objectBuilder.TextEnum = m_textEnum.ToString();
            objectBuilder.TextScale = TextScale;
            objectBuilder.Text = m_text;
            objectBuilder.Font = Font;

            return objectBuilder;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            // String builder has priority when drawing.
            float maxWidth = AutoEllipsis ? Size.X : float.PositiveInfinity;
            MyGuiManager.DrawString(Font, TextForDraw, GetPositionAbsolute(), TextScaleWithLanguage, ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha), OriginAlign, maxTextWidth: maxWidth);
        }

        public Vector2 GetTextSize()
        {
            return MyGuiManager.MeasureString(Font, TextForDraw, TextScaleWithLanguage);
        }

        /// <summary>
        /// If label's text contains params, we can update them here. Also, don't forget
        /// that text is defined two time: one as a definition and one that we draw.
        /// </summary>
        /// <param name="args"></param>
        public void UpdateFormatParams(params object[] args)
        {
            // Empty string is valid, only check null string
            if (m_text == null)
            {
                if (TextToDraw == null) TextToDraw = new StringBuilder();
                TextToDraw.Clear();

                if (args != null)
                    TextToDraw.AppendFormat(MyTexts.GetString(m_textEnum), args);
                else
                    TextToDraw.Append(MyTexts.GetString(m_textEnum));
            }
            else
            {
                if (TextToDraw == null) TextToDraw = new StringBuilder();
                TextToDraw.Clear();

                if (args != null)
                    TextToDraw.AppendFormat(m_text.ToString(), args);
                else
                    TextToDraw.Append(m_text);
            }

            RecalculateSize();
        }

        public void RecalculateSize()
        {
            Size = GetTextSize();
        }
    }
}
