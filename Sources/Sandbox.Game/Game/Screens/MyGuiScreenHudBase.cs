using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage;
using VRage;
using VRage.Generics;
using VRage.Utils;
using VRageMath;
using Color = VRageMath.Color;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;
using Vector2 = VRageMath.Vector2;

//  This class draws crosshair, angles and all HUD stuff.
//
//  Coordinates are ALMOST in <0..1> range, but think about this:
//  Screen size -> width is always 1.0, but height depends on aspect ratio, so usualy it is 0.8 or something.
//
//  Every line is composed of three rectangles. Middle rectangle is for the main line and two other represents circles at the end.
//  IMPORTANT: Above line apply only if DRAW_LINE_ENDS enables it. Reason why I don't use it is because I had problems making that
//  end texture correct and without ends it's even faster (less rectangles to draw)
namespace Sandbox.Game.Gui
{
    //  This enums must have same name as source texture files used to create texture atlas (only ".tga" files are supported)
    //  IMPORTANT: If you change order or names in this enum, update it also in MyEnumsToStrings
    public enum MyHudTexturesEnum : byte
    {
        corner,
        crosshair,
        HudOre,
        Target_enemy,
        Target_friend,
        Target_neutral,
        Target_me,
        TargetTurret,
        DirectionIndicator,
        gravity_point_red,
        gravity_point_white,
        gravity_arrow,
    }

    public class MyGuiScreenHudBase : MyGuiScreenBase
    {
        protected string m_atlas;
        public string TextureAtlas { get { return m_atlas; } }

        protected MyAtlasTextureCoordinate[] m_atlasCoords;
        protected float m_textScale;
        protected StringBuilder m_hudIndicatorText = new StringBuilder();
        protected StringBuilder m_helperSB = new StringBuilder();

        protected MyObjectsPoolSimple<MyHudText> m_texts;

        public MyGuiScreenHudBase()
            : base(Vector2.Zero, null, null)
        {
            CanBeHidden = true;
            CanHideOthers = false;
            CanHaveFocus = false;
            m_drawEvenWithoutFocus = true;
            m_closeOnEsc = false;
            m_texts = new MyObjectsPoolSimple<MyHudText>(MyHudConstants.MAX_HUD_TEXTS_COUNT);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenHudBase";
        }

        public override void LoadContent()
        {
            LoadTextureAtlas(out m_atlas, out m_atlasCoords);
            base.LoadContent();
        }

        public static void LoadTextureAtlas(out string atlasFile, out MyAtlasTextureCoordinate[] atlasCoords)
        {
            MyTextureAtlasUtils.LoadTextureAtlas(Sandbox.Engine.Utils.MyEnumsToStrings.HudTextures, "Textures\\HUD\\", "Textures\\HUD\\HudAtlas.tai", out atlasFile, out atlasCoords);
        }

        public override void UnloadContent()
        {
            m_atlas = null;
            m_atlasCoords = null;
            base.UnloadContent();
        }

        public static Vector2 ConvertHudToNormalizedGuiPosition(ref Vector2 hudPos)
        {
            var safeFullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            var safeFullscreenSize = new Vector2(safeFullscreenRectangle.Width, safeFullscreenRectangle.Height);
            var safeFullscreenOffset = new Vector2(safeFullscreenRectangle.X, safeFullscreenRectangle.Y);

            var safeGuiRectangle = MyGuiManager.GetSafeGuiRectangle();
            var safeGuiSize = new Vector2(safeGuiRectangle.Width, safeGuiRectangle.Height);
            var safeGuiOffset = new Vector2(safeGuiRectangle.X, safeGuiRectangle.Y);

            return ((hudPos * safeFullscreenSize + safeFullscreenOffset) - safeGuiOffset) / safeGuiSize;
        }

        protected static Vector2 ConvertNormalizedGuiToHud(ref Vector2 normGuiPos)
        {
            var safeFullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            var safeFullscreenSize = new Vector2(safeFullscreenRectangle.Width, safeFullscreenRectangle.Height);
            var safeFullscreenOffset = new Vector2(safeFullscreenRectangle.X, safeFullscreenRectangle.Y);

            var safeGuiRectangle = MyGuiManager.GetSafeGuiRectangle();
            var safeGuiSize = new Vector2(safeGuiRectangle.Width, safeGuiRectangle.Height);
            var safeGuiOffset = new Vector2(safeGuiRectangle.X, safeGuiRectangle.Y);

            return ((normGuiPos * safeGuiSize + safeGuiOffset) - safeFullscreenOffset) / safeFullscreenSize;
        }

        public static void DrawCrosshair(string atlas, MyAtlasTextureCoordinate textureCoord, MyHudCrosshair crosshair)
        {
            Vector2 rightVector = new Vector2(crosshair.UpVector.Y, crosshair.UpVector.X);

            float hudSizeX = MyGuiManager.GetSafeFullscreenRectangle().Width / MyGuiManager.GetHudSize().X;
            float hudSizeY = MyGuiManager.GetSafeFullscreenRectangle().Height / MyGuiManager.GetHudSize().Y;
            var pos = crosshair.Position;
            if (MyVideoSettingsManager.IsTripleHead())
                pos.X += 1.0f;

            VRageRender.MyRenderProxy.DrawSpriteAtlas(
                atlas,
                pos,
                textureCoord.Offset,
                textureCoord.Size,
                rightVector,
                new Vector2(hudSizeX, hudSizeY),
                crosshair.Color,
                crosshair.HalfSize);
        }

