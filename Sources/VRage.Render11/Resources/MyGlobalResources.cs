using SharpDX.DXGI;
using VRage.Render11.Common;
using VRageRender;

namespace VRage.Render11.Resources
{
    class MyGlobalResources: IManager
    {
        public static IRtvTexture Gbuffer1Copy;

        public void Create()
        {
            int width = MyRender11.ResolutionI.X;
            int height = MyRender11.ResolutionI.Y;
            int samples = MyRender11.RenderSettings.AntialiasingMode.SamplesCount();

            Gbuffer1Copy = MyManagers.RwTextures.CreateRtv("MyRender11.Gbuffer1Copy", width, height, Format.R10G10B10A2_UNorm, samples);
        }

        public void Destroy()
        {
            MyManagers.RwTextures.DisposeTex(ref Gbuffer1Copy);
        }
    }
}
