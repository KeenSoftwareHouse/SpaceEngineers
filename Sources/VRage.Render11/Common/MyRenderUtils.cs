using System;
using System.Collections.Generic;
using System.Text;
using SharpDX.Direct3D11;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Tools;
using VRage.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VRageRender
{
    static class MyRenderUtils
    {
        public static void BindShaderBundle(MyRenderContext rc, MyMaterialShadersBundleId id)
        {
            rc.SetInputLayout(id.IL);
            rc.VertexShader.Set(id.VS);
            rc.PixelShader.Set(id.PS);
        }

        public static unsafe void MoveConstants(MyRenderContext rc, ref MyConstantsPack desc)
        {
            if (desc.CB == null)
                return;
            // IMPORTANT: It is optimisation but that has not been very well implemented, it is not considered multithread approach, usage of buffers in multiple passes, 
            // reset device and and refillage of the buffers. More complex task to do it properly
            //if (m_staticData.m_constantsVersion.Get(desc.CB) != desc.Version)
            //{
            //    m_staticData.m_constantsVersion[desc.CB] = desc.Version;

                var mapping = MyMapping.MapDiscard(rc, desc.CB);
                mapping.WriteAndPosition(desc.Data, desc.Data.Length);
                mapping.Unmap();
            //}
            //MyRender11.ProcessDebugOutput();
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        public static void SetConstants(MyRenderContext rc, ref MyConstantsPack desc, int slot)
        {
            if ((desc.BindFlag & MyBindFlag.BIND_VS) > 0)
            {
                rc.VertexShader.SetConstantBuffer(slot, desc.CB);
            }
            if ((desc.BindFlag & MyBindFlag.BIND_PS) > 0)
            {
                rc.PixelShader.SetConstantBuffer(slot, desc.CB);
            }
            MyRender11.ProcessDebugOutput();
        }

        internal static void SetSrvs(MyRenderContext rc, ref MySrvTable desc)
        {
            if ((desc.BindFlag & MyBindFlag.BIND_VS) > 0)
            {
                for (int i = 0; i < desc.Srvs.Length; i++)
                    rc.VertexShader.SetSrv(desc.StartSlot + i, desc.Srvs[i]);
            }
            if ((desc.BindFlag & MyBindFlag.BIND_PS) > 0)
            {
                for (int i = 0; i < desc.Srvs.Length; i++)
                    rc.PixelShader.SetSrv(desc.StartSlot + i, desc.Srvs[i]);
            }
            MyRender11.ProcessDebugOutput();
        }

        internal static CommandList JoinAndGetCommandList(MyRenderContext rc)
        {
            MyGpuProfiler.Join(rc.ProfilingQueries);
            return rc.FinishCommandList(false);
        }
    }
}
