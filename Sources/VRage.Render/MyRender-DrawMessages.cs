using SharpDX.Direct3D9;
using System.Collections.Generic;
using VRageRender.Textures;
using VRageRender.Graphics;
using VRageMath;
using System.Text;
using System;
using VRage;
using VRage.Utils;

namespace VRageRender
{
    internal static partial class MyRender
    {
        static SpriteBatch m_spriteBatch;
        static int m_spriteBatchUsageCount = 0;

        //  Safe coordinates and size of GUI screen. It makes sure we are OK with aspect ratio and
        //  also it makes sure that if very-wide resolution is used (5000x1000 or so), we draw GUI only in the middle screen.
        static Rectangle m_safeGuiRectangle;            //  Rectangle for safe GUI, it independent from screen aspect ration so GUI elements look same on any resolution (especially their width)
        static Rectangle m_safeFullscreenRectangle;     //  Rectangle for safe fullscreen and GUI - use only when you draw fullscreen images and want to stretch it from left to right. 
        static Rectangle m_fullscreenRectangle;         //  Real fullscreen

        static Queue<IMyRenderMessage> m_drawMessages = new Queue<IMyRenderMessage>();
        static Queue<IMyRenderMessage> m_debugDrawMessages = new Queue<IMyRenderMessage>();

        static MyRender.MyRenderSetup m_backup = new MyRender.MyRenderSetup();

        static ushort m_frameCounter = 0;


