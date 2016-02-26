using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlSeparatorList))]
    public class MyGuiControlSeparatorList : MyGuiControlBase
    {
        struct Separator
        {
            public Vector2 Start;
            public Vector2 Size;
            public Vector4 Color;
        }

        List<Separator> m_separators;

        #region Construction and serialization

        public MyGuiControlSeparatorList():
            base (isActiveControl: false,
                  highlightType: MyGuiControlHighlightType.NEVER)
        {
            m_separators = new List<Separator>();
        }

        public override void Init(MyObjectBuilder_GuiControlBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_GuiControlSeparatorList)builder;
            m_separators.Clear();
            m_separators.Capacity = ob.Separators.Count;
            foreach (var separator in ob.Separators)
            {
                m_separators.Add(new Separator()
                {
                    Start = new Vector2(separator.StartX, separator.StartY),
                    Size  = new Vector2(separator.SizeX, separator.SizeY),
                });
            }
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_GuiControlSeparatorList)base.GetObjectBuilder();
            ob.Separators = new List<MyObjectBuilder_GuiControlSeparatorList.Separator>(m_separators.Count);
            foreach (var separator in m_separators)
            {
                ob.Separators.Add(new MyObjectBuilder_GuiControlSeparatorList.Separator()
                {
                    StartX = separator.Start.X,
                    StartY = separator.Start.Y,
                    SizeX  = separator.Size.X,
                    SizeY  = separator.Size.Y
                });
            }

            return ob;
        }

        #endregion

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            var center = GetPositionAbsoluteCenter();

            foreach (var separator in m_separators)
            {
                var color = ApplyColorMaskModifiers(ColorMask * separator.Color, Enabled, transitionAlpha);

                Vector2 leftTopInPixels = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(GetPositionAbsoluteCenter() + separator.Start);
                Vector2 sizeInPixels = MyGuiManager.GetScreenSizeFromNormalizedSize(separator.Size);

                if (sizeInPixels.X == 0f)
                    sizeInPixels.X += 1;
                else if (sizeInPixels.Y == 0f)
                    sizeInPixels.Y += 1;

                MyGuiManager.DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE, (int)leftTopInPixels.X, (int)leftTopInPixels.Y, (int)sizeInPixels.X, (int)sizeInPixels.Y, color);
            }

            //base.Draw();
        }

        public void AddHorizontal(Vector2 start, float length, float width = 0.0f, Vector4? color = null)
        {
            m_separators.Add(new Separator()
            {
                Start = start,
                Size = new Vector2(length, width),
                Color = color ?? MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4(),
            });
        }

        public void AddVertical(Vector2 start, float length, float width = 0.0f, Vector4? color = null)
        {
            m_separators.Add(new Separator()
            {
                Start = start,
                Size = new Vector2(width, length),
                Color = color ?? MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4(),
            });
        }

        public void Clear()
        {
            m_separators.Clear();
        }
    }
}
