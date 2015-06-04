using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.DXGI;
using VRage.Utils;
using VRageRender.Resources;
using Color = VRageMath.Color;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;

namespace VRageRender
{
    partial class MyRender11
    {
        static Queue<IMyRenderMessage> m_drawQueue = new Queue<IMyRenderMessage>();
        static Queue<IMyRenderMessage> m_debugDrawMessages = new Queue<IMyRenderMessage>();
        static bool m_reloadShaders;

        static MyScreenshot? m_screenshot;

        internal static void Draw(bool draw = true)
        {
            try
            {
                GetRenderProfiler().StartProfilingBlock("ProcessMessages");
                ProcessMessageQueue();
                GetRenderProfiler().EndProfilingBlock();


                GetRenderProfiler().StartProfilingBlock("RebuildShaders");

                //

                //MyShaderCache.CompilePending();
                //MyShaderFactory.RunCompilation();

                GetRenderProfiler().EndProfilingBlock();

                if (draw)
                {
                    MyImmediateRC.RC.Clear();
                    GetRenderProfiler().StartProfilingBlock("ProcessDrawQueue");
                    ProcessDrawQueue();
                    GetRenderProfiler().EndProfilingBlock();

                    GetRenderProfiler().StartProfilingBlock("ProcessDebugMessages");
                    ProcessDebugMessages();
                    GetRenderProfiler().EndProfilingBlock();

                    GetRenderProfiler().StartProfilingBlock("MySpritesRenderer.Draw");
                    MyCommon.UpdateFrameConstants();
                    MySpritesRenderer.Draw(Backbuffer.m_RTV, new MyViewport(ViewportResolution.X, ViewportResolution.Y));
                    GetRenderProfiler().EndProfilingBlock();

                    MyTextures.Load();
                }

                if (m_profilingStarted)
                {
                    MyGpuProfiler.IC_BeginBlock("Waiting for present");
                }

                MyLinesRenderer.Clear();
                MySpritesRenderer.Clear();

                m_drawQueue.Clear();
                m_debugDrawMessages.Clear();
            }
            catch(SharpDXException e)
            {
                Log.IncreaseIndent();
                Log.WriteLine(" " +e);
                if (e.Descriptor == ResultCode.DeviceRemoved)
                {
                    Log.WriteLine("Reason: " + Device.DeviceRemovedReason);
                }
                Log.DecreaseIndent();

                throw e;
            }
        }

        internal delegate void MessagesProcessedCallback();
        internal static MessagesProcessedCallback OnMessagesProcessedOnce;

