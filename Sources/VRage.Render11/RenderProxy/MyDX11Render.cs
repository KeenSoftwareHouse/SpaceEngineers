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

namespace VRageRender
{
    public class MyDX11Render : IMyRender
    {
        public string RootDirectory { get { return MyRender11.RootDirectory; } set { MyRender11.RootDirectory = value; } }
        public string RootDirectoryEffects { get { return MyRender11.RootDirectoryEffects; } set { MyRender11.RootDirectoryEffects = value; } }
        public string RootDirectoryDebug { get { return MyRender11.RootDirectoryDebug; } set { MyRender11.RootDirectoryDebug = value; } }


        public MyRenderSettings Settings { get { return MyRender11.Settings; } }
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
            MyRender11.CheckAdapterChange(settings);
            MyRender11.ApplySettings(settings);
        }

        public void Present()
        {
            MyRender11.Present();
        }

        public void ClearBackbuffer(ColorBGRA clearColor)
        {
            MyRender11.ClearBackbuffer(clearColor);
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

        public void EnqueueMessage(IMyRenderMessage message, bool limitMaxQueueSize)
        {
            MyRender11.EnqueueMessage(message, limitMaxQueueSize);
        }

        public void ProcessMessages()
        {
        }

        public void EnqueueOutputMessage(IMyRenderMessage message)
        {
            MyRender11.EnqueueOutputMessage(message);
        }

        public void ResetEnvironmentProbes()
        {
            // TODO
        }

        public void Draw(bool draw = true)
        {
            MyRender11.Draw(draw);
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
                var adapters = MyRender11.GetAdaptersList();
                return adapters.Length > 0;
            }
        }

    }
}