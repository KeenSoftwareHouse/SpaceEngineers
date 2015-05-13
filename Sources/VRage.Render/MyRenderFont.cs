using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using VRage.Utils;
using VRageMath;
using VRageRender.Textures;

namespace VRageRender
{
    public class MyRenderFont : MyFont
    {
        private Dictionary<int, MyTexture2D> m_bitmapTextureById = new Dictionary<int, MyTexture2D>();

        internal MyRenderFont(string fontFilePath)
            : base(fontFilePath)
        {
        }

        internal void LoadContent()
        {
            foreach (var entry in m_bitmapInfoByID)
            {
                var texture = MyTextureManager.GetTexture<MyTexture2D>(Path.Combine(m_fontDirectory, entry.Value.strFilename), "", null, LoadingMode.Immediate, TextureFlags.IgnoreQuality);
                m_bitmapTextureById[entry.Key] = texture;
            }
        }

        /// <summary>
        /// Draw the given string at vOrigin using the specified color
        /// </summary>
        /// <param name="position">Position on the baseline. Text will advance from this position.</param>
        /// <param name="maxTextWidth">Maximum width of the text. Texts wider than this will be truncated and they will end with an ellipsis.</param>
        /// <returns>Width of the text (in pixels).</returns>
        internal float DrawString(Vector2 position, Color colorMask, StringBuilder text, float scale, float maxTextWidth = float.PositiveInfinity)
        {
            scale *= MyRenderGuiConstants.FONT_SCALE;
            Vector2 vOrigin = new Vector2(0, 0);
            float pxWidth = 0;
            char cLast = '\0';

            Vector2 vAt = position;

            float spacingScaled = Spacing * scale;

            int line = 0;

            //  Draw each character in the string
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == NEW_LINE)
                {
                    vAt.X = position.X;
                    line++;
                    continue;
                }

                if (!CanWriteOrReplace(ref c))
                    continue;

                // Stop drawing current line and replace rest with '...'
                float currentAdvance = ComputeScaledAdvanceWithKern(c, cLast, scale);
                float ellipsisAdvance = ComputeScaledAdvanceWithKern(ELLIPSIS, c, scale);
                float currentWidth = vAt.X - position.X;
                if (currentWidth + currentAdvance + ellipsisAdvance >= maxTextWidth && (i + 1 != text.Length))
                {
                    DrawCharGlyph(ref position, scale, ref colorMask, ref vOrigin, ref pxWidth, ref cLast, ref vAt, line, ELLIPSIS);
                    pxWidth += spacingScaled;
                    vAt.X += spacingScaled;

                    while (i < text.Length && text[i] != NEW_LINE)
                        ++i;
                    if (i < text.Length && text[i] == NEW_LINE)
                        --i;

                    continue;
                }

                DrawCharGlyph(ref position, scale, ref colorMask, ref vOrigin, ref pxWidth, ref cLast, ref vAt, line, c);

                //  Spacing
                if (i < (text.Length - 1))
                {
                    pxWidth += spacingScaled;
                    vAt.X += spacingScaled;
                }
            }

            return pxWidth;
        }

        private void DrawCharGlyph(ref Vector2 vAtOriginal, float scale, ref Color currentColor, ref Vector2 vOrigin, ref float pxWidth, ref char cLast, ref Vector2 vAt, int line, char c)
        {
            MyGlyphInfo ginfo = m_glyphInfoByChar[c];

            //  If kerning is enabled, get the kern adjustment for this char pair
            if (KernEnabled)
            {
                int pxKern = CalcKern(cLast, c);
                vAt.X += pxKern * scale;
                pxWidth += pxKern * scale;
                cLast = c;
            }

            //  This will fix vertical coordinate in case we use "gpad" - left/top blank space in every character
            vAt.Y = vAtOriginal.Y + (ginfo.pxLeftSideBearing + MyRenderGuiConstants.FONT_TOP_SIDE_BEARING + line * LineHeight) * scale;

            //  Draw the glyph
            vAt.X += ginfo.pxLeftSideBearing * scale;
            if (ginfo.pxWidth != 0 && ginfo.pxHeight != 0)
            {
                Rectangle rSource = new Rectangle(ginfo.pxLocX, ginfo.pxLocY, ginfo.pxWidth, ginfo.pxHeight);
                Nullable<Rectangle> rect = rSource;
                Color color = currentColor;
                MyRender.DrawSprite(m_bitmapTextureById[ginfo.nBitmapID], vAt, rSource, color, Vector2.UnitX, vOrigin, scale, VRageRender.Graphics.SpriteEffects.None, Depth);
            }

            // update the string width and advance the pen to the next drawing position
            pxWidth += ginfo.pxAdvanceWidth * scale;
            vAt.X += (ginfo.pxAdvanceWidth - ginfo.pxLeftSideBearing) * scale;
        }

    }
}