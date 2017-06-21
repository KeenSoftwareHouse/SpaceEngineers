using System;
using SharpDX.Direct3D11;
using VRage.Render11.Resources;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VRage.Render11.RenderContext
{
    enum MyGBufferSrvFilter
    {
        ALL,
        NO_STENCIL
    }

    class MyCommonStage
    {
        protected DeviceContext m_deviceContext;
        protected CommonShaderStage m_shaderStage;
        protected MyRenderContextStatistics m_statistics;

        IConstantBuffer[] m_constantBuffers = new IConstantBuffer[8];
        SamplerState[] m_samplers = new SamplerState[16];
        ShaderResourceView[] m_srvs = new ShaderResourceView[32];

        [ThreadStatic]
        static SamplerState[] m_tmpSamplers;
        [ThreadStatic]
        static ShaderResourceView[] m_tmpSrvs;

        internal void Init(DeviceContext context, CommonShaderStage shaderStage, MyRenderContextStatistics statistics)
        {
            m_deviceContext = context;
            m_shaderStage = shaderStage;
            m_statistics = statistics;
        }

        internal virtual void ClearState()
        {
            for (int i = 0; i < m_constantBuffers.Length; i++)
                m_constantBuffers[i] = null;

            for (int i = 0; i < m_samplers.Length; i++)
                m_samplers[i] = null;

            for (int i = 0; i < m_srvs.Length; i++)
                m_srvs[i] = null;
        }

        internal void SetConstantBuffer(int slot, IConstantBuffer constantBuffer)
        {
            Buffer buffer = null;
            if (constantBuffer != null)
                buffer = constantBuffer.Buffer;

            if (constantBuffer == m_constantBuffers[slot])
                return;
            m_constantBuffers[slot] = constantBuffer;
            m_shaderStage.SetConstantBuffer(slot, buffer);
            m_statistics.SetConstantBuffers++;
        }

        internal void SetSampler(int slot, ISamplerState sampler)
        {
            SamplerState dxObject = null;
            if (sampler != null)
            {
                ISamplerStateInternal samplerInternal = (ISamplerStateInternal) sampler;
                dxObject = samplerInternal.Resource;
            }

            if (dxObject == m_samplers[slot])
                return;
            m_samplers[slot] = dxObject;
            m_shaderStage.SetSampler(slot, dxObject);
            m_statistics.SetSamplers++;
        }

        internal void SetSamplers(int startSlot, params ISamplerState[] samplers)
        {
            if (m_tmpSamplers == null)
                m_tmpSamplers = new SamplerState[6];

            for (int i = 0; i < samplers.Length; i++)
            {
                ISamplerState sampler = samplers[i];

                SamplerState dxObject = null;
                if (sampler != null)
                {
                    ISamplerStateInternal samplerInternal = (ISamplerStateInternal) sampler;
                    dxObject = samplerInternal.Resource;
                }

                int slot = startSlot + i;
                if (dxObject == m_samplers[slot])
                    continue;
                m_samplers[slot] = dxObject;
                m_tmpSamplers[i] = dxObject;
            }

            m_shaderStage.SetSamplers(startSlot, samplers.Length, m_tmpSamplers);
            m_statistics.SetSamplers++;
        }

        internal void SetSrv(int slot, ISrvBindable srvBind)
        {
            ShaderResourceView srv = null;
            if (srvBind != null)
                srv = srvBind.Srv;

            if (srv == m_srvs[slot])
                return;
            m_srvs[slot] = srv;
            m_shaderStage.SetShaderResource(slot, srv);
            m_statistics.SetSrvs++;
        }

        internal void SetSrvs(int startSlot, params ISrvBindable[] srvs)
        {
            if (m_tmpSrvs == null)
                m_tmpSrvs = new ShaderResourceView[6];

            bool isChanging = false;
            for (int i = 0; i < srvs.Length; i++)
            {
                ShaderResourceView srv = null;
                if (srvs[i] != null)
                    srv = srvs[i].Srv;

                int slot = startSlot + i;
                if (srv != m_srvs[slot])
                    isChanging = true;

                m_srvs[slot] = srv;
                m_tmpSrvs[i] = srv;
            }

            if (isChanging)
            {
                m_shaderStage.SetShaderResources(startSlot, srvs.Length, m_tmpSrvs);
                m_statistics.SetSrvs++;
            }
        }

        internal void SetSrvs(int startSlot, MyGBuffer gbuffer, MyGBufferSrvFilter mode = MyGBufferSrvFilter.ALL)
        {
            ISrvBindable srvStencil = null;
            if (mode == MyGBufferSrvFilter.ALL)
                srvStencil = gbuffer.DepthStencil.SrvStencil;

            SetSrvs(0,
                gbuffer.DepthStencil.SrvDepth,
                gbuffer.GBuffer0,
                gbuffer.GBuffer1,
                gbuffer.GBuffer2,
                srvStencil);
        }

        internal void ResetSrvs(int startSlot, MyGBufferSrvFilter mode)
        {
            if (mode == MyGBufferSrvFilter.ALL)
            {
                SetSrvs(startSlot,
                    null,
                    null,
                    null,
                    null,
                    null);
            }
            else
            {
                SetSrvs(startSlot,
                    null,
                    null,
                    null,
                    null);
            }
        }
    }

    internal class MyVertexStage: MyCommonStage
    {
        VertexShader m_vertexShader;

        internal override void ClearState()
        {
            base.ClearState();
            m_vertexShader = null;
        }

        internal void Set(VertexShader shader)
        {
            if (shader == m_vertexShader)
                return;
            m_vertexShader = shader;
            m_deviceContext.VertexShader.Set(shader);
            m_statistics.SetVertexShaders++;
        }
    }

    internal class MyGeometryStage: MyCommonStage
    {
        GeometryShader m_geometryShader;

        internal override void ClearState()
        {
            base.ClearState();

            m_geometryShader = null;
        }

        internal void Set(GeometryShader shader)
        {
            if (shader == m_geometryShader)
                return;
            m_geometryShader = shader;
            m_deviceContext.GeometryShader.Set(shader);
            m_statistics.SetGeometryShaders++;
        }
    }
    
    internal class MyPixelStage: MyCommonStage
    {
        PixelShader m_pixelShader;

        internal override void ClearState()
        {
            base.ClearState();

            m_pixelShader = null;
        }
        
        public void Set(PixelShader shader)
        {
            if (shader == m_pixelShader)
                return;
            m_pixelShader = shader;
            m_deviceContext.PixelShader.Set(shader);
            m_statistics.SetPixelShaders++;
        }
    }

    internal class MyComputeStage: MyCommonStage
    {
        ComputeShader m_computeShader;
        UnorderedAccessView[] m_uavs = new UnorderedAccessView[4];
        int[] m_uavsInitialCount = new int[4];

        internal override void ClearState()
        {
            base.ClearState();
            m_computeShader = null;
        }

        internal void Set(ComputeShader shader)
        {
            if (shader == m_computeShader)
                return;
            m_computeShader = shader;
            m_deviceContext.ComputeShader.Set(shader);
            m_statistics.SetComputeShaders++;
        }

        internal void SetUav(int slot, IUavBindable uavBindable)
        {
            UnorderedAccessView uav = null;
            if (uavBindable != null)
                uav = uavBindable.Uav;

            if (uav == m_uavs[slot])
                return;
            m_uavs[slot] = uav;
            m_deviceContext.ComputeShader.SetUnorderedAccessView(slot, uav);
            m_statistics.SetUavs++;
        }

        internal void SetUav(int slot, IUavBindable uavBindable, int uavInitialCount)
        {
            UnorderedAccessView uav = null;
            if (uavBindable != null)
                uav = uavBindable.Uav;

            if (uav == m_uavs[slot] && uavInitialCount == m_uavsInitialCount[slot])
                return;
            m_uavs[slot] = uav;
            m_uavsInitialCount[slot] = uavInitialCount;
            m_deviceContext.ComputeShader.SetUnorderedAccessView(slot, uav, uavInitialCount); 
            m_statistics.SetUavs++;
        }

        internal void SetUavs(int startSlot, params IUavBindable[] uavs)
        {
            for (int i = 0; i < uavs.Length; i++)
            {
                UnorderedAccessView uav = null;
                if (uavs[i] != null)
                    uav = uavs[i].Uav;

                int slot = startSlot + i;
                if (uav == m_uavs[slot])
                    continue;

                m_uavs[slot] = uav;
                m_deviceContext.ComputeShader.SetUnorderedAccessView(startSlot, uav);
                m_statistics.SetUavs++;
            }
        }
    }


    // TODO: temporal object, should be removed due to the optimizing and replaced by specifig stage:
    internal class MyAllShaderStages
    {
        MyVertexStage m_vertexStage;
        MyGeometryStage m_geometryStage;
        MyPixelStage m_pixelStage;
        MyComputeStage m_computeStage;

        internal MyAllShaderStages(MyVertexStage vertexStage, MyGeometryStage geometryStage, MyPixelStage pixelStage, MyComputeStage computeStage)
        {
            m_vertexStage = vertexStage;
            m_geometryStage = geometryStage;
            m_pixelStage = pixelStage;
            m_computeStage = computeStage;
        }

        internal void SetConstantBuffer(int slot, IConstantBuffer constantBuffer)
        {
            m_vertexStage.SetConstantBuffer(slot, constantBuffer);
            m_geometryStage.SetConstantBuffer(slot, constantBuffer);
            m_pixelStage.SetConstantBuffer(slot, constantBuffer);
            m_computeStage.SetConstantBuffer(slot, constantBuffer);
        }

        internal void SetSrv(int slot, ISrvBindable srv)
        {
            m_vertexStage.SetSrv(slot, srv);
            m_pixelStage.SetSrv(slot, srv);
            m_computeStage.SetSrv(slot, srv);
        }
    }
}
