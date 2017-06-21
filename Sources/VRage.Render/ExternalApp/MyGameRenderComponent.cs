using System;
using VRage.Library.Utils;

namespace VRageRender.ExternalApp
{
    public class MyGameRenderComponent : IDisposable
    {
        public MyRenderThread RenderThread { get; private set; }

        /// <summary>
        /// Creates and starts render thread
        /// </summary>
        public void Start(MyGameTimer timer, InitHandler windowInitializer, MyRenderDeviceSettings? settingsToTry, MyRenderQualityEnum renderQuality, float maxFrameRate)
        {
            RenderThread = MyRenderThread.Start(timer, windowInitializer, settingsToTry, renderQuality, maxFrameRate);
        }

        /// <summary>
        /// Stops and clears render thread
        /// </summary>
        public void Stop()
        {
            RenderThread.Exit();
            RenderThread = null;
        }

        public void StartSync(MyGameTimer timer, IMyRenderWindow window, MyRenderDeviceSettings? settings, MyRenderQualityEnum renderQuality, float maxFrameRate)
        {
            RenderThread = MyRenderThread.StartSync(timer, window, settings, renderQuality, maxFrameRate);
        }

        public void Dispose()
        {
            if (RenderThread != null)
            {
                Stop();
            }

            MyRenderProxy.DisposeDevice();
        }
    }
}
