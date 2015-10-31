using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static RasterizerId m_nocullRasterizerState;
        internal static RasterizerId m_invTriRasterizerState;
        internal static RasterizerId m_wireframeRasterizerState;
        internal static RasterizerId m_linesRasterizerState;
        internal static RasterizerId m_cascadesRasterizerState;
        internal static RasterizerId m_shadowRasterizerState;
        internal static RasterizerId m_nocullWireframeRasterizerState;
        internal static RasterizerId m_scissorTestRasterizerState;

        static void InitializeRasterizerStates()
        {
            m_wireframeRasterizerState = MyPipelineStates.CreateRasterizerState(new RasterizerStateDescription
                {
                    FillMode = FillMode.Wireframe,
                    CullMode = CullMode.Back
                });

            RasterizerStateDescription desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Wireframe;
            desc.CullMode = CullMode.Back;
            m_wireframeRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Front;
            m_invTriRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            m_nocullRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc.FillMode = FillMode.Wireframe;
            desc.CullMode = CullMode.None;
            m_nocullWireframeRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Back;
            m_linesRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.IsFrontCounterClockwise = true;
            desc.DepthBias = 20;
            desc.DepthBiasClamp = 2;
            desc.SlopeScaledDepthBias = 4;
            m_cascadesRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.DepthBias = 0;
            desc.DepthBiasClamp = 0;
            desc.SlopeScaledDepthBias = 0;
            m_shadowRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Back;
            desc.IsFrontCounterClockwise = false;
            desc.IsScissorEnabled = true;
            m_scissorTestRasterizerState = MyPipelineStates.CreateRasterizerState(desc);
        }
    }
}