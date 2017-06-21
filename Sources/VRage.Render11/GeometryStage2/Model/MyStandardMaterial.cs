using System;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    struct MyStandardMaterialKey : IEquatable<MyStandardMaterialKey>
    {
        public MyMeshDrawTechnique Technique;
        public string CmFilepath;
        public string NgFilepath;
        public string ExtFilepath;
        public string AlphamaskFilepath;

        public override bool Equals(object key)
        {
            if (!(key is MyStandardMaterialKey))
                return false;

            return Equals((MyStandardMaterialKey)key);
        }

        public bool Equals(MyStandardMaterialKey key)
        {
            if (Technique != key.Technique)
                return false;
            if (CmFilepath != key.CmFilepath)
                return false;
            if (NgFilepath != key.NgFilepath)
                return false;
            if (ExtFilepath != key.ExtFilepath)
                return false;
            if (AlphamaskFilepath != key.AlphamaskFilepath)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return Technique.GetHashCode() ^ CmFilepath.GetHashCode() ^ NgFilepath.GetHashCode() ^ ExtFilepath.GetHashCode() ^ AlphamaskFilepath.GetHashCode();
        }
    }

    class MyStandardMaterial
    {
        ISrvBindable[] m_srvs;
        IDepthStencilState m_depthStencilState;
        IBlendState m_blendState;
        IRasterizerState m_rasterizerState;
        bool m_isFaceCullingEnabled;

        public void InitStandard(string cmFilepath, string ngFilepath, string extFilepath)
        {
            m_srvs = new ISrvBindable[]
            {
                MyManagers.FileTextures.GetTexture(cmFilepath, MyFileTextureEnum.COLOR_METAL),
                MyManagers.FileTextures.GetTexture(ngFilepath, MyFileTextureEnum.NORMALMAP_GLOSS),
                MyManagers.FileTextures.GetTexture(extFilepath, MyFileTextureEnum.EXTENSIONS)
            };
            m_depthStencilState = MyDepthStencilStateManager.DefaultDepthState;
            m_blendState = null;
            m_rasterizerState = null;
            m_isFaceCullingEnabled = true;
        }

        IBlendState GetAlphamaskBlendState(string cmFilepath, string ngFilepath, string extFilepath, bool isPremultipliedAlpha)
        {
            bool isCm = !string.IsNullOrEmpty(cmFilepath);
            bool isNg = !string.IsNullOrEmpty(ngFilepath);
            bool isExt = !string.IsNullOrEmpty(extFilepath);
            if (isCm && isNg && isExt)
            {
                if (isPremultipliedAlpha)
                    return MyBlendStateManager.BlendDecalNormalColorExt;
                else
                    return MyBlendStateManager.BlendDecalNormalColorExtNoPremult;
            }
            if (isCm && isNg && !isExt)
            {
                if (isPremultipliedAlpha)
                    return MyBlendStateManager.BlendDecalNormalColor;
                else
                    return MyBlendStateManager.BlendDecalNormalColorNoPremult;
            }
            if (!isCm && isNg && !isExt)
            {
                if (isPremultipliedAlpha)
                    return MyBlendStateManager.BlendDecalNormal;
                else
                    return MyBlendStateManager.BlendDecalNormalNoPremult;
            }
            if (isCm && !isNg && !isExt)
            {
                if (isPremultipliedAlpha)
                    return MyBlendStateManager.BlendDecalColor;
                else
                    return MyBlendStateManager.BlendDecalColorNoPremult;
            }
            MyRenderProxy.Error("Unknown alphamask texture pattern");
            return null;
        }

        public void InitAlphamask(string cmFilepath, string ngFilepath, string extFilepath, string alphamaskFilepath)
        {
            m_srvs = new ISrvBindable[]
            {
                MyManagers.FileTextures.GetTexture(cmFilepath, MyFileTextureEnum.COLOR_METAL),
                MyManagers.FileTextures.GetTexture(ngFilepath, MyFileTextureEnum.NORMALMAP_GLOSS),
                MyManagers.FileTextures.GetTexture(extFilepath, MyFileTextureEnum.EXTENSIONS),
                MyManagers.FileTextures.GetTexture(alphamaskFilepath, MyFileTextureEnum.ALPHAMASK)
            };
            m_depthStencilState = MyDepthStencilStateManager.DefaultDepthState;
            m_blendState = null;            m_rasterizerState = null;
            m_isFaceCullingEnabled = false;
        }

        public void InitDecal(string cmFilepath, string ngFilepath, string extFilepath, string alphamaskFilepath, bool isPremultipliedAlpha, bool isCutout)
        {
            m_srvs = new ISrvBindable[]
            {
                MyManagers.FileTextures.GetTexture(cmFilepath, MyFileTextureEnum.COLOR_METAL),
                MyManagers.FileTextures.GetTexture(ngFilepath, MyFileTextureEnum.NORMALMAP_GLOSS),
                MyManagers.FileTextures.GetTexture(extFilepath, MyFileTextureEnum.EXTENSIONS),
                MyManagers.FileTextures.GetTexture(alphamaskFilepath, MyFileTextureEnum.ALPHAMASK)
            }; 
            m_depthStencilState = MyDepthStencilStateManager.DepthTestReadOnly;
            if (isCutout)
                m_blendState = null;
            else
                m_blendState = GetAlphamaskBlendState(cmFilepath, ngFilepath, extFilepath, isPremultipliedAlpha);
            m_rasterizerState = MyRasterizerStateManager.DecalRasterizerState;
            m_isFaceCullingEnabled = true;
        }

        public void Init(MyStandardMaterialKey key)
        {
            if (key.Technique == MyMeshDrawTechnique.MESH)
                InitStandard(key.CmFilepath, key.NgFilepath, key.ExtFilepath);
            else if (key.Technique == MyMeshDrawTechnique.DECAL)
                InitDecal(key.CmFilepath, key.NgFilepath, key.ExtFilepath, key.AlphamaskFilepath, true, false);
            else if (key.Technique == MyMeshDrawTechnique.DECAL_NOPREMULT)
                InitDecal(key.CmFilepath, key.NgFilepath, key.ExtFilepath, key.AlphamaskFilepath, false, false);
            else if (key.Technique == MyMeshDrawTechnique.DECAL_CUTOUT)
                InitDecal(key.CmFilepath, key.NgFilepath, key.ExtFilepath, key.AlphamaskFilepath, true, true);
            else if (key.Technique == MyMeshDrawTechnique.ALPHA_MASKED)
                InitAlphamask(key.CmFilepath, key.NgFilepath, key.ExtFilepath, key.AlphamaskFilepath);
            else if (key.Technique == MyMeshDrawTechnique.GLASS)
                MyRenderProxy.Error("Glass material cannot be processed by this object");
            else
                MyRenderProxy.Error("Material is not resolved, please extend the functionality of the new pipeline or move object to the old pipeline");
        }

        public ISrvBindable[] Srvs
        {
            get { return m_srvs; }
        }

        public IDepthStencilState DepthStencilState
        {
            get { return m_depthStencilState; }
        }

        public IBlendState BlendState
        {
            get { return m_blendState;}
        }

        public IRasterizerState RasterizerState
        {
            get { return m_rasterizerState; }
        }

        public bool IsFaceCullingEnabled
        {
            get { return m_isFaceCullingEnabled; }
        }
    }
}
