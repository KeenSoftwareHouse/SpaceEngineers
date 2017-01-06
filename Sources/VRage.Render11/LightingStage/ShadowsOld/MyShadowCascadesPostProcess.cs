using Color4 = SharpDX.Color4;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using SharpDX.Mathematics.Interop;
using VRage.Render11.Common;
using VRage.Render11.LightingStage.Shadows;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;


namespace VRageRender
{
    internal class MyShadowCascadesPostProcess : MyImmediateRC
    {
        private static VertexShaderId m_markVS = VertexShaderId.NULL;
        private static PixelShaderId m_markPS = PixelShaderId.NULL;
        private static InputLayoutId m_inputLayout = InputLayoutId.NULL;

        private ComputeShaderId m_gatherCS_LD = ComputeShaderId.NULL;
        private ComputeShaderId m_gatherCS_MD = ComputeShaderId.NULL;
        private ComputeShaderId m_gatherCS_HD = ComputeShaderId.NULL;
        private static PixelShaderId m_combinePS = PixelShaderId.NULL;

        private IVertexBuffer m_cascadesBoundingsVertices;
        private IIndexBuffer m_cubeIB;

        private static IConstantBuffer m_inverseConstants;

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
            if (m_inverseConstants == null)
                m_inverseConstants = MyManagers.Buffers.CreateConstantBuffer("MyShadowCascadesPostProcess", sizeof(Matrix), usage: ResourceUsage.Dynamic);
        }

        private void InitShaders()
        {
            if (m_markVS == VertexShaderId.NULL)
                m_markVS = MyShaders.CreateVs("ShadowsOld/Shape.hlsl");

            if (m_markPS == PixelShaderId.NULL)
                m_markPS = MyShaders.CreatePs("ShadowsOld/Shape.hlsl");

            if (m_inputLayout == InputLayoutId.NULL)
                m_inputLayout = MyShaders.CreateIL(m_markVS.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3));

            if (m_combinePS == PixelShaderId.NULL)
                m_combinePS = MyShaders.CreatePs("ShadowsOld/CombineShadows.hlsl");

