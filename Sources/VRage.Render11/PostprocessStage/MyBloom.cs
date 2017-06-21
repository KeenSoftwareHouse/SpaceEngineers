using SharpDX.Direct3D;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageMath;

namespace VRageRender
{
    class MyBloom : MyImmediateRC
    {
        const int MAX_GAUSSIAN_SAMPLES = 11;

        static ComputeShaderId m_bloomShader;
        static ComputeShaderId m_downscale2Shader;
        static ComputeShaderId m_downscale4Shader;
        static ComputeShaderId[] m_blurH;
        static ComputeShaderId[] m_blurV;

        const int m_numthreads = 8;

        internal static void Init()
        {
            var threadMacro = new[] { new ShaderMacro("NUMTHREADS", 8) };
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
            m_bloomShader = MyShaders.CreateCs("Postprocess/Bloom/Init.hlsl",threadMacro);
            m_downscale2Shader = MyShaders.CreateCs("Postprocess/Bloom/Downscale2.hlsl", threadMacro);
            m_downscale4Shader = MyShaders.CreateCs("Postprocess/Bloom/Downscale4.hlsl", threadMacro);
            m_blurH = new ComputeShaderId[MAX_GAUSSIAN_SAMPLES];
            m_blurV = new ComputeShaderId[MAX_GAUSSIAN_SAMPLES];
            for (int i = 0; i < MAX_GAUSSIAN_SAMPLES; i ++)
            {
                var macros = new[] {new ShaderMacro("HORIZONTAL", null), new ShaderMacro("NUMTHREADS", 8), 
                    new ShaderMacro("NUM_GAUSSIAN_SAMPLES", ((i % 2) > 0)? (i + 1) : i)};
                m_blurH[i] = MyShaders.CreateCs("Postprocess/Bloom/Blur.hlsl", macros);
                macros = new[] {new ShaderMacro("NUMTHREADS", 8), 
                    new ShaderMacro("NUM_GAUSSIAN_SAMPLES", ((i % 2) > 0)? (i + 1) : i)};
                m_blurV[i] = MyShaders.CreateCs("Postprocess/Bloom/Blur.hlsl", macros);
            }
        }

        internal static IConstantBuffer GetCB_blur(MyStereoRegion region, Vector2I uavSize)
        {
            int offX = 0;
            int maxX = uavSize.X - 1;
            if (region == MyStereoRegion.LEFT)
                maxX = uavSize.X / 2 - 1;
            else if (region == MyStereoRegion.RIGHT)
            {
                offX = uavSize.X / 2;
                maxX = uavSize.X / 2 - 1;
            }

            var buffer = MyCommon.GetObjectCB(16);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref offX);
            mapping.WriteAndPosition(ref maxX);
            var size = new Vector2(uavSize.X, uavSize.Y);
            mapping.WriteAndPosition(ref size);
            mapping.Unmap();
            return buffer;
        }
        
