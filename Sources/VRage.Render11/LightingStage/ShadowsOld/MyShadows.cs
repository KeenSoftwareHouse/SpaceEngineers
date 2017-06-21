using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using SharpDX.DXGI;
using VRage.Collections;
using VRage.Render11.Common;
using VRage.Render11.LightingStage;
using VRage.Render11.LightingStage.Shadows;
using VRage.Render11.Resources;
using VRageMath;


namespace VRageRender
{                     
    struct MyViewTransformation
    {
        internal Vector3D CameraPosition;
    }

    partial class MyRender11
    {
        internal static bool UseComplementaryDepthBuffer = true;
        internal static float DepthClearValue { get { return UseComplementaryDepthBuffer ? 0 : 1; } }
    }

    class MyLightsCameraDistanceComparer : IComparer<LightId> {

        public int Compare(LightId x, LightId y)
        {
            return x.ViewerDistanceSquared.CompareTo(y.ViewerDistanceSquared);
        }
    }

    internal class MyShadows: MyImmediateRC
    {
        const int MAX_SPOTLIGHT_SHADOWCASTERS = 4;
        const int SpotlightShadowmapSize = 512;

        #region Fields
        private MyShadowCascades m_cascadeHandler;

        private readonly List<MyShadowmapQuery> m_shadowmapQueries = new List<MyShadowmapQuery>();
        internal readonly List<IDepthTexture> ShadowmapsPool = new List<IDepthTexture>();
        #endregion

        internal MyShadowCascades ShadowCascades { get { return m_cascadeHandler; } }

        static readonly MyLightsCameraDistanceComparer m_spotlightCastersComparer = new MyLightsCameraDistanceComparer();

        internal MyShadows(int numberOfCascades, int cascadeResolution)
        {
            Init(numberOfCascades, cascadeResolution);
        }

        private void Init(int numberOfCascades, int cascadeResolution)
        {
            if (m_cascadeHandler == null)
                m_cascadeHandler = new MyShadowCascades(numberOfCascades, cascadeResolution);
            else
                m_cascadeHandler.Reset(numberOfCascades, cascadeResolution);
        }

        internal void Reset(int numberOfCascades, int cascadeResolution)
        {
            UnloadResources();

            Init(numberOfCascades, cascadeResolution);
        }

        internal void UnloadResources()
        {
            m_cascadeHandler.UnloadResources();
        }

        internal ListReader<MyShadowmapQuery> PrepareQueries()
        {
            m_shadowmapQueries.Clear();

            if (MyRender11.DebugOverrides.SpotLights)
                PrepareSpotlights();
            m_cascadeHandler.PrepareQueries(m_shadowmapQueries);

            for (int shadowQueryIndex = 0; shadowQueryIndex < m_shadowmapQueries.Count; ++shadowQueryIndex)
            {
                RC.ClearDsv(m_shadowmapQueries[shadowQueryIndex].DepthBuffer, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            }

            return m_shadowmapQueries;
        }

        private void PrepareSpotlights()
        {
            int index = 0;
            int casterIndex = 0;
            var worldMatrix = MatrixD.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition);
            foreach (var id in MyLightsRendering.VisibleSpotlights)
            {
                if (id.CastsShadows && casterIndex < MAX_SPOTLIGHT_SHADOWCASTERS)
                {
                    if (ShadowmapsPool.Count <= casterIndex)
                        ShadowmapsPool.Add(MyManagers.RwTextures.CreateDepth("ShadowmapsPool.Item", SpotlightShadowmapSize, SpotlightShadowmapSize, Format.R32_Typeless, Format.R32_Float, Format.D32_Float));

                    MyLights.SetCastsShadowsThisFrame(id, true);

                    MatrixD viewProjection = MyLights.GetSpotlightViewProjection(id);
                    var query = new MyShadowmapQuery
                    {
                        DepthBuffer = ShadowmapsPool[casterIndex],
                        Viewport = new MyViewport(SpotlightShadowmapSize, SpotlightShadowmapSize),
                        QueryType = MyFrustumEnum.ShadowProjection,
                        Index = casterIndex,
                        ProjectionInfo = new MyProjectionInfo
                        {
                            WorldCameraOffsetPosition = MyRender11.Environment.Matrices.CameraPosition,
                            WorldToProjection = viewProjection,
                            LocalToProjection = worldMatrix * viewProjection
                        },
                        IgnoredEntities = MyLights.GetEntitiesIgnoringShadow(id)
                    };
                    m_shadowmapQueries.Add(query);
                    ++casterIndex;
                }
                else
                {
                    MyLights.SetCastsShadowsThisFrame(id, false);
                }

                index++;
            }
        }
    }
}
