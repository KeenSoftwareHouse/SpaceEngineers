using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;

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

        private static IConstantBuffer m_blurConstantBuffer;

        // Item1 is horizontal, Item2 is vertical pass
        private static Dictionary<int, MyTuple<PixelShaderId, PixelShaderId>> m_blurShaders = null; 

        internal static unsafe void Init()
        {
            int typeCount = Enum.GetValues(typeof(MyBlurDensityFunctionType)).Length;
            m_blurShaders = new Dictionary<int, MyTuple<PixelShaderId, PixelShaderId>>();

            if (m_blurConstantBuffer == null)
                m_blurConstantBuffer = MyManagers.Buffers.CreateConstantBuffer("MyBlur", sizeof(BlurConstants), usage: ResourceUsage.Dynamic);
        }

        private static int GetShaderKey(MyBlurDensityFunctionType densityFunctionType, int maxOffset, bool useDepthDiscard)
        {
            return  (useDepthDiscard ? 1 : 0) << 31 |
                    (((int)densityFunctionType + 1) << 15) |
                    (maxOffset);
        }

        private static int InitShaders(MyBlurDensityFunctionType densityFunctionType, int maxOffset, float depthDiscardThreshold)
        {
            bool useDepthDiscard = depthDiscardThreshold > 0;
            int shaderKey = GetShaderKey(densityFunctionType, maxOffset, useDepthDiscard);

            if(!m_blurShaders.ContainsKey(shaderKey))
            {
                ShaderMacro depthMacro = new ShaderMacro(useDepthDiscard ? "DEPTH_DISCARD_THRESHOLD" : "", useDepthDiscard ? depthDiscardThreshold : 1);

                var macrosHorizontal = new[] { new ShaderMacro("HORIZONTAL_PASS", null), new ShaderMacro("MAX_OFFSET", maxOffset), new ShaderMacro("DENSITY_FUNCTION", (int)densityFunctionType), depthMacro };
                var macrosVertical = new[] { new ShaderMacro("VERTICAL_PASS", null), new ShaderMacro("MAX_OFFSET", maxOffset), new ShaderMacro("DENSITY_FUNCTION", (int)densityFunctionType), depthMacro };
                var shaderPair = MyTuple.Create(MyShaders.CreatePs("Postprocess/Blur.hlsl", macrosHorizontal), MyShaders.CreatePs("Postprocess/Blur.hlsl", macrosVertical));
                m_blurShaders.Add(shaderKey, shaderPair);
            }
            return shaderKey;
        }

        /// <param name="clearColor">Color used to clear render targets. Defaults to black</param>
        internal static void Run(IRtvBindable renderTarget, IRtvTexture intermediate, ISrvBindable initialResourceView,
            int maxOffset = 5, MyBlurDensityFunctionType densityFunctionType = MyBlurDensityFunctionType.Gaussian, float WeightParameter = 1.5f,
            IDepthStencilState depthStencilState = null, int stencilRef = 0x0, Color4 clearColor = default(Color4),
            float depthDiscardThreshold = 0.0f, MyViewport? viewport = null)
        {
            ProfilerShort.Begin("MyBlur.Run");
            MyGpuProfiler.IC_BeginBlock("MyBlur.Run");
            Debug.Assert(initialResourceView != null);
            Debug.Assert(intermediate != null);
            Debug.Assert(renderTarget != null);

            int shaderKey = InitShaders(densityFunctionType, maxOffset, depthDiscardThreshold);

            RC.PixelShader.SetConstantBuffer(5, m_blurConstantBuffer);

            BlurConstants constants = new BlurConstants
            {
                DistributionWeight = WeightParameter,
                StencilRef = stencilRef,
            };
            var mapping = MyMapping.MapDiscard(m_blurConstantBuffer);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();

            // Horizontal pass
            // NOTE: DepthStencilState is not used here because the resulted target
            // would not be usable to perform vertical pass. DepthStencil is stil
            // bindinded as SRV since it is sampled in the shader 
            RC.ClearRtv(intermediate, clearColor);
            RC.SetRtv(intermediate);
            RC.PixelShader.SetSrv(0, MyGBuffer.Main.DepthStencil.SrvDepth);
            RC.PixelShader.SetSrv(4, MyGBuffer.Main.DepthStencil.SrvStencil);
            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            RC.PixelShader.SetSrv(5, initialResourceView);
            RC.PixelShader.Set(m_blurShaders[shaderKey].Item1);
            MyScreenPass.DrawFullscreenQuad(viewport);
            RC.PixelShader.SetSrv(5, null);

            // Vertical pass
            RC.ClearRtv(renderTarget, clearColor);
            if (depthStencilState == null)
            {
                RC.SetRtv(renderTarget);
            }
            else
            {
                RC.SetDepthStencilState(depthStencilState, stencilRef);
                RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, renderTarget);
            }

            RC.PixelShader.SetSrv(5, intermediate);
            RC.PixelShader.Set(m_blurShaders[shaderKey].Item2);
            MyScreenPass.DrawFullscreenQuad(viewport);

            RC.PixelShader.SetSrv(0, null);
            RC.PixelShader.SetSrv(4, null);
            RC.PixelShader.SetSrv(5, null);
            RC.SetRtv(null);

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
        }
    }
}
