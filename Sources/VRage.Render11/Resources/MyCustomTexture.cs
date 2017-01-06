
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
    internal interface ICustomTexture : IResource
    {
        IRtvTexture SRgb { get; }

        IRtvTexture Linear { get; }
    }

    namespace Internal
    {
        internal class MyCustomTextureFormat : IRtvTexture
        {
            MyCustomTexture m_owner;
            Format m_format;

            ShaderResourceView m_srv;
            RenderTargetView m_rtv;

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }

            public RenderTargetView Rtv
            {
                get { return m_rtv; }
            }

            public Resource Resource
            {
                get { return m_owner.Resource; }
            }

            public string Name
            {
                get { return m_owner.Name; }
            }

            public Vector2I Size
            {
                get { return m_owner.Size; }
            }

            public Vector3I Size3
            {
                get { return m_owner.Size3; }
            }

            public int MipmapCount
            {
                get { return 1; }
            }

            public Format Format { get { return m_format; } }

            public void Init(MyCustomTexture owner, Format format)
            {
                m_owner = owner;
                m_format = format;
            }

            public void OnDeviceInit()
            {
                {
                    ShaderResourceViewDescription desc = new ShaderResourceViewDescription();
                    desc.Format = m_format;
                    desc.Dimension = ShaderResourceViewDimension.Texture2D;
                    desc.Texture2D.MipLevels = 1;
                    desc.Texture2D.MostDetailedMip = 0;
                    m_srv = new ShaderResourceView(MyRender11.Device, m_owner.Resource, desc);
                    m_srv.DebugName = m_owner.Name;
                }
                {
                    RenderTargetViewDescription desc = new RenderTargetViewDescription();
                    desc.Format = m_format;
                    desc.Dimension = RenderTargetViewDimension.Texture2D;
                    desc.Texture2D.MipSlice = 0;
                    m_rtv = new RenderTargetView(MyRender11.Device, m_owner.Resource, desc);
                    m_rtv.DebugName = m_owner.Name;
                }
            }

            public void OnDeviceEnd()
            {
                m_srv.Dispose();
                m_srv = null;

                m_rtv.Dispose();
                m_rtv = null;
            }
        }

        class MyCustomTexture : ICustomTexture
        {
            static MyObjectsPool<MyCustomTextureFormat> m_objectsPoolFormats = new MyObjectsPool<MyCustomTextureFormat>(32);

            string m_name;
            Vector2I m_size;
            Format m_resourceFormat;
            int m_samplesCount;
            int m_samplesQuality;

            Resource m_resource;
            MyCustomTextureFormat m_formatSRgb;
            MyCustomTextureFormat m_formatLinear;

            public void Init(string debugName, int width, int height,
                int samplesNum, int samplesQuality)
            {
                m_name = debugName;
                m_size = new Vector2I(width, height);
                m_resourceFormat = Format.R8G8B8A8_Typeless;
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

            public int MipmapCount
            {
                get { return 1; }
            }

            public Resource Resource
            {
                get { return m_resource; }
            }

            public IRtvTexture SRgb
            {
                get { return m_formatSRgb; }
            }

            public IRtvTexture Linear
            {
                get { return m_formatLinear; }
            }

            public MyCustomTexture()
            {
                m_objectsPoolFormats.AllocateOrCreate(out m_formatSRgb);
                m_objectsPoolFormats.AllocateOrCreate(out m_formatLinear);
            }

            ~MyCustomTexture()
            {
                m_objectsPoolFormats.Deallocate(m_formatSRgb);
                m_objectsPoolFormats.Deallocate(m_formatLinear);
            }

            public void OnDeviceInit()
            {
                Texture2DDescription desc = new Texture2DDescription();
                desc.Width = Size.X;
                desc.Height = Size.Y;
                desc.Format = m_resourceFormat;
                desc.ArraySize = 1;
                desc.MipLevels = 1;
                desc.BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess | BindFlags.RenderTarget;
                desc.Usage = ResourceUsage.Default;
                desc.CpuAccessFlags = 0;
                desc.SampleDescription.Count = m_samplesCount;
                desc.SampleDescription.Quality = m_samplesQuality;
                desc.OptionFlags = 0;
                m_resource = new Texture2D(MyRender11.Device, desc);
                m_resource.DebugName = Name;

                m_formatSRgb.Init(this, Format.R8G8B8A8_UNorm_SRgb);
                m_formatSRgb.OnDeviceInit();

                m_formatLinear.Init(this, Format.R8G8B8A8_UNorm);
                m_formatLinear.OnDeviceInit();
            }

            public void OnDeviceEnd()
            {
                m_resource.Dispose();
                m_resource = null;

                m_formatSRgb.OnDeviceEnd();
                m_formatLinear.OnDeviceEnd();
            }
        }
    }

    class MyCustomTextureManager : IManager, IManagerDevice
    {
        private MyObjectsPool<MyCustomTexture> m_objectsPool = new MyObjectsPool<MyCustomTexture>(16);
        private bool m_isDeviceInit = false;

        public ICustomTexture CreateTexture(string debugName, int width, int height, int samplesCount = 1,
            int samplesQuality = 0)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);

            MyCustomTexture tex;
            m_objectsPool.AllocateOrCreate(out tex);
            tex.Init(debugName, width, height, samplesCount, samplesQuality);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        public int GetTexturesCount()
        {
            return m_objectsPool.ActiveCount;
        }

        public void DisposeTex(ref ICustomTexture texture)
        {
            if (texture == null)
                return;

            MyCustomTexture textureInternal = (MyCustomTexture)texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_objectsPool.Deallocate(textureInternal);
        }

        public void OnDeviceInit()
        {
            m_isDeviceInit = true;
            foreach (var tex in m_objectsPool.Active)
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