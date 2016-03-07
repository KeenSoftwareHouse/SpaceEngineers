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

        internal MatrixD CurrentLocalToProjection { get { return MatrixD.CreateTranslation(MyEnvironment.CameraPosition - WorldCameraOffsetPosition) * LocalToProjection; } }
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
            MyLights.SpotlightsBvh.OverlapAllFrustum(ref MyEnvironment.ViewFrustumClippedD, MyLightRendering.VisibleSpotlights);

            if (MyLightRendering.VisibleSpotlights.Count == 0)
                OtherShadowsTriangleCounter = 0;
            
            MyLightRendering.VisibleSpotlights.Sort(m_spotlightCastersComparer);
            MyArrayHelpers.Reserve(ref MyLightRendering.Spotlights, MyLightRendering.VisibleSpotlights.Count);

            int index = 0;
            int casterIndex = 0;
            foreach (var id in MyLightRendering.VisibleSpotlights)
            {
                var nearPlaneDistance = 0.5f;
                var worldMatrix = MatrixD.CreateTranslation(MyEnvironment.CameraPosition);
                var viewMatrix = MatrixD.CreateLookAt(id.Position, id.Position + MyLights.Spotlights[id.Index].Direction, MyLights.Spotlights[id.Index].Up);
                var projectionMatrix = MatrixD.CreatePerspectiveFieldOfView((float)(Math.Acos(MyLights.Spotlights[id.Index].ApertureCos) * 2), 1.0f, nearPlaneDistance, Math.Max(id.ShadowDistance, nearPlaneDistance));
                var viewProjection = viewMatrix * projectionMatrix;
                MatrixD worldViewProjection = worldMatrix * viewProjection;
                MyLightRendering.Spotlights[index].ShadowMatrix = Matrix.Transpose(worldViewProjection * MyMatrixHelpers.ClipspaceToTexture);

                if (id.CastsShadows && casterIndex < MAX_SPOTLIGHT_SHADOWCASTERS)
                {
                    if(ShadowmapsPool.Count <= casterIndex)
                        ShadowmapsPool.Add(MyRwTextures.CreateShadowmap(SpotlightShadowmapSize, SpotlightShadowmapSize));

                    MyLights.Lights.Data[id.Index].CastsShadowsThisFrame = true;

                    var query = new MyShadowmapQuery
                    {
                        DepthBuffer = ShadowmapsPool[casterIndex].Dsv,
                        Viewport = new MyViewport(SpotlightShadowmapSize, SpotlightShadowmapSize),
                        QueryType = MyFrustumEnum.ShadowProjection,
                        ProjectionInfo = new MyProjectionInfo
                        {
                            WorldCameraOffsetPosition = MyEnvironment.CameraPosition,
                            WorldToProjection = viewProjection,
                            LocalToProjection = worldViewProjection
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
                MyLights.WriteSpotlightConstants(id, ref MyLightRendering.Spotlights[index]);

                index++;
            }
        }
    }
}
