using SharpDX;
using System;
using VRage.Library.Utils;
using VRage.Profiler;
using VRageMath;
using VRageRender.Profiler;
using VRage.Utils;
using VRageRender.Messages;

namespace VRageRender
{
    /// <summary>
    /// Describes the state of a video player
    /// </summary>
    public enum VideoState
    {
        Playing,
        Paused,
        Stopped
    }

    public enum MyRenderDeviceCooperativeLevel
    {
        Ok,
        Lost,
        NotReset,
        DriverError
    }

    public class MyDeviceErrorException : Exception
    {
        public string Message;

        public MyDeviceErrorException(string message)
        {
            Message = message;
        }
    }

    public delegate void OnShaderCacheProgressDelegate(float percents, string file, string profile, string vertexLayout, string macros, string message, bool importantMessage);

    public interface IMyRender
    {
        /// <summary>
        /// Must be possible to query during startup before render thread and window is created.
        /// </summary>
        bool IsSupported { get; }

        string RootDirectory { get; set; }
        string RootDirectoryEffects { get; set; }
        string RootDirectoryDebug { get; set; }

        //MyRenderFont TryGetFont(int id);    
        MySharedData SharedData { get; }
        MyTimeSpan CurrentDrawTime { set; get; }

        MyLog Log { get; }
        
        MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry);
        void DisposeDevice();
        long GetAvailableTextureMemory();
        MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel();
        bool ResetDevice();

        void DrawBegin();
        void DrawEnd();

        bool SettingsChanged(MyRenderDeviceSettings settings);
        void ApplySettings(MyRenderDeviceSettings settings);

        void Present();
        MyViewport MainViewport { get; }
        Vector2I BackBufferResolution { get; }

        void LoadContent(global::VRageRender.MyRenderQualityEnum quality);
        void UnloadContent();
        void UnloadData();
        void ReloadContent(MyRenderQualityEnum quality);

        MyMessageQueue OutputQueue { get; }
        uint GlobalMessageCounter { get; set; }
        void EnqueueMessage(MyRenderMessageBase message, bool limitMaxQueueSize);
        void ProcessMessages();
        void EnqueueOutputMessage(MyRenderMessageBase message);

        void ResetEnvironmentProbes();

        MyRenderProfiler GetRenderProfiler();
        void Draw(bool draw = true);
       
        //Video
        bool IsVideoValid(uint id);
        VideoState GetVideoState(uint id);

        void HandleFocusMessage(MyWindowFocusMessage msg);

        void GenerateShaderCache(bool clean, OnShaderCacheProgressDelegate onShaderCacheProgress);
    }
}
