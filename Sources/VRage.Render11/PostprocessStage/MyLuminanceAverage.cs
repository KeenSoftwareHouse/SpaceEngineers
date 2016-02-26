using System.Diagnostics;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace VRageRender
{
    class MyLuminanceAverage : MyImmediateRC
    {
        static ComputeShaderId m_initialShader;
        static ComputeShaderId m_sumShader;
        static ComputeShaderId m_finalShader;

        const int m_numthreads = 8;

        internal static int NumThreads { get { return m_numthreads; } }

        internal static void Init()
        {
            var threadMacro = new[] {new ShaderMacro("NUMTHREADS", 8)};
            m_initialShader = MyShaders.CreateCs("luminance_reduction_init.hlsl", threadMacro);
            m_sumShader = MyShaders.CreateCs("luminance_reduction.hlsl", threadMacro);
            m_finalShader = MyShaders.CreateCs("luminance_reduction.hlsl", new[] { new ShaderMacro("NUMTHREADS", 8), new ShaderMacro("_FINAL", null) });
        }

        internal static MyBindableResource Run(MyBindableResource uav0, MyBindableResource uav1, MyBindableResource src,
            MyBindableResource prevLum)
        {
            var size = src.GetSize();
            var texelsNum = size.X * size.Y;
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

            RC.BindUAV(0, uav0);
            RC.BindSRV(0, src);
            RC.SetCS(m_initialShader);

            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(m_sumShader);
            int i = 0;
            while (true)
            {
                size.X = (size.X + m_numthreads - 1) / m_numthreads;
                size.Y = (size.Y + m_numthreads - 1) / m_numthreads;

                if (size.X <= 8 && size.Y <= 8)
                    break;

                //mapping = MyMapping.MapDiscard(MyCommon.GetObjectBuffer(16).Buffer);
                //mapping.stream.Write(new Vector2I(size.X, size.Y));
                //mapping.Unmap();

                RC.BindUAV(0, (i % 2 == 0) ? uav1 : uav0);
                RC.BindSRV(0, (i % 2 == 0) ? uav0 : uav1);

                RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

                //might not be exactly correct if we skip this
                var dirty = (i % 2 == 0) ? uav0 : uav1;
                RC.DeviceContext.ClearUnorderedAccessView((dirty as IUnorderedAccessBindable).UAV, new SharpDX.Int4(0, 0, 0, 0));

                i++;
            }

            RC.SetCS(m_finalShader);

            //mapping = MyMapping.MapDiscard(MyCommon.GetObjectBuffer(16).Buffer);
            //mapping.stream.Write(new Vector2I(size.X, size.Y));
            //mapping.stream.Write(texelsNum);
            //mapping.Unmap();

            RC.BindUAV(0, (i % 2 == 0) ? uav1 : uav0);
            RC.BindSRVs(0, (i % 2 == 0) ? uav0 : uav1, prevLum);

            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(null);

            var output = (i % 2 == 0) ? uav1 : uav0;

            RC.DeviceContext.CopySubresourceRegion(output.m_resource, 0, new ResourceRegion(0, 0, 0, 1, 1, 1), prevLum.m_resource, 0);

            return output;
        }
    }
}
