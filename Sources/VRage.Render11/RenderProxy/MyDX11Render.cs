using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using VRage;
using VRageMath;
using VRageRender.Profiler;
using Vector2 = VRageMath.Vector2;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Render11.Shader;
using VRage.Utils;
using VRageRender.Messages;

namespace VRageRender
{
    public class MyDX11Render : IMyRender
    {
        public string RootDirectory { get { return MyRender11.RootDirectory; } set { MyRender11.RootDirectory = value; } }
        public string RootDirectoryEffects { get { return MyRender11.RootDirectoryEffects; } set { MyRender11.RootDirectoryEffects = value; } }
        public string RootDirectoryDebug { get { return MyRender11.RootDirectoryDebug; } set { MyRender11.RootDirectoryDebug = value; } }

        public MyLog Log { get { return MyRender11.Log; } }

        public MySharedData SharedData { get { return MyRender11.SharedData; } }
        public MyTimeSpan CurrentDrawTime
        {
            get { return MyRender11.CurrentDrawTime; }
            set { MyRender11.PreviousDrawTime = MyRender11.CurrentDrawTime; MyRender11.CurrentDrawTime = value; }
        }


        public MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settings)
        {
            return MyRender11.CreateDevice(windowHandle, settings);
        }

        public void DisposeDevice()
        {
            MyRender11.DisposeDevice();
        }

        public long GetAvailableTextureMemory()
        {
            return MyRender11.GetAvailableTextureMemory();
        }

        public MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel()
        {
            return MyRenderDeviceCooperativeLevel.Ok;
        }

        public bool ResetDevice()
        {
            return true;
        }

        public void DrawBegin()
        {
        }

        public void DrawEnd()
        {
        }

        public bool SettingsChanged(MyRenderDeviceSettings settings)
        {
            return MyRender11.SettingsChanged(settings);
        }

        public void ApplySettings(MyRenderDeviceSettings settings)
        {
            MyRender11.ApplySettings(settings);
        }

        public void Present()
        {
            MyRender11.Present();
        }

        public Vector2I BackBufferResolution { get { return MyRender11.BackBufferResolution; } }

        public MyViewport MainViewport
        {
            get
            {
                var resolution = BackBufferResolution;
                return new MyViewport(resolution.X, resolution.Y);
            }
        }

        public void LoadContent(MyRenderQualityEnum quality)
        {
        }

        public void UnloadContent()
        {
        }

        public void UnloadData()
        {
        }

        public void ReloadContent(MyRenderQualityEnum quality)
        {
        }

        public MyMessageQueue OutputQueue { get { return MyRender11.OutputQueue; } }
        public uint GlobalMessageCounter { get { return MyRender11.GlobalMessageCounter; } set { MyRender11.GlobalMessageCounter = value; } }

        public void EnqueueMessage(MyRenderMessageBase message, bool limitMaxQueueSize)
        {
            MyRender11.EnqueueMessage(message, limitMaxQueueSize);
        }

        public void ProcessMessages()
        {
        }

        public void EnqueueOutputMessage(MyRenderMessageBase message)
        {
            MyRender11.EnqueueOutputMessage(message);
        }

        public void ResetEnvironmentProbes()
        {
            // TODO
        }

        public void Draw(bool draw = true)
        {
#if DEBUG
            try
            {
                MyRender11.Draw(draw);
            }
            catch (Exception ex)
            {
                MyRender11.ProcessDebugOutput();
                System.Diagnostics.Debug.WriteLine(ex);
                MyRenderProxy.Assert(false, "Exception in render!\n" + ex);
            }
#else
            MyRender11.Draw(draw);
#endif
        }

        public MyRenderProfiler GetRenderProfiler()
        {
            return MyRender11.GetRenderProfiler();
        }

        public bool IsVideoValid(uint id)
        {
            MyVideoFactory.VideoMutex.WaitOne();
            var result = MyVideoFactory.Videos.Get(id) != null;
            MyVideoFactory.VideoMutex.ReleaseMutex();
            return result;
        }

        public VideoState GetVideoState(uint id)
        {
            VideoState result = VideoState.Stopped;
            MyVideoFactory.VideoMutex.WaitOne();
            var video = MyVideoFactory.Videos.Get(id);
            if(video != null)
            {
                result = (VideoState)((int)video.CurrentState);
            }
            MyVideoFactory.VideoMutex.ReleaseMutex();
            return result;
        }

        public void HandleFocusMessage(MyWindowFocusMessage msg)
        {
            if (msg == MyWindowFocusMessage.Activate && MyRenderProxy.RenderThread.CurrentSettings.WindowMode == MyWindowModeEnum.Fullscreen)
            {
                MyRenderProxy.RenderThread.UpdateSize(MyWindowModeEnum.FullscreenWindow);
            }

            if (msg == MyWindowFocusMessage.SetFocus && MyRenderProxy.RenderThread.CurrentSettings.WindowMode == MyWindowModeEnum.Fullscreen)
            {
                MyRenderProxy.RenderThread.UpdateSize(MyWindowModeEnum.Fullscreen);
                MyRender11.RestoreFullscreenMode();
            }
        }

        public bool IsSupported
        {
            get
            {
                try
                {
                    var adapters = MyRender11.GetAdaptersList();

                    foreach(var adapter in adapters) {
                        if (adapter.IsDx11Supported)
                        { 
                            return true;
                        }
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void GenerateShaderCache(bool clean, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            MyShaderCacheGenerator.Generate(clean, onShaderCacheProgress);
        }
    }
}
