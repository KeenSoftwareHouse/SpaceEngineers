#region Using

using System;
using System.Collections.Generic;
using System.Threading;
using DShowNET;
using SharpDX;
using SharpDX.Direct3D9;
using VRage;
using VRage.Generics;
using VRage.Import;
using VRageMath;
using VRageRender.Graphics;
using VRageRender.Lights;
using VRageRender.Messages;
using VRageRender.Profiler;
using System.Diagnostics;
using System.Text;
using Vector2 = VRageMath.Vector2;
using VRage.Utils;
using VRage.Library.Utils;

#endregion

namespace VRageRender
{
    public class MyDX9Render : IMyRender
    {
        public string RootDirectory { get { return MyRender.RootDirectory; } set { MyRender.RootDirectory = value; } }
        public string RootDirectoryEffects { get { return MyRender.RootDirectoryEffects; } set { MyRender.RootDirectoryEffects = value; } }
        public string RootDirectoryDebug { get { return MyRender.RootDirectoryDebug; } set { MyRender.RootDirectoryDebug = value; } }

        public MyRenderFont TryGetFont(int id)
        {
            return MyRender.TryGetFont(id);
        }

        public MyRenderSettings Settings { get { return MyRender.Settings; } }
        public MySharedData SharedData { get { return MyRender.SharedData; } }
        public MyTimeSpan CurrentDrawTime { 
            get { return MyRender.CurrentDrawTime; }
            set { MyRender.CurrentDrawTime = value; }
        }

        public MyDX9Render()
        {
            MyLog.Default.WriteLine("Creating Direct3D");
            // Temporary d3d object to hack Optimus and some ATI adapter reporting issues.
            new Direct3D().Dispose();
            MyLog.Default.WriteLine("Creating Direct3D - success");

            MyRender.m_d3d = new Direct3D();
        }

        public MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            return MyRender.CreateDevice(windowHandle, settingsToTry);
        }

        public void DisposeDevice()
        {
            MyRender.DisposeDevice();
        }

        public long GetAvailableTextureMemory()
        {
            return MyRender.GetAvailableTextureMemory();
        }

        public MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel()
        {
            return MyRender.TestDeviceCooperativeLevel();
        }

        public bool ResetDevice()
        {
            return MyRender.ResetDevice();
        }

        public void DrawBegin()
        {
            MyRender.Device.BeginScene();
        }

        public void DrawEnd()
        {
            MyRender.Device.EndScene();
        }

        public bool IsSupported
        {
            get { return true; } // We could possibly do the same check as Dx11, non-zero count of adapters or something.
        }

        public void Present()
        {
            try
            {
                MyRender.Device.Present();
            }
            catch (SharpDXException e)
            {
                if (e.ResultCode == ResultCode.DeviceLost.Result)
                    throw new MyDeviceLostException();
                throw;
            }
        }

        public void ClearBackbuffer(ColorBGRA clearColor)
        {
            MyRender.Device.Clear(ClearFlags.Target, clearColor, 1, 0);
        }

        public MyViewport MainViewport 
        {
            get
            {
                var viewport = MyRender.Device.Viewport;
                return new MyViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
            }
        }

        public Vector2I BackBufferResolution
        {
            get
            {
                return new Vector2I(MyRender.Parameters.BackBufferWidth, MyRender.Parameters.BackBufferHeight);
            }
        }

        public bool SettingsChanged(MyRenderDeviceSettings settings)
        {
            return MyRender.SettingsChanged(settings);
        }

        public void ApplySettings(MyRenderDeviceSettings settings)
        {
            MyRender.ApplySettings(settings);
        }

        public void LoadContent(MyRenderQualityEnum quality)
        {
            MyRender.LoadContent(quality);
        }

        public void UnloadContent()
        {
            MyRender.UnloadContent();
        }

        public void UnloadData()
        {
            MyRender.UnloadData();
        }

        public void ReloadContent(MyRenderQualityEnum quality)
        {
            MyRender.ReloadContent(quality);
        }

        public MyMessageQueue OutputQueue { get { return MyRender.OutputQueue; } }
        public uint GlobalMessageCounter { get { return MyRender.GlobalMessageCounter; } set { MyRender.GlobalMessageCounter = value; } }

        public void EnqueueMessage(IMyRenderMessage message, bool limitMaxQueueSize)
        {
            MyRender.EnqueueMessage(message, limitMaxQueueSize);
        }

        public void ProcessMessages()
        {
//            MyRender.processmes
        }

        public void EnqueueOutputMessage(IMyRenderMessage message)
        {
            MyRender.EnqueueOutputMessage(message);
        }

        public void ResetEnvironmentProbes()
        {
            MyEnvironmentMap.Reset();
        }

        public MyRenderProfiler GetRenderProfiler()
        {
            return MyRender.GetRenderProfiler();
        }

        public void Draw(bool draw = true)
        {
            MyRender.Draw(draw);
        }

        //Video
        public bool IsVideoValid(uint id)
        {
            return MyRender.IsVideoValid(id);
        }

        public VideoState GetVideoState(uint id)
        {
            return MyRender.GetVideoState(id);
        }

        public void HandleFocusMessage(MyWindowFocusMessage msg) { }
    }
}
