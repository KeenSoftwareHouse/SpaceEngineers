using SharpDX.Direct3D;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Render11.Resources.Internal;

namespace VRageRender
{
    class MyToneMapping : MyImmediateRC
    {
        static ComputeShaderId m_cs;
        static ComputeShaderId m_csSkip;

        const int m_numthreads = 8;

        internal static void Init()
        {
            m_cs = MyShaders.CreateCs("Postprocess/Tonemapping/Main.hlsl", new[] { new ShaderMacro("NUMTHREADS", 8) });
            m_csSkip = MyShaders.CreateCs("Postprocess/Tonemapping/Main.hlsl", new[] { new ShaderMacro("NUMTHREADS", 8), new ShaderMacro("DISABLE_TONEMAPPING", null) });
        }

        internal static IBorrowedUavTexture Run(ISrvBindable src, ISrvBindable avgLum, ISrvBindable bloom, bool enableTonemapping = true)
        {
            IBorrowedUavTexture dst;
            if (MyRender11.FxaaEnabled)
                dst = MyManagers.RwTexturesPool.BorrowUav("DrawGameScene.Tonemapped", Format.R8G8B8A8_UNorm);
            else
                dst = MyManagers.RwTexturesPool.BorrowUav("DrawGameScene.Tonemapped", Format.R10G10B10A2_UNorm);

            RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.ComputeShader.SetConstantBuffer(1, MyCommon.GetObjectCB(16));

            RC.ComputeShader.SetUav(0, dst);
            RC.ComputeShader.SetSrvs(0, src, avgLum, bloom);

            RC.ComputeShader.SetSampler(0, MySamplerStateManager.Default);
            RC.ComputeShader.SetSampler(1, MySamplerStateManager.Point);
            RC.ComputeShader.SetSampler(2, MySamplerStateManager.Default);

            if (enableTonemapping)
            {
                RC.ComputeShader.Set(m_cs);
            }
            else
            {
                RC.ComputeShader.Set(m_csSkip);
            }

            var size = dst.Size;
            RC.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.ComputeShader.SetUav(0, null);
            RC.ComputeShader.Set(null);

            return dst;
        }
    }
}
