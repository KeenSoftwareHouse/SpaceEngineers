#region Using

using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using VRage.FileSystem;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Color = VRageMath.Color;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;


#endregion

namespace Sandbox.Graphics
{

    public struct MyFontDescription
    {
        public string Id;
        public string Path;
        public bool IsDebug;
    }

    public static class MyGuiManager
    {

#if !XB1
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
#endif // !XB1

        private static Vector2 vector2Zero = Vector2.Zero;
        private static Rectangle? nullRectangle;

        public static int TotalTimeInMilliseconds;

        public static MatrixD Camera 
        {
            get { return m_camera.WorldMatrix; }
        }

        public static VRageMath.MatrixD CameraView
        {
            get { return m_camera.ViewMatrix; }
        }

        public const int FAREST_TIME_IN_PAST = -60 * 1000;

        private static VRage.Game.Utils.MyCamera m_camera;
        public static void SetCamera(VRage.Game.Utils.MyCamera camera)
        {
            m_camera = camera;
        }


        //  Textures for GUI elements such as screen background, etc... for wide or tall screens and backgrounds
        public class MyGuiTextureScreen
        {
            string m_filename;
            float m_aspectRatio;

            //  We don't allow default constructor for this class
            private MyGuiTextureScreen() { }

            public MyGuiTextureScreen(string filename, int width, int height)
            {
                m_filename = filename;
                m_aspectRatio = (float)width / (float)height;
            }

            public string GetFilename()
            {
                return m_filename;
            }

            public float GetAspectRatio()
            {
                return m_aspectRatio;
            }
        }

        //  Safe coordinates and size of GUI screen. It makes sure we are OK with aspect ratio and
        //  also it makes sure that if very-wide resolution is used (5000x1000 or so), we draw GUI only in the middle screen.
        static Rectangle m_safeGuiRectangle;            //  Rectangle for safe GUI, it independent from screen aspect ration so GUI elements look same on any resolution (especially their width)
        static Rectangle m_safeFullscreenRectangle;     //  Rectangle for safe fullscreen and GUI - use only when you draw fullscreen images and want to stretch it from left to right. 
        static float m_safeScreenScale;             //  Height scale of actual screen if compared to reference 1200p (calculated as real_height / 1200)
        static Rectangle m_fullscreenRectangle;         //  Real fullscreen

        //  Current debug screens
        //static MyGuiScreenDebugBase m_currentStatisticsScreen;
        //static MyGuiScreenDebugBase m_currentDebugScreen;
        static bool m_debugScreensEnabled = true;

        public static bool IsDebugScreenEnabled() { return m_debugScreensEnabled; }

        //  Normalized coordinates where width is always 1.0 and height something like 0.8
        //  Don't confuse with GUI normalized coordinates. They are different.
        static Vector2 m_hudSize;
        static Vector2 m_hudSizeHalf;

        //  Min and max mouse coords (in normalized units). Must be calculated from fullscreen, no GUI rectangle.
        static Vector2 m_minMouseCoord;
        static Vector2 m_maxMouseCoord;
        //  Min and max mouse coords for the fullscreen HUD (in normalized units). Must be calculated from fullscreen, no GUI rectangle.
        static Vector2 m_minMouseCoordFullscreenHud;
        static Vector2 m_maxMouseCoordFullscreenHud;

        static string m_mouseCursorTexture;
        //        static MyTexture2D m_mouseCursorArrowTexture;
#if !XB1
        static System.Drawing.Bitmap m_mouseCursorBitmap;
#endif

        static List<MyGuiTextureScreen> m_backgroundScreenTextures;

        static MyGuiScreenBase m_lastScreenWithFocus;

        static bool m_fullScreenHudEnabled = false;
        public static bool FullscreenHudEnabled { get { return m_fullScreenHudEnabled; } set { m_fullScreenHudEnabled = value; } }

        static Dictionary<MyStringHash, VRageRender.MyFont> m_fontsById = new Dictionary<MyStringHash, VRageRender.MyFont>();

        //static MyEffectSpriteBatchOriginal m_spriteEffect;

        public class MyScreenShot
        {
            public bool IgnoreSprites;
            public VRageMath.Vector2 SizeMultiplier;
            public string Path;

            public MyScreenShot(VRageMath.Vector2 sizeMultiplier, string path, bool ignoreSprites)
            {
                IgnoreSprites = ignoreSprites;
                Path = path;
                SizeMultiplier = sizeMultiplier;
            }
        }



        static MyScreenShot m_screenshot;
        public static MyScreenShot GetScreenshot()
        {
            return m_screenshot;
        }

     

