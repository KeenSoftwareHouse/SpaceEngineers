using Color4 = SharpDX.Color4;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;
using VRageRender.Resources;

namespace VRageRender
{
    internal class MyShadowCascadesPostProcess
    {
        private static VertexShaderId m_markVS = VertexShaderId.NULL;
        private static PixelShaderId m_markPS = PixelShaderId.NULL;
        private static InputLayoutId m_inputLayout = InputLayoutId.NULL;

        private ComputeShaderId m_gatherCS = ComputeShaderId.NULL;
        private static PixelShaderId m_combinePS = PixelShaderId.NULL;

        private VertexBufferId m_cascadesBoundingsVertices = VertexBufferId.NULL;
        private IndexBufferId m_cubeIB = IndexBufferId.NULL;

        private static ConstantsBufferId m_inverseConstants = ConstantsBufferId.NULL;

        private const int m_pixelsPerThreadX = 1;
        private const int m_pixelsPerThreadY = 1;
        private int m_threadGroupCountX = 32;
        private int m_threadGroupCountY = 32;

        internal MyShadowCascadesPostProcess(int numberOfCascades)
        {
            Init(numberOfCascades);
        }

        private void Init(int numberOfCascades)
        {
            InitResources(numberOfCascades);
            InitShaders();
        }

        internal void Reset(int numberOfCascades)
        {
            UnloadResources();
            Init(numberOfCascades);
        }

        private void InitResources(int numberOfCascades)
        {
            InitVertexBuffer(numberOfCascades);
            InitIndexBuffer();
            InitConstantBuffer();
        }

        internal void UnloadResources()
        {
            DestroyVertexBuffer();
            DestroyIndexBuffer();
        }

