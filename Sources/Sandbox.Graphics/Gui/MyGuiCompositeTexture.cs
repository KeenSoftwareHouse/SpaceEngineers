using ObjectBuilders.Definitions.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    /// <summary>
    /// Composite texture is built from several parts. Currently there is Top and Bottom
    /// which are rendered at their original aspect ratio and size, and Center which
    /// fills up space between Top and Bottom.
    /// </summary>
    public class MyGuiCompositeTexture
    {
        private MyGuiSizedTexture m_leftTop;
        private MyGuiSizedTexture m_leftCenter;
        private MyGuiSizedTexture m_leftBottom;
        private MyGuiSizedTexture m_centerTop;
        private MyGuiSizedTexture m_center;
        private MyGuiSizedTexture m_centerBottom;
        private MyGuiSizedTexture m_rightTop;
        private MyGuiSizedTexture m_rightCenter;
        private MyGuiSizedTexture m_rightBottom;

        private bool m_sizeLimitsDirty;
        private Vector2 m_minSizeGui = Vector2.Zero;
        private Vector2 m_maxSizeGui = Vector2.One * float.PositiveInfinity;

        public MyGuiSizedTexture LeftTop
        {
            get { return m_leftTop; }
            set { m_leftTop = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture LeftCenter
        {
            get { return m_leftCenter; }
            set { m_leftCenter = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture LeftBottom
        {
            get { return m_leftBottom; }
            set { m_leftBottom = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture CenterTop
        {
            get { return m_centerTop; }
            set { m_centerTop = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture Center
        {
            get { return m_center; }
            set { m_center = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture CenterBottom
        {
            get { return m_centerBottom; }
            set { m_centerBottom = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture RightTop
        {
            get { return m_rightTop; }
            set { m_rightTop = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture RightCenter
        {
            get { return m_rightCenter; }
            set { m_rightCenter = value; m_sizeLimitsDirty = true; }
        }
        public MyGuiSizedTexture RightBottom
        {
            get { return m_rightBottom; }
            set { m_rightBottom = value; m_sizeLimitsDirty = true; }
        }

        public Vector2 MinSizeGui
        {
            get
            {
                if (m_sizeLimitsDirty)
                    RefreshSizeLimits();
                return m_minSizeGui;
            }
        }
        public Vector2 MaxSizeGui
        {
            get
            {
                if (m_sizeLimitsDirty)
                    RefreshSizeLimits();
                return m_maxSizeGui;
            }
        }

        private void RefreshSizeLimits()
        {
            m_minSizeGui.X = Math.Max(m_leftTop.SizeGui.X + m_rightTop.SizeGui.X,
                                   m_leftBottom.SizeGui.X + m_rightBottom.SizeGui.X);
            m_minSizeGui.Y = Math.Max(m_leftTop.SizeGui.Y + m_leftBottom.SizeGui.Y,
                                     m_rightTop.SizeGui.Y + m_rightBottom.SizeGui.Y);
            if (m_center.Texture != null)
            {
                m_maxSizeGui.X = float.PositiveInfinity;
                m_maxSizeGui.Y = float.PositiveInfinity;
            }
            else
            {
                m_maxSizeGui.X = (m_centerTop.Texture != null || m_centerBottom.Texture != null)
                    ? float.PositiveInfinity
                    : m_minSizeGui.X;

                m_maxSizeGui.Y = (m_leftCenter.Texture != null || m_rightCenter.Texture != null)
                    ? float.PositiveInfinity
                    : m_minSizeGui.Y;

                if (m_leftTop.Texture == null    && m_centerTop.Texture == null    && m_rightTop.Texture == null &&
                    m_leftCenter.Texture == null && m_center.Texture == null       && m_rightCenter.Texture == null &&
                    m_leftBottom.Texture == null && m_centerBottom.Texture == null && m_rightBottom.Texture == null)
                    m_maxSizeGui = Vector2.PositiveInfinity;
            }
            m_sizeLimitsDirty = false;
        }

        public MyGuiCompositeTexture(string centerTexture = null)
        {
            Center = new MyGuiSizedTexture() { Texture = centerTexture };
        }

        /// <summary>
        /// Draw the composite texture at specified position with given height (width is implicit from size of each part).
        /// </summary>
        /// <param name="positionTopLeft">Position of the top left corner of the composite texture.</param>
        /// <param name="innerHeight">Height of expandable area within composite texture (real height will include top and bottom as well).</param>
        public void Draw(Vector2 positionTopLeft, float innerHeight, Color colorMask)
        {
            Vector2 screenSize;
            Rectangle target;

            positionTopLeft = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(positionTopLeft);
            target.X        = (int)positionTopLeft.X;
            target.Y        = (int)positionTopLeft.Y;
            target.Width    = 0;
            target.Height   = 0;

            if (!string.IsNullOrEmpty(m_leftTop.Texture))
            {
                screenSize    = MyGuiManager.GetScreenSizeFromNormalizedSize(m_leftTop.SizeGui);
                target.Width  = (int)screenSize.X;
                target.Height = (int)screenSize.Y;
                MyGuiManager.DrawSprite(m_leftTop.Texture, target, colorMask);
            }

            target.Y += target.Height;
            if (!string.IsNullOrEmpty(m_leftCenter.Texture))
            {
                screenSize    = MyGuiManager.GetScreenSizeFromNormalizedSize(new Vector2(m_leftCenter.SizeGui.X, innerHeight));
                target.Width  = (int)screenSize.X;
                target.Height = (int)screenSize.Y;
                MyGuiManager.DrawSprite(m_leftCenter.Texture, target, colorMask);
            }

            target.Y += target.Height;
            if (!string.IsNullOrEmpty(m_leftBottom.Texture))
            {
                screenSize    = MyGuiManager.GetScreenSizeFromNormalizedSize(m_leftBottom.SizeGui);
                target.Width  = (int)screenSize.X;
                target.Height = (int)screenSize.Y;
                MyGuiManager.DrawSprite(m_leftBottom.Texture, target, colorMask);
            }
        }

        public void Draw(Vector2 positionLeftTop, Vector2 size, Color colorMask, float textureScale = 1f)
        {
            Rectangle target;
            size = Vector2.Clamp(size, MinSizeGui * textureScale, MaxSizeGui * textureScale);

            // L-left, R-right, T-top, B-bottom, C-center.
            Vector2I screenPosLT, screenPosLB, screenPosRT, screenPosRB;
            Vector2I screenSize, screenSizeLT, screenSizeCT, screenSizeRT,
                                 screenSizeLC,               screenSizeRC,
                                 screenSizeLB, screenSizeCB, screenSizeRB;

            screenSizeLT = screenSizeCT = screenSizeRT = Vector2I.Zero;
            screenSizeLC =                screenSizeRC = Vector2I.Zero;
            screenSizeLB = screenSizeCB = screenSizeRB = Vector2I.Zero;

            screenSize  = new Vector2I(MyGuiManager.GetScreenSizeFromNormalizedSize(size));
            screenPosLT = new Vector2I(MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(positionLeftTop));
            screenPosRB = screenPosLT + screenSize;
            screenPosLB = new Vector2I(screenPosLT.X, screenPosRB.Y);
            screenPosRT = new Vector2I(screenPosRB.X, screenPosLT.Y);

            // Render corners.
            if (!string.IsNullOrEmpty(m_leftTop.Texture))
            {
                screenSizeLT = (Vector2I)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_leftTop.SizeGui) * textureScale);
                SetTargetRectangle(out target, ref screenPosLT, ref screenSizeLT, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                MyGuiManager.DrawSprite(m_leftTop.Texture, target, colorMask);
            }

            if (!string.IsNullOrEmpty(m_leftBottom.Texture))
            {
                screenSizeLB = (Vector2I)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_leftBottom.SizeGui) * textureScale);
                SetTargetRectangle(out target, ref screenPosLB, ref screenSizeLB, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
                MyGuiManager.DrawSprite(m_leftBottom.Texture, target, colorMask);
            }

            if (!string.IsNullOrEmpty(m_rightTop.Texture))
            {
                screenSizeRT = (Vector2I)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_rightTop.SizeGui) * textureScale);
                SetTargetRectangle(out target, ref screenPosRT, ref screenSizeRT, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                MyGuiManager.DrawSprite(m_rightTop.Texture, target, colorMask);
            }

            if (!string.IsNullOrEmpty(m_rightBottom.Texture))
            {
                screenSizeRB = (Vector2I)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_rightBottom.SizeGui) * textureScale);
                SetTargetRectangle(out target, ref screenPosRB, ref screenSizeRB, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                MyGuiManager.DrawSprite(m_rightBottom.Texture, target, colorMask);
            }

            // Render expandable sides to fill space between corners.
            if (!string.IsNullOrEmpty(m_centerTop.Texture))
            {
                screenSizeCT.X = (int)screenSize.X - ((int)screenSizeLT.X + (int)screenSizeRT.X);
                screenSizeCT.Y = (int)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_centerTop.SizeGui).Y * textureScale);
                var pos        = screenPosLT + new Vector2I(screenSizeLT.X, 0);
                SetTargetRectangle(out target, ref pos, ref screenSizeCT, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                MyGuiManager.DrawSprite(m_centerTop.Texture, target, colorMask);
            }

            if (!string.IsNullOrEmpty(m_centerBottom.Texture))
            {
                screenSizeCB.X = (int)screenSize.X - ((int)screenSizeLB.X + (int)screenSizeRB.X);
                screenSizeCB.Y = (int)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_centerBottom.SizeGui).Y * textureScale);
                var pos = screenPosLB + new Vector2I(screenSizeLB.X, 0);
                SetTargetRectangle(out target, ref pos, ref screenSizeCB, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
                MyGuiManager.DrawSprite(m_centerBottom.Texture, target, colorMask);
            }

            if (!string.IsNullOrEmpty(m_leftCenter.Texture))
            {
                screenSizeLC.X = (int)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_leftCenter.SizeGui).X * textureScale);
                screenSizeLC.Y = (int)screenSize.Y - ((int)screenSizeLT.Y + (int)screenSizeLB.Y);
                var pos = screenPosLT + new Vector2I(0, screenSizeLT.Y);
                SetTargetRectangle(out target, ref pos, ref screenSizeLC, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                MyGuiManager.DrawSprite(m_leftCenter.Texture, target, colorMask);
            }

            if (!string.IsNullOrEmpty(m_rightCenter.Texture))
            {
                screenSizeRC.X = (int)(MyGuiManager.GetScreenSizeFromNormalizedSize(m_rightCenter.SizeGui).X * textureScale);
                screenSizeRC.Y = (int)screenSize.Y - ((int)screenSizeRT.Y + (int)screenSizeRB.Y);
                var pos = screenPosRT + new Vector2I(0, screenSizeRT.Y);
                SetTargetRectangle(out target, ref pos, ref screenSizeRC, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                MyGuiManager.DrawSprite(m_rightCenter.Texture, target, colorMask);
            }

            // Render expandable center to fill everything in between.
            if (!string.IsNullOrEmpty(m_center.Texture))
            {
                int leftWidth    = MathHelper.Max(screenSizeLT.X, screenSizeLC.X, screenSizeLB.X);
                int rightWidth   = MathHelper.Max(screenSizeRT.X, screenSizeRC.X, screenSizeRB.X);
                int topHeight    = MathHelper.Max(screenSizeLT.Y, screenSizeCT.Y, screenSizeRT.Y);
                int bottomHeight = MathHelper.Max(screenSizeLB.Y, screenSizeCB.Y, screenSizeRB.Y);

                var screenSizeC = screenSize - new Vector2I(leftWidth + rightWidth, topHeight + bottomHeight);
                var pos = screenPosLT + new Vector2I(leftWidth, topHeight);
                SetTargetRectangle(out target, ref pos, ref screenSizeC, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                MyGuiManager.DrawSprite(m_center.Texture, target, colorMask);
            }
        }

        private static void SetTargetRectangle(out Rectangle target, ref Vector2I screenPos, ref Vector2I screenSize, MyGuiDrawAlignEnum posAlign)
        {
            var posTL = MyUtils.GetCoordTopLeftFromAligned(screenPos, screenSize, posAlign);
            target.X      = (int)posTL.X;
            target.Y      = (int)posTL.Y;
            target.Width  = (int)screenSize.X;
            target.Height = (int)screenSize.Y;
        }

        public static implicit operator MyGuiCompositeTexture(SerializableCompositeTexture texture)
        {
            var composite = new MyGuiCompositeTexture(texture.Center);

            float centerWidth = texture.Size.X - texture.BorderSizes.Left - texture.BorderSizes.Right;
            float centerHeight = texture.Size.Y - texture.BorderSizes.Top - texture.BorderSizes.Bottom;

            var lt = new MyGuiSizedTexture() { SizePx = new Vector2(texture.BorderSizes.Left,  texture.BorderSizes.Top),    Texture = texture.LeftTop };
            var lc = new MyGuiSizedTexture() { SizePx = new Vector2(texture.BorderSizes.Left,  centerHeight),               Texture = texture.LeftCenter };
            var lb = new MyGuiSizedTexture() { SizePx = new Vector2(texture.BorderSizes.Left,  texture.BorderSizes.Bottom), Texture = texture.LeftBottom };
            var ct = new MyGuiSizedTexture() { SizePx = new Vector2(centerWidth,               texture.BorderSizes.Top),    Texture = texture.CenterTop };
            var cc = new MyGuiSizedTexture() { SizePx = new Vector2(centerWidth,               centerHeight),               Texture = texture.Center };
            var cb = new MyGuiSizedTexture() { SizePx = new Vector2(centerWidth,               texture.BorderSizes.Bottom), Texture = texture.CenterBottom };
            var rt = new MyGuiSizedTexture() { SizePx = new Vector2(texture.BorderSizes.Right, texture.BorderSizes.Top),    Texture = texture.RightTop };
            var rc = new MyGuiSizedTexture() { SizePx = new Vector2(texture.BorderSizes.Right, centerHeight),               Texture = texture.RightCenter };
            var rb = new MyGuiSizedTexture() { SizePx = new Vector2(texture.BorderSizes.Right, texture.BorderSizes.Bottom), Texture = texture.RightBottom };

            if (texture.LeftTop != null) composite.LeftTop = lt;
            if (texture.LeftCenter != null) composite.LeftCenter = lc;
            if (texture.LeftBottom != null) composite.LeftBottom = lb;
            if (texture.CenterTop != null) composite.CenterTop = ct;
            if (texture.Center != null) composite.Center = cc;
            if (texture.CenterBottom != null) composite.CenterBottom = cb;
            if (texture.RightTop != null) composite.RightTop = rt;
            if (texture.RightCenter != null) composite.RightCenter = rc;
            if (texture.RightBottom != null) composite.RightBottom = rb;

            return composite;
        }

        public static implicit operator SerializableCompositeTexture(MyGuiCompositeTexture texture)
        {
            var serializable = new SerializableCompositeTexture();

            serializable.BorderSizes.Left   = texture.LeftCenter.SizePx.X;
            serializable.BorderSizes.Right  = texture.RightCenter.SizePx.X;
            serializable.BorderSizes.Top    = texture.CenterTop.SizePx.Y;
            serializable.BorderSizes.Bottom = texture.CenterBottom.SizePx.Y;

            float w = serializable.BorderSizes.Left + texture.Center.SizePx.X + serializable.BorderSizes.Right;
            float h = serializable.BorderSizes.Top + texture.Center.SizePx.Y + serializable.BorderSizes.Bottom;

            serializable.Size = new Vector2(w, h);

            serializable.LeftTop = texture.LeftTop.Texture;
            serializable.LeftCenter = texture.LeftCenter.Texture;
            serializable.LeftBottom = texture.LeftBottom.Texture;
            serializable.CenterTop = texture.CenterTop.Texture;
            serializable.Center = texture.Center.Texture;
            serializable.CenterBottom = texture.CenterBottom.Texture;
            serializable.RightTop = texture.RightTop.Texture;
            serializable.RightCenter = texture.RightCenter.Texture;
            serializable.RightBottom = texture.RightBottom.Texture;

            return serializable;
        }
    }
}
