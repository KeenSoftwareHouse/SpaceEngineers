using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using VRageRender;
using Resource = SharpDX.Direct3D11.Resource;

namespace VRage.Render11.Resources.Textures
{
    internal abstract class MyTextureInternal : ISrvTexture
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

        public Format Format { get { return m_resourceFormat; } }

        public int MipmapCount
        {
            get { return m_mipmapLevels; }
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
        RenderTargetView m_rtv;
        UnorderedAccessView m_uav;

        public RenderTargetView Rtv
        {
            get { return m_rtv; }
        }

        public UnorderedAccessView Uav
        {
            get { return m_uav; }
        }

        public void OnDeviceInit()
        {
            OnDeviceInitInternal();

            m_rtv = new RenderTargetView(MyRender11.Device, m_resource);
            m_rtv.DebugName = m_name;

            m_uav = new UnorderedAccessView(MyRender11.Device, m_resource);
            m_uav.DebugName = m_name;
        }

        public void OnDeviceEnd()
        {
            if (m_rtv != null)
            {
                m_rtv.Dispose();
                m_rtv = null;
            }

            if (m_uav != null)
            {
                m_uav.Dispose();
                m_uav = null;
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

            DepthStencilViewDescription desc = new DepthStencilViewDescription
            {
                Format = m_dsvFormat,
                Dimension = DepthStencilViewDimension.Texture2D,
                Texture2D = { MipSlice = 0 }
            };
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
