using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Generics;
using VRageMath;

namespace VRageRender
{
    enum MyFrustumEnum
    {
        Unassigned,
        MainFrustum,
        ShadowCascade,
        ShadowProjection
    }

    class MyFrustumCullQuery
    {
        internal int Bitmask { get; set; }
        internal BoundingFrustumD Frustum { get; set; }
        internal readonly List<MyCullProxy> List = new List<MyCullProxy>();
        internal readonly List<bool> IsInsideList = new List<bool>();
        internal readonly List<MyCullProxy_2> List2 = new List<MyCullProxy_2>();
        internal readonly List<bool> IsInsideList2 = new List<bool>();
        internal MyCullingSmallObjects? SmallObjects;
        internal MyFrustumEnum Type;
        internal int CascadeIndex;
        internal HashSet<uint> Ignored;


        internal void Clear()
        {
            Bitmask = 0;

            if (Frustum != null)
            {
                DeallocateFrustum(Frustum);
                Frustum = null;
            }

            List.Clear();
            IsInsideList.SetSize(0);

            List2.Clear();
            IsInsideList2.SetSize(0);

            SmallObjects = null;
            Type = MyFrustumEnum.Unassigned;
            CascadeIndex = 0;
            Ignored = null;
        }

        #region Frustum pool
        private readonly static MyDynamicObjectPool<BoundingFrustumD> m_frustumPool = new MyDynamicObjectPool<BoundingFrustumD>(8);

        internal BoundingFrustumD AllocateFrustum()
        {
            return m_frustumPool.Allocate();
        }

        internal void DeallocateFrustum(BoundingFrustumD frustum)
        {
            m_frustumPool.Deallocate(frustum);
        }

        #endregion
    }

    class MyCullQuery
    {
        private const int MAX_FRUSTUM_CULL_QUERY_COUNT = 32;
        internal readonly MyFrustumCullQuery[] FrustumCullQueries = new MyFrustumCullQuery[MAX_FRUSTUM_CULL_QUERY_COUNT];
        internal readonly MyRenderingPass[] RenderingPasses = new MyRenderingPass[MAX_FRUSTUM_CULL_QUERY_COUNT];

        internal int Size { get; private set; }

        internal MyCullQuery()
        {
            for (int i = 0; i < MAX_FRUSTUM_CULL_QUERY_COUNT; i++)
            {
                FrustumCullQueries[i] = new MyFrustumCullQuery();
            }
        }

        internal void Reset()
        {
            for (int frustumCullQueryIndex = 0; frustumCullQueryIndex < Size; ++frustumCullQueryIndex)
            {
                FrustumCullQueries[frustumCullQueryIndex].Clear();
                if (RenderingPasses[frustumCullQueryIndex] != null)
                {
                    MyObjectPoolManager.Deallocate(RenderingPasses[frustumCullQueryIndex]);
                    RenderingPasses[frustumCullQueryIndex] = null;
                }
            }
            Size = 0;
        }

        internal void AddMainViewPass(MyViewport viewport, MyGBuffer gbuffer)
        {
            int frustumMask = AddFrustum(ref MyEnvironment.ViewProjectionD);
            FrustumCullQueries[Size - 1].Type = MyFrustumEnum.MainFrustum;

            var pass = MyObjectPoolManager.Allocate<MyGBufferPass>();
            pass.ProcessingMask = frustumMask;
            pass.ViewProjection = MyEnvironment.ViewProjectionAt0;
            pass.Viewport = viewport;
            pass.GBuffer = gbuffer;

            pass.PerFrame();

            RenderingPasses[Size - 1] = pass;
        }

        internal void AddForwardPass(ref Matrix offsetedViewProjection, ref MatrixD viewProjection, MyViewport viewport, DepthStencilView dsv, RenderTargetView rtv)
        {
            int frustumMask = AddFrustum(ref viewProjection);

            MyForwardPass pass = MyObjectPoolManager.Allocate<MyForwardPass>();
            pass.ProcessingMask = frustumMask;
            pass.ViewProjection = offsetedViewProjection;
            pass.Viewport = viewport;
            pass.DSV = dsv;
            pass.RTV = rtv;

            pass.PerFrame();

            RenderingPasses[Size - 1] = pass;
        }

