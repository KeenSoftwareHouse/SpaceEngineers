using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Format = SharpDX.DXGI.Format;
using Matrix = VRageMath.Matrix;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using VRage;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Voxels;
using System.Diagnostics;

namespace VRageRender
{
    partial class MyRender11
    {
        static Queue<MyRenderMessageBase> m_drawQueue = new Queue<MyRenderMessageBase>();
        static Queue<MyRenderMessageBase> m_debugDrawMessages = new Queue<MyRenderMessageBase>();
        static bool m_reloadShaders;

        static MyScreenshot? m_screenshot;

        internal static void Draw(bool draw = true)
        {
            //if (false) Debug.Assert(MyClipmap.LodLevel.DrewLastFrame);
            //MyClipmap.LodLevel.DrewLastFrame = false;

            try
            {
                GetRenderProfiler().StartProfilingBlock("ProcessMessages");
                TransferLocalMessages();
                ProcessMessageQueue();
                GetRenderProfiler().EndProfilingBlock();

                if (draw)
                {
                    MyImmediateRC.RC.Clear();
                    GetRenderProfiler().StartProfilingBlock("ProcessDrawQueue");
                    ProcessDrawQueue();
                    GetRenderProfiler().EndProfilingBlock();

                    /*GetRenderProfiler().StartProfilingBlock("ProcessDebugMessages");
                    ProcessDebugMessages();
                    GetRenderProfiler().EndProfilingBlock();*/

                    GetRenderProfiler().StartProfilingBlock("MySpritesRenderer.Draw");
                    //MyCommon.UpdateFrameConstants();
                    MySpritesRenderer.Draw(MyRender11.Backbuffer.m_RTV, new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y));
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
            catch (SharpDXException e)
            {
                MyRender11.Log.IncreaseIndent();
                MyRender11.Log.WriteLine(" " + e);
                if (e.Descriptor == SharpDX.DXGI.ResultCode.DeviceRemoved)
                {
                    MyRender11.Log.WriteLine("Reason: " + Device.DeviceRemovedReason);
                }
                MyRender11.Log.DecreaseIndent();

                throw;
            }
        }

        internal delegate void MessagesProcessedCallback();
        internal static MessagesProcessedCallback OnMessagesProcessedOnce;

        internal static void ProcessDrawQueue()
        {
            while (m_drawQueue.Count > 0)
            {
                var drawMessage = m_drawQueue.Dequeue();
                ProfilerShort.Begin(MyEnum<MyRenderMessageEnum>.GetName(drawMessage.MessageType));
                ProcessDrawMessage(drawMessage);
                ProfilerShort.End();
            }

            ProfilerShort.Begin("OnMessagesProcessedOnce");
            if (OnMessagesProcessedOnce != null)
                OnMessagesProcessedOnce();
            OnMessagesProcessedOnce = null;
            ProfilerShort.End();
        }

        private static void ProcessDrawMessage(MyRenderMessageBase drawMessage)
        {
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

                        MySpritesRenderer.AddSingleSprite(MyTextures.GetTexture(sprite.Texture, MyTextureEnum.GUI, waitTillLoaded: sprite.WaitTillLoaded), sprite.Color, sprite.Origin, sprite.RightVector, sprite.SourceRectangle, sprite.DestinationRectangle);

                        break;
                    }

                case MyRenderMessageEnum.DrawSpriteNormalized:
                    {
                        MyRenderMessageDrawSpriteNormalized sprite = (MyRenderMessageDrawSpriteNormalized)drawMessage;

                        var rotation = sprite.Rotation;
                        if (sprite.RotationSpeed != 0)
                        {
                            rotation += sprite.RotationSpeed * (float)(MyRender11.CurrentDrawTime - MyRender11.CurrentUpdateTime).Seconds;
                        }

                        Vector2 rightVector = rotation != 0f ? new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) : sprite.RightVector;

                        int safeGuiSizeY = MyRender11.ResolutionI.Y;
                        int safeGuiSizeX = (int)(safeGuiSizeY * 1.3333f);     //  This will mantain same aspect ratio for GUI elements

                        var safeGuiRectangle = new VRageMath.Rectangle(MyRender11.ResolutionI.X / 2 - safeGuiSizeX / 2, 0, safeGuiSizeX, safeGuiSizeY);
                        var safeScreenScale = (float)safeGuiSizeY / MyRenderGuiConstants.REFERENCE_SCREEN_HEIGHT;
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
                        if (sprite.OriginNormalized.HasValue)
                        {
                            origin = sprite.OriginNormalized.Value * sizeInPixels;
                        }
                        else
                        {
                            origin = sizeInPixels / 2;
                        }

                        sprite.OriginNormalized = sprite.OriginNormalized ?? new Vector2(0.5f);

                        MySpritesRenderer.AddSingleSprite(MyTextures.GetTexture(sprite.Texture, MyTextureEnum.GUI, waitTillLoaded: sprite.WaitTillLoaded), sprite.Color, origin, rightVector, null, rect);

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

                        VRageMath.RectangleF destRect = new VRageMath.RectangleF(
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

                        var font = MyRender11.GetFont(message.FontIndex);
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

                        ProfilerShort.Begin("DrawScene");
                        DrawGameScene(Backbuffer);
                        ProfilerShort.Begin("TransferPerformanceStats");
                        TransferPerformanceStats();
                        ProfilerShort.End();
                        ProfilerShort.End();

                        ProfilerShort.Begin("Draw scene debug");
                        MyGpuProfiler.IC_BeginBlock("Draw scene debug");
                        DrawSceneDebug();
                        MyGpuProfiler.IC_EndBlock();
                        ProfilerShort.End();

                        ProfilerShort.Begin("ProcessDebugMessages");
                        ProcessDebugMessages();
                        ProfilerShort.End();

                        ProfilerShort.Begin("MyDebugRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MyDebugRenderer.Draw");
                        MyDebugRenderer.Draw(MyRender11.Backbuffer);
                        MyGpuProfiler.IC_EndBlock();
                        ProfilerShort.End();

                        var testingDepth = MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth : MyGBuffer.Main.DepthStencil;

                        ProfilerShort.Begin("MyPrimitivesRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MyPrimitivesRenderer.Draw");
                        MyPrimitivesRenderer.Draw(MyRender11.Backbuffer, testingDepth);
                        MyGpuProfiler.IC_EndBlock();
                        ProfilerShort.End();

                        ProfilerShort.Begin("MyLinesRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MyLinesRenderer.Draw");
                        MyLinesRenderer.Draw(MyRender11.Backbuffer, testingDepth);
                        MyGpuProfiler.IC_EndBlock();
                        ProfilerShort.End();

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

                        ProfilerShort.Begin("MySpritesRenderer.Draw");
                        MyGpuProfiler.IC_BeginBlock("MySpritesRenderer.Draw");
                        MySpritesRenderer.Draw(MyRender11.Backbuffer.m_RTV, new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y));
                        MyGpuProfiler.IC_EndBlock();
                        ProfilerShort.End();

                        if (MyRenderProxy.DRAW_RENDER_STATS)
                        {
                            MyRender11.GetRenderProfiler().StartProfilingBlock("MyRenderStatsDraw.Draw");
                            MyRenderStatsDraw.Draw(MyRenderStats.m_stats, 0.6f, VRageMath.Color.Yellow);
                            ProfilerShort.End();
                        }

                        break;
                    }
            }
        }
    }
}
