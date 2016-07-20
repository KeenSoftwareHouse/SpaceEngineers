using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using VRage.Collections;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{                     
    struct MyViewTransformation
    {
        internal Matrix View3x3;
        internal Vector3D CameraPosition;
    }

    partial class MyRender11
    {
        internal static bool UseComplementaryDepthBuffer = true;
        internal static float DepthClearValue { get { return UseComplementaryDepthBuffer ? 0 : 1; } }
    }

    struct MyProjectionInfo
    {
        internal MatrixD WorldToProjection;
        internal MatrixD LocalToProjection;
        internal Vector3D WorldCameraOffsetPosition;

        internal MatrixD CurrentLocalToProjection { get { return MatrixD.CreateTranslation(MyRender11.Environment.CameraPosition - WorldCameraOffsetPosition) * LocalToProjection; } }
    }

    internal struct MyShadowmapQuery
    {
        internal DepthStencilView DepthBuffer;
        internal MyViewport Viewport;
        internal MyProjectionInfo ProjectionInfo;
        internal Vector3 ProjectionDir;
        internal float ProjectionFactor;
        internal MyFrustumEnum QueryType;
        internal int CascadeIndex;

        internal HashSet<uint> IgnoredEntities;
    }

    class MyLightsCameraDistanceComparer : IComparer<LightId> {

        public int Compare(LightId x, LightId y)
        {
            return x.ViewerDistanceSquared.CompareTo(y.ViewerDistanceSquared);
        }
    }

    internal class MyShadows
    {
        const int MAX_SPOTLIGHT_SHADOWCASTERS = 4;
        const int SpotlightShadowmapSize = 512;

        #region Fields
        internal static int OtherShadowsTriangleCounter = -1;

        private MyShadowCascades m_cascadeHandler;

        private readonly List<MyShadowmapQuery> m_shadowmapQueries = new List<MyShadowmapQuery>();
        internal readonly List<RwTexId> ShadowmapsPool = new List<RwTexId>();
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
                MyRender11.DeviceContext.ClearDepthStencilView(m_shadowmapQueries[shadowQueryIndex].DepthBuffer, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            }

            return m_shadowmapQueries;
        }

        internal void PostProcess()
        {
            m_cascadeHandler.PostProcess(MyRender11.PostProcessedShadows, MyScene.SeparateGeometry ? MyShadowCascades.CombineShadowmapArray : m_cascadeHandler.CascadeShadowmapArray);
        }

        private void PrepareSpotlights()
        {
            MyLights.Update();

            MyLights.SpotlightsBvh.OverlapAllFrustum(ref MyRender11.Environment.ViewFrustumClippedD, MyLightRendering.VisibleSpotlights);

            if (MyLightRendering.VisibleSpotlights.Count == 0)
                OtherShadowsTriangleCounter = 0;

            MyLightRendering.VisibleSpotlights.Sort(m_spotlightCastersComparer);
            
            int index = 0;
            int casterIndex = 0;
            var worldMatrix = MatrixD.CreateTranslation(MyRender11.Environment.CameraPosition);
            foreach (var id in MyLightRendering.VisibleSpotlights)
            {
                if (id.CastsShadows && casterIndex < MAX_SPOTLIGHT_SHADOWCASTERS)
                {
                    if (ShadowmapsPool.Count <= casterIndex)
                        ShadowmapsPool.Add(MyRwTextures.CreateShadowmap(SpotlightShadowmapSize, SpotlightShadowmapSize));

                    MyLights.Lights.Data[id.Index].CastsShadowsThisFrame = true;

                    MatrixD viewProjection = MyLights.GetSpotlightViewProjection(id);
                    var query = new MyShadowmapQuery
                    {
                        DepthBuffer = ShadowmapsPool[casterIndex].Dsv,
                        Viewport = new MyViewport(SpotlightShadowmapSize, SpotlightShadowmapSize),
                        QueryType = MyFrustumEnum.ShadowProjection,
                        ProjectionInfo = new MyProjectionInfo
                        {
                            WorldCameraOffsetPosition = MyRender11.Environment.CameraPosition,
                            WorldToProjection = viewProjection,
                            LocalToProjection = worldMatrix * viewProjection
                        },
                        IgnoredEntities = MyLights.IgnoredEntitites.ContainsKey(id) ? MyLights.IgnoredEntitites[id] : null,
                    };
                    m_shadowmapQueries.Add(query);
                    ++casterIndex;
                }
                else
                {
                    MyLights.Lights.Data[id.Index].CastsShadowsThisFrame = false;
                }

                index++;
            }
        }
    }
}