        internal static void DrawMessageQueue()
        {
            if (m_spriteBatch == null)
                return;



            BeginSpriteBatch();

            /*
            if (m_drawMessages.Count == 0)
            {
                GraphicsDevice.Clear(ClearFlags.All, new SharpDX.ColorBGRA(1.0f, 1, 0, 1), 1, 0);
                DrawText(new Vector2(0, 300), new StringBuilder("No draw input"), Color.White, 1);
            } */


            while (m_drawMessages.Count > 0)
            {
                IMyRenderMessage drawMessage = m_drawMessages.Dequeue();

                MyRenderMessageEnum messageType = drawMessage.MessageType;
                switch (messageType)
                {
                    case MyRenderMessageEnum.SpriteScissorPush:
                        {
                            var msg = drawMessage as MyRenderMessageSpriteScissorPush;
                            m_spriteBatch.ScissorStack.Push(msg.ScreenRectangle);
                            break;
                        }

                    case MyRenderMessageEnum.SpriteScissorPop:
                        {
                            m_spriteBatch.ScissorStack.Pop();
                            break;
                        }

                    case MyRenderMessageEnum.DrawSprite:
                        {
                            MyRenderMessageDrawSprite sprite = (MyRenderMessageDrawSprite)drawMessage;

                            ProcessSpriteMessage(sprite);
                            break;
                        }

                    case MyRenderMessageEnum.DrawSpriteNormalized:
                        {
                            MyRenderMessageDrawSpriteNormalized sprite = (MyRenderMessageDrawSpriteNormalized)drawMessage;

                            ProcessNormalizedSpriteMessage(sprite);

                            break;
                        }
                                                       
                    case MyRenderMessageEnum.DrawSpriteAtlas:
                        {
                            MyRenderMessageDrawSpriteAtlas sprite = (MyRenderMessageDrawSpriteAtlas)drawMessage;

                            ProcessAtlasSprite(sprite);

                            break;
                        }

                    case MyRenderMessageEnum.DrawString:
                        {
                            var message = drawMessage as MyRenderMessageDrawString;

                            var font = MyRender.GetFont(message.FontIndex);
                            font.DrawString(
                                message.ScreenCoord * m_sizeMultiplierForStrings,
                                message.ColorMask,
                                message.Text,
                                message.ScreenScale * m_sizeMultiplierForStrings.X,
                                message.ScreenMaxWidth * m_sizeMultiplierForStrings.X);

                            break;
                        }

                    case MyRenderMessageEnum.DrawScene:
                        {
                            EndSpriteBatch();

                            MyRenderCamera.UpdateCamera();
                            MyRender.SetDeviceViewport(MyRenderCamera.Viewport);

                            Draw3D();

                            DrawDebugMessages();
                            
                            BeginSpriteBatch();

                            break;
                        }

                    case MyRenderMessageEnum.DrawVideo:
                        {
                            var rMessage = (MyRenderMessageDrawVideo)drawMessage;
                            MyRender.DrawVideo(rMessage.ID, rMessage.Rectangle, rMessage.Color, rMessage.FitMode);

                            break;
                        }

                    case MyRenderMessageEnum.UpdateEnvironmentMap:
                        {
                            MyRender.GetRenderProfiler().StartProfilingBlock("MyEnvironmentMap.Update");
                            MyEnvironmentMap.Update();
                            MyRender.GetRenderProfiler().EndProfilingBlock();

                            break;
                        }

                    case MyRenderMessageEnum.DrawSecondaryCamera:
                        {
                            var rMessage = (MyRenderMessageDrawSecondaryCamera)drawMessage;

                            MySecondaryCameraRenderer.Instance.ViewMatrix = (MatrixD)rMessage.ViewMatrix;
                            MySecondaryCameraRenderer.Instance.Render();

                            break;
                        }

                    case MyRenderMessageEnum.DrawSecondaryCameraSprite:
                        {
                            MyRenderMessageDrawSecondaryCameraSprite sprite = (MyRenderMessageDrawSecondaryCameraSprite)drawMessage;

                            Vector2 rightVector = sprite.Rotation != 0f ? new Vector2((float)Math.Cos(sprite.Rotation), (float)Math.Sin(sprite.Rotation)) : sprite.RightVector;
                            //rightVector = new Vector2(1, 1);

                            DrawSpriteMain(
                             MySecondaryCameraRenderer.Instance.GetRenderedTexture(),
                             null,
                             ref sprite.DestinationRectangle,
                             sprite.ScaleDestination,
                             sprite.SourceRectangle,
                             sprite.Color,
                             rightVector,
                             ref sprite.Origin,
                             sprite.Effects,
                             sprite.Depth
                         );

                            break;
                        }

                    case MyRenderMessageEnum.UpdateBillboardsColorize:
                        {
                            var rMessage = (MyRenderMessageUpdateBillboardsColorize)drawMessage;

                            MyTransparentGeometry.EnableColorize = rMessage.Enable;
                            MyTransparentGeometry.ColorizeColor = rMessage.Color;
                            MyTransparentGeometry.ColorizePlaneDistance = rMessage.Distance;
                            MyTransparentGeometry.ColorizePlaneNormal = rMessage.Normal;

                            break;
                        }                   

                    default:
                        {
                            System.Diagnostics.Debug.Assert(false, "Unknown draw message");
                            break;
                        }
                }
            }

            GetRenderProfiler().StartProfilingBlock("SpriteBatchRestart");
            EndSpriteBatch();
            BeginSpriteBatch();
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("DrawDebugMessages");
            DrawDebugMessages();
            GetRenderProfiler().EndProfilingBlock();

            if (MyRenderProxy.DRAW_RENDER_STATS)
            {
                MyRenderStatsDraw.Draw(MyRenderStats.m_stats, 0.6f, Color.Yellow);
            }

            if (MyRender.Settings.TearingTest)
            {
                DrawTearingTest();
            }

            if (MyRender.Settings.MultimonTest)
            {
                // Middle screen
                var from = new Vector2(GraphicsDevice.Viewport.Width / 2.0f, 0);
                var to = new Vector2(GraphicsDevice.Viewport.Width / 2.0f, GraphicsDevice.Viewport.Height);
                VRageRender.MyRenderProxy.DebugDrawLine2D(from, to, Color.Orange, Color.Orange);

                from = new Vector2(GraphicsDevice.Viewport.Width / 3.0f, 0);
                to = new Vector2(GraphicsDevice.Viewport.Width / 3.0f, GraphicsDevice.Viewport.Height);
                VRageRender.MyRenderProxy.DebugDrawLine2D(from, to, Color.Yellow, Color.Yellow);
                from = new Vector2(GraphicsDevice.Viewport.Width / 3.0f * 2.0f, 0);
                to = new Vector2(GraphicsDevice.Viewport.Width / 3.0f * 2.0f, GraphicsDevice.Viewport.Height);
                VRageRender.MyRenderProxy.DebugDrawLine2D(from, to, Color.Yellow, Color.Yellow);
            }

            GetRenderProfiler().StartProfilingBlock("SpriteBatchEnd");
            EndSpriteBatch();
            GetRenderProfiler().EndProfilingBlock();


            System.Diagnostics.Debug.Assert(m_spriteBatchUsageCount == 0);
        }