        internal static void ProcessDrawQueue()
        {
            while (m_drawQueue.Count > 0)
            {
                var drawMessage = m_drawQueue.Dequeue();


                switch (drawMessage.MessageType)
                {
                    case MyRenderMessageEnum.SpriteScissorPush:
                    {
                        var msg = drawMessage as MyRenderMessageSpriteScissorPush;

                        MySpritesRenderer.ScissorStackPush(msg.ScreenRectangle);

                        break;
                    }

                    case MyRenderMessageEnum.SpriteScissorPop:
                    {
                        MySpritesRenderer.ScissorStackPop();

                        break;
                    }

                    case MyRenderMessageEnum.DrawSprite:
                    {
                        MyRenderMessageDrawSprite sprite = (MyRenderMessageDrawSprite)drawMessage;

                        MySpritesRenderer.AddSingleSprite(MyTextures.GetTexture(sprite.Texture, MyTextureEnum.GUI, true), sprite.Color, sprite.Origin, sprite.RightVector, sprite.SourceRectangle, sprite.DestinationRectangle);

                        break;
                    }

                    case MyRenderMessageEnum.DrawSpriteNormalized:
                    {
                        MyRenderMessageDrawSpriteNormalized sprite = (MyRenderMessageDrawSpriteNormalized)drawMessage;

                        var rotation = sprite.Rotation;
                        if (sprite.RotationSpeed != 0)
                        {
                            rotation += sprite.RotationSpeed * (float)(CurrentDrawTime - CurrentUpdateTime).Seconds;
                        }

                        Vector2 rightVector = rotation != 0f ? new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) : sprite.RightVector;

                        int safeGuiSizeY = ResolutionI.Y;
                        int safeGuiSizeX = (int)(safeGuiSizeY * 1.3333f);     //  This will mantain same aspect ratio for GUI elements

                        var safeGuiRectangle = new Rectangle(ResolutionI.X / 2 - safeGuiSizeX / 2, 0, safeGuiSizeX, safeGuiSizeY);
                        var safeScreenScale = safeGuiSizeY / MyRenderGuiConstants.REFERENCE_SCREEN_HEIGHT;
                        float fixedScale = sprite.Scale * safeScreenScale;

                        var tex = MyTextures.GetTexture(sprite.Texture, MyTextureEnum.GUI, true);

                        var normalizedCoord = sprite.NormalizedCoord;
                        var screenCoord = new Vector2(safeGuiRectangle.Left + safeGuiRectangle.Width * normalizedCoord.X,
                            safeGuiRectangle.Top + safeGuiRectangle.Height * normalizedCoord.Y);

                        var sizeInPixels = MyTextures.GetSize(tex);
                        var sizeInPixelsScaled = sizeInPixels * fixedScale;

                        Vector2 alignedScreenCoord = screenCoord;
                        var drawAlign = sprite.DrawAlign;

                        if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
                        {
                            //  Nothing to do as position is already at this point
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
                        {
                            //  Move position to the texture center
                            alignedScreenCoord -= sizeInPixelsScaled / 2.0f;
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP)
                        {
                            alignedScreenCoord.X -= sizeInPixelsScaled.X / 2.0f;
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM)
                        {
                            alignedScreenCoord.X -= sizeInPixelsScaled.X / 2.0f;
                            alignedScreenCoord.Y -= sizeInPixelsScaled.Y;
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM)
                        {
                            alignedScreenCoord -= sizeInPixelsScaled;
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
                        {
                            alignedScreenCoord.Y -= sizeInPixelsScaled.Y / 2.0f;
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER)
                        {
                            alignedScreenCoord.X -= sizeInPixelsScaled.X;
                            alignedScreenCoord.Y -= sizeInPixelsScaled.Y / 2.0f;
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM)
                        {
                            alignedScreenCoord.Y -= sizeInPixelsScaled.Y;// *0.75f;
                        }
                        else if (drawAlign == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP)
                        {
                            alignedScreenCoord.X -= sizeInPixelsScaled.X;
                        }

                        screenCoord = alignedScreenCoord;

                        var rect = new RectangleF(screenCoord.X, screenCoord.Y, fixedScale * sizeInPixels.X, fixedScale * sizeInPixels.Y);
                        Vector2 origin;
                        if(sprite.OriginNormalized.HasValue)
                        {
                            origin = sprite.OriginNormalized.Value * sizeInPixels;
                        }
                        else
                        {
                            origin = sizeInPixels / 2;
                        }

                        sprite.OriginNormalized = sprite.OriginNormalized ?? new Vector2(0.5f);

                        MySpritesRenderer.AddSingleSprite(MyTextures.GetTexture(sprite.Texture, MyTextureEnum.GUI, true), sprite.Color, origin, rightVector, null, rect);

                        break;
                    }


                    case MyRenderMessageEnum.DrawSpriteAtlas:
                    {
                        MyRenderMessageDrawSpriteAtlas sprite = (MyRenderMessageDrawSpriteAtlas)drawMessage;

                        var tex = MyTextures.GetTexture(sprite.Texture, MyTextureEnum.GUI, true);
                        var textureSize = MyTextures.GetSize(tex);

                        Rectangle? sourceRect = new Rectangle(
                          (int)(textureSize.X * sprite.TextureOffset.X),
                          (int)(textureSize.Y * sprite.TextureOffset.Y),
                          (int)(textureSize.X * sprite.TextureSize.X),
                          (int)(textureSize.Y * sprite.TextureSize.Y));

                        RectangleF destRect = new RectangleF(
                                     (sprite.Position.X) * sprite.Scale.X,
                                     (sprite.Position.Y) * sprite.Scale.Y,
                                     sprite.HalfSize.X * sprite.Scale.X * 2,
                                     sprite.HalfSize.Y * sprite.Scale.Y * 2);

                        Vector2 origin = new Vector2(textureSize.X * sprite.TextureSize.X * 0.5f, textureSize.Y * sprite.TextureSize.Y * 0.5f);

                        MySpritesRenderer.AddSingleSprite(MyTextures.GetTexture(sprite.Texture, MyTextureEnum.GUI, true), sprite.Color, origin, sprite.RightVector, sourceRect, destRect);

                        break;
                    }

                    case MyRenderMessageEnum.DrawString:
                    {
                        var message = drawMessage as MyRenderMessageDrawString;

                        var font = GetFont(message.FontIndex);
                        font.DrawString(
                            message.ScreenCoord,
                            message.ColorMask,
                            message.Text,
                            message.ScreenScale,
                            message.ScreenMaxWidth);

                        break;
                    }

                    case MyRenderMessageEnum.DrawScene:
                    {
                        UpdateSceneFrame();

                        GetRenderProfiler().StartProfilingBlock("DrawScene");
                        DrawGameScene(true);
                        TransferPerformanceStats();
                        GetRenderProfiler().EndProfilingBlock();

                        GetRenderProfiler().StartProfilingBlock("Draw scene debug");
                        MyGpuProfiler.IC_BeginBlock("Draw scene debug");
                        DrawSceneDebug();
                        MyGpuProfiler.IC_EndBlock();
                        GetRenderProfiler().EndProfilingBlock();

                        GetRenderProfiler().StartProfilingBlock("ProcessDebugMessages");
                        ProcessDebugMessages();
                        GetRenderProfiler().EndProfilingBlock();

                        GetRenderProfiler().StartProfilingBlock("MyDebugRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MyDebugRenderer.Draw");
                        MyDebugRenderer.Draw();
                        MyGpuProfiler.IC_EndBlock();
                        GetRenderProfiler().EndProfilingBlock();

                        var testingDepth = MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth : MyGBuffer.Main.DepthStencil;

                        GetRenderProfiler().StartProfilingBlock("MyPrimitivesRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MyPrimitivesRenderer.Draw");
                        MyPrimitivesRenderer.Draw(testingDepth);
                        MyGpuProfiler.IC_EndBlock();
                        GetRenderProfiler().EndProfilingBlock();

                        GetRenderProfiler().StartProfilingBlock("MyLinesRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MyLinesRenderer.Draw");
                        MyLinesRenderer.Draw(testingDepth);
                        MyGpuProfiler.IC_EndBlock();
                        GetRenderProfiler().EndProfilingBlock();

                        if (m_screenshot.HasValue && m_screenshot.Value.IgnoreSprites)
                        {
                            if (m_screenshot.Value.SizeMult == Vector2.One)
                            {
                                SaveScreenshotFromResource(Backbuffer.m_resource);
                            }
                            else
                            {
                                TakeCustomSizedScreenshot(m_screenshot.Value.SizeMult);
                            }
                        }

                        GetRenderProfiler().StartProfilingBlock("MySpritesRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MySpritesRenderer.Draw");
                        MySpritesRenderer.Draw(Backbuffer.m_RTV, new MyViewport(ViewportResolution.X, ViewportResolution.Y));
                        MyGpuProfiler.IC_EndBlock();
                        GetRenderProfiler().EndProfilingBlock();

                        if (MyRenderProxy.DRAW_RENDER_STATS)
                        {
                            GetRenderProfiler().StartProfilingBlock("MyRenderStatsDraw.Draw");
                            MyRenderStatsDraw.Draw(MyRenderStats.m_stats, 0.6f, Color.Yellow);
                            GetRenderProfiler().EndProfilingBlock();
                        }

                        break;
                    }
                }
            }

            if (OnMessagesProcessedOnce != null)
                OnMessagesProcessedOnce();
            OnMessagesProcessedOnce =  null;
        }
    }
}
