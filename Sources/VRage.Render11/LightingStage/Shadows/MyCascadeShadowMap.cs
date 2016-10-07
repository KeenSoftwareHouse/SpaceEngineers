
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    interface ICascadeShadowMapSlice
    {
        bool IsUpdated { get; set; }
        MatrixD MatrixWorldToShadowSpace { get; }
        MatrixD MatrixShadowToWorldSpace { get; }
        Matrix MatrixWorldAt0ToShadowSpace { get; }
        Matrix MatrixShadowToWorldAt0Space { get; }

        void SetVolume(Matrix matrixWorldToShadowSpace);
    }

    interface ICascadeShadowMap
    {
        int SlicesCount { get; }
        ICascadeShadowMapSlice GetSlice(int index);
        ICsmPlacementStrategy CsmPlacementStrategy { get; set; }
        IDepthArrayTexture DepthArrayTexture { get; }
    }

    namespace Internal
    {
        class MyCascadeShadowMapSlice : ICascadeShadowMapSlice
        {
            MyShadowVolume m_shadowVolume;

            public void Init(MyShadowVolume sharedVolume, int texSize)
            {
                IsUpdated = true;
                m_shadowVolume = sharedVolume;
            }

            public void Destroy()
            {
                m_shadowVolume = null;
            }

            public void SetVolume(Matrix matrixWorldToShadowSpaceAt0)
            {
                m_shadowVolume.SetMatrixWorldAt0ToShadow(matrixWorldToShadowSpaceAt0);
            }

            public MatrixD MatrixWorldToShadowSpace
            {
                get { return m_shadowVolume.MatrixWorldToShadowSpace; }
            }

            public MatrixD MatrixShadowToWorldSpace
            {
                get { return Matrix.Invert(MatrixWorldToShadowSpace); }
            }

            public Matrix MatrixWorldAt0ToShadowSpace
            {
                get { return m_shadowVolume.MatrixWorldAt0ToShadowSpace; }
            }

            public Matrix MatrixShadowToWorldAt0Space
            {
                get { return m_shadowVolume.MatrixShadowToWorldAt0Space; }
            }

            public bool IsUpdated { get; set; }

            public MyShadowmapQuery GetShadowmapQuery(IDsvBindable dsv, int slice)
            {
                return m_shadowVolume.GetShadowmapQueryForCsm(dsv, slice);
            }

            public void DrawVolume()
            {
                m_shadowVolume.DrawShadowVolumeIntoWorld();
            }
        }

        class MyCascadeShadowMap : ICascadeShadowMap
        {
            static MyCsmOldPlacementStrategy m_csmDefaultPlacementStrategy = new MyCsmOldPlacementStrategy();

            IDepthArrayTexture m_depthArrayTexture;
            MyCascadeShadowMapSlice[] m_slices;
            MyShadowVolume[] m_volumes;

            public ICsmPlacementStrategy CsmPlacementStrategy { get; set; }
        
            public void Init(int texSize, int numSlices)
            {
                m_volumes = new MyShadowVolume[numSlices];
                for (int i = 0; i < numSlices; i++)
                {
                    m_volumes[i] = new MyShadowVolume();
                }

                m_slices = new MyCascadeShadowMapSlice[numSlices];
                for (int i = 0; i < numSlices; i++)
                {
                    m_slices[i] = new MyCascadeShadowMapSlice();
                    m_slices[i].Init(m_volumes[i], texSize);
                }

                m_depthArrayTexture = MyManagers.ArrayTextures.CreateDepthArray("MyCascadeShadowMap.DepthArrayTexture", texSize, texSize, numSlices,
                    Format.R32_Typeless, Format.R32_Float, Format.D32_Float);

                CsmPlacementStrategy = m_csmDefaultPlacementStrategy;
            }

            public void Destroy()
            {
                MyManagers.ArrayTextures.DisposeTex(ref m_depthArrayTexture);

                for (int i = 0; i < m_slices.Length; i++)
                    m_slices[i].Destroy();
                m_slices = null;

                m_volumes = null;
            }

            public void Update(ref MyShadowsSettings settings)
            {
                CsmPlacementStrategy.Update(m_volumes, ref settings, DepthArrayTexture.Size.X);
            }

            public void AddShadowmapQuery(ref List<MyShadowmapQuery> shadowmapQueries)
            {
                for (int i = 0; i < m_slices.Length; i++)
                {
                    MyCascadeShadowMapSlice slice = m_slices[i];
                    if (slice.IsUpdated)
                    {
                        MyRender11.RC.ClearDsv(m_depthArrayTexture.SubresourceDsv(i), DepthStencilClearFlags.Depth, 1.0f, 0);
                        shadowmapQueries.Add(slice.GetShadowmapQuery(m_depthArrayTexture.SubresourceDsv(i), i));
                    }
                }
            }

            public int SlicesCount { get { return m_slices.Length; } }

            public ICascadeShadowMapSlice GetSlice(int index)
            {
                return m_slices[index];
            }

            public IDepthArrayTexture DepthArrayTexture
            {
                get { return m_depthArrayTexture; }
            }

            public void DrawVolumes()
            {
                foreach (var slice in m_slices)
                    slice.DrawVolume();
            }
        }
    }
}
