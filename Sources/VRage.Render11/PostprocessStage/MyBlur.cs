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

        private static int GetShaderKey(MyBlurDensityFunctionType densityFunctionType, int maxOffset)
        {
            return ((int)densityFunctionType + 1) << 16 + maxOffset;
        }

        private static int InitShadersForOffset(MyBlurDensityFunctionType densityFunctionType, int maxOffset)
        {
            int shaderKey = GetShaderKey(densityFunctionType, maxOffset);

            if(!m_blurShaders.ContainsKey(shaderKey))
            {
                var macrosHorizontal = new[] {new ShaderMacro("HORIZONTAL_PASS", null), new ShaderMacro("MAX_OFFSET", maxOffset), new ShaderMacro("DENSITY_FUNCTION", (int)densityFunctionType)};
                var macrosVertical = new[] { new ShaderMacro("VERTICAL_PASS", null), new ShaderMacro("MAX_OFFSET", maxOffset), new ShaderMacro("DENSITY_FUNCTION", (int)densityFunctionType) };
                var shaderPair = MyTuple.Create(MyShaders.CreatePs("blur.hlsl", macrosHorizontal), MyShaders.CreatePs("blur.hlsl", macrosVertical));
                m_blurShaders.Add(shaderKey, shaderPair);
            }
            return shaderKey;
        }

        internal static void Run(MyBindableResource destinationResource, MyBindableResource intermediateResource, MyBindableResource resourceToBlur,
                                 int maxOffset = 5, MyBlurDensityFunctionType densityFunctionType = MyBlurDensityFunctionType.Gaussian, float weightParameter = 1.5f,
                                 DepthStencilState depthStencilState = null, int stencilRef = 0x0, MyViewport? viewport = null)
        {
            var initialResourceView = resourceToBlur as IShaderResourceBindable;
            var intermediateResourceView = intermediateResource as IShaderResourceBindable;
            var intermediateTarget = intermediateResource as IRenderTargetBindable;
            var destinationTarget = destinationResource as IRenderTargetBindable;
            Run(destinationTarget.RTV, intermediateTarget.RTV, intermediateResourceView.SRV, initialResourceView.SRV, maxOffset, densityFunctionType, weightParameter, depthStencilState, stencilRef, viewport);
        }

        internal static void Run(RenderTargetView renderTarget, RenderTargetView intermediateRenderTarget, ShaderResourceView intermediateResourceView, ShaderResourceView initialResourceView,
                                 int maxOffset = 5, MyBlurDensityFunctionType densityFunctionType = MyBlurDensityFunctionType.Gaussian, float WeightParameter = 1.5f,
                                DepthStencilState depthStencilState = null, int stencilRef = 0x0, MyViewport? viewport = null)
        {
            ProfilerShort.Begin("MyBlur.Run");
            MyGpuProfiler.IC_BeginBlock("MyBlur.Run");
            Debug.Assert(initialResourceView != null);
            Debug.Assert(intermediateResourceView != null);
            Debug.Assert(intermediateRenderTarget != null);
            Debug.Assert(renderTarget != null);

            int shaderKey = InitShadersForOffset(densityFunctionType, maxOffset);

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
            RC.DeviceContext.PixelShader.SetShaderResource(0, initialResourceView);
            RC.DeviceContext.PixelShader.SetShaderResource(4, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
            RC.SetPS(m_blurShaders[shaderKey].Item1);
            MyScreenPass.DrawFullscreenQuad(viewport);

            // Vertical pass
            MyRender11.DeviceContext.ClearRenderTargetView(renderTarget, Color4.White);
            RC.DeviceContext.OutputMerger.SetTargets(renderTarget);
            RC.SetDS(depthStencilState, stencilRef);
            RC.DeviceContext.PixelShader.SetShaderResource(0, intermediateResourceView);
            RC.DeviceContext.PixelShader.SetShaderResource(4, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
            RC.SetPS(m_blurShaders[shaderKey].Item2);
            MyScreenPass.DrawFullscreenQuad(viewport);

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
        }
    }
}
