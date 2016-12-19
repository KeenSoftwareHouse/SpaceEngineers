using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Textures;

namespace VRage.Render11.Resources
{
    internal interface ISrvTexture : ISrvBindable, ITexture
    { }

    internal interface IRtvTexture : ISrvTexture, IRtvBindable
    { }

    internal interface IUavTexture : IRtvTexture, IUavBindable
    { }

    internal interface IDepthTexture : ISrvTexture, IDsvBindable
    { }


    internal class MyRwTextureManager : IManager, IManagerDevice
    {
        MyTextureStatistics m_statistics = new MyTextureStatistics();
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

            m_statistics.Add(tex);
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

            m_statistics.Add(tex);
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

            m_statistics.Add(tex);
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

            m_statistics.Add(tex);
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

            MySrvTexture textureInternal = (MySrvTexture)texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_srvTextures.Deallocate(textureInternal);
            m_statistics.Remove(textureInternal);
            texture = null;
        }

        public void DisposeTex(ref IRtvTexture texture)
        {
            if (texture == null)
                return;

            MyRtvTexture textureInternal = (MyRtvTexture)texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_rtvTextures.Deallocate(textureInternal);
            m_statistics.Remove(textureInternal);
            texture = null;
        }

        public void DisposeTex(ref IUavTexture texture)
        {
            if (texture == null)
                return;

            MyUavTexture textureInternal = (MyUavTexture)texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_uavTextures.Deallocate(textureInternal);
            m_statistics.Remove(textureInternal);
            texture = null;
        }

        public void DisposeTex(ref IDepthTexture texture)
        {
            if (texture == null)
                return;

            MyDepthTexture textureInternal = (MyDepthTexture)texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            m_depthTextures.Deallocate(textureInternal);
            m_statistics.Remove(textureInternal);
            texture = null;
        }

        public void OnDeviceInit()
        {
            m_isDeviceInit = true;
            foreach (var tex in m_srvTextures.Active)
                tex.OnDeviceInit();
            foreach (var tex in m_rtvTextures.Active)
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

        public MyTextureStatistics Statistics
        {
            get { return m_statistics; }
        }
    }
}
