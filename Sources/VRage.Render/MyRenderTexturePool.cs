using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public struct MyRenderTextureId
    {
        public long EntityId;
        public uint RenderObjectId;
    }
    public class MyRenderTexturePool
    {
        const int POOL_RESOURCES = 512*512* 20;
        private static int m_maxFreeResources = POOL_RESOURCES;
        static int m_currentFreeResources = m_maxFreeResources;
        static float m_renderQualityScale = 1;

        static Dictionary<MyRenderTextureId, Texture> m_renderTextureTargetsInUse = new Dictionary<MyRenderTextureId, Texture>();
        static Dictionary<MyRenderTextureId, Texture> m_pooledRenderTextureTargets = new Dictionary<MyRenderTextureId, Texture>();
        static List<MyRenderTextureId> m_texturesToRemove = new List<MyRenderTextureId>();

        static public Texture GetRenderTexture(MyRenderTextureId id, int resolution, int aspectRatio)
        {
            resolution = (int)(resolution*m_renderQualityScale);
            Texture renderTexture = null;
            int textureResourceSize = resolution*resolution * aspectRatio;
            if (false == m_renderTextureTargetsInUse.TryGetValue(id, out renderTexture) && m_currentFreeResources >= textureResourceSize)
            {
                renderTexture = FindTextureInPool(id,resolution, aspectRatio);
                if (renderTexture != null)
                {
                    m_currentFreeResources -= textureResourceSize;
                    renderTexture.AutoMipGenerationFilter = TextureFilter.Linear;
                    m_renderTextureTargetsInUse[id] = renderTexture;
                }               
            }
            return renderTexture;
        }
        static Texture FindTextureInPool(MyRenderTextureId id, int resolution, int aspectRatio)
        {
            int textureResourceSizeNeeded = resolution * resolution * aspectRatio;
            //first check if we have some pooledTexture
            Texture tex = null;
            if (m_pooledRenderTextureTargets.TryGetValue(id, out tex) && tex != null)
            {
                m_pooledRenderTextureTargets.Remove(id);
                return tex;
            }
            foreach (var texture in m_pooledRenderTextureTargets)
            {
                SurfaceDescription desc = texture.Value.GetLevelDescription(0);
                if (desc.Height == resolution && desc.Width == resolution * aspectRatio)
                {
                    m_pooledRenderTextureTargets.Remove(texture.Key);
                    return texture.Value;
                }
            }

            //try to free some resources

            int textureResourceSizeFreed = 0;
            m_texturesToRemove.Clear();
            foreach (var texture in m_pooledRenderTextureTargets)
            {
                SurfaceDescription desc = texture.Value.GetLevelDescription(0);
                textureResourceSizeFreed += desc.Width * desc.Height;
                m_currentFreeResources += textureResourceSizeFreed;
                texture.Value.Dispose();
                m_texturesToRemove.Add(texture.Key);
                if (textureResourceSizeFreed >= textureResourceSizeNeeded)
                {
                    break;
                }
            }

            foreach (var texture in m_texturesToRemove)
            {
                m_pooledRenderTextureTargets.Remove(texture);
            }

            return new Texture(MyRender.GraphicsDevice, resolution * aspectRatio, resolution, 0, Usage.RenderTarget | Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Default);
        }
        static public bool ReleaseRenderTexture(MyRenderTextureId id)
        {
            Texture renderTexture = null;
            if (true == m_renderTextureTargetsInUse.TryGetValue(id, out renderTexture))
            {
                SurfaceDescription desc = renderTexture.GetLevelDescription(0);
                int textureResourceSize = desc.Width* desc.Height;
                m_currentFreeResources += textureResourceSize;
                m_pooledRenderTextureTargets.Add(id, renderTexture);
                m_renderTextureTargetsInUse.Remove(id);
               return true;
            }
            return false;
        }
        static public int FreeResourcesCount()
        {
            return (int)(m_currentFreeResources / (m_renderQualityScale * m_renderQualityScale));
        }
        static public void ReleaseResources()
        {
            foreach (var texture in m_pooledRenderTextureTargets)
            {
                texture.Value.Dispose();
            }
            m_pooledRenderTextureTargets.Clear();

            foreach (var texture in m_renderTextureTargetsInUse)
            {
                SurfaceDescription desc = texture.Value.GetLevelDescription(0);
                int textureResourceSize = desc.Width* desc.Height;
                m_currentFreeResources += textureResourceSize;
                texture.Value.Dispose();
                MyRenderProxy.TextNotDrawnToTexture(texture.Key.EntityId);
                MyRenderProxy.RenderTextureFreed(FreeResourcesCount());
            }
            m_renderTextureTargetsInUse.Clear();
            m_currentFreeResources = m_maxFreeResources;
        }

        static public float RenderQualityScale()
        {
            return m_renderQualityScale;
        }
        static public void RenderQualityChanged(MyRenderQualityEnum newSettings)
        {
            switch (newSettings)
            {
                case MyRenderQualityEnum.NORMAL :
                    m_renderQualityScale = 0.5f;
                    break;
                case MyRenderQualityEnum.HIGH:
                    m_renderQualityScale = 1.0f;
                    break;
                case MyRenderQualityEnum.EXTREME :
                    m_renderQualityScale = 2.0f;
                    break;        
            }

            // Update available resources according to new render quality
            var newMaxResources = (int)(POOL_RESOURCES * m_renderQualityScale * m_renderQualityScale);
            m_currentFreeResources += newMaxResources - m_maxFreeResources;
            m_maxFreeResources = newMaxResources;
        }
    }
}
