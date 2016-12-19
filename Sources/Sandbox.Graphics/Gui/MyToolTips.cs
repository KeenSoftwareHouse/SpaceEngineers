using System;
using System.Collections.Specialized;
using VRage.Collections;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyToolTips
    {
        public readonly ObservableCollection<MyColoredText> ToolTips;

        public Vector2 Size;

        public bool Highlight { get; set; }
        public Vector4 HighlightColor { get; set; }

        /// <summary>
        /// Creates new instance with empty tooltips
        /// </summary>
        public MyToolTips()
        {
            ToolTips = new ObservableCollection<MyColoredText>();
            ToolTips.CollectionChanged += ToolTips_CollectionChanged;
            Size = new Vector2(-1f);
            HighlightColor = Color.Orange.ToVector4();
        }

        void ToolTips_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RecalculateSize();
        }

        /// <summary>
        /// Creates new instance with one default tooltip
        /// </summary>
        /// <param name="toolTip">Tooltip's text</param>
        public MyToolTips(String toolTip)
            : this()
        {
            AddToolTip(toolTip);
        }

        public void AddToolTip(String toolTip,
            float textScale = MyGuiConstants.TOOL_TIP_TEXT_SCALE,
            string font = MyFontEnum.Blue)
        {
            if (toolTip != null)
                ToolTips.Add(new MyColoredText(toolTip, Color.White, font: font, textScale: textScale));
        }

        /// <summary>
        /// Recalculates size of tooltips
        /// </summary>
        public void RecalculateSize()
        {
            float width = 0f;
            float height = 4f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y; // add a little bit of vertical space to ensure text is nicely centered.
            bool isEmptyToolTip = true;
            for (int i = 0; i < ToolTips.Count; i++)
            {
                if (ToolTips[i].Text.Length > 0)
                    isEmptyToolTip = false;
                
                Vector2 actualToolTipSize = MyGuiManager.MeasureString(MyFontEnum.Blue, ToolTips[i].Text, ToolTips[i].ScaleWithLanguage);
                width = Math.Max(Size.X, actualToolTipSize.X);
                height += actualToolTipSize.Y;
            }

            if (isEmptyToolTip)
            {
                Size.X = -1f;
                Size.Y = -1f;
            }
            else
            {
                Size.X = width;
                Size.Y = height;
            }
        }

        public void Draw(Vector2 mousePosition)
        {
            const bool centerHeight = false;
            var normalizedPosition = mousePosition + MyGuiConstants.TOOL_TIP_RELATIVE_DEFAULT_POSITION;

            if (Size.X > -1f)
            {
                Vector2 innerBorder = new Vector2(0.005f, 0.002f);
                Vector2 bgSize = Size + 2 * innerBorder;
                Vector2 bgPosition = normalizedPosition - new Vector2(innerBorder.X, centerHeight ? bgSize.Y / 2 : 0);

                var screenRectangle = MyGuiManager.FullscreenHudEnabled ? MyGuiManager.GetFullscreenRectangle() : MyGuiManager.GetSafeFullscreenRectangle();
                var topleft = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate(new Vector2(screenRectangle.Left, screenRectangle.Top)) + new Vector2(MyGuiConstants.TOOLTIP_DISTANCE_FROM_BORDER);
                var rightbottom = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate(new Vector2(screenRectangle.Right, screenRectangle.Bottom)) - new Vector2(MyGuiConstants.TOOLTIP_DISTANCE_FROM_BORDER);

                if (bgPosition.X + bgSize.X > rightbottom.X) bgPosition.X = rightbottom.X - bgSize.X;
                if (bgPosition.Y + bgSize.Y > rightbottom.Y) bgPosition.Y = rightbottom.Y - bgSize.Y;
                if (bgPosition.X < topleft.X) bgPosition.X = topleft.X;
                if (bgPosition.Y < topleft.Y) bgPosition.Y = topleft.Y;

                if (Highlight)
                {
                    Vector2 offset = new Vector2(0.003f, 0.004f);
                    Vector2 highlightPosition = bgPosition - offset;
                    Vector2 highlightSize = bgSize + 2 * offset;

                    MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.Draw(highlightPosition, highlightSize, HighlightColor);
                }

                MyGuiManager.DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE, bgPosition, bgSize, MyGuiConstants.THEMED_GUI_BACKGROUND_COLOR, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                MyGuiManager.DrawBorders(bgPosition, bgSize, MyGuiConstants.THEMED_GUI_LINE_COLOR, 1);

                Vector2 toolTipPosition = bgPosition + new Vector2(innerBorder.X, bgSize.Y / 2 - Size.Y / 2f);

                foreach (MyColoredText toolTip in ToolTips)
                {
                    toolTip.Draw(toolTipPosition, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, 1f, false);
                    toolTipPosition.Y += toolTip.Size.Y;
                }
            }
        }
    }
}