        internal static IConstantBuffer GetCBSize(float width, float height)
        {
            var buffer = MyCommon.GetObjectCB(8);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref width);
            mapping.WriteAndPosition(ref height);
            mapping.Unmap();
            return buffer;
        }

        const int BLOOM_TARGET_SIZE_DIVIDER = 4;

        // IMPORTANT: The returned object needs to be returned to MyManagers.RwTexturePool after the usage
        internal static IBorrowedUavTexture Run(ISrvBindable src, ISrvBindable srcGBuffer2, ISrvBindable srcDepth)
        {
            RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.ComputeShader.SetSampler(0, MySamplerStateManager.Default);

            int screenX = MyRender11.ResolutionI.X;
            int screenY = MyRender11.ResolutionI.Y;
            Format formatLBuffer = MyGBuffer.LBufferFormat;
            MyBorrowedRwTextureManager rwTexturePool = MyManagers.RwTexturesPool;
            IBorrowedUavTexture uavHalfScreen = rwTexturePool.BorrowUav("MyBloom.UavHalfScreen", screenX / 2, screenY / 2, formatLBuffer);
            IBorrowedUavTexture uavBlurScreen = rwTexturePool.BorrowUav("MyBloom.UavBlurScreen", screenX / BLOOM_TARGET_SIZE_DIVIDER, screenY / BLOOM_TARGET_SIZE_DIVIDER, formatLBuffer);
            IBorrowedUavTexture uavBlurScreenHelper = rwTexturePool.BorrowUav("MyBloom.UavBlurScreenHelper", screenX / BLOOM_TARGET_SIZE_DIVIDER, screenY / BLOOM_TARGET_SIZE_DIVIDER, formatLBuffer);
            RC.ComputeShader.SetUav(0, uavHalfScreen);
            RC.ComputeShader.SetSrv(0, src);
            RC.ComputeShader.SetSrv(1, srcGBuffer2);
            RC.ComputeShader.SetSrv(2, srcDepth);

            RC.ComputeShader.Set(m_bloomShader);

            var size = uavHalfScreen.Size;
            VRageMath.Vector2I threadGroups = new VRageMath.Vector2I((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads);
            RC.Dispatch(threadGroups.X, threadGroups.Y, 1);

            bool skipDownScale = false;
            switch (BLOOM_TARGET_SIZE_DIVIDER)
            {
                case 2:
                    skipDownScale = true;
                    break;
                case 4:
                    RC.ComputeShader.Set(m_downscale2Shader);
                    break;
                case 8:
                    RC.ComputeShader.Set(m_downscale4Shader);
                    break;
                default:
                    MyRenderProxy.Assert(false, "Invalid bloom target size divider");
                    break;
            }
            size = uavBlurScreen.Size;
            threadGroups = new VRageMath.Vector2I((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads);
            if (!skipDownScale)
            {
                RC.ComputeShader.SetConstantBuffer(1, GetCBSize(uavHalfScreen.Size.X, uavHalfScreen.Size.Y));

                RC.ComputeShader.SetUav(0, uavBlurScreen);
                RC.ComputeShader.SetSrv(0, uavHalfScreen);
                RC.Dispatch(threadGroups.X, threadGroups.Y, 1);
            }

            RC.ComputeShader.SetConstantBuffer(1, GetCB_blur(MyStereoRegion.FULLSCREEN, size));
            RC.ComputeShader.Set(m_blurV[MyRender11.Postprocess.BloomSize]);
            RC.ComputeShader.SetUav(0, uavBlurScreenHelper);
            RC.ComputeShader.SetSrv(0, uavBlurScreen); 
            RC.Dispatch(threadGroups.X, threadGroups.Y, 1);
            RC.ComputeShader.SetSrv(0, null);
            RC.ComputeShader.SetUav(0, null);

            RC.ComputeShader.Set(m_blurH[MyRender11.Postprocess.BloomSize]);
            RC.ComputeShader.SetUav(0, uavBlurScreen);
            RC.ComputeShader.SetSrv(0, uavBlurScreenHelper);

            int nPasses = 1;
            if (MyStereoRender.Enable)
            {
                threadGroups.X /= 2;
                nPasses = 2;
            }
            for (int nPass = 0; nPass < nPasses; nPass++)
            {
                MyStereoRegion region = MyStereoRegion.FULLSCREEN;
                if (MyStereoRender.Enable)
                    region = nPass == 0 ? MyStereoRegion.LEFT : MyStereoRegion.RIGHT;

                RC.ComputeShader.SetConstantBuffer(1, GetCB_blur(region, size));
                RC.Dispatch(threadGroups.X, threadGroups.Y, 1);
            }            
            
            if (MyRender11.Settings.DisplayBloomFilter)
                MyDebugTextureDisplay.Select(uavHalfScreen);
            else if (MyRender11.Settings.DisplayBloomMin)
                MyDebugTextureDisplay.Select(uavBlurScreen);

            RC.ComputeShader.SetUav(0, null);
            RC.ComputeShader.SetSrv(0, null);
            uavHalfScreen.Release();
            uavBlurScreenHelper.Release();

            return uavBlurScreen;
        }

    }
}
