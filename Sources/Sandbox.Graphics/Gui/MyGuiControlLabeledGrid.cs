using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlLabeledGrid : MyGuiControlGrid
    {
        public List<String> Labels = new List<String>();

        StringBuilder textBuilder = new StringBuilder();

        public float TextScale = 1.0f;

        public MyGuiControlLabeledGrid()
        {
            m_styleDef.FitSizeToItems = false;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            DrawLabels(transitionAlpha);
        }

        private void DrawLabels(float transitionAlpha)
        {
            var padding = m_styleDef.ItemPadding;
            var normalFont = m_styleDef.ItemFontNormal;           

            for (int row = 0; row < RowsCount; ++row)
            {
                for (int col = 0; col < ColumnsCount; ++col)
                {
                    int idx = ComputeIndex(row, col);
                    var item = TryGetItemAt(idx);
                    if (item != null && Labels.IsValidIndex(idx))
                    {
                        var label = Labels[idx];
                        textBuilder.Clear();
                        textBuilder.Append(label);

                        var drawPosition = m_itemsRectangle.Position + m_itemStep * new Vector2((float)col, (float)row);

                        drawPosition.X += m_itemStep.X + padding.MarginStep.X;
                        drawPosition.Y += m_itemStep.Y * 0.5f;
                        
                        bool enabled = this.Enabled && item.Enabled;

                        var maxLabelWidth = Math.Abs(Size.X  - drawPosition.X);

                        MyGuiManager.DrawString(
                                font: normalFont,
                                text: textBuilder,
                                normalizedCoord: drawPosition,
                                scale: TextScale,
                                colorMask: ApplyColorMaskModifiers(item.IconColorMask, Enabled, transitionAlpha),
                                drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                                maxTextWidth: maxLabelWidth);
                    }
                }
            }
        }

        public override void Clear()
        {
            base.Clear();

            Labels.Clear();
        }

        public void AddLabeledItem(Item gridItem, String label)
        {
            Add(gridItem);

            Labels.Add(label);
        }        
    }
}
