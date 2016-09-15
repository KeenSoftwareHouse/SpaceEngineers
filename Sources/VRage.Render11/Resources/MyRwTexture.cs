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
    internal interface ISrvTexture : ISrvBindable
    {

    }

    internal interface IRtvTexture : ISrvTexture, IRtvBindable
    {
        
    }

    internal interface IUavTexture : IRtvTexture, IUavBindable
    {
        
    }

    internal interface IDepthTexture : ISrvTexture, IDsvBindable
    {
        
    }

    namespace Internal
    {
        internal abstract class MyTextureInternal : IResource, ISrvBindable
        {
            ShaderResourceView m_srv;

            protected Resource m_resource;
            protected string m_name;
            protected Vector2I m_size;
            protected Format m_resourceFormat;
            protected Format m_srvFormat;
            protected BindFlags m_bindFlags;
            protected int m_samplesCount;
            protected int m_samplesQuality;
            protected ResourceOptionFlags m_roFlags;
            protected ResourceUsage m_resourceUsage;
            protected int m_mipmapLevels;
            protected CpuAccessFlags m_cpuAccessFlags;

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }

            public Resource Resource
            {
                get { return m_resource; }
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

            public void Init(
                string name,
                int width,
                int height,
                Format resourceFormat,
                Format srvFormat,
                BindFlags bindFlags,
                int samplesCount,
                int samplesQuality,
                ResourceOptionFlags roFlags,
                ResourceUsage ru,
                int mipmapLevels,
                CpuAccessFlags cpuAccessFlags)
            {
                m_name = name;
                m_size = new Vector2I(width, height);
                m_resourceFormat = resourceFormat;
                m_srvFormat = srvFormat;
                m_bindFlags = bindFlags;
                m_samplesCount = samplesCount;
                m_samplesQuality = samplesQuality;
                m_roFlags = roFlags;
                m_resourceUsage = ru;
                m_mipmapLevels = mipmapLevels;
                m_cpuAccessFlags = cpuAccessFlags;
            }

            public void OnDeviceInitInternal()
            {
                {
                    Texture2DDescription desc = new Texture2DDescription();
                    desc.Width = Size.X;
                    desc.Height = Size.Y;
                    desc.Format = m_resourceFormat;
                    desc.ArraySize = 1;
                    desc.MipLevels = m_mipmapLevels;
                    desc.BindFlags = m_bindFlags;
                    desc.Usage = m_resourceUsage;
                    desc.CpuAccessFlags = m_cpuAccessFlags;
                    desc.SampleDescription.Count = m_samplesCount;
                    desc.SampleDescription.Quality = m_samplesQuality;
                    desc.OptionFlags = m_roFlags;
                    m_resource = new Texture2D(MyRender11.Device, desc);
                }
                {
                    ShaderResourceViewDescription desc = new ShaderResourceViewDescription();
                    desc.Format = m_srvFormat;
                    desc.Dimension = ShaderResourceViewDimension.Texture2D;
                    desc.Texture2D.MipLevels = m_mipmapLevels;
                    desc.Texture2D.MostDetailedMip = 0;
                    m_srv = new ShaderResourceView(MyRender11.Device, m_resource, desc);
                }

                m_resource.DebugName = m_name;
                m_srv.DebugName = m_name;
            }

            public void OnDeviceEndInternal()
            {
                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }
                if (m_resource != null)
                {
                    m_resource.Dispose();
                    m_resource = null;
                }
            }
        }

        internal class MySrvTexture : MyTextureInternal, ISrvTexture
        {
            public void OnDeviceInit()
            {
                OnDeviceInitInternal();
            }

            public void OnDeviceEnd()
            {
                OnDeviceEndInternal();
            }
        }

        internal class MyRtvTexture : MyTextureInternal, IRtvTexture
        {
            RenderTargetView m_rtv;

            public RenderTargetView Rtv
            {
                get { return m_rtv; }
            }

            public void OnDeviceInit()
            {
                OnDeviceInitInternal();

                m_rtv = new RenderTargetView(MyRender11.Device, m_resource);
                m_rtv.DebugName = m_name;
            }

            public void OnDeviceEnd()
            {
                if (m_rtv != null)
                {
                    m_rtv.Dispose();
                    m_rtv = null;
                }

                OnDeviceEndInternal();
            }
        }

        internal class MyUavTexture : MyTextureInternal, IUavTexture
        {
            RenderTargetView m_Rtv;
            UnorderedAccessView m_UAV;

            public RenderTargetView Rtv
            {
                get { return m_Rtv; }
            }

            public UnorderedAccessView Uav
            {
                get { return m_UAV; }
            }

            public void OnDeviceInit()
            {
                OnDeviceInitInternal();

                m_Rtv = new RenderTargetView(MyRender11.Device, m_resource);
                m_Rtv.DebugName = m_name;

                m_UAV = new UnorderedAccessView(MyRender11.Device, m_resource);
                m_UAV.DebugName = m_name;
            }

            public void OnDeviceEnd()
            {
                if (m_Rtv != null)
                {
                    m_Rtv.Dispose();
                    m_Rtv = null;
                }

                if (m_UAV != null)
                {
                    m_UAV.Dispose();
                    m_UAV = null;
                }

                OnDeviceEndInternal();
            }
        }

        internal class MyDepthTexture : MyTextureInternal, IDepthTexture
        {
            Format m_dsvFormat;
            DepthStencilView m_dsv;

            public DepthStencilView Dsv
            {
                get { return m_dsv; }
            }

            public void Init(
                string name,
                int width,
                int height,
                Format resourceFormat,
                Format srvFormat,
                Format dsvFormat,
                BindFlags bindFlags,
                int samplesCount,
                int samplesQuality,
                ResourceOptionFlags roFlags,
                ResourceUsage ru,
                int mipmapLevels,
                CpuAccessFlags cpuAccessFlags)
            {
                base.Init(name, width, height, resourceFormat, srvFormat, bindFlags, samplesCount, samplesQuality,
                    roFlags, ru, mipmapLevels, cpuAccessFlags);

                m_dsvFormat = dsvFormat;
            }

            public void OnDeviceInit()
            {
                OnDeviceInitInternal();

                DepthStencilViewDescription desc = new DepthStencilViewDescription();
                desc.Format = m_dsvFormat;
                desc.Dimension = DepthStencilViewDimension.Texture2D;
                desc.Texture2D.MipSlice = 0;
                m_dsv = new DepthStencilView(MyRender11.Device, m_resource, desc);
                m_dsv.DebugName = m_name;
            }

            public void OnDeviceEnd()
            {
                if (m_dsv != null)
                {
                    m_dsv.Dispose();
                    m_dsv = null;
                }

                OnDeviceEndInternal();
            }
        }
    }

    internal class MyRwTextureManager : IManager, IManagerDevice
    {
        MyObjectsPool<MySrvTexture> m_srvTextures = new MyObjectsPool<MySrvTexture>(16);
        MyObjectsPool<MyRtvTexture> m_rtvTextures = new MyObjectsPool<MyRtvTexture>(64);
        MyObjectsPool<MyUavTexture> m_uavTextures = new MyObjectsPool<MyUavTexture>(64);
        MyObjectsPool<MyDepthTexture> m_depthTextures = new MyObjectsPool<MyDepthTexture>(16);
        bool m_isDeviceInit = false;

        public ISrvTexture CreateSrv(string debugName, int width, int height, Format format, int samplesCount = 1,
            int samplesQuality = 0, ResourceOptionFlags optionFlags = 0, ResourceUsage resourceUsage = ResourceUsage.Default, int mipmapLevels = 1, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None)
        {
            MySrvTexture tex;
            m_srvTextures.AllocateOrCreate(out tex);
            tex.Init(debugName, width, height, format, format,
                BindFlags.ShaderResource,
                samplesCount, samplesQuality, optionFlags, resourceUsage, mipmapLevels, cpuAccessFlags);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        public IRtvTexture CreateRtv(string debugName, int width, int height, Format format, int samplesCount = 1,
            int samplesQuality = 0, ResourceOptionFlags optionFlags = 0, ResourceUsage resourceUsage = ResourceUsage.Default, int mipmapLevels = 1, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None)
        {
            MyRtvTexture tex;
            m_rtvTextures.AllocateOrCreate(out tex);
            tex.Init(debugName, width, height, format, format,
                BindFlags.RenderTarget | BindFlags.ShaderResource,
                samplesCount, samplesQuality, optionFlags, resourceUsage, mipmapLevels, cpuAccessFlags);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        public IUavTexture CreateUav(string debugName, int width, int height, Format format, int samplesCount = 1,
            int samplesQuality = 0, ResourceOptionFlags roFlags = 0, int mipmapLevels = 1, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None)
        {
            MyUavTexture tex;
            m_uavTextures.AllocateOrCreate(out tex);
            tex.Init(debugName, width, height, format, format,
                BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess,
                samplesCount, samplesQuality, roFlags, ResourceUsage.Default, mipmapLevels, cpuAccessFlags);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        public IDepthTexture CreateDepth(string debugName, int width, int height, Format resourceFormat, Format srvFormat, Format dsvFormat, int samplesCount = 1,
            int samplesQuality = 0, ResourceOptionFlags roFlags = 0, int mipmapLevels = 1, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None)
        {
            MyDepthTexture tex;
            m_depthTextures.AllocateOrCreate(out tex);
            tex.Init(debugName, width, height, resourceFormat, srvFormat, dsvFormat,
                BindFlags.ShaderResource | BindFlags.DepthStencil,
                samplesCount, samplesQuality, roFlags, ResourceUsage.Default, mipmapLevels, cpuAccessFlags);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        public int GetTexturesCount()
        {
            int count = 0;
            count += m_srvTextures.ActiveCount;
            count += m_rtvTextures.ActiveCount;
            count += m_uavTextures.ActiveCount;
            count += m_depthTextures.ActiveCount;
            return count;
        }

        public void DisposeTex(ref ISrvTexture texture)
        {
            if (texture == null)
                return;

            MySrvTexture textureInternal = (MySrvTexture) texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_srvTextures.Deallocate(textureInternal);
            texture = null;
        }

        public void DisposeTex(ref IRtvTexture texture)
        {
            if (texture == null)
                return;

            MyRtvTexture textureInternal = (MyRtvTexture) texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_rtvTextures.Deallocate(textureInternal);
            texture = null;
        }

        public void DisposeTex(ref IUavTexture texture)
        {
            if (texture == null)
                return;

            MyUavTexture textureInternal = (MyUavTexture) texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_uavTextures.Deallocate(textureInternal);
            texture = null;
        }

        public void DisposeTex(ref IDepthTexture texture)
        {
            if (texture == null)
                return;

            MyDepthTexture textureInternal = (MyDepthTexture) texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_depthTextures.Deallocate(textureInternal);
            texture = null;
        }
        
        public void OnDeviceInit()
        {
            m_isDeviceInit = true;
            foreach (var tex in m_srvTextures.Active)
                tex.OnDeviceInit();
            foreach(var tex in m_rtvTextures.Active)
                tex.OnDeviceInit();
            foreach (var tex in m_uavTextures.Active)
                tex.OnDeviceInit();
            foreach (var tex in m_depthTextures.Active)
                tex.OnDeviceInit();
        }

        public void OnDeviceReset()
        {
            foreach (var tex in m_srvTextures.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            } 
            foreach (var tex in m_rtvTextures.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            } 
            foreach (var tex in m_uavTextures.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            }
            foreach (var tex in m_depthTextures.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            }
        }

        public void OnDeviceEnd()
        {
            m_isDeviceInit = false;
            foreach (var tex in m_srvTextures.Active)
                tex.OnDeviceEnd();
            foreach (var tex in m_rtvTextures.Active)
                tex.OnDeviceEnd();
            foreach (var tex in m_uavTextures.Active)
                tex.OnDeviceEnd();
            foreach (var tex in m_depthTextures.Active)
                tex.OnDeviceEnd();
        }

        public void OnUnloadData()
        {
            
        }

        public void OnFrameEnd()
        {
        }
    }
}