        internal void AddDepthPass(ref MatrixD worldToProjection, Matrix viewProjectionLocal, MyViewport viewport, DepthStencilView depthTarget, bool isCascade, string debugName)
        {
            int frustumMask = AddFrustum(ref worldToProjection);

            MyDepthPass pass = MyObjectPoolManager.Allocate<MyDepthPass>();
            pass.DebugName = debugName;
            pass.ProcessingMask = frustumMask;
            pass.ViewProjection = viewProjectionLocal;
            pass.Viewport = viewport;

            pass.DSV = depthTarget;
            pass.DefaultRasterizer = isCascade ? MyRender11.m_cascadesRasterizerState : MyRender11.m_shadowRasterizerState;

            pass.PerFrame();

            RenderingPasses[Size - 1] = pass;
        }

        private int AddFrustum(ref MatrixD frustumMatrix)
        {
            Debug.Assert(Size < MAX_FRUSTUM_CULL_QUERY_COUNT);
            FrustumCullQueries[Size].Clear();
            var frustum = FrustumCullQueries[Size].AllocateFrustum();
            frustum.Matrix = frustumMatrix;
            FrustumCullQueries[Size].Frustum = frustum;
            var bitmask = 1 << Size;
            FrustumCullQueries[Size].Bitmask = bitmask;
            ++Size;
            return bitmask;
        }
    };

    enum MyCullingType
    {
        FrustumCulling,
        CHCOcclusion,
    }

    abstract class MyVisibilityCuller
    {
        public static string ToString(MyFrustumEnum frustumEnum)
        {
            switch (frustumEnum)
            {
                case MyFrustumEnum.MainFrustum:
                    return "MainFrustum";
                case MyFrustumEnum.ShadowCascade:
                    return "ShadowCascade";
                case MyFrustumEnum.ShadowProjection:
                    return "ShadowProjection";
                default:
                    return "Unassigned";
            }
        }

        internal static void PrepareCullQuery(MyCullQuery cullQuery, ListReader<MyShadowmapQuery> shadowmapQueries, bool updateEnvironmentMap)
        {
            cullQuery.AddMainViewPass(new MyViewport(MyRender11.ViewportResolution), MyGBuffer.Main);

            foreach (var shadowmapQuery in shadowmapQueries)
            {
                bool isCascade = shadowmapQuery.QueryType == MyFrustumEnum.ShadowCascade;
                var matrix = shadowmapQuery.ProjectionInfo.WorldToProjection;
                cullQuery.AddDepthPass(ref matrix, shadowmapQuery.ProjectionInfo.CurrentLocalToProjection, shadowmapQuery.Viewport, shadowmapQuery.DepthBuffer, isCascade, ToString(shadowmapQuery.QueryType));

                if (isCascade)
                {
                    var smallCulling = new MyCullingSmallObjects
                    {
                        ProjectionDir = shadowmapQuery.ProjectionDir,
                        ProjectionFactor = shadowmapQuery.ProjectionFactor,
                        SkipThreshold = MyRenderProxy.Settings.ShadowCascadeSmallSkipThresholds[shadowmapQuery.CascadeIndex]
                    };
                    cullQuery.FrustumCullQueries[cullQuery.Size - 1].SmallObjects = smallCulling;
                    cullQuery.FrustumCullQueries[cullQuery.Size - 1].CascadeIndex = shadowmapQuery.CascadeIndex;
                }

                cullQuery.FrustumCullQueries[cullQuery.Size - 1].Type = shadowmapQuery.QueryType;
                cullQuery.FrustumCullQueries[cullQuery.Size - 1].Ignored = shadowmapQuery.IgnoredEntities;
            }

            if (updateEnvironmentMap)
                MyEnvironmentProbe.UpdateEnvironmentProbes(cullQuery);
        }

        internal void PerformCulling(MyCullQuery cullQuery, MyDynamicAABBTreeD renderableBVH)
        {
            ProfilerShort.Begin("DispatchCulling");
            DispatchCullQuery(cullQuery, renderableBVH);

            ProfilerShort.BeginNextBlock("ProcessCullResults");
            ProcessCullQueryResults(cullQuery);
            ProfilerShort.End();
        }

        protected abstract void DispatchCullQuery(MyCullQuery frustumCullQueries, MyDynamicAABBTreeD renderables);
        protected abstract void ProcessCullQueryResults(MyCullQuery cullQuery);
        
    }
}
