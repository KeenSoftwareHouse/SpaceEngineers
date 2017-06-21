using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    class MyLuminanceAverage : MyImmediateRC
    {
        static ComputeShaderId m_initialShader;
        static ComputeShaderId m_sumShader;
        static ComputeShaderId m_finalShader;
        static ComputeShaderId m_skipShader;

        static IUavTexture m_prevLum;

        const int NUM_THREADS = 8;

        internal static void Init()
        {
            var threadMacros = new[] { new ShaderMacro("NUMTHREADS", NUM_THREADS) };
            m_initialShader = MyShaders.CreateCs("Postprocess/LuminanceReduction/Init.hlsl", threadMacros);
            m_sumShader = MyShaders.CreateCs("Postprocess/LuminanceReduction/Sum.hlsl", threadMacros);

            threadMacros = new[] { new ShaderMacro("NUMTHREADS", NUM_THREADS), new ShaderMacro("_FINAL", null) };
            m_finalShader = MyShaders.CreateCs("Postprocess/LuminanceReduction/Sum.hlsl", threadMacros);

            m_skipShader = MyShaders.CreateCs("Postprocess/LuminanceReduction/Skip.hlsl");

            m_prevLum = MyManagers.RwTextures.CreateUav("MyLuminanceAverage.PrevLum", 1, 1, Format.R32G32_Float);
        }

        internal static void Reset()
        {
            MyRender11.RC.ClearUav(m_prevLum, Int4.Zero);
        }

        internal static IBorrowedUavTexture Skip()
        {
            IBorrowedUavTexture borrowedUav = MyManagers.RwTexturesPool.BorrowUav("MyLuminanceAverage.Skip", Format.R32G32_Float);
            RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.ComputeShader.SetUav(0, borrowedUav);
            RC.ComputeShader.Set(m_skipShader);
            RC.Dispatch(1, 1, 1);
            RC.ComputeShader.SetUav(0, null);
            //luminance_reduction_skip
            return borrowedUav;
        }

        internal static IBorrowedUavTexture Run(ISrvBindable src)
        {
            IBorrowedUavTexture uav0 = MyManagers.RwTexturesPool.BorrowUav("MyLuminanceAverage.Uav0", Format.R32G32_Float);
            IBorrowedUavTexture uav1 = MyManagers.RwTexturesPool.BorrowUav("MyLuminanceAverage.Uav1", Format.R32G32_Float);

            Vector2I size = src.Size;
            int texelsNum = size.X * size.Y;
            uint sizeX = (uint)size.X;
            uint sizeY = (uint)size.Y;
            var buffer = MyCommon.GetObjectCB(16);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref sizeX);
            mapping.WriteAndPosition(ref sizeY);
            mapping.WriteAndPosition(ref texelsNum);
            mapping.Unmap();

            RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.ComputeShader.SetConstantBuffer(1, MyCommon.GetObjectCB(16));

            RC.ComputeShader.Set(m_initialShader);

            IBorrowedUavTexture output = uav0;
            ISrvBindable inputSrc = src;
            RC.ComputeShader.SetUav(0, output);
            RC.ComputeShader.SetSrv(0, inputSrc);

            int threadGroupCountX = ComputeGroupCount(size.X);
            int threadGroupCountY = ComputeGroupCount(size.Y);
            RC.Dispatch(threadGroupCountX, threadGroupCountY, 1);
            RC.ComputeShader.Set(m_sumShader);

            IBorrowedUavTexture input;
            int i = 0;
            while (true)
            {
                size.X = threadGroupCountX;
                size.Y = threadGroupCountY;

                if (size.X <= NUM_THREADS && size.Y <= NUM_THREADS)
                    break;

                output = (i % 2 == 0) ? uav1 : uav0;
                input = (i % 2 == 0) ? uav0 : uav1;

                RC.ComputeShader.SetSrv(0, null);
                RC.ComputeShader.SetUav(0, output);
                RC.ComputeShader.SetSrv(0, input);

                threadGroupCountX = ComputeGroupCount(size.X);
                threadGroupCountY = ComputeGroupCount(size.Y);
                RC.Dispatch(threadGroupCountX, threadGroupCountY, 1);

                i++;
            }

            RC.ComputeShader.Set(m_finalShader);

            output = (i % 2 == 0) ? uav1 : uav0;
            input = (i % 2 == 0) ? uav0 : uav1;

            RC.ComputeShader.SetSrv(0, null);
            RC.ComputeShader.SetUav(0, output);
            RC.ComputeShader.SetSrvs(0, input, m_prevLum);

            threadGroupCountX = ComputeGroupCount(size.X);
            threadGroupCountY = ComputeGroupCount(size.Y);
            RC.Dispatch(threadGroupCountX, threadGroupCountY, 1);

            RC.ComputeShader.Set(null);
            RC.ComputeShader.SetUav(0, null);

            // Backup the result for later process
            RC.CopySubresourceRegion(output, 0, new ResourceRegion(0, 0, 0, 1, 1, 1), m_prevLum, 0);

            input.Release();
            return output;
        }

        private static int ComputeGroupCount(int dim)
        {
            return (dim + NUM_THREADS - 1) / NUM_THREADS;
        }
    }
}
