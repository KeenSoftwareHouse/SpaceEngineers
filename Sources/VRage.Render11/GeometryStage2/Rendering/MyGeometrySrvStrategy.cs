using System;
using System.Collections.Generic;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Rendering
{
    interface IGeometrySrvStrategy
    {
        ISrvBindable[] GetSrvs(MyPart part);
    }

    class MyStandardGeometrySrvStrategy: IGeometrySrvStrategy
    {
        public ISrvBindable[] GetSrvs(MyPart part)
        {     
            return part.StandardMaterial.Srvs;
        }
    }

    class MyLodGeometrySrvStrategy : IGeometrySrvStrategy
    {
        ITexture[] m_fileTextures;
        readonly FastResourceLock m_lock = new FastResourceLock();

        void PrepareFileTextures()
        {
            if (m_fileTextures != null)
                return;
            m_fileTextures = new ITexture[6];
            m_fileTextures[0] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Red.dds", MyFileTextureEnum.COLOR_METAL);
            m_fileTextures[1] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Green.dds", MyFileTextureEnum.COLOR_METAL);
            m_fileTextures[2] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Blue.dds", MyFileTextureEnum.COLOR_METAL);
            m_fileTextures[3] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Yellow.dds", MyFileTextureEnum.COLOR_METAL);
            m_fileTextures[4] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Cyan.dds", MyFileTextureEnum.COLOR_METAL);
            m_fileTextures[5] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Magenta.dds", MyFileTextureEnum.COLOR_METAL);
        }

        ISrvBindable[] m_tmpSrvs = new ISrvBindable[4];
        public ISrvBindable[] GetSrvs(MyPart part)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                PrepareFileTextures();

                ISrvBindable[] srvs = part.StandardMaterial.Srvs;
                for (int i = 0; i < srvs.Length; i++)
                    m_tmpSrvs[i] = srvs[i];
                for (int i = srvs.Length; i < m_tmpSrvs.Length; i++)
                    m_tmpSrvs[i] = null;

                int lodNum = part.Parent.LodNum;
                m_tmpSrvs[0] = m_fileTextures[lodNum%m_fileTextures.Length];
                m_tmpSrvs[1] = MyGeneratedTextureManager.ReleaseMissingNormalGlossTex;
                m_tmpSrvs[2] = MyGeneratedTextureManager.ReleaseMissingExtensionTex;

                return m_tmpSrvs;
            }
        }
    }

    class MyMipmapGeometrySrvStrategy : IGeometrySrvStrategy
    {
        Dictionary<Vector2I, ISrvBindable> m_textures = new Dictionary<Vector2I, ISrvBindable>();
        ITexture[] m_fileTextures;
        ITexture m_blackTexture;
        readonly FastResourceLock m_lock = new FastResourceLock();

        void PrepareFileTextures()
        {
            if (m_fileTextures != null)
                return;
            m_fileTextures = new ITexture[6];
            m_fileTextures[0] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Red.dds", MyFileTextureEnum.COLOR_METAL, true);
            m_fileTextures[1] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Green.dds", MyFileTextureEnum.COLOR_METAL, true);
            m_fileTextures[2] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Blue.dds", MyFileTextureEnum.COLOR_METAL, true);
            m_fileTextures[3] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Yellow.dds", MyFileTextureEnum.COLOR_METAL, true);
            m_fileTextures[4] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Cyan.dds", MyFileTextureEnum.COLOR_METAL, true);
            m_fileTextures[5] = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Magenta.dds", MyFileTextureEnum.COLOR_METAL, true);

            m_blackTexture = MyManagers.FileTextures.GetTexture("Textures\\Debug\\Black.dds", MyFileTextureEnum.COLOR_METAL, true);
        }

        ISrvBindable CreateNewTexture(Vector2I resolution)
        {
            string debugName = string.Format("Debug-mipmap-{0}x{1}", resolution.X, resolution.Y);
            int mipmapLevels = MyResourceUtils.GetMipmapsCount(Math.Max(resolution.X, resolution.Y));
            resolution.X = MyResourceUtils.GetMipmapStride(resolution.X, 0); // this is required by texture compression
            resolution.Y = MyResourceUtils.GetMipmapStride(resolution.Y, 0); // this is required by texture compression
            ISrvTexture dstTex = MyManagers.RwTextures.CreateSrv(debugName, resolution.X, resolution.Y, Format.BC7_UNorm_SRgb, mipmapLevels: mipmapLevels);

            for (int i = 0; i < mipmapLevels; i++)
            {
                ISrvBindable srcTex = m_fileTextures[i % m_fileTextures.Length];
                Vector2I mipmapResolution = new Vector2I(MyResourceUtils.GetMipmapSize(resolution.X, i),
                    MyResourceUtils.GetMipmapSize(resolution.Y, i));
                for (int x = 0; x < mipmapResolution.X; x += srcTex.Size.X)
                    for (int y = 0; y < mipmapResolution.Y; y += srcTex.Size.Y)
                        MyRender11.RC.CopySubresourceRegion(srcTex, 0, null, dstTex, i, x, y);
            }

            return dstTex;
        }

        ISrvBindable[] m_tmpSrvs = new ISrvBindable[4];
        public ISrvBindable[] GetSrvs(MyPart part)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                PrepareFileTextures();

                Vector2I texResolution = new Vector2I(0, 0);

                if (part.StandardMaterial.Srvs != null && part.StandardMaterial.Srvs.Length >= 1)
                    texResolution = part.StandardMaterial.Srvs[0].Size;

                ISrvBindable gbuffer0Tex;
                // if the texture does not have size, resolving of mipmaping cannot be used
                if (texResolution == new Vector2I(0, 0)) 
                    gbuffer0Tex = m_blackTexture;
                else if (!m_textures.TryGetValue(texResolution, out gbuffer0Tex))
                {
                    gbuffer0Tex = CreateNewTexture(texResolution);
                    m_textures.Add(texResolution, gbuffer0Tex);
                }

                m_tmpSrvs[0] = gbuffer0Tex;
                m_tmpSrvs[1] = MyGeneratedTextureManager.ReleaseMissingNormalGlossTex;
                m_tmpSrvs[2] = MyGeneratedTextureManager.ReleaseMissingExtensionTex;

                return m_tmpSrvs;
            }
        }
    }

    class MyGeometrySrvResolver: IManager
    {
        IGeometrySrvStrategy m_standardStrategy = new MyStandardGeometrySrvStrategy();
        IGeometrySrvStrategy m_lodStrategy = new MyLodGeometrySrvStrategy();
        IGeometrySrvStrategy m_mipmapStrategy = new MyMipmapGeometrySrvStrategy();

        public IGeometrySrvStrategy GetGeometrySrvStrategy()
        {
            if (MyRender11.Settings.DisplayGbufferLOD)
                return m_lodStrategy;
            if (MyRender11.Settings.DisplayMipmap)
                return m_mipmapStrategy;
            return m_standardStrategy;
        }
    }
}
