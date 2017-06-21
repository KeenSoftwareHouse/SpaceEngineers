using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

using VRageMath;
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
using System.Diagnostics;
using VRage.OpenVRWrapper;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.LightingStage.Shadows;
using VRage.Render11.Profiler;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageRender.Messages;
using VRageRender.Utils;

namespace VRageRender
{
    partial class MyRender11
    {
        public const string DEFAULT_TEXTURE_TARGET = "DefaultOffscreenTarget";

        static Dictionary<string, List<MySpriteDrawRenderMessage>> m_spriteTextureBatches = new Dictionary<string, List<MySpriteDrawRenderMessage>>();
        static Queue<MyRenderMessageBase> m_drawQueue = new Queue<MyRenderMessageBase>();
        static Queue<MyRenderMessageBase> m_debugDrawMessages = new Queue<MyRenderMessageBase>();
        static bool m_drawScene;

        static bool m_drawStatsSwitch = true; // Start with new paged stats

        static MyScreenshot? m_screenshot;

        static List<renderColoredTextureProperties> m_texturesToRender = new List<renderColoredTextureProperties>();
        static readonly StringBuilder m_exceptionBuilder = new StringBuilder();
        internal static void Draw(bool draw = true)
        {
            try
            {
                MyGpuProfiler.IC_BeginBlock("Draw");
                GetRenderProfiler().StartProfilingBlock("ProcessMessages");
                MyGpuProfiler.IC_BeginBlock("ProcessMessageQueue");
                TransferLocalMessages();
                ProcessMessageQueue();
                MyGpuProfiler.IC_EndBlock();
                GetRenderProfiler().EndProfilingBlock();

                if (draw)
                {
                    m_drawScene = false;
                    DispatchDrawQueue();

                    if (m_drawScene)
                        DrawScene();

                    if (!(MyRender11.Settings.OffscreenSpritesRendering && m_drawScene))
                    {
                        ProcessDrawQueue();
                        DrawSprites(MyRender11.Backbuffer, new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y));
                    }

                    MyFileTextureManager texManager = MyManagers.FileTextures;
                    texManager.LoadAllRequested();

                    if (m_texturesToRender.Count > 0)
                        VRage.Render11.PostprocessStage.MySaveExportedTextures.RenderColoredTextures(m_texturesToRender);
                }

                MyLinesRenderer.Clear();
                MySpritesRenderer.Clear();

                m_drawQueue.Clear();
                MyGpuProfiler.IC_EndBlock();
            }
            catch (SharpDXException e)
            {
                MyRender11.Log.IncreaseIndent();
                MyRender11.Log.WriteLine(" " + e);
                if (e.Descriptor == SharpDX.DXGI.ResultCode.DeviceRemoved)
                {
                    MyRender11.Log.WriteLine("Reason: " + Device.DeviceRemovedReason);
                }

                // Include the stats
                m_exceptionBuilder.Clear();
                MyStatsUpdater.UpdateStats();
                MyStatsDisplay.WriteTo(m_exceptionBuilder);
                MyRender11.Log.WriteLine(m_exceptionBuilder.ToString());
                MyRender11.Log.Flush();
                MyRender11.Log.DecreaseIndent();
                throw;
            }
        }