            m_gatherCS_LD = MyShaders.CreateCs("ShadowsOld/Shadows.hlsl");
            m_gatherCS_MD = MyShaders.CreateCs("ShadowsOld/Shadows.hlsl", new[] { new ShaderMacro("ENABLE_PCF", null) });
            m_gatherCS_HD = MyShaders.CreateCs("ShadowsOld/Shadows.hlsl", new[] { new ShaderMacro("ENABLE_PCF", null), new ShaderMacro("ENABLE_DISTORTION", null) });
        }

        private unsafe void InitVertexBuffer(int numberOfCascades)
        {
            DestroyVertexBuffer();
            m_cascadesBoundingsVertices = MyManagers.Buffers.CreateVertexBuffer(
                "MyShadowCascadesPostProcess", 8 * numberOfCascades, sizeof(Vector3),
                usage: ResourceUsage.Dynamic);
        }

        private void DestroyVertexBuffer()
        {
            if (m_cascadesBoundingsVertices != null)
                MyManagers.Buffers.Dispose(m_cascadesBoundingsVertices); m_cascadesBoundingsVertices = null;
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

            m_cubeIB = MyManagers.Buffers.CreateIndexBuffer(
                "MyScreenDecals", indexCount, new IntPtr(indices),
                MyIndexBufferFormat.UShort, ResourceUsage.Immutable);
        }

        private void DestroyIndexBuffer()
        {
            if (m_cubeIB != null)
                MyManagers.Buffers.Dispose(m_cubeIB); m_cubeIB = null;
        }

        internal static void Combine(IDepthArrayTexture targetArray, MyShadowCascades firstCascades, MyShadowCascades secondCascades)
        {
            // Not tested at all!

            //if (!MyRender11.Settings.EnableShadows || !MyRender11.DebugOverrides.Shadows)
            //    return;

            //ProfilerShort.Begin("MyShadowCascadesPostProcess.Combine");
            //MyGpuProfiler.IC_BeginBlock("MyShadowCascadesPostProcess.Combine");

            //firstCascades.FillConstantBuffer(firstCascades.CascadeConstantBuffer);
            //secondCascades.FillConstantBuffer(secondCascades.CascadeConstantBuffer);
            //secondCascades.PostProcessor.MarkCascadesInStencil(secondCascades.CascadeInfo);

            //MyRenderContext renderContext = MyRenderContext.Immediate;
            //DeviceContext deviceContext = renderContext.DeviceContext;

            //deviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            //deviceContext.PixelShader.SetConstantBuffer(10, firstCascades.CascadeConstantBuffer);
            //deviceContext.PixelShader.SetConstantBuffer(11, secondCascades.CascadeConstantBuffer);
            //deviceContext.PixelShader.SetConstantBuffer(12, m_inverseConstants);

            //for (int subresourceIndex = 0; subresourceIndex < targetArray.Description2d.ArraySize; ++subresourceIndex)
            //{
            //    renderContext.BindGBufferForRead(0, MyGBuffer.Main);
            //    deviceContext.OutputMerger.SetTargets((DepthStencilView)null, (RenderTargetView)targetArray.SubresourceRtv(subresourceIndex));
            //    deviceContext.PixelShader.SetShaderResource(0, firstCascades.CascadeShadowmapArray.SubresourceSrv(subresourceIndex));
            //    deviceContext.PixelShader.SetShaderResource(1, secondCascades.CascadeShadowmapArray.Srv);
            //    //deviceContext.PixelShader.SetShaderResource(4, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
            //    renderContext.PixelShader.Set(m_combinePS);

            //    Matrix inverseCascadeMatrix = MatrixD.Transpose(MatrixD.Invert(firstCascades.CascadeInfo[subresourceIndex].CurrentLocalToProjection * MyMatrixHelpers.ClipspaceToTexture));
            //    var mapping = MyMapping.MapDiscard(m_inverseConstants);
            //    mapping.WriteAndPosition(ref inverseCascadeMatrix);
            //    mapping.Unmap();

            //    MyScreenPass.DrawFullscreenQuad(new MyViewport(0, 0, targetArray.Description2d.Width, targetArray.Description2d.Height));
            //}

            //deviceContext.OutputMerger.SetTargets(null as DepthStencilView, null as RenderTargetView);
            //deviceContext.PixelShader.SetShaderResource(0, null);
            //deviceContext.PixelShader.SetShaderResource(1, null);
            //deviceContext.PixelShader.SetShaderResource(2, null);
            //MyGpuProfiler.IC_EndBlock();
            //ProfilerShort.End();
        }

        Vector2I GetThreadGroupCount()
        {
            Vector2I viewportRes = MyRender11.ResolutionI;
            Vector2I threads = new Vector2I(32, 32);
            Vector2I groups = new Vector2I(viewportRes.X / threads.X, viewportRes.Y / threads.Y);
            groups.X += viewportRes.X % threads.X == 0 ? 0 : 1;
            groups.Y += viewportRes.Y % threads.Y == 0 ? 0 : 1;
            return groups;
        }

        internal void GatherArray(IUavTexture postprocessTarget, ISrvBindable cascadeArray, MyProjectionInfo[] cascadeInfo, IConstantBuffer cascadeConstantBuffer)
        {
            MyShadowsQuality shadowsQuality = MyRender11.Settings.User.ShadowQuality.GetShadowsQuality();
            if (!MyRender11.Settings.EnableShadows || !MyRender11.DebugOverrides.Shadows || shadowsQuality == MyShadowsQuality.DISABLED)
            {
                RC.ClearUav(postprocessTarget, new RawInt4());
                return;
            }

            MarkCascadesInStencil(cascadeInfo);

            MyGpuProfiler.IC_BeginBlock("Cascades postprocess");

            if (shadowsQuality == MyShadowsQuality.LOW)
                RC.ComputeShader.Set(m_gatherCS_LD);
            else if (shadowsQuality == MyShadowsQuality.MEDIUM)
                RC.ComputeShader.Set(m_gatherCS_MD);
            else if (shadowsQuality == MyShadowsQuality.HIGH)
                RC.ComputeShader.Set(m_gatherCS_HD);

            RC.ComputeShader.SetUav(0, postprocessTarget);

            RC.ComputeShader.SetSrv(0, MyGBuffer.Main.ResolvedDepthStencil.SrvDepth);
            RC.ComputeShader.SetSrv(1, MyGBuffer.Main.DepthStencil.SrvStencil);
            RC.ComputeShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MySamplerStateManager.Shadowmap);
            if (!MyStereoRender.Enable)
                RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else
                MyStereoRender.CSBindRawCB_FrameConstants(RC);
            //RC.ComputeShader.SetConstantBuffer(4, MyManagers.Shadows.GetCsmConstantBufferOldOne());
            RC.ComputeShader.SetConstantBuffer(4, cascadeConstantBuffer);
            RC.ComputeShader.SetSrv(MyCommon.CASCADES_SM_SLOT, cascadeArray);
            //RC.ComputeShader.SetSrv(MyCommon.CASCADES_SM_SLOT, MyManagers.Shadow.GetCsmForGbuffer());

            Vector2I threadGroups = GetThreadGroupCount();
            RC.Dispatch(threadGroups.X, threadGroups.Y, 1);

            RC.ComputeShader.SetUav(0, null);
            RC.ComputeShader.SetSrv(0, null);
            RC.ComputeShader.SetSrv(1, null);

            if (shadowsQuality == MyShadowsQuality.HIGH && MyShadowCascades.Settings.Data.EnableShadowBlur)
            {
                IBorrowedUavTexture helper = MyManagers.RwTexturesPool.BorrowUav("MyShadowCascadesPostProcess.Helper", Format.R8_UNorm);
                MyBlur.Run(postprocessTarget, helper, postprocessTarget,
                    depthStencilState: MyDepthStencilStateManager.IgnoreDepthStencil,
                    depthDiscardThreshold: 0.2f, clearColor: Color4.White);
                helper.Release();
            }

            MyGpuProfiler.IC_EndBlock();
        }

        private unsafe void MarkCascadesInStencil(MyProjectionInfo[] cascadeInfo)
        {
            MyGpuProfiler.IC_BeginBlock("MarkCascadesInStencil");

            //RC.SetRS(MyRasterizerState.CullCW);

            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            RC.SetVertexBuffer(0, m_cascadesBoundingsVertices);
            RC.SetIndexBuffer(m_cubeIB);
            RC.SetInputLayout(m_inputLayout);
            RC.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            if (!MyStereoRender.Enable)
                RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else
                MyStereoRender.BindRawCB_FrameConstants(RC);
            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.DepthReadOnly);
            RC.VertexShader.Set(m_markVS);
            RC.PixelShader.Set(m_markPS);

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

            var mapping = MyMapping.MapDiscard(m_cascadesBoundingsVertices);
            for (int cascadeIndex = 0; cascadeIndex < MyShadowCascades.Settings.NewData.CascadesCount; ++cascadeIndex)
            {
                var inverseViewProj = MatrixD.Invert(cascadeInfo[cascadeIndex].CurrentLocalToProjection);
                for (int arrayIndex = 0; arrayIndex < vertexCount; ++arrayIndex)
                {
                    Vector3D.Transform(ref frustumVerticesSS[arrayIndex], ref inverseViewProj, out lightVertices[arrayIndex]);
                    tmpFloatVertices[arrayIndex] = lightVertices[arrayIndex];
                }

                for (int arrayIndex = 0; arrayIndex < vertexCount; ++arrayIndex)
                    mapping.WriteAndPosition(ref tmpFloatVertices[arrayIndex]);
            }
            mapping.Unmap();

            if (MyStereoRender.Enable)
                MyStereoRender.SetViewport(RC);

            for (int cascadeIndex = 0; cascadeIndex < MyShadowCascades.Settings.NewData.CascadesCount; ++cascadeIndex)
            {
                RC.SetDepthStencilState(MyDepthStencilStateManager.MarkIfInsideCascade[cascadeIndex], 0xf - cascadeIndex);
                // mark ith bit on depth near

                RC.DrawIndexed(36, 0, 8 * cascadeIndex);
            }

            RC.SetRtv(null);

            RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);
            RC.SetRasterizerState(null);

            MyGpuProfiler.IC_EndBlock();
        }
    }
}