        private unsafe void InitConstantBuffer()
        {
            if(m_inverseConstants == ConstantsBufferId.NULL)
                m_inverseConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix));
        }

        Vector2I GetThreadsPerThreadGroup(Vector2I textureSizeInPixels)
        {
            int pixelsPerThreadGroupX = textureSizeInPixels.X / m_threadGroupCountX;
            int pixelsPerThreadGroupY = textureSizeInPixels.Y/ m_threadGroupCountY;
            return new Vector2I(
            (pixelsPerThreadGroupX + m_pixelsPerThreadX - 1) / m_pixelsPerThreadX,
            (pixelsPerThreadGroupY + m_pixelsPerThreadY - 1) / m_pixelsPerThreadY);
        }

        private void InitShaders()
        {
            if (m_markVS == VertexShaderId.NULL)
                m_markVS = MyShaders.CreateVs("shape.hlsl");

            if (m_markPS == PixelShaderId.NULL)
                m_markPS = MyShaders.CreatePs("shape.hlsl");

            if (m_inputLayout == InputLayoutId.NULL)
                m_inputLayout = MyShaders.CreateIL(m_markVS.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3));

            if (m_combinePS == PixelShaderId.NULL)
                m_combinePS = MyShaders.CreatePs("CombineShadows.hlsl");

            m_threadGroupCountX = 32;
            m_threadGroupCountY = 32;
            Vector2I pixelCount = MyRender11.ViewportResolution;
            Vector2I threadsPerThreadGroup = GetThreadsPerThreadGroup(pixelCount);

            int tryIndex = 0;
            while(threadsPerThreadGroup.X*threadsPerThreadGroup.Y > 1024)   // Make sure we do are not over the limit of threads per thread group
            {
                if (tryIndex++ % 2 == 0)
                    ++m_threadGroupCountX;
                else
                    ++m_threadGroupCountY;

                threadsPerThreadGroup = GetThreadsPerThreadGroup(pixelCount);
            }

            int pixelsCoveredX = threadsPerThreadGroup.X * m_pixelsPerThreadX * m_threadGroupCountX;
            if (pixelsCoveredX < pixelCount.X)
                ++m_threadGroupCountX;
            int pixelsCoveredY = threadsPerThreadGroup.Y * m_pixelsPerThreadY * m_threadGroupCountY;
            if (pixelsCoveredY < pixelCount.Y)
                ++m_threadGroupCountY;

            m_gatherCS = MyShaders.CreateCs("shadows.hlsl", new [] {
                new ShaderMacro("NUMTHREADS_X", threadsPerThreadGroup.X),
                new ShaderMacro("NUMTHREADS_Y", threadsPerThreadGroup.Y),
                new ShaderMacro("THREAD_GROUPS_X", m_threadGroupCountX),
                new ShaderMacro("THREAD_GROUPS_Y", m_threadGroupCountY),
                new ShaderMacro("PIXELS_PER_THREAD_X", m_pixelsPerThreadX),
                new ShaderMacro("PIXELS_PER_THREAD_Y", m_pixelsPerThreadY)});
        }

        private unsafe void InitVertexBuffer(int numberOfCascades)
        {
            DestroyVertexBuffer();
            m_cascadesBoundingsVertices = MyHwBuffers.CreateVertexBuffer(8 * numberOfCascades, sizeof(Vector3), BindFlags.VertexBuffer, ResourceUsage.Dynamic);
        }

        private void DestroyVertexBuffer()
        {
            if (m_cascadesBoundingsVertices != VertexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_cascadesBoundingsVertices);
                m_cascadesBoundingsVertices = VertexBufferId.NULL;
            }
        }

        private unsafe void InitIndexBuffer()
        {
            DestroyIndexBuffer();

            const int indexCount = 36;
            ushort* indices = stackalloc ushort[indexCount];
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            indices[3] = 0; indices[4] = 2; indices[5] = 3;
            indices[6] = 1; indices[7] = 5; indices[8] = 6;
            indices[9] = 1; indices[10] = 6; indices[11] = 2;
            indices[12] = 5; indices[13] = 4; indices[14] = 7;
            indices[15] = 5; indices[16] = 7; indices[17] = 6;
            indices[18] = 4; indices[19] = 0; indices[20] = 3;
            indices[21] = 4; indices[22] = 3; indices[23] = 7;
            indices[24] = 3; indices[25] = 2; indices[26] = 6;
            indices[27] = 3; indices[28] = 6; indices[29] = 7;
            indices[30] = 1; indices[31] = 0; indices[32] = 4;
            indices[33] = 1; indices[34] = 4; indices[35] = 5;

            m_cubeIB = MyHwBuffers.CreateIndexBuffer(indexCount, Format.R16_UInt, BindFlags.IndexBuffer, ResourceUsage.Immutable, new IntPtr(indices));
        }

        private void DestroyIndexBuffer()
        {
            if (m_cubeIB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_cubeIB);
                m_cubeIB = IndexBufferId.NULL;
            }
        }

        internal static void Combine(RwTexId targetArray, MyShadowCascades firstCascades, MyShadowCascades secondCascades)
        {
            if (!MyRender11.Settings.EnableShadows)
                return;

            ProfilerShort.Begin("MyShadowCascadesPostProcess.Combine");
            MyGpuProfiler.IC_BeginBlock("MyShadowCascadesPostProcess.Combine");

            firstCascades.FillConstantBuffer(firstCascades.CascadeConstantBuffer);
            secondCascades.FillConstantBuffer(secondCascades.CascadeConstantBuffer);
            secondCascades.PostProcessor.MarkCascadesInStencil(secondCascades.CascadeInfo);

            MyRenderContext renderContext = MyRenderContext.Immediate;
            DeviceContext deviceContext = renderContext.DeviceContext;

            deviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            deviceContext.PixelShader.SetConstantBuffer(10, firstCascades.CascadeConstantBuffer);
            deviceContext.PixelShader.SetConstantBuffer(11, secondCascades.CascadeConstantBuffer);
            deviceContext.PixelShader.SetConstantBuffer(12, m_inverseConstants);

            for (int subresourceIndex = 0; subresourceIndex < targetArray.Description2d.ArraySize; ++subresourceIndex)
            {
                renderContext.BindGBufferForRead(0, MyGBuffer.Main);
                deviceContext.OutputMerger.SetTargets((DepthStencilView)null, (RenderTargetView)targetArray.SubresourceRtv(subresourceIndex));
                deviceContext.PixelShader.SetShaderResource(0, firstCascades.CascadeShadowmapArray.SubresourceSrv(subresourceIndex));
                deviceContext.PixelShader.SetShaderResource(1, secondCascades.CascadeShadowmapArray.ShaderView);
                //deviceContext.PixelShader.SetShaderResource(4, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
                renderContext.SetPS(m_combinePS);

                Matrix inverseCascadeMatrix = MatrixD.Transpose(MatrixD.Invert(firstCascades.CascadeInfo[subresourceIndex].CurrentLocalToProjection * MyMatrixHelpers.ClipspaceToTexture));
                var mapping = MyMapping.MapDiscard(m_inverseConstants);
                mapping.WriteAndPosition(ref inverseCascadeMatrix);
                mapping.Unmap();
                
                MyScreenPass.DrawFullscreenQuad(new MyViewport(0, 0, targetArray.Description2d.Width, targetArray.Description2d.Height));
            }

            deviceContext.OutputMerger.SetTargets(null as DepthStencilView, null as RenderTargetView);
            deviceContext.PixelShader.SetShaderResource(0, null);
            deviceContext.PixelShader.SetShaderResource(1, null);
            deviceContext.PixelShader.SetShaderResource(2, null);
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
        }

        internal void GatherArray(RwTexId postprocessTarget, RwTexId cascadeArray, MyProjectionInfo[] cascadeInfo, ConstantsBufferId cascadeConstantBuffer)
        {
            if (!MyRenderProxy.Settings.EnableShadows)
                return;

            MarkCascadesInStencil(cascadeInfo);

            MyGpuProfiler.IC_BeginBlock("Cascades postprocess");

            MyRenderContext renderContext = MyRenderContext.Immediate;
            DeviceContext deviceContext = renderContext.DeviceContext;

            renderContext.SetCS(m_gatherCS);
            ComputeShaderId.TmpUav[0] = postprocessTarget.Uav;
            deviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);

            deviceContext.ComputeShader.SetShaderResource(0, MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.m_SRV_depth : MyGBuffer.Main.DepthStencil.m_SRV_depth);
            deviceContext.ComputeShader.SetShaderResource(1, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
            deviceContext.ComputeShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);
            deviceContext.ComputeShader.SetConstantBuffer(0, MyCommon.FrameConstants);
            deviceContext.ComputeShader.SetConstantBuffer(4, cascadeConstantBuffer);
            deviceContext.ComputeShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, cascadeArray.ShaderView);

            deviceContext.Dispatch(m_threadGroupCountX, m_threadGroupCountY, 1);

            ComputeShaderId.TmpUav[0] = null;
            renderContext.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
            deviceContext.ComputeShader.SetShaderResource(0, null);

            if(MyRender11.Settings.EnableShadowBlur)
                MyBlur.Run(postprocessTarget.Rtv, MyRender11.CascadesHelper.Rtv, MyRender11.CascadesHelper.ShaderView, postprocessTarget.ShaderView, depthDiscardThreshold: 0.2f);

            MyGpuProfiler.IC_EndBlock();
        }

        private unsafe void MarkCascadesInStencil(MyProjectionInfo[] cascadeInfo)
        {
            MyGpuProfiler.IC_BeginBlock("MarkCascadesInStencil");

            //RC.SetRS(MyRasterizerState.CullCW);

            MyRenderContext renderContext = MyRenderContext.Immediate;

            renderContext.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderContext.SetVB(0, m_cascadesBoundingsVertices.Buffer, m_cascadesBoundingsVertices.Stride);
            renderContext.SetIB(m_cubeIB.Buffer, m_cubeIB.Format);
            renderContext.SetIL(m_inputLayout);
            renderContext.DeviceContext.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            renderContext.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            renderContext.BindDepthRT(MyGBuffer.Main.DepthStencil, DepthStencilAccess.DepthReadOnly, null);
            renderContext.SetVS(m_markVS);
            renderContext.SetPS(m_markPS);

            const int vertexCount = 8;

            Vector3D* frustumVerticesSS = stackalloc Vector3D[vertexCount];
            frustumVerticesSS[0] = new Vector3D(-1, -1, 0);
            frustumVerticesSS[1] = new Vector3D(-1, 1, 0);
            frustumVerticesSS[2] = new Vector3D(1, 1, 0);
            frustumVerticesSS[3] = new Vector3D(1, -1, 0);
            frustumVerticesSS[4] = new Vector3D(-1, -1, 1);
            frustumVerticesSS[5] = new Vector3D(-1, 1, 1);
            frustumVerticesSS[6] = new Vector3D(1, 1, 1);
            frustumVerticesSS[7] = new Vector3D(1, -1, 1);

            Vector3D* lightVertices = stackalloc Vector3D[vertexCount];
            Vector3* tmpFloatVertices = stackalloc Vector3[vertexCount];

            var mapping = MyMapping.MapDiscard(m_cascadesBoundingsVertices.Buffer);
            for (int cascadeIndex = 0; cascadeIndex < MyRender11.Settings.ShadowCascadeCount; ++cascadeIndex)
            {
                var inverseViewProj = MatrixD.Invert(cascadeInfo[cascadeIndex].CurrentLocalToProjection);
                for (int arrayIndex = 0; arrayIndex < vertexCount; ++arrayIndex)
                {
                    Vector3D.Transform(ref frustumVerticesSS[arrayIndex], ref inverseViewProj, out lightVertices[arrayIndex]);
                    tmpFloatVertices[arrayIndex] = lightVertices[arrayIndex];
                }

                for (int arrayIndex = 0; arrayIndex < vertexCount; ++arrayIndex )
                    mapping.WriteAndPosition(ref tmpFloatVertices[arrayIndex]);
            }
            mapping.Unmap();

            for (int cascadeIndex = 0; cascadeIndex < MyRender11.Settings.ShadowCascadeCount; ++cascadeIndex)
            {
                renderContext.SetDS(MyDepthStencilState.MarkIfInsideCascade[cascadeIndex], 1 << cascadeIndex);
                // mark ith bit on depth near
                renderContext.DeviceContext.DrawIndexed(36, 0, 8 * cascadeIndex);
            }

            renderContext.BindDepthRT(null, DepthStencilAccess.DepthReadOnly, null);

            renderContext.SetDS(null);
            renderContext.SetRS(null);

            MyGpuProfiler.IC_EndBlock();
        }
    }
}
