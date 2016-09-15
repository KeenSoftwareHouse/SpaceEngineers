using SharpDX.Direct3D11;
using VRageMath;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal class MyBackbuffer : IRtvBindable
    {
        RenderTargetView m_rtv;
        Resource m_resource;
        string m_debugName = "MyBackbuffer";

        public string Name
        {
            get { return m_debugName; }
        }

        public RenderTargetView Rtv
        {
            get { return m_rtv; }
        }

        public UnorderedAccessView Uav
        {
            get
            {
                MyRenderProxy.Assert(false);
                return null;
            }
        }

        public Resource Resource
        {
            get { return m_resource; }
        }

        public Vector3I Size3
        {
            get { return new Vector3I(MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, 1); }
        }

        public Vector2I Size
        {
            get { return new Vector2I(MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y); }
        }

        internal MyBackbuffer(SharpDX.Direct3D11.Resource swapChainBB)
        {
            m_resource = swapChainBB;
            m_resource.DebugName = m_debugName;
            m_rtv = new RenderTargetView(MyRender11.Device, swapChainBB);
            m_rtv.DebugName = m_debugName;
        }

        internal void Release()
        {
            if (m_rtv != null)
            {
                m_rtv.Dispose();
                m_rtv = null;
            }
            if (m_resource != null)
            {
                m_resource.Dispose();
                m_resource = null;
            }
        }
    }
}