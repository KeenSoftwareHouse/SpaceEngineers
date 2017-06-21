using System;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace Sandbox.Gui
{
    public class MyGuiControlProgressBarHud : MyGuiControlBase
    {
        private MyGuiProgressCompositeTexture m_texture;

        public MyGuiControlProgressBarHud()
        {
            m_texture = new MyGuiProgressCompositeTexture
            {
                LeftTop = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.LeftTop,
                LeftCenter = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.LeftCenter,
                LeftBottom = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.LeftBottom,
                CenterTop = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.CenterTop,
                Center = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.Center,
                CenterBottom = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.CenterBottom,
                RightTop = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.RightTop,
                RightCenter = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.RightCenter,
                RightBottom = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.RightBottom,
            };
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            m_texture.Position = new Vector2(0.1f,0.5f);
            m_texture.Size = new Vector2(0.1f, 0.08f);

            m_texture.Draw(0.8f, Color.White);
        }
    }

    public class MyGuiProgressCompositeTexture
    {
        struct TextureData
        {
            public Vector2 Position;
            public Vector2 Size;
            public MyGuiSizedTexture Texture;

            public override string ToString()
            {
                return "Position: " + Position + " Size: " + Size;
            }
        }

        private readonly TextureData[,] m_textures = new TextureData[3, 3];
        private bool m_positionsAndSizesDirty = true;
        private Vector2 m_position = Vector2.Zero;
        private Vector2 m_size = Vector2.Zero;

        public MyGuiSizedTexture LeftTop
        {
            get { return m_textures[0, 0].Texture; }
            set 
            { 
                m_textures[0, 0].Texture = value; 
                m_textures[0, 0].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true; 
            }
        }
        public MyGuiSizedTexture LeftCenter
        {
            get { return m_textures[1, 0].Texture; }
            set
            {
                m_textures[1, 0].Texture = value; 
                m_textures[1, 0].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true;
            }
        }
        public MyGuiSizedTexture LeftBottom
        {
            get { return m_textures[2, 0].Texture; }
            set
            {
                m_textures[2, 0].Texture = value; 
                m_textures[2, 0].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true;
            }
        }
        public MyGuiSizedTexture CenterTop
        {
            get { return m_textures[0, 1].Texture; }
            set
            {
                m_textures[0, 1].Texture = value; 
                m_textures[0, 1].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true;
            }
        }
        public MyGuiSizedTexture Center
        {
            get { return m_textures[1, 1].Texture; }
            set
            {
                m_textures[1, 1].Texture = value; 
                m_textures[1, 1].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true;
            }
        }
        public MyGuiSizedTexture CenterBottom
        {
            get { return m_textures[2, 1].Texture; }
            set
            {
                m_textures[2, 1].Texture = value; 
                m_textures[2, 1].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true;
            }
        }
        public MyGuiSizedTexture RightTop
        {
            get { return m_textures[0, 2].Texture; }
            set
            {
                m_textures[0, 2].Texture = value; 
                m_textures[0, 2].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true;
            }
        }
        public MyGuiSizedTexture RightCenter
        {
            get { return m_textures[1, 2].Texture; }
            set { m_textures[1, 2].Texture = value; 
                m_textures[1, 2].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true; }
        }
        public MyGuiSizedTexture RightBottom
        {
            get { return m_textures[2, 2].Texture; }
            set
            {
                m_textures[2, 2].Texture = value; 
                m_textures[2, 2].Size = MyGuiManager.GetScreenSizeFromNormalizedSize(value.SizeGui); 
                m_positionsAndSizesDirty = true;
            }
        }

        public Vector2 Position
        {
            get { return m_position; }
            set { m_position = value; m_positionsAndSizesDirty = true; }
        }

        public Vector2 Size
        {
            get { return m_size; }
            set { m_size = value; m_positionsAndSizesDirty = true; }
        }

        public void Draw(float progression, Color colorMask)
        {
            if(m_positionsAndSizesDirty) RefreshPositionsAndSizes();

            var targetRect = new Rectangle();
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var textureData = m_textures[i, j];

                    if (i == 1 && j == 1) 
                    {
                        // Center handling #different
                        var middleBarDefaultSize = Math.Round(m_textures[1, 1].Size.X);
                        var currentProgressionLength = middleBarDefaultSize;
                        var fullProgressionLength = Math.Ceiling(m_textures[0, 1].Size.X);
                        var progressionLength = Math.Ceiling(fullProgressionLength * progression);

                        SetTarget(ref targetRect, textureData);
                        for (; currentProgressionLength < progressionLength; currentProgressionLength += middleBarDefaultSize)
                        {
                            MyGuiManager.DrawSprite(textureData.Texture.Texture, targetRect, colorMask);

                            targetRect.X = (int)(textureData.Position.X + currentProgressionLength);
                        }

                        var lastBarSize = middleBarDefaultSize - (currentProgressionLength - progressionLength);
                        targetRect.Width = (int)lastBarSize;
                        MyGuiManager.DrawSprite(textureData.Texture.Texture, targetRect, colorMask);
                    }
                    else
                    {
                        // All other parts can be rendered as usual
                        SetTarget(ref targetRect, textureData);

                        MyGuiManager.DrawSprite(textureData.Texture.Texture, targetRect, colorMask);
                    }
                }
            }
        }

        private void SetTarget(ref Rectangle rect, TextureData texData)
        {
            rect.X = (int)Math.Ceiling(texData.Position.X);
            rect.Y = (int)Math.Ceiling(texData.Position.Y);
            rect.Width = (int)Math.Ceiling(texData.Size.X);
            rect.Height = (int)Math.Ceiling(texData.Size.Y);
        }

        private void RefreshPositionsAndSizes()
        {
            m_textures[0, 0].Position = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(m_position);

            // Reduce by cornersize
            var sizeToScaleTo = MyGuiManager.GetScreenSizeFromNormalizedSize(m_size) - m_textures[0, 0].Size - m_textures[2, 2].Size;
            // Adjust the size of sides
            m_textures[1, 0].Size.Y = sizeToScaleTo.Y;
            m_textures[1, 2].Size.Y = sizeToScaleTo.Y;
            m_textures[0, 1].Size.X = sizeToScaleTo.X;
            m_textures[2, 1].Size.X = sizeToScaleTo.X;
            
            // Change size of the center to reflect the height change only
            m_textures[1, 1].Size.Y = sizeToScaleTo.Y;

            // Insert fake size for center to ease the position calculation
            var storedCenterSize = m_textures[1, 1].Size;
            m_textures[1, 1].Size = sizeToScaleTo;

            // Adjust the textures positions
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    if(i == 0 && j == 0) continue;

                    var x = j > 0 ? m_textures[i, j - 1].Position.X + m_textures[i, j - 1].Size.X : m_textures[0, 0].Position.X;
                    var y = i > 0 ? m_textures[i - 1, j].Position.Y + m_textures[i - 1, j].Size.Y : m_textures[0, 0].Position.Y;

                    m_textures[i, j].Position = new Vector2(x, y);
                }
            }

            // Restore the center size
            m_textures[1, 1].Size = storedCenterSize;
        }
    }
}
