using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRageRender.Resources;

namespace VRageRender
{
    class MyBlur : MyScreenPass
    {
        // These values can be changed at will as they are not saved anywhere, but please make sure they are consecutive numbers starting from 0 (and change the shader too)
        internal enum MyBlurDensityFunctionType
        {
            Exponential = 0,
            Gaussian = 1,
        }

        private struct BlurConstants
        {
            internal float DistributionWeight;
            internal int StencilRef;
            internal Vector2 _padding;
        }

        private static ConstantsBufferId m_blurConstantBuffer;

        // Item1 is horizontal, Item2 is vertical pass
        private static Dictionary<int, MyTuple<PixelShaderId, PixelShaderId>> m_blurShaders = null; 

        internal static unsafe void Init()
        {
            int typeCount = Enum.GetValues(typeof(MyBlurDensityFunctionType)).Length;
            m_blurShaders = new Dictionary<int, MyTuple<PixelShaderId, PixelShaderId>>();

            if (m_blurConstantBuffer == ConstantsBufferId.NULL)
                m_blurConstantBuffer = MyHwBuffers.CreateConstantsBuffer(sizeof(BlurConstants));
        }

        private static int GetShaderKey(MyBlurDensityFunctionType densityFunctionType, int maxOffset, bool copyOnStencilFail, bool useDepthDiscard)
        {
            return  (copyOnStencilFail ? 1 : 0) << 31 |
                    (useDepthDiscard ? 1 : 0) << 30 |
                    (((int)densityFunctionType + 1) << 15) |
                    (maxOffset);
        }

        private static int InitShaders(MyBlurDensityFunctionType densityFunctionType, int maxOffset, bool copyOnStencilFail, float depthDiscardThreshold)
        {
            bool useDepthDiscard = depthDiscardThreshold > 0;
            int shaderKey = GetShaderKey(densityFunctionType, maxOffset, copyOnStencilFail, useDepthDiscard);

            if(!m_blurShaders.ContainsKey(shaderKey))
            {
                ShaderMacro copyMacro = new ShaderMacro(copyOnStencilFail ? "COPY_ON_STENCIL_FAIL" : "", 1);

                ShaderMacro depthMacro = new ShaderMacro(useDepthDiscard ? "DEPTH_DISCARD_THRESHOLD" : "", useDepthDiscard ? depthDiscardThreshold : 1);

                var macrosHorizontal = new[] { new ShaderMacro("HORIZONTAL_PASS", null), new ShaderMacro("MAX_OFFSET", maxOffset), new ShaderMacro("DENSITY_FUNCTION", (int)densityFunctionType), copyMacro, depthMacro };
                var macrosVertical = new[] { new ShaderMacro("VERTICAL_PASS", null), new ShaderMacro("MAX_OFFSET", maxOffset), new ShaderMacro("DENSITY_FUNCTION", (int)densityFunctionType), copyMacro, depthMacro };
                var shaderPair = MyTuple.Create(MyShaders.CreatePs("blur.hlsl", macrosHorizontal), MyShaders.CreatePs("blur.hlsl", macrosVertical));
                m_blurShaders.Add(shaderKey, shaderPair);
            }
            return shaderKey;
        }

        internal static void Run(MyBindableResource destinationResource, MyBindableResource intermediateResource, MyBindableResource resourceToBlur)
        {
            var initialResourceView = resourceToBlur as IShaderResourceBindable;
            var intermediateResourceView = intermediateResource as IShaderResourceBindable;
            var intermediateTarget = intermediateResource as IRenderTargetBindable;
            var destinationTarget = destinationResource as IRenderTargetBindable;
            Run(destinationTarget.RTV, intermediateTarget.RTV, intermediateResourceView.SRV, initialResourceView.SRV);
        }

        internal static void Run(RenderTargetView renderTarget, RenderTargetView intermediateRenderTarget, ShaderResourceView intermediateResourceView, ShaderResourceView initialResourceView,
                                 int maxOffset = 5, MyBlurDensityFunctionType densityFunctionType = MyBlurDensityFunctionType.Gaussian, float WeightParameter = 1.5f,
                                 DepthStencilState depthStencilState = null, int stencilRef = 0x0, bool copyOnStencilFail = false,
                                 float depthDiscardThreshold = 0.0f, MyViewport? viewport = null)
        {
            ProfilerShort.Begin("MyBlur.Run");
            MyGpuProfiler.IC_BeginBlock("MyBlur.Run");
            Debug.Assert(initialResourceView != null);
            Debug.Assert(intermediateResourceView != null);
            Debug.Assert(intermediateRenderTarget != null);
            Debug.Assert(renderTarget != null);

            int shaderKey = InitShaders(densityFunctionType, maxOffset, copyOnStencilFail, depthDiscardThreshold);

            RC.DeviceContext.PixelShader.SetConstantBuffer(5, m_blurConstantBuffer);

            BlurConstants constants = new BlurConstants
            {
                DistributionWeight = WeightParameter,
                StencilRef = stencilRef,
            };
            var mapping = MyMapping.MapDiscard(m_blurConstantBuffer);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();

            // Horizontal pass
            MyRender11.DeviceContext.ClearRenderTargetView(intermediateRenderTarget, Color4.White);

            RC.DeviceContext.OutputMerger.SetTargets(intermediateRenderTarget);
            RC.SetDS(depthStencilState, stencilRef);
            RC.DeviceContext.PixelShader.SetShaderResource(0, MyGBuffer.Main.DepthStencil.m_SRV_depth);
            RC.DeviceContext.PixelShader.SetShaderResource(4, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
            RC.DeviceContext.PixelShader.SetShaderResource(5, initialResourceView);
            RC.SetPS(m_blurShaders[shaderKey].Item1);
            MyScreenPass.DrawFullscreenQuad(viewport);

            // Vertical pass
            MyRender11.DeviceContext.ClearRenderTargetView(renderTarget, Color4.White);
            RC.DeviceContext.OutputMerger.SetTargets(renderTarget);
            RC.SetDS(depthStencilState, stencilRef);
            RC.DeviceContext.PixelShader.SetShaderResource(0, MyGBuffer.Main.DepthStencil.m_SRV_depth);
            RC.DeviceContext.PixelShader.SetShaderResource(4, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
            RC.DeviceContext.PixelShader.SetShaderResource(5, intermediateResourceView);
            RC.SetPS(m_blurShaders[shaderKey].Item2);
            MyScreenPass.DrawFullscreenQuad(viewport);

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
        }
    }
}
