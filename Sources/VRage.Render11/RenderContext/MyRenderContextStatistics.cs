namespace VRage.Render11.RenderContext
{
    internal class MyRenderContextStatistics
    {
        public int Draws;
        public int Dispatches;

        public int ClearStates;
        public int SetInputLayout;
        public int SetPrimitiveTopologies;
        public int SetIndexBuffers;
        public int SetVertexBuffers;
        public int SetBlendStates;
        public int SetDepthStencilStates;
        public int SetRasterizerStates;
        public int SetViewports;
        public int SetTargets;

        public int SetConstantBuffers;
        public int SetSamplers;
        public int SetSrvs;
        public int SetVertexShaders;
        public int SetGeometryShaders;
        public int SetPixelShaders;
        public int SetComputeShaders;
        public int SetUavs;

        internal void Clear()
        {
            Draws = 0;
            Dispatches = 0;

            ClearStates = 0;
            SetInputLayout = 0;
            SetPrimitiveTopologies = 0;
            SetIndexBuffers = 0;
            SetVertexBuffers = 0;
            SetBlendStates = 0;
            SetDepthStencilStates = 0;
            SetRasterizerStates = 0;
            SetViewports = 0;
            SetTargets = 0;

            SetConstantBuffers = 0;
            SetSamplers = 0;
            SetSrvs = 0;
            SetVertexShaders = 0;
            SetGeometryShaders = 0;
            SetPixelShaders = 0;
            SetComputeShaders = 0;
            SetUavs = 0;
        }

        internal void Gather(MyRenderContextStatistics other)
        {
            Draws += other.Draws;
            Dispatches += other.Dispatches;

            ClearStates += other.ClearStates;
            SetInputLayout += other.SetInputLayout;
            SetPrimitiveTopologies += other.SetPrimitiveTopologies;
            SetIndexBuffers += other.SetIndexBuffers;
            SetVertexBuffers += other.SetVertexBuffers;
            SetBlendStates += other.SetBlendStates;
            SetDepthStencilStates += other.SetDepthStencilStates;
            SetRasterizerStates += other.SetRasterizerStates;
            SetViewports += other.SetViewports;
            SetTargets += other.SetTargets;

            SetConstantBuffers += other.SetConstantBuffers;
            SetSamplers += other.SetSamplers;
            SetSrvs += other.SetSrvs;
            SetVertexShaders += other.SetVertexShaders;
            SetGeometryShaders += other.SetGeometryShaders;
            SetPixelShaders += other.SetPixelShaders;
            SetComputeShaders += other.SetComputeShaders;
            SetUavs += other.SetUavs;
        }
    }
}
