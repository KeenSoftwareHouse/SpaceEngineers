using SharpDX.Direct3D11;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static RasterizerId m_nocullRasterizerState;
        internal static RasterizerId m_wireframeRasterizerState;
        internal static RasterizerId m_linesRasterizerState;
        internal static RasterizerId m_cascadesRasterizerState;
        internal static RasterizerId m_shadowRasterizerState;
        internal static RasterizerId m_nocullWireframeRasterizerState;
        internal static RasterizerId m_scissorTestRasterizerState;

        private static void InitializeRasterizerStates()
        {
            m_wireframeRasterizerState = MyPipelineStates.CreateRasterizerState(new RasterizerStateDescription
            {
                FillMode = FillMode.Wireframe,
                CullMode = CullMode.Back
            });

            RasterizerStateDescription desc = new RasterizerStateDescription
            {
                FillMode = FillMode.Wireframe,
                CullMode = CullMode.Back
            };
            m_wireframeRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            m_nocullRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc.FillMode = FillMode.Wireframe;
            desc.CullMode = CullMode.None;
            m_nocullWireframeRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc = new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsAntialiasedLineEnabled = true
            };
            m_linesRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc = new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsFrontCounterClockwise = true,
                DepthBias = 25000,
                DepthBiasClamp = 2,
                SlopeScaledDepthBias = 2
            };
            m_cascadesRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc = new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                DepthBias = 0,
                DepthBiasClamp = 0,
                SlopeScaledDepthBias = 0
            };
            m_shadowRasterizerState = MyPipelineStates.CreateRasterizerState(desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Back;
            desc.IsFrontCounterClockwise = false;
            desc.IsScissorEnabled = true;
            m_scissorTestRasterizerState = MyPipelineStates.CreateRasterizerState(desc);
        }
    }
}