        private static void DispatchDrawQueue(bool ignoreDrawScene = false)
        {
            while (m_drawQueue.Count > 0)
            {
                var message = m_drawQueue.Dequeue();
                switch (message.MessageType)
                {
                    case MyRenderMessageEnum.SpriteScissorPush:
                    case MyRenderMessageEnum.SpriteScissorPop:
                    case MyRenderMessageEnum.DrawSprite:
                    case MyRenderMessageEnum.DrawSpriteNormalized:
                    case MyRenderMessageEnum.DrawSpriteAtlas:
                    case MyRenderMessageEnum.DrawString:
                        {
                            MySpriteDrawRenderMessage spriteMessage = message as MySpriteDrawRenderMessage;

                            string textureName = spriteMessage.TargetTexture == null ? DEFAULT_TEXTURE_TARGET : spriteMessage.TargetTexture;
                            List<MySpriteDrawRenderMessage> messages;
                            if (!m_spriteTextureBatches.TryGetValue(textureName, out messages))
                            {
                                messages = new List<MySpriteDrawRenderMessage>();
                                m_spriteTextureBatches.Add(textureName, messages);
                            }

                            messages.Add(spriteMessage);
                            break;
                        }
                    case MyRenderMessageEnum.DrawScene:
                        {
                            if (!ignoreDrawScene)
                                m_drawScene = true;
                            break;
                        }
                }
            }
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

                        MyFileTextureManager texManager = MyManagers.FileTextures;
                        MySpritesRenderer.AddSingleSprite(texManager.GetTexture(sprite.Texture, MyFileTextureEnum.GUI, waitTillLoaded: sprite.WaitTillLoaded), sprite.Color, sprite.Origin, sprite.RightVector, sprite.SourceRectangle, sprite.DestinationRectangle);

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


                        var tex = MyManagers.FileTextures.GetTexture(sprite.Texture, MyFileTextureEnum.GUI, true);

                        var normalizedCoord = sprite.NormalizedCoord;
                        var screenCoord = new Vector2(safeGuiRectangle.Left + safeGuiRectangle.Width * normalizedCoord.X,
                            safeGuiRectangle.Top + safeGuiRectangle.Height * normalizedCoord.Y);

                        Vector2 sizeInPixels = tex.Size;
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

                        MyFileTextureManager texManager = MyManagers.FileTextures;
                        MySpritesRenderer.AddSingleSprite(texManager.GetTexture(sprite.Texture, MyFileTextureEnum.GUI, waitTillLoaded: sprite.WaitTillLoaded), sprite.Color, origin, rightVector, null, rect);

                        break;
                    }