        //  This one cas be public and not-readonly because we may want to change it from other screens or controls
        private static Vector2 m_mouseCursorPosition;
        public static Vector2 MouseCursorPosition
        {
            get { return m_mouseCursorPosition; }
            set { m_mouseCursorPosition = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        static MyGuiManager()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            MyGuiControlsFactory.RegisterDescriptorsFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            MyGuiControlsFactory.RegisterDescriptorsFromAssembly(Assembly.GetCallingAssembly());
#endif // !XB1
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        public static void LoadData()
        {

        }

        public static bool FontExists(string font)
        {
            return m_fontsById.ContainsKey(MyStringHash.GetOrCompute(font));
        }

        public static void LoadContent(MyFontDescription[] fonts)
        {
            VRageRender.MyRenderProxy.Log.WriteLine("MyGuiManager2.LoadContent() - START");
            VRageRender.MyRenderProxy.Log.IncreaseIndent();

            var path = Path.Combine(MyFileSystem.ContentPath, Path.Combine("Textures", "GUI", "MouseCursorHW.png"));
#if !XB1
            using (var stream = MyFileSystem.OpenRead(path))
            {
                m_mouseCursorBitmap = System.Drawing.Bitmap.FromStream(stream) as System.Drawing.Bitmap;
            }
            SetHWCursorBitmap(m_mouseCursorBitmap);
#endif
            SetMouseCursorTexture(MyGuiConstants.CURSOR_ARROW);
           
            m_backgroundScreenTextures = new List<MyGuiTextureScreen>
            {
                new MyGuiTextureScreen(MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.Texture,
                                       (int)MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.SizePx.X,
                                       (int)MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.SizePx.Y),
            };

            foreach (var font in fonts)
            {
                m_fontsById[MyStringHash.GetOrCompute(font.Id)] = new VRageRender.MyFont(font.Path);
                VRageRender.MyRenderProxy.CreateFont((int)MyStringHash.GetOrCompute(font.Id), font.Path, font.IsDebug);
            }

            VRageRender.MyRenderProxy.PreloadTextures(@"Textures\GUI\Icons", true);
            VRageRender.MyRenderProxy.PreloadTextures(@"Textures\GUI\Controls", true);

            MouseCursorPosition = new Vector2(0.5f, 0.5f);// new MyMwcVector2Int(MySandboxGame.ScreenSizeHalf.X, MySandboxGame.ScreenSizeHalf.Y);

            VRageRender.MyRenderProxy.Log.DecreaseIndent();
            VRageRender.MyRenderProxy.Log.WriteLine("MyGuiManager2.LoadContent() - END");
        }

        //  Normalized size of screen for HUD (we are geting it from GUI, because GUI knows safe screen)
        static Vector2 CalculateHudSize()
        {
            return new Vector2(1.0f, (float)m_safeFullscreenRectangle.Height / (float)m_safeFullscreenRectangle.Width);
        }

        public static float GetSafeScreenScale()
        {
            return m_safeScreenScale;
        }

        public static Rectangle GetSafeGuiRectangle()
        {
            return m_safeGuiRectangle;
        }

        public static Rectangle GetSafeFullscreenRectangle()
        {
            return m_safeFullscreenRectangle;
        }

        public static Rectangle GetFullscreenRectangle()
        {
            return m_fullscreenRectangle;
        }

        /// <summary>
        /// Computes aligned coordinate for screens without size (Size == null) with optional pixel offset from given origin.
        /// </summary>
        public static Vector2 ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum align, int pixelOffsetX = 54, int pixelOffsetY = 54)
        {
            float deltaPixelsX = pixelOffsetX * m_safeScreenScale;
            float deltaPixelsY = pixelOffsetY * m_safeScreenScale;
            var topLeft = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixelsX, deltaPixelsY));
            switch (align)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:    return topLeft;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER: return new Vector2(topLeft.X, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM: return new Vector2(topLeft.X, 1f - topLeft.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:    return new Vector2(0.5f, topLeft.Y);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER: return new Vector2(0.5f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM: return new Vector2(0.5f, 1f - topLeft.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:    return new Vector2(1f - topLeft.X, topLeft.Y);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER: return new Vector2(1f - topLeft.X, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM: return new Vector2(1f - topLeft.X, 1f - topLeft.Y);

                default:
                    Debug.Fail("Invalid branch.");
                    return topLeft;
            }

        }

        #region Measure and Draw String


        /// <summary>Draws string (string builder) at specified position</summary>
        /// <param name="normalizedCoord">and Y are within interval [0,1]></param>
        /// <param name="scale">Scale for original texture, it's not in pixel/texels,
        /// but multiply of original size. E.g. 1 means unchanged size, 2 means double size.
        /// Scale is uniform, preserves aspect ratio.</param>
        /// <param name="useFullClientArea">True uses full client rectangle. False limits to GUI rectangle</param>
        public static void DrawString(
            string font,
            StringBuilder text,
            Vector2 normalizedCoord,
            float scale,
            Color? colorMask             = null,
            MyGuiDrawAlignEnum drawAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
            bool useFullClientArea       = false,
            float maxTextWidth           = float.PositiveInfinity)
        {
            var size             = MeasureString(font, text, scale);
            size.X               = Math.Min(maxTextWidth, size.X);
            var topLeft          = MyUtils.GetCoordTopLeftFromAligned(normalizedCoord, size, drawAlign);
            Vector2 screenCoord  = GetScreenCoordinateFromNormalizedCoordinate(topLeft, useFullClientArea);
            float screenScale    = scale * m_safeScreenScale;
            float screenMaxWidth = GetScreenSizeFromNormalizedSize(new Vector2(maxTextWidth, 0f)).X;

#if DEBUG_TEXT_SIZE
            DebugTextSize(text, ref size);
#endif

            VRageRender.MyRenderProxy.DrawString(
                (int)MyStringHash.Get(font),
                screenCoord,
                colorMask ?? new Color(MyGuiConstants.LABEL_TEXT_COLOR),
                text.ToString(),
                screenScale,
                screenMaxWidth);
        }

        public static Vector2 MeasureString(string font, StringBuilder text, float scale)
        {
            //  Fix the scale for screen resolution
            float fixedScale = scale * m_safeScreenScale;
            Vector2 sizeInPixelsScaled = m_fontsById[MyStringHash.Get(font)].MeasureString(text, fixedScale);
            return GetNormalizedSizeFromScreenSize(sizeInPixelsScaled);
        }

        internal static int ComputeNumCharsThatFit(string font, StringBuilder text, float scale, float maxTextWidth)
        {
            float fixedScale = scale * m_safeScreenScale;
            float screenMaxWidth = GetScreenSizeFromNormalizedSize(new Vector2(maxTextWidth, 0f)).X;
            return m_fontsById[MyStringHash.Get(font)].ComputeCharsThatFit(text, fixedScale, screenMaxWidth);
        }

        public static float GetFontHeight(string font, float scale)
        {
            //  Fix the scale for screen resolution
            float fixedScale = scale * m_safeScreenScale * MyRenderGuiConstants.FONT_SCALE;
            Vector2 sizeInPixelsScaled = new Vector2(0.0f, fixedScale * m_fontsById[MyStringHash.Get(font)].LineHeight);
            return GetNormalizedSizeFromScreenSize(sizeInPixelsScaled).Y;
        }

        static HashSet<String> m_sizes = new HashSet<string>();

        [Conditional("DEBUG")]
        private static void DebugTextSize(StringBuilder text, ref Vector2 size)
        {
            string str = text.ToString();
            bool inserted = m_sizes.Add(str);
            if (inserted)
                Console.WriteLine("Text = \"" + str + "\", Width = " + size.X);
        }

        #endregion

        #region Draw Sprite Batch
        /// <summary>
        /// Draw a sprite into rectangle specified in screen coordinates (pixels).
        /// </summary>
        /// <param name="texture">Sprite texture as path.</param>
        /// <param name="rectangle">Rectangle in screen coordinates (pixels).</param>
        /// <param name="color">Masking color.</param>
        public static void DrawSprite(string texture, Rectangle rectangle, Color color, bool waitTillLoaded = true)
        {
            var destination = new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            DrawSprite(texture, ref destination, false, ref nullRectangle, color, 0f, ref vector2Zero, SpriteEffects.None, 0f, waitTillLoaded);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        public static void DrawSpriteBatch(string texture, int x, int y, int width, int height, Color color, bool waitTillLoaded = true)
        {
            DrawSprite(texture, new Rectangle(x, y, width, height), color, waitTillLoaded);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        public static void DrawSpriteBatch(string texture, Rectangle destinationRectangle, Color color, bool waitTillLoaded = true)
        {
            if (string.IsNullOrEmpty(texture))
                return;

            var destination = new RectangleF(destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height);
            DrawSprite(texture, ref destination, false, ref nullRectangle, color, 0f, ref vector2Zero, SpriteEffects.None, 0f, waitTillLoaded);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        public static void DrawSpriteBatch(string texture, Vector2 pos, Color color, bool waitTillLoaded = true)
        {
            if (string.IsNullOrEmpty(texture))
                return;

            DrawSprite(texture, pos, color, waitTillLoaded);
        }

        public static void DrawSprite(string texture, Vector2 position, Color color, bool waitTillLoaded = true)
        {
            var destination = new RectangleF(position.X, position.Y, 1f, 1f);
            DrawSprite(texture, ref destination, true, ref nullRectangle, color, 0f, ref vector2Zero, SpriteEffects.None, 0f, waitTillLoaded);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        public static void DrawSpriteBatch(string texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, bool waitTillLoaded = true)
        {
            DrawSprite(texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth, waitTillLoaded);
        }

        public static void DrawSprite(string texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, bool waitTillLoaded = true)
        {
            var destination = new RectangleF(position.X, position.Y, scale.X, scale.Y);
            DrawSprite(texture, ref destination, true, ref sourceRectangle, color, rotation, ref origin, effects, layerDepth, waitTillLoaded);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        public static void DrawSpriteBatch(string texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, bool waitTillLoaded = true)
        {
            DrawSprite(texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth, waitTillLoaded);
        }

        static void DrawSprite(string texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, bool waitTillLoaded = true)
        {
            var destination = new RectangleF(position.X, position.Y, scale, scale);
            DrawSprite(texture, ref destination, true, ref sourceRectangle, color, rotation, ref origin, effects, layerDepth, waitTillLoaded);
        }

        static void DrawSprite(string texture, ref RectangleF destination, bool scaleDestination, ref Rectangle? sourceRectangle, Color color, float rotation, ref Vector2 origin, SpriteEffects effects, float depth, bool waitTillLoaded = true)
        {
            VRageRender.MyRenderProxy.DrawSprite(texture, ref destination, scaleDestination, ref sourceRectangle, color, rotation, Vector2.UnitX, ref origin, effects, depth, waitTillLoaded);
        }

        //  Draws sprite batch at specified NORMALIZED position, with SCREEN-PIXEL width, but NORMALIZED height
        //  Use if you want to draw rectangle where width is in screen coords, but rest is in normalized coord (e.g. textbox carriage blinker)
        public static void DrawSpriteBatch(string texture, Vector2 normalizedCoord, int screenWidth, float normalizedHeight, Color color, MyGuiDrawAlignEnum drawAlign, bool waitTillLoaded = true)
        {
            if (string.IsNullOrEmpty(texture))
                return;

            Vector2 screenCoord = GetScreenCoordinateFromNormalizedCoordinate(normalizedCoord);
            Vector2 screenSize = GetScreenSizeFromNormalizedSize(new Vector2(0, normalizedHeight));
            screenSize.X = screenWidth; //  Replace with desired value
            screenCoord = MyUtils.GetCoordAligned(screenCoord, screenSize, drawAlign);

            DrawSprite(texture, new Rectangle((int)screenCoord.X, (int)screenCoord.Y, (int)screenSize.X, (int)screenSize.Y), color, waitTillLoaded);
        }

        //  Draws sprite batch at specified NORMALIZED position, with NORMALIZED width, but SCREEN-PIXEL height.
        //  Use if you want to draw rectangle where height is in screen coords, but rest is in normalized coord (e.g. slider long line)
        public static void DrawSpriteBatch(string texture, Vector2 normalizedCoord, float normalizedWidth, int screenHeight, Color color, MyGuiDrawAlignEnum drawAlign, bool waitTillLoaded = true)
        {
            if (string.IsNullOrEmpty(texture))
                return;

            Vector2 screenCoord = GetScreenCoordinateFromNormalizedCoordinate(normalizedCoord);
            Vector2 screenSize = GetScreenSizeFromNormalizedSize(new Vector2(normalizedWidth, 0));
            screenSize.Y = screenHeight; //  Replace with desired value
            screenCoord = MyUtils.GetCoordAligned(screenCoord, screenSize, drawAlign);

            DrawSprite(texture, new Rectangle((int)screenCoord.X, (int)screenCoord.Y, (int)screenSize.X, (int)screenSize.Y), color, waitTillLoaded);
        }

        /// <summary>Draws sprite batch at specified position</summary>
        /// <param name="normalizedCoord">X and Y are within interval [0,1]</param>
        /// <param name="normalizedSize">size of destination rectangle (normalized).
        /// Don't forget that it may be distorted by aspect ration, so rectangle size
        /// [1,1] can make larger wide than height on your screen.</param>
        /// <param name="useFullClientArea">True uses full client rectangle. False limits to GUI rectangle</param>
        public static void DrawSpriteBatch(string texture, Vector2 normalizedCoord, Vector2 normalizedSize, Color color, MyGuiDrawAlignEnum drawAlign, bool useFullClientArea = false, bool waitTillLoaded = true)
        {
            if (string.IsNullOrEmpty(texture))
                return;

            Vector2 screenCoord = GetScreenCoordinateFromNormalizedCoordinate(normalizedCoord, useFullClientArea);
            Vector2 screenSize = GetScreenSizeFromNormalizedSize(normalizedSize, useFullClientArea);
            screenCoord = MyUtils.GetCoordAligned(screenCoord, screenSize, drawAlign);

            var rect = new Rectangle((int)screenCoord.X, (int)screenCoord.Y, (int)screenSize.X, (int)screenSize.Y);

            DrawSprite(texture, rect, color, waitTillLoaded);
        }

        // different rounding of coords
        public static void DrawSpriteBatchRoundUp(string texture, Vector2 normalizedCoord, Vector2 normalizedSize, Color color, MyGuiDrawAlignEnum drawAlign, bool waitTillLoaded = true)
        {
            if (string.IsNullOrEmpty(texture))
                return;

            Vector2 screenCoord = GetScreenCoordinateFromNormalizedCoordinate(normalizedCoord);
            Vector2 screenSize = GetScreenSizeFromNormalizedSize(normalizedSize);
            screenCoord = MyUtils.GetCoordAligned(screenCoord, screenSize, drawAlign);

            DrawSprite(texture, new Rectangle((int)Math.Floor(screenCoord.X), (int)Math.Floor(screenCoord.Y), (int)Math.Ceiling(screenSize.X), (int)Math.Ceiling(screenSize.Y)), color, waitTillLoaded);
        }


        /// <summary>
        ///  Draws sprite batch at specified position
        ///  normalizedPosition -> X and Y are within interval <0..1>
        ///  size -> size of destination rectangle (normalized). Don't forget that it may be distorted by aspect ration, so rectangle size [1,1] can make larger wide than height on your screen.
        ///  rotation -> angle in radians. Rotation is always around "origin" coordinate
        ///  originNormalized -> the origin of the sprite. Specify (0,0) for the upper-left corner.
        ///  RETURN: Method returns rectangle where was sprite/texture drawn in normalized coordinates
        /// </summary>
        /// <returns></returns>
        public static void DrawSpriteBatch(string texture, Vector2 normalizedCoord, Vector2 normalizedSize, Color color, MyGuiDrawAlignEnum drawAlign, float rotation, bool waitTillLoaded = true)
        {
            VRageRender.MyRenderProxy.DrawSprite(texture, normalizedCoord, normalizedSize, color, drawAlign, rotation, Vector2.UnitX, 1, null, waitTillLoaded: waitTillLoaded);
        }

        static void DrawSprite(string texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth, bool waitTillLoaded = true)
        {
            var destination = new RectangleF(destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height);
            DrawSprite(texture, ref destination, false, ref sourceRectangle, color, rotation, ref origin, effects, layerDepth, waitTillLoaded);
        }


        //  Draws sprite batch at specified position
        //  normalizedPosition -> X and Y are within interval <0..1>
        //  scale -> scale for original texture, it's not in pixel/texels, but multiply of original size. E.g. 1 means unchanged size, 2 means double size. Scale is uniform, preserves aspect ratio.
        //  rotation -> angle in radians. Rotation is always around "origin" coordinate
        //  originNormalized -> the origin of the sprite. Specify (0,0) for the upper-left corner.
        //  RETURN: Method returns rectangle where was sprite/texture drawn in normalized coordinates
        public static void DrawSpriteBatch(string texture, Vector2 normalizedCoord, float scale, Color color, MyGuiDrawAlignEnum drawAlign, float rotation, Vector2? originNormalized, bool waitTillLoaded = true)
        {
            VRageRender.MyRenderProxy.DrawSprite(texture, normalizedCoord, Vector2.One, color, drawAlign, rotation, Vector2.UnitX, scale, originNormalized, waitTillLoaded: waitTillLoaded);
        }

        public static void DrawSpriteBatchRotate(string texture, Vector2 normalizedCoord, float scale, Color color, MyGuiDrawAlignEnum drawAlign, float rotation, Vector2 originNormalized, float rotationSpeed, bool waitTillLoaded = true)
        {
            VRageRender.MyRenderProxy.DrawSprite(texture, normalizedCoord, Vector2.One, color, drawAlign, rotation, Vector2.UnitX, scale, originNormalized, rotationSpeed, waitTillLoaded);
        }

        #endregion

        //  Find screen texture that best matches aspect ratio of current GUI screen
        public static string GetBackgroundTextureFilenameByAspectRatio(Vector2 normalizedSize)
        {
            Vector2 screenSize = GetScreenSizeFromNormalizedSize(normalizedSize);
            float screenAspectRatio = screenSize.X / screenSize.Y;
            float minDelta = float.MaxValue;
            string ret = null;
            foreach (MyGuiTextureScreen texture in m_backgroundScreenTextures)
            {
                float delta = Math.Abs(screenAspectRatio - texture.GetAspectRatio());
                if (delta < minDelta)
                {
                    minDelta = delta;
                    ret = texture.GetFilename();
                }
            }
            return ret;
        }

        //  Get size of sprite/texture in normalized coordinate <0..1>
        public static Vector2 GetNormalizedSize(Vector2 size, float scale)
        {
            Vector2 sizeScaled = size * scale * m_safeScreenScale;
            return GetNormalizedSizeFromScreenSize(sizeScaled);
        }

        /// <summary>Convertes normalized size [0,1] to screen size (pixels)</summary>
        /// <param name="useFullClientArea">True uses full client rectangle. False limits to GUI rectangle</param>
        public static Vector2 GetScreenSizeFromNormalizedSize(Vector2 normalizedSize, bool useFullClientArea = false)
        {
            if (useFullClientArea)
                return new Vector2((m_safeFullscreenRectangle.Width + 1) * normalizedSize.X, m_safeFullscreenRectangle.Height * normalizedSize.Y);
            else
                return new Vector2((m_safeGuiRectangle.Width + 1) * normalizedSize.X, m_safeGuiRectangle.Height * normalizedSize.Y);
        }

        /// <summary>Convertes normalized coodrinate [0,1] to screen coordinate (pixels)</summary>
        /// <param name="useFullClientArea">True uses full client rectangle. False limits to GUI rectangle</param>
        public static Vector2 GetScreenCoordinateFromNormalizedCoordinate(Vector2 normalizedCoord, bool useFullClientArea = false)
        {
            if (useFullClientArea)
            {
                return new Vector2(
                    m_safeFullscreenRectangle.Left + m_safeFullscreenRectangle.Width * normalizedCoord.X,
                    m_safeFullscreenRectangle.Top + m_safeFullscreenRectangle.Height * normalizedCoord.Y);
            }
            else
            {
                return new Vector2(
                    m_safeGuiRectangle.Left + m_safeGuiRectangle.Width * normalizedCoord.X,
                    m_safeGuiRectangle.Top + m_safeGuiRectangle.Height * normalizedCoord.Y);
            }
        }

        //  Convertes screen coordinate (pixels) to normalized coodrinate <0..1>
        public static Vector2 GetNormalizedCoordinateFromScreenCoordinate(Vector2 screenCoord)
        {
            return new Vector2(
                (screenCoord.X - (float)m_safeGuiRectangle.Left) / (float)m_safeGuiRectangle.Width,
                (screenCoord.Y - (float)m_safeGuiRectangle.Top) / (float)m_safeGuiRectangle.Height);
        }

        public static Vector2 GetNormalizedMousePosition(Vector2 mousePosition, Vector2 mouseAreaSize)
        {
            Vector2 scaledMousePos;
            scaledMousePos.X = mousePosition.X * (m_fullscreenRectangle.Width / mouseAreaSize.X);
            scaledMousePos.Y = mousePosition.Y * (m_fullscreenRectangle.Height / mouseAreaSize.Y);
            return GetNormalizedCoordinateFromScreenCoordinate(scaledMousePos);
        }

        //  Convertes fullscreen screen coordinate (pixels) to safe-GUI normalized coodrinate <0..1>
        public static Vector2 GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(Vector2 fullScreenCoord)
        {
            return GetNormalizedCoordinateFromScreenCoordinate(
                new Vector2(m_safeFullscreenRectangle.Left + fullScreenCoord.X, m_safeFullscreenRectangle.Top + fullScreenCoord.Y));
        }

        //  Convertes screen size (pixels) to normalized size <0..1>
        public static Vector2 GetNormalizedSizeFromScreenSize(Vector2 screenSize)
        {
            float x = m_safeGuiRectangle.Width != 0 ? screenSize.X / (float)m_safeGuiRectangle.Width : 0;
            float y = m_safeGuiRectangle.Height != 0 ? screenSize.Y / (float)m_safeGuiRectangle.Height : 0;
            return new Vector2(x, y);
        }

        //  This is for HUD, therefore not GUI normalized coordinates
        public static Vector2 GetHudNormalizedCoordFromPixelCoord(Vector2 pixelCoord)
        {
            return new Vector2(
                (pixelCoord.X - m_safeFullscreenRectangle.Left) / (float)m_safeFullscreenRectangle.Width,
                ((pixelCoord.Y - m_safeFullscreenRectangle.Top) / (float)m_safeFullscreenRectangle.Height) * m_hudSize.Y);
        }

        //  This is for HUD, therefore not GUI normalized coordinates
        public static Vector2 GetHudNormalizedSizeFromPixelSize(Vector2 pixelSize)
        {
            return new Vector2(
                pixelSize.X / (float)m_safeFullscreenRectangle.Width,
                (pixelSize.Y / (float)m_safeFullscreenRectangle.Height) * m_hudSize.Y);
        }

        //  This is for HUD, therefore not GUI normalized coordinates
        public static Vector2 GetHudPixelCoordFromNormalizedCoord(Vector2 normalizedCoord)
        {
            return new Vector2(
                normalizedCoord.X * (float)m_safeFullscreenRectangle.Width,
                normalizedCoord.Y * (float)m_safeFullscreenRectangle.Height);
        }

        public static Vector2 GetMinMouseCoord()
        {
            return FullscreenHudEnabled ? m_minMouseCoordFullscreenHud : m_minMouseCoord;
        }

        public static Vector2 GetMaxMouseCoord()
        {
            return FullscreenHudEnabled ? m_maxMouseCoordFullscreenHud : m_maxMouseCoord;
        }

        public static void GetSafeHeightFullScreenPictureSize(Vector2I originalSize, out Rectangle outRect)
        {
            GetSafeHeightPictureSize(originalSize, m_safeFullscreenRectangle, out outRect);
        }

        public static void GetSafeAspectRatioFullScreenPictureSize(Vector2I originalSize, out Rectangle outRect)
        {
            GetSafeAspectRatioPictureSize(originalSize, m_safeFullscreenRectangle, out outRect);
        }

        //  This method scales picture to bounding area according to bounding area's height. We don't care about width, so sometimes if picture has wide
        //  aspect ratio, borders of image may be outisde of the bounding area. This method is used when we want to have picture covering whole screen
        //  and don't care if part of picture is invisible. Also, aspect ration is unchanged too.
        static void GetSafeHeightPictureSize(Vector2I originalSize, Rectangle boundingArea, out Rectangle outRect)
        {
            outRect.Height = boundingArea.Height;
            outRect.Width = (int)(((float)outRect.Height / (float)originalSize.Y) * originalSize.X);
            outRect.X = boundingArea.Left + (boundingArea.Width - outRect.Width) / 2;
            outRect.Y = boundingArea.Top + (boundingArea.Height - outRect.Height) / 2;
        }

        //  Return picture size that is safe for displaying in bounding area and doesn't distort the aspect ratio of original picture or bounding area.
        //  Example: picture of video frame is scaled to size of screen, so it fits it as much as possible, but still maintains aspect ration of
        //  original video frame or screen. So if screen's heigh is not enouch, video frame is scaled down to fit height, but also width is scaled
        //  according to original video frame.
        //  It's used whenever we need to scale picture/texture to some area, usually to screen size.
        //  Also this method calculated left/top coordinates, so it's always centered.
        static void GetSafeAspectRatioPictureSize(Vector2I originalSize, Rectangle boundingArea, out Rectangle outRect)
        {
            outRect.Width = boundingArea.Width;
            outRect.Height = (int)(((float)outRect.Width / (float)originalSize.X) * originalSize.Y);

            if (outRect.Height > boundingArea.Height)
            {
                outRect.Height = boundingArea.Height;
                outRect.Width = (int)(outRect.Height * ((float)originalSize.X / (float)originalSize.Y));
            }

            outRect.X = boundingArea.Left + (boundingArea.Width - outRect.Width) / 2;
            outRect.Y = boundingArea.Top + (boundingArea.Height - outRect.Height) / 2;
        }

        public static Vector2 GetScreenTextRightTopPosition()
        {
            float deltaPixels = 25 * GetSafeScreenScale();
            return GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(GetSafeFullscreenRectangle().Width - deltaPixels, deltaPixels));
        }

        public static Vector2 GetScreenTextRightBottomPosition()
        {
            float deltaPixels = 25 * GetSafeScreenScale();
            Vector2 rightAlignedOrigin = GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(GetSafeFullscreenRectangle().Width - deltaPixels, deltaPixels));
            return GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(GetSafeFullscreenRectangle().Width - deltaPixels, GetSafeFullscreenRectangle().Height - (2 * deltaPixels)));
        }

        public static Vector2 GetScreenTextLeftBottomPosition()
        {
            float deltaPixels = 25 * GetSafeScreenScale();
            return GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixels, GetSafeFullscreenRectangle().Height - (2 * deltaPixels)));
        }

        public static Vector2 GetScreenTextLeftTopPosition()
        {
            float deltaPixels = 25 * GetSafeScreenScale();
            return GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixels, deltaPixels));
        }




        public static void UpdateScreenSize(Vector2I screenSize, Vector2I screenSizeHalf, bool isTriple)
        {
            int safeGuiSizeY = screenSize.Y;
            int safeGuiSizeX = (int)(safeGuiSizeY * MyGuiConstants.SAFE_ASPECT_RATIO);     //  This will mantain same aspect ratio for GUI elements

            int safeFullscreenSizeX = screenSize.X;
            int safeFullscreenSizeY = screenSize.Y;

            m_fullscreenRectangle = new Rectangle(0, 0, screenSize.X, screenSize.Y);

            //  Triple head is drawn on three monitors, so we will draw GUI only on the middle one

            if (isTriple == true)
            {
                const int TRIPLE_SUB_SCREENS_COUNT = 3;
                //safeGuiSizeX = safeGuiSizeX / TRIPLE_SUB_SCREENS_COUNT;
                safeFullscreenSizeX = safeFullscreenSizeX / TRIPLE_SUB_SCREENS_COUNT;
            }


            m_safeGuiRectangle = new Rectangle(screenSize.X / 2 - safeGuiSizeX / 2, 0, safeGuiSizeX, safeGuiSizeY);

            //if (MyVideoModeManager.IsTripleHead() == true)
            //m_safeGuiRectangle.X += MySandboxGame.ScreenSize.X / 3;

            m_safeFullscreenRectangle = new Rectangle(screenSize.X / 2 - safeFullscreenSizeX / 2, 0, safeFullscreenSizeX, safeFullscreenSizeY);

            //  This will help as maintain scale/ratio of images, texts during in different resolution
            m_safeScreenScale = (float)safeGuiSizeY / MyGuiConstants.REFERENCE_SCREEN_HEIGHT;

            //  Min and max mouse coords (in normalized units). Must be calculated from fullscreen, no GUI rectangle.
            m_minMouseCoord = GetNormalizedCoordinateFromScreenCoordinate(new Vector2(m_safeFullscreenRectangle.Left, m_safeFullscreenRectangle.Top));
            m_maxMouseCoord = GetNormalizedCoordinateFromScreenCoordinate(new Vector2(m_safeFullscreenRectangle.Left + m_safeFullscreenRectangle.Width, m_safeFullscreenRectangle.Top + m_safeFullscreenRectangle.Height));
            m_minMouseCoordFullscreenHud = GetNormalizedCoordinateFromScreenCoordinate(new Vector2(m_fullscreenRectangle.Left, m_fullscreenRectangle.Top));
            m_maxMouseCoordFullscreenHud = GetNormalizedCoordinateFromScreenCoordinate(new Vector2(m_fullscreenRectangle.Left + m_fullscreenRectangle.Width, m_fullscreenRectangle.Top + m_fullscreenRectangle.Height));

#if XB1
            //XB1_TODO: error: 'VRage.Input.IMyInput' does not contain a definition for 'SetMouseLimits'
            //XB1_TODO: MyInput.Static.SetMouseLimits( new Vector2(m_safeFullscreenRectangle.Left, m_safeFullscreenRectangle.Top),
            //XB1_TODO:    new Vector2(m_safeFullscreenRectangle.Left + m_safeFullscreenRectangle.Width, m_safeFullscreenRectangle.Top + m_safeFullscreenRectangle.Height) );
#endif

            //  Normalized coordinates where width is always 1.0 and height something like 0.8
            //  Don't confuse with GUI normalized coordinates. They are different.
            //  HUD - get normalized screen size -> width is always 1.0, but height depends on aspect ratio, so usualy it is 0.8 or something.
            m_hudSize = CalculateHudSize();
            m_hudSizeHalf = m_hudSize / 2.0f;
        }

#if !XB1
        public static void SetHWCursorBitmap(System.Drawing.Bitmap b)
        {

            System.Windows.Forms.Form f = System.Windows.Forms.Control.FromHandle(MyInput.Static.WindowHandle) as System.Windows.Forms.Form;
            if (f != null)
            {
                // TODO: OP! Make this in thread safe way and optimized
                f.Invoke(new Action(() => f.Cursor = new System.Windows.Forms.Cursor(b.GetHicon())));
            }
        }
#endif



        //  Update all screens
        public static void Update(int totalTimeInMS)
        {
            TotalTimeInMilliseconds = totalTimeInMS;
        }


   
        public static Vector2 GetHudSize()
        {
            return m_hudSize;
        }

        public static Vector2 GetHudSizeHalf()
        {
            return m_hudSizeHalf;
        }

        public static string GetMouseCursorTexture()
        {
            return m_mouseCursorTexture;
        }


        public static void SetMouseCursorTexture(string texture)
        {
            m_mouseCursorTexture = texture;
        }

        public static void DrawBorders(Vector2 topLeftPosition, Vector2 size, Color color, int borderSize)
        {
            Vector2 sizeInPixels = GetScreenSizeFromNormalizedSize(size);
            sizeInPixels = new Vector2((int)sizeInPixels.X, (int)sizeInPixels.Y);
            Vector2 leftTopInPixels = GetScreenCoordinateFromNormalizedCoordinate(topLeftPosition);
            leftTopInPixels = new Vector2((int)leftTopInPixels.X, (int)leftTopInPixels.Y);
            Vector2 rightTopInPixels = leftTopInPixels + new Vector2(sizeInPixels.X, 0);
            Vector2 leftBottomInPixels = leftTopInPixels + new Vector2(0, sizeInPixels.Y);
            // top
            DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE, (int)leftTopInPixels.X, (int)leftTopInPixels.Y, (int)sizeInPixels.X, borderSize, color);
            // right
            DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE, (int)rightTopInPixels.X - borderSize, (int)rightTopInPixels.Y + borderSize, borderSize, (int)sizeInPixels.Y - borderSize * 2, color);
            // bottom
            DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE, (int)leftBottomInPixels.X, (int)leftBottomInPixels.Y - borderSize, (int)sizeInPixels.X, borderSize, color);
            //left
            DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE, (int)leftTopInPixels.X, (int)leftTopInPixels.Y + borderSize, borderSize, (int)sizeInPixels.Y - borderSize * 2, color);
        }

        public static float LanguageTextScale
        {
            get;
            set;
        }

        public static SpriteScissorToken UsingScissorRectangle(ref RectangleF normalizedRectangle)
        {
            Vector2 screenSize     = GetScreenSizeFromNormalizedSize(normalizedRectangle.Size);
            Vector2 screenPosition = GetScreenCoordinateFromNormalizedCoordinate(normalizedRectangle.Position);
            var screenRectangle = new Rectangle((int)Math.Round(screenPosition.X, MidpointRounding.AwayFromZero),
                                                (int)Math.Round(screenPosition.Y, MidpointRounding.AwayFromZero),
                                                (int)Math.Round(screenSize.X, MidpointRounding.AwayFromZero),
                                                (int)Math.Round(screenSize.Y, MidpointRounding.AwayFromZero));
            VRageRender.MyRenderProxy.SpriteScissorPush(screenRectangle);
            return new SpriteScissorToken();
        }

        public struct SpriteScissorToken : IDisposable
        {
            public void Dispose()
            {
                VRageRender.MyRenderProxy.SpriteScissorPop();
            }
        }

    }
}