        private static void DrawDebugMessages()
        {
            //DepthStencilState.None.Apply();
            DepthStencilState.DepthRead.Apply();
            BlendState.NonPremultiplied.Apply();

            while (m_debugDrawMessages.Count > 0)
            {
                IMyRenderMessage debugDrawMessage = m_debugDrawMessages.Dequeue();

                MyRenderMessageEnum messageType = debugDrawMessage.MessageType;

                switch (messageType)
                {
                    case MyRenderMessageEnum.DebugDrawLine3D:
                        {
                            MyRenderMessageDebugDrawLine3D message = (MyRenderMessageDebugDrawLine3D)debugDrawMessage;

                            MyDebugDraw.DrawLine3D(
                                message.PointFrom,
                                message.PointTo,
                                message.ColorFrom,
                                message.ColorTo,
                                message.DepthRead);

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawLine2D:
                        {
                            MyRenderMessageDebugDrawLine2D message = (MyRenderMessageDebugDrawLine2D)debugDrawMessage;

                            MyDebugDraw.DrawLine2D(
                                message.PointFrom,
                                message.PointTo,
                                message.ColorFrom,
                                message.ColorTo,
                                message.Projection);

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawSphere:
                        {
                            MyRenderMessageDebugDrawSphere message = (MyRenderMessageDebugDrawSphere)debugDrawMessage;

                            MyDebugDraw.DrawSphere(
                                    message.Position,
                                    message.Radius,
                                    message.Color,
                                    message.Alpha,
                                    message.DepthRead,
                                    message.Smooth,
                                    message.Cull);

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawAABB:
                        {
                            MyRenderMessageDebugDrawAABB message = (MyRenderMessageDebugDrawAABB)debugDrawMessage;

                            Color color = new Color(message.Color, message.Alpha);
                            var aabb = new BoundingBoxD(message.AABB.Min, message.AABB.Max);
                            MyDebugDraw.DrawAABBLine(
                                ref aabb,
                                ref color,
                                message.Scale,
                                message.DepthRead);

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawAxis:
                        {
                            MyRenderMessageDebugDrawAxis message = (MyRenderMessageDebugDrawAxis)debugDrawMessage;

                            MyDebugDraw.DrawAxis(
                                (MatrixD)message.Matrix,
                                message.AxisLength,
                                1,
                                message.DepthRead);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawOBB:
                        {
                            MyRenderMessageDebugDrawOBB message = (MyRenderMessageDebugDrawOBB)debugDrawMessage;

                            if (message.Smooth)
                            {
                                MyDebugDraw.DrawLowresBoxSmooth(
                                    message.Matrix,
                                    message.Color,
                                    message.Alpha,
                                    message.DepthRead,
                                    message.Cull);
                            }
                            else
                            {
                                  MyDebugDraw.DrawOBBLine(
                                    new MyOrientedBoundingBoxD(message.Matrix),
                                    message.Color,
                                    message.Alpha,
                                    message.DepthRead);

                                //BoundingBoxD bd = new BoundingBoxD(message.Matrix.Translation - new Vector3(100),message.Matrix.Translation + new Vector3(100));

                                //Vector4 c = new Vector4(message.Color.X, message.Color.Y, message.Color.Z, message.Alpha);
                                //MyDebugDraw.DrawAABBLine(ref bd, ref c, 1, false);
                            }
                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawCylinder:
                        {
                            MyRenderMessageDebugDrawCylinder message = (MyRenderMessageDebugDrawCylinder)debugDrawMessage;

                            if (message.Smooth)
                            {
                                MyDebugDraw.DrawLowresCylinderSmooth(
                                    (MatrixD)message.Matrix,
                                    message.Color,
                                    message.Alpha,
                                    message.DepthRead);
                            }
                            else
                            {
                                MyDebugDraw.DrawLowresCylinderWireframe(
                                    (MatrixD)message.Matrix,
                                    message.Color,
                                    message.Alpha,
                                    message.DepthRead);
                            }
                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawTriangle:
                        {
                            MyRenderMessageDebugDrawTriangle message = (MyRenderMessageDebugDrawTriangle)debugDrawMessage;

                            MyDebugDraw.DrawTriangle((Vector3D)message.Vertex0, (Vector3D)message.Vertex1, (Vector3D)message.Vertex2, message.Color, message.Color, message.Color, message.Smooth, message.DepthRead);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawTriangles:
                        {
                            MyRenderMessageDebugDrawTriangles message = (MyRenderMessageDebugDrawTriangles)debugDrawMessage;

                            MyDebugDraw.DrawTriangles(message.WorldMatrix, message.Vertices, message.Indices, message.Color,
                                message.DepthRead, message.Shaded);
                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawCapsule:
                        {
                            MyRenderMessageDebugDrawCapsule message = (MyRenderMessageDebugDrawCapsule)debugDrawMessage;

                            if (message.Shaded)
                            {
                                MyDebugDraw.DrawCapsuleShaded(message.P0, message.P1, message.Radius, message.Color, message.DepthRead);
                            }
                            else
                            {
                                MyDebugDraw.DrawCapsule(message.P0, message.P1, message.Radius, message.Color,message.DepthRead);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawText2D:
                        {
                            MyRenderMessageDebugDrawText2D message = (MyRenderMessageDebugDrawText2D)debugDrawMessage;

                            MyDebugDraw.DrawText(
                                message.Coord,
                                new StringBuilder(message.Text),
                                message.Color,
                                message.Scale,
                                false,
                                message.Align);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawText3D:
                        {
                            MyRenderMessageDebugDrawText3D message = (MyRenderMessageDebugDrawText3D)debugDrawMessage;

                            MyDebugDraw.DrawText(
                                (Vector3D)message.Coord,
                                new StringBuilder(message.Text),
                                message.Color,
                                message.Scale,
                                message.DepthRead,
                                message.Align,
                                message.CustomViewProjection);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawModel:
                        {
                            MyRenderMessageDebugDrawModel message = (MyRenderMessageDebugDrawModel)debugDrawMessage;

                            MyDebugDraw.DrawModel(MyRenderModels.GetModel(message.Model), message.WorldMatrix, message.Color,
                                message.DepthRead);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawPlane:
                        {
                            MyRenderMessageDebugDrawPlane message = (MyRenderMessageDebugDrawPlane)debugDrawMessage;

                            MyDebugDraw.DrawPlane((Vector3D)message.Position, message.Normal, message.Color, message.DepthRead);

                            break;
                        }

                    default:
                        {
                            System.Diagnostics.Debug.Assert(false, "Unknown debug draw message");
                            break;
                        }
                }
            }
        }

        public static void ProcessAtlasSprite(MyRenderMessageDrawSpriteAtlas sprite)
        {
            MyTexture2D tex = MyTextureManager.GetTexture<MyTexture2D>(sprite.Texture);

            if (m_screenshot != null)
            {
                sprite.Scale *= m_screenshot.SizeMultiplier;
            }

            Rectangle? sourceRect = new Rectangle(
                          (int)(tex.Width * sprite.TextureOffset.X),
                          (int)(tex.Height * sprite.TextureOffset.Y),
                          (int)(tex.Width * sprite.TextureSize.X),
                          (int)(tex.Height * sprite.TextureSize.Y));

            VRageMath.RectangleF destRect = new VRageMath.RectangleF(
                         (sprite.Position.X) * sprite.Scale.X,
                         (sprite.Position.Y) * sprite.Scale.Y,
                         sprite.HalfSize.X * sprite.Scale.X * 2,
                         sprite.HalfSize.Y * sprite.Scale.Y * 2);

            Vector2 origin = new Vector2(tex.Width * sprite.TextureSize.X * 0.5f, tex.Height * sprite.TextureSize.Y * 0.5f);

            DrawSpriteMain(
                    (MyTexture)tex,
                    null,
                    ref destRect,
                    false,
                    sourceRect,
                    sprite.Color,
                    sprite.RightVector,
                    ref origin,
                    SpriteEffects.None,
                    0
                );
        }

        public static void ProcessNormalizedSpriteMessage(MyRenderMessageDrawSpriteNormalized sprite)
        {
            var rotation = sprite.Rotation;
            if (sprite.RotationSpeed != 0)
            {
                rotation += sprite.RotationSpeed * (float)(MyRender.CurrentDrawTime - MyRender.CurrentUpdateTime).Seconds;
            }

            Vector2 rightVector = rotation != 0f ? new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) : sprite.RightVector;

            MyTexture2D tex = MyTextureManager.GetTexture<MyTexture2D>(sprite.Texture);

            if (sprite.OriginNormalized.HasValue)
            {
                DrawSpriteBatch(tex, sprite.NormalizedCoord, sprite.Scale, sprite.Color, sprite.DrawAlign, rightVector, sprite.OriginNormalized.Value);
            }
            else
            {
                DrawSpriteBatch(tex, sprite.NormalizedCoord, sprite.NormalizedSize, sprite.Color, sprite.DrawAlign, rightVector);
            }
        }

        public static void ProcessSpriteMessage(MyRenderMessageDrawSprite sprite)
        {
            Vector2 rightVector = sprite.Rotation != 0f ? new Vector2((float)Math.Cos(sprite.Rotation), (float)Math.Sin(sprite.Rotation)) : sprite.RightVector;

            MyTexture tex = MyTextureManager.GetTexture<MyTexture2D>(sprite.Texture);

            //System.Diagnostics.Debug.Assert(tex != null);
            if (tex != null)
            {
                RectangleF destinationRectangle = sprite.DestinationRectangle;
                if (m_screenshot != null)
                {
                    destinationRectangle.X *= m_screenshot.SizeMultiplier.X;
                    destinationRectangle.Y *= m_screenshot.SizeMultiplier.Y;
                    destinationRectangle.Width *= m_screenshot.SizeMultiplier.X;
                    destinationRectangle.Height *= m_screenshot.SizeMultiplier.Y;
                }

                DrawSpriteMain(
                        tex,
                        null,
                        ref destinationRectangle,
                        sprite.ScaleDestination,
                        sprite.SourceRectangle,
                        sprite.Color,
                        rightVector,
                        ref sprite.Origin,
                        sprite.Effects,
                        sprite.Depth
                    );
            }
        }

        static void DrawTearingTest()
        {
            unchecked { m_frameCounter++; }

            const int lineCount = 20;
            const int gap = 20; // gap 20px
            const int speed = 5; // 10px per ms
            int baseX = (m_frameCounter * speed) % (GraphicsDevice.Viewport.Width - lineCount * gap);
            VRageMath.Vector2 from, to;
            for (int i = 0; i < lineCount; i++)
            {
                from = new Vector2(baseX + i * gap, 0);
                to = new Vector2(baseX + i * gap, GraphicsDevice.Viewport.Height);
                VRageRender.MyRenderProxy.DebugDrawLine2D(from, to, Color.Yellow, Color.Yellow);
            }
        }

        private static void DrawSpriteMain(BaseTexture texture, CubeMapFace? face, ref RectangleF destination, bool scaleDestination, Rectangle? sourceRectangle, Color color, Vector2 rightVector, ref Vector2 origin, SpriteEffects effects, float depth)
        {
            if (m_screenshot != null && m_screenshot.IgnoreSprites)
                return;

            m_spriteBatch.DrawSprite(
                texture,
                face,
                ref destination,
                scaleDestination,
                sourceRectangle,
                color,
                rightVector,
                ref origin,
                effects,
                depth);
        }

        //  This is default sprite batch used by all GUI screens and controls. Only GamePlay is different because renders 3D objects, and
        //  also some controls (e.g. textbox, combobox) are special cases because they use stencil-mask
        //  We can use and render more SpriteBatch objects at the same time. XNA should handle it.
        public static void BeginSpriteBatch()
        {
            BeginSpriteBatch(MyStateObjects.GuiDefault_BlendState, MyStateObjects.GuiDefault_DepthStencilState, RasterizerState.CullNone);
        }

        public static void BeginSpriteBatch(BlendState blendState)
        {
            BeginSpriteBatch(blendState, MyStateObjects.GuiDefault_DepthStencilState, RasterizerState.CullNone);
        }

        internal static void BeginSpriteBatch(BlendState blendState, DepthStencilState depthState, RasterizerState rasterizerState)
        {
            if (m_spriteBatchUsageCount == 0)
            {
                //  Deferred means that draw call will be send to GPU not on every Draw(), but only at the End() or if we change
                //  a texture between Begin/End. It's faster than Immediate mode.
                m_spriteBatch.Begin(SpriteSortMode.Deferred,
                                    blendState,
                                    VRageRender.Graphics.SamplerState.LinearClamp,
                                    depthState,
                                    rasterizerState);
            }
            m_spriteBatchUsageCount++;
        }

        public static void EndSpriteBatch()
        {
            System.Diagnostics.Debug.Assert(m_spriteBatchUsageCount > 0);

            if (m_spriteBatchUsageCount == 0)
            {
                MyRender.Log.WriteLine("Sprite batch usage count is 0!");
            }

            if (m_spriteBatchUsageCount == 1)
            {
                m_spriteBatch.End();
            }
            m_spriteBatchUsageCount--;
        }

        internal static void DrawSpriteBatch(Texture texture, Vector2 normalizedCoord, Vector2 normalizedSize, Color color, MyGuiDrawAlignEnum drawAlign, Vector2 rightVector)
        {
            System.Diagnostics.Debug.Assert(m_spriteBatchUsageCount > 0);

            if (texture == null)
                return;

            if (m_screenshot != null && m_screenshot.IgnoreSprites)
                return;

            Vector2 screenCoord = GetScreenCoordinateFromNormalizedCoordinate(normalizedCoord);
            Vector2 screenSize = GetScreenSizeFromNormalizedSize(normalizedSize);
            screenCoord = GetAlignedCoordinate(screenCoord, screenSize, drawAlign);

            Vector2 origin;
            origin.X = texture.GetLevelDescription(0).Width / 2f;
            origin.Y = texture.GetLevelDescription(0).Height / 2f;

            //m_spriteBatch.Draw(texture, new DrawingRectangle((int)screenCoord.X, (int)screenCoord.Y, (int)screenSize.X, (int)screenSize.Y), null, SharpDXHelper.ToSharpDX(color), rotation, SharpDXHelper.ToSharpDX(origin), SpriteEffects.None, 0);
            RectangleF rect = new RectangleF(screenCoord.X, screenCoord.Y, screenSize.X, screenSize.Y);
            m_spriteBatch.DrawSprite(texture, null, ref rect, false, null, color, rightVector, ref origin, VRageRender.Graphics.SpriteEffects.None, 0);
        }

        //  Draws sprite batch at specified position
        //  normalizedPosition -> X and Y are within interval <0..1>
        //  scale -> scale for original texture, it's not in pixel/texels, but multiply of original size. E.g. 1 means unchanged size, 2 means double size. Scale is uniform, preserves aspect ratio.
        //  rotation -> angle in radians. Rotation is always around "origin" coordinate
        //  originNormalized -> the origin of the sprite. Specify (0,0) for the upper-left corner.
        //  RETURN: Method returns rectangle where was sprite/texture drawn in normalized coordinates
        internal static void DrawSpriteBatch(Texture texture, Vector2 normalizedCoord, float scale, Color color, MyGuiDrawAlignEnum drawAlign, Vector2 rightVector, Vector2 originNormalized)
        {
            System.Diagnostics.Debug.Assert(m_spriteBatchUsageCount > 0);
            if (texture == null)
                return;

            if (m_screenshot != null && m_screenshot.IgnoreSprites)
                return;

            Vector2 screenCoord = GetScreenCoordinateFromNormalizedCoordinate(normalizedCoord);

            //  Fix the scale for screen resolution
            float fixedScale = scale * m_safeScreenScale;

            Vector2 sizeInPixels = new Vector2(texture.GetLevelDescription(0).Width, texture.GetLevelDescription(0).Height);
            Vector2 sizeInPixelsScaled = sizeInPixels * fixedScale;

            screenCoord = GetAlignedCoordinate(screenCoord, sizeInPixelsScaled, drawAlign);

            //m_spriteBatch.Draw(texture, SharpDXHelper.ToSharpDX(screenCoord), null, SharpDXHelper.ToSharpDX(color), rotation, SharpDXHelper.ToSharpDX(originNormalized * sizeInPixels), fixedScale, SpriteEffects.None, 0);
            RectangleF rect = new RectangleF(screenCoord.X, screenCoord.Y, fixedScale, fixedScale);
            Vector2 origin = originNormalized * sizeInPixels;
            m_spriteBatch.DrawSprite(texture, null, ref rect, true, null, color, rightVector, ref origin, VRageRender.Graphics.SpriteEffects.None, 0);
        }

        //  Convertes normalized coodrinate <0..1> to screen coordinate (pixels)
        internal static Vector2 GetScreenCoordinateFromNormalizedCoordinate(Vector2 normalizedCoord, bool fullscreen = false)
        {
            if (fullscreen)
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

        //  Convertes normalized size <0..1> to screen size (pixels)
        internal static Vector2 GetScreenSizeFromNormalizedSize(Vector2 normalizedSize)
        {
            return new Vector2((m_safeGuiRectangle.Width + 1) * normalizedSize.X, m_safeGuiRectangle.Height * normalizedSize.Y);
        }

        //  Aligns rectangle, works in screen coordinates / texture / pixel... (not normalized coordinates)
        internal static Vector2 GetAlignedCoordinate(Vector2 screenCoord, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            Vector2 alignedScreenCoord = screenCoord;

            if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                //  Nothing to do as position is already at this point
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
            {
                //  Move position to the texture center
                alignedScreenCoord -= size / 2.0f;
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP)
            {
                alignedScreenCoord.X -= size.X / 2.0f;
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM)
            {
                alignedScreenCoord.X -= size.X / 2.0f;
                alignedScreenCoord.Y -= size.Y;
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM)
            {
                alignedScreenCoord -= size;
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
            {
                alignedScreenCoord.Y -= size.Y / 2.0f;
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER)
            {
                alignedScreenCoord.X -= size.X;
                alignedScreenCoord.Y -= size.Y / 2.0f;
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM)
            {
                alignedScreenCoord.Y -= size.Y;// *0.75f;
            }
            else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP)
            {
                alignedScreenCoord.X -= size.X;
            }
            else
            {
                throw new InvalidBranchException();
            }

            return alignedScreenCoord;
        }






        private static Vector2 vector2Zero = Vector2.Zero;
        private static Rectangle? nullRectangle;


        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        internal static void DrawSpriteBatch(Texture texture, int x, int y, int width, int height, Color color)
        {
            DrawSprite(texture, new Rectangle(x, y, width, height), color);
        }

        internal static void DrawSprite(Texture texture, Rectangle rectangle, Color color)
        {
            var destination = new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            DrawSprite(texture, null, ref destination, false, ref nullRectangle, color, Vector2.UnitX, ref vector2Zero, VRageRender.Graphics.SpriteEffects.None, 0f);

        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        internal static void DrawSpriteBatch(Texture texture, Rectangle destinationRectangle, Color color)
        {
            //System.Diagnostics.Debug.Assert(m_spriteBatchUsageCount > 0);
            if (texture == null)
                return;

            var destination = new RectangleF(destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height);
            DrawSprite(texture, null, ref destination, false, ref nullRectangle, color, Vector2.UnitX, ref vector2Zero, VRageRender.Graphics.SpriteEffects.None, 0f);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        internal static void DrawSpriteBatch(Texture texture, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color)
        {
            //System.Diagnostics.Debug.Assert(m_spriteBatchUsageCount > 0);
            if (texture == null)
                return;

            var destination = new RectangleF(destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height);
            Rectangle? source = sourceRectangle;
            DrawSprite(texture, null, ref destination, false, ref source, color, Vector2.UnitX, ref vector2Zero, VRageRender.Graphics.SpriteEffects.None, 0f);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        internal static void DrawSpriteBatch(Texture texture, Vector2 pos, Color color)
        {
            System.Diagnostics.Debug.Assert(m_spriteBatchUsageCount > 0);
            if (texture == null)
                return;

            DrawSprite(texture, pos, color);
        }

        internal static void DrawSprite(Texture texture, Vector2 position, Color color)
        {
            var destination = new RectangleF(position.X, position.Y, 1f, 1f);
            DrawSprite(texture, null, ref destination, true, ref nullRectangle, color, Vector2.UnitX, ref vector2Zero, VRageRender.Graphics.SpriteEffects.None, 0f);
        }



        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        internal static void DrawSpriteBatch(Texture texture, Vector2 position, Rectangle? sourceRectangle, Color color, Vector2 rightVector, Vector2 origin, Vector2 scale, VRageRender.Graphics.SpriteEffects effects, float layerDepth)
        {
            //m_spriteBatch.Draw(texture, SharpDXHelper.ToSharpDX(position), SharpDXHelper.ToSharpDX(sourceRectangle), SharpDXHelper.ToSharpDX(color), rotation, SharpDXHelper.ToSharpDX(origin), SharpDXHelper.ToSharpDX(scale), effects, layerDepth);
            DrawSprite(texture, position, sourceRectangle, color, rightVector, origin, scale, effects, layerDepth);
        }

        internal static void DrawSprite(Texture texture, Vector2 position, Rectangle? sourceRectangle, Color color, Vector2 rightVector, Vector2 origin, Vector2 scale, VRageRender.Graphics.SpriteEffects effects, float layerDepth)
        {
            var destination = new RectangleF(position.X, position.Y, scale.X, scale.Y);
            DrawSprite(texture, null, ref destination, true, ref sourceRectangle, color, rightVector, ref origin, effects, layerDepth);
        }

        //  Draws sprite batch at specified SCREEN position (in screen coordinates, not normalized coordinates).
        internal static void DrawSpriteBatch(Texture texture, Vector2 position, Rectangle? sourceRectangle, Color color, Vector2 rightVector, Vector2 origin, float scale, VRageRender.Graphics.SpriteEffects effects, float layerDepth)
        {
            //m_spriteBatch.Draw(texture, SharpDXHelper.ToSharpDX(position), SharpDXHelper.ToSharpDX(sourceRectangle), SharpDXHelper.ToSharpDX(color), rotation, SharpDXHelper.ToSharpDX(origin), scale, effects, layerDepth);
            DrawSprite(texture, position, sourceRectangle, color, rightVector, origin, scale, effects, layerDepth);
        }

        internal static void DrawSprite(Texture texture, Vector2 position, Rectangle? sourceRectangle, Color color, Vector2 rightVector, Vector2 origin, float scale, VRageRender.Graphics.SpriteEffects effects, float layerDepth)
        {
            var destination = new RectangleF(position.X, position.Y, scale, scale);
            DrawSprite(texture, null, ref destination, true, ref sourceRectangle, color, rightVector, ref origin, effects, layerDepth);
        }


        internal static void DrawSprite(BaseTexture texture, CubeMapFace? face, ref RectangleF destination, bool scaleDestination, ref Rectangle? sourceRectangle, Color color, Vector2 rightVector, ref Vector2 origin, VRageRender.Graphics.SpriteEffects effects, float depth)
        {
            DrawSpriteMain(texture, face, ref destination, scaleDestination, sourceRectangle, color, rightVector, ref origin, effects, depth);
        }

        internal static float DrawTextShadow(Vector2 screenCoord, StringBuilder text, Color color, float scale)
        {
            BeginSpriteBatch();
            float textLenght = MyRender.GetDebugFont().DrawString(screenCoord, color, text, scale);
            EndSpriteBatch();

            return textLenght;
        }

        internal static float DrawText(Vector2 screenCoord, StringBuilder text, Color color, float scale)
        {
            BeginSpriteBatch();
            float textLenght = MyRender.GetDebugFont().DrawString(screenCoord, color, text, scale);
            EndSpriteBatch();

            return textLenght;
        }

        internal static Vector2 MeasureText(StringBuilder text, float scale)
        {
            return MyRender.GetDebugFont().MeasureString(text, scale);
        }
    }
}
