using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyIconTexts : Dictionary<MyGuiDrawAlignEnum, MyColoredText>
    {
        public MyIconTexts()
            : base()
        {            
        }

        /// <summary>
        /// Returns text's position from icon's position and size
        /// </summary>
        /// <param name="iconPosition">Icon's top-left position</param>
        /// <param name="iconSize">Icon's size</param>
        /// <param name="drawAlign">Text's draw align</param>
        /// <returns></returns>
        private Vector2 GetPosition(Vector2 iconPosition, Vector2 iconSize, MyGuiDrawAlignEnum drawAlign)
        {
            Vector2 textPosition;
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    textPosition = iconPosition + new Vector2(iconSize.X / 2f, iconSize.Y);
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    textPosition = iconPosition + new Vector2(iconSize.X / 2f, iconSize.Y / 2f);
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    textPosition = iconPosition + new Vector2(iconSize.X / 2f, 0f);
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    textPosition = iconPosition + new Vector2(0f, iconSize.Y);
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    textPosition = iconPosition + new Vector2(0f, iconSize.Y / 2f);
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    textPosition = iconPosition;
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    textPosition = iconPosition + new Vector2(iconSize.X, iconSize.Y);
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    textPosition = iconPosition + new Vector2(iconSize.X, iconSize.Y / 2f);
                    break;
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    textPosition = iconPosition + new Vector2(iconSize.X, 0f);
                    break;
                default:
                    throw new Exception();
            }

            return textPosition;
        }

        /// <summary>
        /// Draws non highlight texts into icon
        /// </summary>
        /// <param name="iconPosition">Icon's top-left position</param>
        /// <param name="iconSize">Icon's size</param>
        /// <param name="backgroundAlphaFade">Background's alpha fade</param>
        /// <param name="colorMultiplicator">Color multiplicator</param>
        public void Draw(Vector2 iconPosition, Vector2 iconSize, float backgroundAlphaFade, float colorMultiplicator = 1f)
        {
            Draw(iconPosition, iconSize, backgroundAlphaFade, false, colorMultiplicator);
        }

        /// <summary>
        /// Draws texts into icon
        /// </summary>
        /// <param name="iconPosition">Icon's top-left position</param>
        /// <param name="iconSize">Icon's size</param>
        /// <param name="backgroundAlphaFade">Background's alpha fade</param>
        /// <param name="isHighlight">Defines if texts will be highlighted</param>
        /// <param name="colorMultiplicator">Color multiplicator</param> 
        public void Draw(Vector2 iconPosition, Vector2 iconSize, float backgroundAlphaFade, bool isHighlight, float colorMultiplicator = 1f)
        {
            foreach (KeyValuePair<MyGuiDrawAlignEnum, MyColoredText> keyValuePair in this)
            {
                Vector2 positionedTextPosition = GetPosition(iconPosition, iconSize, keyValuePair.Key);
                keyValuePair.Value.Draw(positionedTextPosition, keyValuePair.Key, backgroundAlphaFade, isHighlight, colorMultiplicator);
            }
        }
    }
}
