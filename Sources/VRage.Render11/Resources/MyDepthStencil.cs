using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRageMath;
using VRageRender;
using Resource = SharpDX.Direct3D11.Resource;

namespace VRage.Render11.Resources
{
    internal interface IDepthStencil : IResource
    {
        ISrvBindable SrvDepth { get; }
        ISrvBindable SrvStencil { get; }
    }

    internal interface IDepthStencilInternal : IDepthStencil
    {
        DepthStencilView Dsv { get; }
        DepthStencilView Dsv_ro { get; }
        DepthStencilView Dsv_roStencil { get; }
        DepthStencilView Dsv_roDepth { get; }
    }

    namespace Internal
    {
        internal class MyDepthStencilSrv : ISrvBindable
        {
            MyDepthStencil m_owner;
            ShaderResourceView m_srv;

            public void OnDeviceInit(MyDepthStencil owner, ShaderResourceViewDescription desc)
            {
                m_owner = owner;
                m_srv = new ShaderResourceView(MyRender11.Device, m_owner.Resource, desc);
                m_srv.DebugName = owner.Name;
            }

            public void OnDeviceEnd()
            {
                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }
            }

            public string Name
            {
                get { return m_owner.Name; }
            }

            public Resource Resource
            {
                get { return m_owner.Resource; }
            }

            public Vector3I Size3
            {
                get { return m_owner.Size3; }
            }