                case MyRenderMessageEnum.DrawSpriteAtlas:
                    {
                        MyRenderMessageDrawSpriteAtlas sprite = (MyRenderMessageDrawSpriteAtlas)drawMessage;

                        MyFileTextureManager texManager = MyManagers.FileTextures;
                        var tex = texManager.GetTexture(sprite.Texture, MyFileTextureEnum.GUI, true);
                        var textureSize = tex.Size;

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

                        MySpritesRenderer.AddSingleSprite(texManager.GetTexture(sprite.Texture, MyFileTextureEnum.GUI, true), sprite.Color, origin, sprite.RightVector, sourceRect, destRect);

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

                default:
                    throw new Exception();
            }
        }

        private static void DrawScene()
        {
            AddDebugQueueMessage("Frame render start");

            MyGpuProfiler.IC_BeginBlock("UpdateSceneFrame");
            UpdateSceneFrame();
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("Clear");
            MyGBuffer.Main.Clear(VRageMath.Color.Black);
            MyGpuProfiler.IC_EndBlock();

            if (MyOpenVR.Static != null)
            {
                ProfilerShort.Begin("OpenVR.WaitForNextStart");
                MyOpenVR.WaitForNextStart();
                ProfilerShort.End();
            }

            IBorrowedRtvTexture debugAmbientOcclusion; // TODO: Think of another way to get this texture to the DebugRenderer...

            ProfilerShort.Begin("DrawGameScene");
            DrawGameScene(Backbuffer, out debugAmbientOcclusion);
            ProfilerShort.End();
            if (MyOpenVR.Static != null)
            {
                ProfilerShort.Begin("OpenVR.DisplayEye");
                MyGpuProfiler.IC_BeginBlock("OpenVR.DisplayEye");
                MyGBuffer.Main.Clear(VRageMath.Color.Black);//image is in HMD now, lets draw the rest for overlay
                MyOpenVR.Static.DisplayEye(MyRender11.Backbuffer.Resource.NativePointer);
                MyGpuProfiler.IC_EndBlock();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Draw scene debug");
            MyGpuProfiler.IC_BeginBlock("Draw scene debug");
            DrawSceneDebug();
            ProfilerShort.End();

            ProfilerShort.Begin("ProcessDebugMessages");
            ProcessDebugMessages();
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            ProfilerShort.Begin("MyDebugRenderer.Draw");
            MyGpuProfiler.IC_BeginBlock("MyDebugRenderer.Draw");
            MyDebugRenderer.Draw(MyRender11.Backbuffer, debugAmbientOcclusion);
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            debugAmbientOcclusion.Release();

            ProfilerShort.Begin("MyPrimitivesRenderer.Draw");
            MyGpuProfiler.IC_BeginBlock("MyPrimitivesRenderer.Draw");
            MyPrimitivesRenderer.Draw(MyRender11.Backbuffer);
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            ProfilerShort.Begin("MyLinesRenderer.Draw");
            MyGpuProfiler.IC_BeginBlock("MyLinesRenderer.Draw");
            MyLinesRenderer.Draw(MyRender11.Backbuffer);
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            if (m_screenshot.HasValue && m_screenshot.Value.IgnoreSprites)
            {
                ProfilerShort.Begin("Screenshot");
                if (m_screenshot.Value.SizeMult == Vector2.One)
                {
                    SaveScreenshotFromResource(Backbuffer);
                }
                else
                {
                    TakeCustomSizedScreenshot(m_screenshot.Value.SizeMult);
                }
                ProfilerShort.End();
            }

            ProfilerShort.Begin("ProcessDebugOutput");
            AddDebugQueueMessage("Frame render end");
            ProcessDebugOutput();
            ProfilerShort.End();
        }

        public static IBorrowedRtvTexture DrawSpritesOffscreen(string textureName, int widht, int height,
            Format format = Format.B8G8R8A8_UNorm, Color? clearColor = null)
        {
            if (String.IsNullOrEmpty(textureName))
                textureName = DEFAULT_TEXTURE_TARGET;

            if (widht == -1)
                widht = MyRender11.ViewportResolution.X;

            if (height == -1)
                height = MyRender11.ViewportResolution.Y;

            var texture = MyManagers.RwTexturesPool.BorrowRtv(textureName, widht, height, format);
            MyImmediateRC.RC.ClearRtv(texture, clearColor == null ? Color.Zero : clearColor.Value);
            DispatchDrawQueue(true);

            MySpritesRenderer.PushState(new Vector2(widht, height));
            bool processed = ProcessDrawSpritesQueue(textureName);
            if (!processed)
            {
                MySpritesRenderer.PopState();
                return texture;
            }

            DrawSprites(texture, new MyViewport(widht, height));
            MySpritesRenderer.PopState();
            return texture;
        }


        private static void ProcessDrawQueue()
        {
            ProcessDrawSpritesQueue(DEFAULT_TEXTURE_TARGET);
        }

        private static bool ProcessDrawSpritesQueue(string textureName)
        {
            List<MySpriteDrawRenderMessage> messages;
            if (!m_spriteTextureBatches.TryGetValue(textureName, out messages) || messages.Count == 0)
                return false;

            GetRenderProfiler().StartProfilingBlock("ProcessDrawQueue");

            foreach (var message in messages)
            {
                ProfilerShort.Begin(message.MessageType.ToString());
                ProcessDrawMessage(message);
                ProfilerShort.End();
            }
            messages.Clear();

            if (textureName == DEFAULT_TEXTURE_TARGET && MyRenderProxy.DrawRenderStats >= MyRenderProxy.MyStatsState.Draw)
            {
                // Stats will be drawn one frame later
                ProfilerShort.Begin("MyRenderStatsDraw.Draw");
                MyGpuProfiler.IC_BeginBlock("MyRenderStatsDraw.Draw");

                if (!m_drawStatsSwitch)
                    MyRenderStatsDraw.Draw(MyRenderStats.m_stats, 0.6f, VRageMath.Color.Yellow);
                else
                {
                    MyStatsUpdater.UpdateStats();
                    MyStatsDisplay.Draw();
                }

                if (MyRenderProxy.DrawRenderStats == MyRenderProxy.MyStatsState.MoveNext)
                {
                    MyRenderProxy.DrawRenderStats = MyRenderProxy.MyStatsState.Draw;

                    if (!m_drawStatsSwitch)
                    {
                        m_drawStatsSwitch = !m_drawStatsSwitch;
                        // We finished cycling through the last page -- signal turning off of drawing stats on the next MoveNext
                        MyRenderProxy.DrawRenderStats = MyRenderProxy.MyStatsState.ShouldFinish;
                    }
                    else
                    {
                        m_drawStatsSwitch = MyStatsDisplay.MoveToNextPage(); // After the last page (returns false), switch to old stat display
                    }
                }

                MyGpuProfiler.IC_EndBlock();
                ProfilerShort.End();
            }

            GetRenderProfiler().EndProfilingBlock();
            return true;
        }


        private static void DrawSprites(IRtvBindable texture, MyViewport viewPort)
        {
            GetRenderProfiler().StartProfilingBlock("MySpritesRenderer.Draw");
            MyStatsUpdater.Timestamps.Update(ref MyStatsUpdater.Timestamps.PreDrawSprites_Draw);
            MyGpuProfiler.IC_BeginBlock("SpriteRenderer");
            MySpritesRenderer.Draw(texture, viewPort);
            MyGpuProfiler.IC_EndBlock();
            MyStatsUpdater.Timestamps.Update(ref MyStatsUpdater.Timestamps.PostDrawSprites_Draw);
            GetRenderProfiler().EndProfilingBlock();
        }
    }
}
