using System.Collections.Generic;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;

namespace VRage.Render11.LightingStage.Shadows
{
    interface ISingleShadowmap
    {
        void SetPosition(Matrix matrix);
        bool IsUpdated { get; set; }
    }
    namespace Internal
    {
        class MySingleShadowmap : ISingleShadowmap
        {
            MyShadowVolume m_shadowVolume;
            IDepthTexture m_depthTexture;

            public bool IsUpdated { get; set; }

            public void SetPosition(Matrix matrixShadowToWorldAt0)
            {
                m_shadowVolume.SetMatrixWorldAt0ToShadow(matrixShadowToWorldAt0);
            }

            public void Init(int texSize)
            {
                if (m_depthTexture != null)
                    Destroy();

                m_depthTexture = MyManagers.RwTextures.CreateDepth("MySingleShadowmap.DepthTexture", texSize, texSize,
                    Format.R32_Typeless, Format.R32_Float, Format.D32_Float);

                SetPosition(Matrix.Identity);
            }

            public void Destroy()
            {
                MyManagers.RwTextures.DisposeTex(ref m_depthTexture);
            }

            public void AddShadowmapQuery(int index, List<MyShadowmapQuery> shadowmapQueries)
            {
                shadowmapQueries.Add(m_shadowVolume.GetShadowmapQueryForSingleShadow(index, m_depthTexture));
            }

            public void DrawVolume()
            {
                m_shadowVolume.DrawShadowVolumeIntoWorld();
            }
        }
    }
}