            public Vector2I Size
            {
                get { return m_owner.Size; }
            }

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }
        }

        internal class MyDepthStencil : IDepthStencilInternal
        {
            static MyObjectsPool<MyDepthStencilSrv> m_objectsPoolSrvs = new MyObjectsPool<MyDepthStencilSrv>(32);

            string m_name;
            Vector2I m_size;
            Format m_resourceFormat;
            Format m_dsvFormat;
            Format m_srvDepthFormat;
            Format m_srvStencilFormat;
            int m_samplesCount;
            int m_samplesQuality;

            Resource m_resource;
            DepthStencilView m_dsv;
            DepthStencilView m_dsv_roDepth;
            DepthStencilView m_dsv_roStencil;
            DepthStencilView m_dsv_ro;

            MyDepthStencilSrv m_srvDepth;
            MyDepthStencilSrv m_srvStencil;

            public MyDepthStencil()
            {
                m_objectsPoolSrvs.AllocateOrCreate(out m_srvDepth);
                m_objectsPoolSrvs.AllocateOrCreate(out m_srvStencil);
            }

            ~MyDepthStencil()
            {
                m_objectsPoolSrvs.Deallocate(m_srvDepth);
                m_objectsPoolSrvs.Deallocate(m_srvStencil);
            }

            public void Init(string debugName, int width, int height,
                Format resourceFormat, Format dsvFormat, Format srvDepthFormat, Format srvStencilFormat,
                int samplesNum, int samplesQuality)
            {
                m_name = debugName;
                m_size = new Vector2I(width, height);
                m_resourceFormat = resourceFormat;
                m_dsvFormat = dsvFormat;
                m_srvDepthFormat = srvDepthFormat;
                m_srvStencilFormat = srvStencilFormat;
                m_samplesCount = samplesNum;
                m_samplesQuality = samplesQuality;
            }

            public string Name
            {
                get { return m_name; }
            }

            public Vector2I Size
            {
                get { return m_size; }
            }

            public Vector3I Size3
            {
                get { return new Vector3I(m_size.X, m_size.Y, 1); }
            }

            public Resource Resource
            {
                get { return m_resource; }
            }

            public DepthStencilView Dsv
            {
                get { return m_dsv; }
            }

            public DepthStencilView Dsv_roDepth
            {
                get { return m_dsv_roDepth; }
            }

            public DepthStencilView Dsv_roStencil
            {
                get { return m_dsv_roStencil; }
            }

            public DepthStencilView Dsv_ro
            {
                get { return m_dsv_ro; }
            }

            public ISrvBindable SrvDepth
            {
                get { return m_srvDepth; }
            }

            public ISrvBindable SrvStencil
            {
                get { return m_srvStencil; }
            }

            public Format Format { get { return m_dsvFormat; } }


            public void OnDeviceInit()
            {
                Texture2DDescription desc = new Texture2DDescription();
                desc.Width = Size.X;
                desc.Height = Size.Y;
                desc.Format = m_resourceFormat;
                desc.ArraySize = 1;
                desc.MipLevels = 1;
                desc.BindFlags = BindFlags.ShaderResource | BindFlags.DepthStencil;
                desc.Usage = ResourceUsage.Default;
                desc.CpuAccessFlags = 0;
                desc.SampleDescription.Count = m_samplesCount;
                desc.SampleDescription.Quality = m_samplesQuality;
                desc.OptionFlags = 0;
                m_resource = new Texture2D(MyRender11.Device, desc);
                m_resource.DebugName = Name;

                DepthStencilViewDescription dsvDesc = new DepthStencilViewDescription();
                dsvDesc.Format = m_dsvFormat;
                if (m_samplesCount == 1)
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                    dsvDesc.Flags = DepthStencilViewFlags.None;
                    dsvDesc.Texture2D.MipSlice = 0;
                }
                else
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                    dsvDesc.Flags = DepthStencilViewFlags.None;
                }
                m_dsv = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);
                if (m_samplesCount == 1)
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                    dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyDepth;
                    dsvDesc.Texture2D.MipSlice = 0;
                }
                else
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                    dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyDepth;
                }
                m_dsv_roDepth = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);
                if (m_samplesCount == 1)
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                    dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyStencil;
                    dsvDesc.Texture2D.MipSlice = 0;
                }
                else
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                    dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyStencil;
                }
                m_dsv_roStencil = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);
                dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyStencil | DepthStencilViewFlags.ReadOnlyDepth;
                if (m_samplesCount == 1)
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                    dsvDesc.Texture2D.MipSlice = 0;
                }
                else
                {
                    dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                }
                m_dsv_ro = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);

                ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription();
                srvDesc.Format = m_srvDepthFormat;
                if (m_samplesCount == 1)
                {
                    srvDesc.Dimension = ShaderResourceViewDimension.Texture2D;
                    srvDesc.Texture2D.MipLevels = -1;
                    srvDesc.Texture2D.MostDetailedMip = 0;
                }
                else
                {
                    srvDesc.Dimension = ShaderResourceViewDimension.Texture2DMultisampled;
                }
                m_srvDepth.OnDeviceInit(this, srvDesc);
                srvDesc.Format = m_srvStencilFormat;
                if (m_samplesCount == 1)
                {
                    srvDesc.Dimension = ShaderResourceViewDimension.Texture2D;
                    srvDesc.Texture2D.MipLevels = -1;
                    srvDesc.Texture2D.MostDetailedMip = 0;
                }
                else
                {
                    srvDesc.Dimension = ShaderResourceViewDimension.Texture2DMultisampled;
                }
                m_srvStencil.OnDeviceInit(this, srvDesc);
            }

            public void OnDeviceEnd()
            {
                if (m_resource != null)
                {
                    m_resource.Dispose();
                    m_resource = null;
                }

                if (m_dsv != null)
                    m_dsv.Dispose();

                if (m_dsv_roDepth != null)                    
                    m_dsv_roDepth.Dispose();

                if (m_dsv_roStencil != null)
                    m_dsv_roStencil.Dispose();

                if (m_dsv_ro != null)
                    m_dsv_ro.Dispose();

                m_srvDepth.OnDeviceEnd();
                m_srvStencil.OnDeviceEnd();
            }
        }
    }

    internal class MyDepthStencilManager : IManager, IManagerDevice
    {
        private MyObjectsPool<MyDepthStencil> m_objectsPool = new MyObjectsPool<MyDepthStencil>(16);
        private bool m_isDeviceInit = false;

        public IDepthStencil CreateDepthStencil(string debugName, int width, int height,
            Format resourceFormat = Format.R32G8X24_Typeless, 
            Format dsvFormat = Format.D32_Float_S8X24_UInt,
            Format srvDepthFormat = Format.R32_Float_X8X24_Typeless, 
            Format srvStencilFormat = Format.X32_Typeless_G8X24_UInt,
            int samplesCount = 1, 
            int samplesQuality = 0)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);

            MyDepthStencil tex = m_objectsPool.Allocate();
            tex.Init(debugName, width, height, resourceFormat, dsvFormat, srvDepthFormat, srvStencilFormat, 
                samplesCount, samplesQuality);

            if (m_isDeviceInit)
            {
                try
                {
                    tex.OnDeviceInit();
                }
                catch (System.Exception ex)
                {
                    IDepthStencil t = tex;
                    DisposeTex(ref t);
                    throw;
                }
            }

            return tex;
        }

        public int GetDepthStencilsCount()
        {
            return m_objectsPool.ActiveCount;
        }

        internal void DisposeTex(ref IDepthStencil tex)
        {
            if (tex == null)
                return;

            MyDepthStencil texture = (MyDepthStencil)tex;

            if (m_isDeviceInit)
                texture.OnDeviceEnd();
            
            m_objectsPool.Deallocate(texture);
        }

        public void OnDeviceInit()
        {
            m_isDeviceInit = true;
            foreach(var tex in m_objectsPool.Active)
                tex.OnDeviceInit();
        }

        public void OnDeviceReset()
        {
            foreach (var tex in m_objectsPool.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            }
        }

        public void OnDeviceEnd()
        {
            m_isDeviceInit = false;
            foreach (var tex in m_objectsPool.Active)
                tex.OnDeviceEnd();
        }
    }
}
