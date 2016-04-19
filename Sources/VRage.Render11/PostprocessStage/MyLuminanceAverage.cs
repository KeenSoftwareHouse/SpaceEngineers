using System.Diagnostics;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageMath;

namespace VRageRender
{
    class MyLuminanceAverage : MyImmediateRC
    {
        static ComputeShaderId m_initialShader;
        static ComputeShaderId m_sumShader;
        static ComputeShaderId m_finalShader;

        const int NUM_THREADS = 8;

        internal static void Init()
        {
            var threadMacros = new[] { new ShaderMacro("NUMTHREADS", NUM_THREADS) };
            m_initialShader = MyShaders.CreateCs("luminance_reduction_init.hlsl", threadMacros);
            m_sumShader = MyShaders.CreateCs("luminance_reduction.hlsl", threadMacros);

            threadMacros = new[] { new ShaderMacro("NUMTHREADS", NUM_THREADS), new ShaderMacro("_FINAL", null) };
            m_finalShader = MyShaders.CreateCs("luminance_reduction.hlsl", threadMacros);
        }

        internal static MyBindableResource Run(MyBindableResource uav0, MyBindableResource uav1, MyBindableResource src,
            MyBindableResource prevLum)
        {
            Vector3I size = src.GetSize();
            int texelsNum = size.X * size.Y;
            uint sizeX = (uint)size.X;
            uint sizeY = (uint)size.Y;
            float adaptationFactor = MyRender11.Postprocess.EnableEyeAdaptation ? -1.0f : MyRender11.Postprocess.ConstantLuminance;
            var buffer = MyCommon.GetObjectCB(16);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref sizeX);
            mapping.WriteAndPosition(ref sizeY);
            mapping.WriteAndPosition(ref texelsNum);
            mapping.WriteAndPosition(ref adaptationFactor);
            mapping.Unmap();

            RC.CSSetCB(0, MyCommon.FrameConstants);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            RC.SetCS(m_initialShader);

            MyBindableResource output = uav0;
            MyBindableResource input = src;
            RC.BindUAV(0, output);
            RC.BindSRV(0, input);

            int threadGroupCountX = ComputeGroupCount(size.X);
            int threadGroupCountY = ComputeGroupCount(size.Y);
            RC.DeviceContext.Dispatch(threadGroupCountX, threadGroupCountY, 1);

            RC.SetCS(m_sumShader);

            int i = 0;
            while (true)
            {
                size.X = threadGroupCountX;
                size.Y = threadGroupCountY;

                if (size.X <= NUM_THREADS && size.Y <= NUM_THREADS)
                    break;

                output = (i % 2 == 0) ? uav1 : uav0;
                input = (i % 2 == 0) ? uav0 : uav1;
                RC.BindUAV(0, output);
                RC.BindSRV(0, input);

                threadGroupCountX = ComputeGroupCount(size.X);
                threadGroupCountY = ComputeGroupCount(size.Y);
                RC.DeviceContext.Dispatch(threadGroupCountX, threadGroupCountY, 1);

                i++;
            }

            RC.SetCS(m_finalShader);

            output = (i % 2 == 0) ? uav1 : uav0;
            input = (i % 2 == 0) ? uav0 : uav1;
            RC.BindUAV(0, output);
            RC.BindSRVs(0, input, prevLum);

            threadGroupCountX = ComputeGroupCount(size.X);
            threadGroupCountY = ComputeGroupCount(size.Y);
            RC.DeviceContext.Dispatch(threadGroupCountX, threadGroupCountY, 1);

            RC.SetCS(null);

            // Backup the result for later process
            RC.DeviceContext.CopySubresourceRegion(output.m_resource, 0, new ResourceRegion(0, 0, 0, 1, 1, 1), prevLum.m_resource, 0);

            return output;
        }

        private static int ComputeGroupCount(int dim)
        {
            return (dim + NUM_THREADS - 1) / NUM_THREADS;
        }
    }
}