        public static void DrawSelectedObjectHighlight(string atlasTexture, MyAtlasTextureCoordinate textureCoord, MyHudSelectedObject selection)
        {
            var rect = MyGuiManager.GetSafeFullscreenRectangle();

            Vector2 hudSize = new Vector2(rect.Width, rect.Height);

            var worldViewProj = selection.InteractiveObject.ActivationMatrix * MySector.MainCamera.ViewMatrix * (MatrixD)MySector.MainCamera.ProjectionMatrix;
            BoundingBoxD screenSpaceAabb = new BoundingBoxD(-Vector3D.One / 2, Vector3D.One / 2).Transform(worldViewProj);
            var min = new Vector2((float)screenSpaceAabb.Min.X, (float)screenSpaceAabb.Min.Y);
            var max = new Vector2((float)screenSpaceAabb.Max.X, (float)screenSpaceAabb.Max.Y);

            var minToMax = min - max;

            min = min * 0.5f + 0.5f * Vector2.One;
            max = max * 0.5f + 0.5f * Vector2.One;

            min.Y = 1 - min.Y;
            max.Y = 1 - max.Y;

            float textureScale = (float)Math.Pow(Math.Abs(minToMax.X), 0.35f) * 2.5f;

            if (selection.InteractiveObject.ShowOverlay)
            {
                BoundingBoxD one = new BoundingBoxD(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f));
                Color color = Color.Gold;
                color *= 0.4f;
                var m = selection.InteractiveObject.ActivationMatrix;
                var localToWorld = MatrixD.Invert(selection.InteractiveObject.WorldMatrix);
                //MySimpleObjectDraw.DrawTransparentBox(ref m, ref one, ref color, MySimpleObjectRasterizer.Solid, 0, 0.05f, "Square", null, true);

                MySimpleObjectDraw.DrawAttachedTransparentBox(ref m, ref one, ref color, selection.InteractiveObject.RenderObjectID,
                    ref localToWorld, MySimpleObjectRasterizer.Solid, 0, 0.05f, "Square", null, true);
            }

            DrawSelectionCorner(atlasTexture, selection, textureCoord, hudSize, min, -Vector2.UnitY, textureScale);
            DrawSelectionCorner(atlasTexture, selection, textureCoord, hudSize, new Vector2(min.X, max.Y), Vector2.UnitX, textureScale);
            DrawSelectionCorner(atlasTexture, selection, textureCoord, hudSize, new Vector2(max.X, min.Y), -Vector2.UnitX, textureScale);
            DrawSelectionCorner(atlasTexture, selection, textureCoord, hudSize, max, Vector2.UnitY, textureScale);
        }

        public static void DrawSelectionCorner(string atlasTexture, MyHudSelectedObject selection, MyAtlasTextureCoordinate textureCoord, Vector2 scale, Vector2 pos, Vector2 rightVector, float textureScale)
        {
            if (MyVideoSettingsManager.IsTripleHead())
                pos.X = pos.X * 3;

            VRageRender.MyRenderProxy.DrawSpriteAtlas(
                atlasTexture,
                pos,
                textureCoord.Offset,
                textureCoord.Size,
                rightVector,
                scale,
                selection.Color,
                selection.HalfSize / MyGuiManager.GetHudSize() * textureScale);
        }

        /// <summary>
        /// Draws fog (eg. background for notifications) at specified position in normalized GUI coordinates.
        /// </summary>
        public static void DrawFog(ref Vector2 centerPosition, ref Vector2 textSize)
        {
            Color color = new Color(0, 0, 0, (byte)(255 * 0.85f));
            Vector2 fogFadeSize = textSize * new Vector2(1.4f, 3.0f);

            MyGuiManager.DrawSpriteBatch(MyGuiConstants.FOG_SMALL, centerPosition, fogFadeSize, color,
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyVideoSettingsManager.IsTripleHead());
        }

        public MyHudText AllocateText()
        {
            return m_texts.Allocate();
        }

        public void DrawTexts()
        {
            if (m_texts.GetAllocatedCount() <= 0) return;

            for (int i = 0; i < m_texts.GetAllocatedCount(); i++)
            {
                MyHudText text = m_texts.GetAllocatedItem(i);

                var font = text.Font;
                text.Position /= MyGuiManager.GetHudSize();
                var normalizedCoord = ConvertHudToNormalizedGuiPosition(ref text.Position);

                Vector2 textSize = MyGuiManager.MeasureString(font, text.GetStringBuilder(), MyGuiSandbox.GetDefaultTextScaleWithLanguage());
                textSize.X *= 0.9f;
                textSize.Y *= 0.7f;
                MyGuiScreenHudBase.DrawFog(ref normalizedCoord, ref textSize);

                MyGuiManager.DrawString(font, text.GetStringBuilder(), normalizedCoord, text.Scale, colorMask: text.Color, drawAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }

            m_texts.ClearAllAllocated();
        }

        public MyAtlasTextureCoordinate GetTextureCoord(MyHudTexturesEnum texture)
        {
            return m_atlasCoords[(int)texture];
        }
    }
}