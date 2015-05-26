using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using VRageMath;

namespace VRage.Utils
{
    public static partial class MyUtils
    {
        public const string C_CRLF = "\r\n";

        /// <summary>
        /// Default number suffix, k = thousand, m = million, g/b = billion
        /// </summary>
        public static Tuple<string, float>[] DefaultNumberSuffix = new Tuple<string, float>[]
        {
            new Tuple<string, float>("k", 1000),
            new Tuple<string, float>("m", 1000 * 1000),
            new Tuple<string, float>("g", 1000 * 1000 * 1000),
            new Tuple<string, float>("b", 1000 * 1000 * 1000),
        };

        //  Example: for AlignIntToRight(12, 4, "0") it returns "0012"
        public static string AlignIntToRight(int value, int charsCount, char ch)
        {
            string ret = value.ToString();
            int length = ret.Length;
            if (length > charsCount) return ret;
            return new string(ch, charsCount - length) + ret;
        }

        public static bool TryParseWithSuffix(this string text, NumberStyles numberStyle, IFormatProvider formatProvider, out float value, Tuple<string, float>[] suffix = null)
        {
            foreach (var s in suffix ?? DefaultNumberSuffix)
            {
                if (text.EndsWith(s.Item1, StringComparison.InvariantCultureIgnoreCase))
                {
                    bool result = float.TryParse(text.Substring(0, text.Length - s.Item1.Length), numberStyle, formatProvider, out value);
                    value *= s.Item2;
                    return result;
                }
            }
            return float.TryParse(text, out value);
        }

        #region Coordinate computation from alignment
        /// <summary>
        /// Aligns rectangle, works in screen/texture/pixel coordinates, not normalized coordinates.
        /// </summary>
        /// <returns>Pixel coordinates for texture.</returns>
        public static Vector2 GetCoordAligned(Vector2 coordScreen, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return coordScreen;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return coordScreen - size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return coordScreen - size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return coordScreen - size * new Vector2(0.5f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return coordScreen - size;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return coordScreen - size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return coordScreen - size * new Vector2(1.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return coordScreen - size * new Vector2(0.0f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return coordScreen - size * new Vector2(1.0f, 0.0f);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Modifies input coordinate (in center) using alignment and
        /// size of the rectangle. Result is at position inside rectangle
        /// specified by alignment.
        /// </summary>
        public static Vector2 GetCoordAlignedFromCenter(Vector2 coordCenter, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP: return coordCenter + size * new Vector2(-0.5f, -0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER: return coordCenter + size * new Vector2(-0.5f, 0.0f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM: return coordCenter + size * new Vector2(-0.5f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP: return coordCenter + size * new Vector2(0.0f, -0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER: return coordCenter;
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM: return coordCenter + size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP: return coordCenter + size * new Vector2(0.5f, -0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER: return coordCenter + size * new Vector2(0.5f, 0.0f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM: return coordCenter + size * new Vector2(0.5f, 0.5f);

                default:
                    throw new InvalidBranchException();
            }
        }

        public static Vector2 GetCoordAlignedFromTopLeft(Vector2 topLeft, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP: return topLeft;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER: return topLeft + size * new Vector2(0f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM: return topLeft + size * new Vector2(0f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP: return topLeft + size * new Vector2(0.5f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER: return topLeft + size * new Vector2(0.5f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM: return topLeft + size * new Vector2(0.5f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP: return topLeft + size * new Vector2(1f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER: return topLeft + size * new Vector2(1f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM: return topLeft + size * new Vector2(1f, 1f);

                default:
                    Debug.Fail("Invalid branch reached.");
                    return topLeft;
            }
        }

        /// <summary>
        /// Reverses effect of alignment to compute top-left corner coordinate.
        /// </summary>
        public static Vector2 GetCoordTopLeftFromAligned(Vector2 alignedCoord, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return alignedCoord;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return alignedCoord - size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return alignedCoord - size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * new Vector2(0.5f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return alignedCoord - size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return alignedCoord - size * new Vector2(1.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * new Vector2(0.0f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return alignedCoord - size * new Vector2(1.0f, 0.0f);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Reverses effect of alignment to compute top-left corner coordinate.
        /// </summary>
        public static Vector2I GetCoordTopLeftFromAligned(Vector2I alignedCoord, Vector2I size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return alignedCoord;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return new Vector2I(
                        alignedCoord.X,
                        alignedCoord.Y - size.Y / 2);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return new Vector2I(
                        alignedCoord.X,
                        alignedCoord.Y - size.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return new Vector2I(
                        alignedCoord.X - size.X / 2,
                        alignedCoord.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return new Vector2I(
                        alignedCoord.X - size.X / 2,
                        alignedCoord.Y - size.Y / 2);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return new Vector2I(
                        alignedCoord.X - size.X / 2,
                        alignedCoord.Y - size.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return new Vector2I(
                        alignedCoord.X - size.X,
                        alignedCoord.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return new Vector2I(
                        alignedCoord.X - size.X,
                        alignedCoord.Y - size.Y / 2);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return new Vector2I(
                        alignedCoord.X - size.X,
                        alignedCoord.Y - size.Y);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Reverses effect of alignment to compute center coordinate.
        /// </summary>
        public static Vector2 GetCoordCenterFromAligned(Vector2 alignedCoord, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return alignedCoord + size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return alignedCoord;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return alignedCoord + size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return alignedCoord + size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return alignedCoord - size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return alignedCoord + size * new Vector2(0.5f, -0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return alignedCoord + size * new Vector2(-0.5f, 0.5f);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Returns coordinate within given rectangle specified by draw align. Rectangle position should be
        /// upper left corner. Conversion assumes that Y coordinates increase downwards.
        /// </summary>
        public static Vector2 GetCoordAlignedFromRectangle(ref RectangleF rect, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP: return rect.Position;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER: return rect.Position + rect.Size * new Vector2(0f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM: return rect.Position + rect.Size * new Vector2(0f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP: return rect.Position + rect.Size * new Vector2(0.5f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER: return rect.Position + rect.Size * 0.5f;
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM: return rect.Position + rect.Size * new Vector2(0.5f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP: return rect.Position + rect.Size * new Vector2(1f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER: return rect.Position + rect.Size * new Vector2(1f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM: return rect.Position + rect.Size * 1f;

                default:
                    throw new InvalidBranchException();
            }
        }
        #endregion
    }
}
