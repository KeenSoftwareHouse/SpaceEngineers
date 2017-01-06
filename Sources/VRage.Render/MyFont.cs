using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    public class MyFont
    {
        /// <summary>
        /// Replacement character shown when we don't have something in our texture.
        /// Normally, this would be \uFFFD, but BMFontGen refuses to generate it, so I put its glyph at \u25A1 (empty square)
        /// </summary>
        protected const char REPLACEMENT_CHARACTER = '\u25A1';
        protected const char ELLIPSIS = '…';
        public const char NEW_LINE = '\n';
        private static readonly KernPairComparer m_kernPairComparer = new KernPairComparer();

        protected readonly Dictionary<int, MyBitmapInfo> m_bitmapInfoByID = new Dictionary<int, MyBitmapInfo>();
        protected readonly Dictionary<char, MyGlyphInfo> m_glyphInfoByChar = new Dictionary<char, MyGlyphInfo>();
        protected readonly Dictionary<KernPair, sbyte> m_kernByPair = new Dictionary<KernPair, sbyte>(m_kernPairComparer);
        protected readonly string m_fontDirectory;


        #region Properties

        /// <summary>
        /// This is artificial spacing in between two characters (in pixels).
        /// Using it we can make spaces wider or narrower
        /// </summary>
        public int Spacing = 0;

        /// <summary>
        /// Enable / disable kerning of adjacent character pairs.
        /// </summary>
        public bool KernEnabled = true;

        /// <summary>
        /// Distance from top of font to the baseline
        /// </summary>
        public int Baseline { get; private set; }

        /// <summary>
        /// Distance from top to bottom of the font
        /// </summary>
        public int LineHeight { get; private set; }

        /// <summary>
        /// The depth at which to draw the font
        /// </summary>
        public float Depth = 0.0f;

        #endregion

        /// <summary>
        /// Create a new font from the info in the specified font descriptor (XML) file
        /// </summary>
        public MyFont(string fontFilePath, int spacing = 1)
        {
            MyRenderProxy.Log.WriteLine("MyFont.Ctor - START");
            using (var indent = MyRenderProxy.Log.IndentUsing(LoggingOptions.MISC_RENDER_ASSETS))
            {
                Spacing = spacing;
                MyRenderProxy.Log.WriteLine("Font filename: " + fontFilePath);

                string path = fontFilePath;

                if (!Path.IsPathRooted(fontFilePath))
                    path = Path.Combine(MyFileSystem.ContentPath, fontFilePath);

                if (!MyFileSystem.FileExists(path))
                {
                    var message = string.Format("Unable to find font path '{0}'.", path);
                    Debug.Fail(message);
                    throw new Exception(message);
                }

                m_fontDirectory = Path.GetDirectoryName(path);

                LoadFontXML(path);

                MyRenderProxy.Log.WriteLine("FontFilePath: " + path);
                MyRenderProxy.Log.WriteLine("LineHeight: " + LineHeight);
                MyRenderProxy.Log.WriteLine("Baseline: " + Baseline);
                MyRenderProxy.Log.WriteLine("KernEnabled: " + KernEnabled);
            }
            MyRenderProxy.Log.WriteLine("MyFont.Ctor - END");
        }

        //  Calculate the width of the given string.
        //  Returns: Width and height (in pixels) of the string
        public Vector2 MeasureString(StringBuilder text, float scale)
        {
            scale *= MyRenderGuiConstants.FONT_SCALE;
            float pxWidth = 0;
            char cLast = '\0';

            float maxPxWidth = 0;
            int lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                //  New line
                if (c == NEW_LINE)
                {
                    lines++;
                    pxWidth = 0;
                    cLast = '\0';
                    continue;
                }

                if (!CanWriteOrReplace(ref c))
                    continue;

                MyGlyphInfo ginfo = m_glyphInfoByChar[c];

                // if kerning is enabled, get the kern adjustment for this char pair
                if (KernEnabled)
                {
                    pxWidth += CalcKern(cLast, c);
                    cLast = c;
                }

                //  update the string width
                pxWidth += ginfo.pxAdvanceWidth;

                //  Spacing
                if (i < (text.Length - 1)) pxWidth += Spacing;

                //  Because new line
                if (pxWidth > maxPxWidth) maxPxWidth = pxWidth;
            }

            return new Vector2(maxPxWidth * scale, lines * LineHeight * scale);
        }

        protected bool CanWriteOrReplace(ref char c)
        {
            if (!m_glyphInfoByChar.ContainsKey(c))
            {
                if (!CanUseReplacementCharacter(c))
                    return false;
                c = REPLACEMENT_CHARACTER;
            }
            return true;
        }

        public int ComputeCharsThatFit(StringBuilder text, float scale, float maxTextWidth)
        {
            scale *= MyRenderGuiConstants.FONT_SCALE;
            maxTextWidth /= scale;
            float pxWidth = 0;
            char cLast = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                Debug.Assert(c != NEW_LINE);

                if (!CanWriteOrReplace(ref c))
                    continue;

                MyGlyphInfo ginfo = m_glyphInfoByChar[c];

                // if kerning is enabled, get the kern adjustment for this char pair
                if (KernEnabled)
                {
                    pxWidth += CalcKern(cLast, c);
                    cLast = c;
                }

                //  update the string width
                pxWidth += ginfo.pxAdvanceWidth;

                //  Spacing
                if (i < (text.Length - 1)) pxWidth += Spacing;

                //  Because new line
                if (pxWidth > maxTextWidth)
                    return i;
            }

            return text.Length;
        }

        protected float ComputeScaledAdvanceWithKern(char c, char cLast, float scale)
        {
            MyGlyphInfo ginfo = m_glyphInfoByChar[c];
            float advance = 0f;
            if (KernEnabled)
            {
                int pxKern = CalcKern(cLast, c);
                advance += pxKern * scale;
            }
            advance += ginfo.pxAdvanceWidth * scale;
            return advance;
        }

        protected bool CanUseReplacementCharacter(char c)
        {
            return !Char.IsWhiteSpace(c) && !Char.IsControl(c);
        }

        //  Get the kern value for the given pair of characters
        //  Returns: Amount to kern (in pixels)
        protected int CalcKern(char chLeft, char chRight)
        {
            sbyte kern = 0;
            m_kernByPair.TryGetValue(new KernPair(chLeft, chRight), out kern);
            return kern;
        }

        #region LoadFontXML

        private void LoadFontXML(string path)
        {
            var xd = new XmlDocument();
            using (var stream = MyFileSystem.OpenRead(path))
            {
                xd.Load(stream);
            }
            LoadFontXML(xd.ChildNodes);
        }

        /// <summary>
        /// Load the font data from an XML font descriptor file
        /// </summary>
        /// <param name="xnl">XML node list containing the entire font descriptor file</param>
        private void LoadFontXML(XmlNodeList xnl)
        {
            foreach (XmlNode xn in xnl)
            {
                if (xn.Name == "font")
                {
                    Baseline = Int32.Parse(GetXMLAttribute(xn, "base"));
                    LineHeight = Int32.Parse(GetXMLAttribute(xn, "height"));

                    LoadFontXML_font(xn.ChildNodes);
                }
            }
        }

        /// <summary>
        /// Load the data from the "font" node
        /// </summary>
        /// <param name="xnl">XML node list containing the "font" node's children</param>
        private void LoadFontXML_font(XmlNodeList xnl)
        {
            foreach (XmlNode xn in xnl)
            {
                if (xn.Name == "bitmaps")
                    LoadFontXML_bitmaps(xn.ChildNodes);
                if (xn.Name == "glyphs")
                    LoadFontXML_glyphs(xn.ChildNodes);
                if (xn.Name == "kernpairs")
                    LoadFontXML_kernpairs(xn.ChildNodes);
            }
        }

        /// <summary>
        /// Load the data from the "bitmaps" node
        /// </summary>
        /// <param name="xnl">XML node list containing the "bitmaps" node's children</param>
        private void LoadFontXML_bitmaps(XmlNodeList xnl)
        {
            foreach (XmlNode xn in xnl)
            {
                if (xn.Name == "bitmap")
                {
                    string strID = GetXMLAttribute(xn, "id");
                    string strFilename = GetXMLAttribute(xn, "name");
                    string strSize = GetXMLAttribute(xn, "size");
                    string[] aSize = strSize.Split('x');

                    MyBitmapInfo bminfo;
                    bminfo.strFilename = strFilename;
                    bminfo.nX = Int32.Parse(aSize[0]);
                    bminfo.nY = Int32.Parse(aSize[1]);

                    m_bitmapInfoByID[Int32.Parse(strID)] = bminfo;
                }
            }
        }

        /// <summary>
        /// Load the data from the "glyphs" node
        /// </summary>
        /// <param name="xnl">XML node list containing the "glyphs" node's children</param>
        private void LoadFontXML_glyphs(XmlNodeList xnl)
        {
            foreach (XmlNode xn in xnl)
            {
                if (xn.Name == "glyph")
                {
                    string strChar = GetXMLAttribute(xn, "ch");
                    string strBitmapID = GetXMLAttribute(xn, "bm");
                    string strLoc = GetXMLAttribute(xn, "loc");
                    string strSize = GetXMLAttribute(xn, "size");
                    string strAW = GetXMLAttribute(xn, "aw");
                    string strLSB = GetXMLAttribute(xn, "lsb");

                    if (strLoc == "")
                        strLoc = GetXMLAttribute(xn, "origin"); // obsolete - use loc instead

                    string[] aLoc = strLoc.Split(',');
                    string[] aSize = strSize.Split('x');

                    MyGlyphInfo ginfo = new MyGlyphInfo();
                    ginfo.nBitmapID = UInt16.Parse(strBitmapID);
                    ginfo.pxLocX = ushort.Parse(aLoc[0]);
                    ginfo.pxLocY = ushort.Parse(aLoc[1]);
                    ginfo.pxWidth = Byte.Parse(aSize[0]);
                    ginfo.pxHeight = Byte.Parse(aSize[1]);
                    ginfo.pxAdvanceWidth = Byte.Parse(strAW);
                    ginfo.pxLeftSideBearing = SByte.Parse(strLSB);

                    m_glyphInfoByChar[strChar[0]] = ginfo;
                }
            }
        }

        /// <summary>
        /// Load the data from the "kernpairs" node
        /// </summary>
        /// <param name="xnl">XML node list containing the "kernpairs" node's children</param>
        private void LoadFontXML_kernpairs(XmlNodeList xnl)
        {
            foreach (XmlNode xn in xnl)
            {
                if (xn.Name == "kernpair")
                {
                    var left = GetXMLAttribute(xn, "left")[0];
                    var right = GetXMLAttribute(xn, "right")[0];
                    var adjust = GetXMLAttribute(xn, "adjust");

                    var pair = new KernPair(left, right);

                    Debug.Assert(!m_kernByPair.ContainsKey(pair));
                    m_kernByPair[pair] = SByte.Parse(adjust);
                }
            }
        }

        /// <summary>
        /// Get the XML attribute value
        /// </summary>
        /// <param name="n">XML node</param>
        /// <param name="strAttr">Attribute name</param>
        /// <returns>Attribute value, or the empty string if the attribute doesn't exist</returns>
        private static string GetXMLAttribute(XmlNode n, string strAttr)
        {
            XmlAttribute attr = n.Attributes.GetNamedItem(strAttr) as XmlAttribute;
            if (attr != null)
                return attr.Value;
            return "";
        }

        #endregion

        #region Nested types

        /// <summary>
        ///  Info for each glyph in the font - where to find the glyph image and other properties
        /// </summary>
        protected struct MyGlyphInfo
        {
            public ushort nBitmapID;
            public ushort pxLocX;
            public ushort pxLocY;
            public byte pxWidth;
            public byte pxHeight;
            public byte pxAdvanceWidth;
            public sbyte pxLeftSideBearing;
        }

        /// <summary>
        /// Info for each font bitmap
        /// </summary>
        protected struct MyBitmapInfo
        {
            public string strFilename;
            public int nX, nY;
        }

        protected struct KernPair
        {
            public char Left;
            public char Right;

            public KernPair(char l, char r)
            {
                Left = l;
                Right = r;
            }
        }

        protected class KernPairComparer : IComparer<KernPair>, IEqualityComparer<KernPair>
        {
            public int Compare(KernPair x, KernPair y)
            {
                if (x.Left != y.Left)
                    return x.Left.CompareTo(y.Left);
                return x.Right.CompareTo(y.Right);
            }

            public bool Equals(KernPair x, KernPair y)
            {
                return x.Left == y.Left && x.Right == y.Right;
            }

            public int GetHashCode(KernPair x)
            {
                return x.Left.GetHashCode() ^ x.Right.GetHashCode();
            }
        }
        #endregion
    }

}
