using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Color = VRageMath.Color;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;
using ParallelTasks;
using VRage.Library.Utils;
using System.IO;

namespace VRageRender
{
    struct MyRenderCullResult
    {
        internal UInt64 SortKey;
        internal BitVector32 ProcessingMask;
        internal MyRenderableProxy RenderProxy;
    }

    struct MyRenderCullResultFlat
    {
        internal UInt64 SortKey;
        internal MyRenderableProxy RenderProxy;
    }

    class MyCullResultsComparer : IComparer<MyRenderCullResult>, IComparer<MyRenderCullResultFlat>
    {
        public int Compare(MyRenderCullResult lhs, MyRenderCullResult rhs)
        {
            return lhs.SortKey.CompareTo(rhs.SortKey);
        }

        public int Compare(MyRenderCullResultFlat lhs, MyRenderCullResultFlat rhs)
        {
            return lhs.SortKey.CompareTo(rhs.SortKey);
        }

        internal static MyCullResultsComparer Instance = new MyCullResultsComparer();
    }

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
        internal List<MyCullProxy> List = new List<MyCullProxy>();
        internal List<bool> IsInsideList = new List<bool>();
        internal List<MyCullProxy_2> List2 = new List<MyCullProxy_2>();
        internal List<bool> IsInsideList2 = new List<bool>();
        internal MyCullingSmallObjects? SmallObjects;
        internal MyFrustumEnum Type;
        internal int CascadeIndex;
        internal HashSet<uint> Ignored;

        internal void Clear()
        {
            Bitmask = 0;
            Frustum = null;

            List.Clear();
            IsInsideList.Clear();

            List2.Clear();
            IsInsideList2.Clear();

            SmallObjects = null;
            Type = MyFrustumEnum.Unassigned;
			CascadeIndex = 0;
			Ignored = null;
        }
    }

    class MyCullQuery
    {
        private const int m_maxFrustumCullQueryCount = 32;
        internal MyFrustumCullQuery[] FrustumQueries = new MyFrustumCullQuery[m_maxFrustumCullQueryCount];

        internal int Size { get { return m_reservedFrusta; } }
        int m_reservedFrusta;

        internal MyCullQuery()
        {
            for (int i = 0; i < m_maxFrustumCullQueryCount; i++)
            {
                FrustumQueries[i] = new MyFrustumCullQuery();
            }
        }

        internal int AddFrustum(BoundingFrustumD frustum)
        {
            Debug.Assert(m_reservedFrusta < m_maxFrustumCullQueryCount);
            FrustumQueries[m_reservedFrusta].Clear();
            FrustumQueries[m_reservedFrusta].Frustum = frustum;
            var bitmask = 1 << m_reservedFrusta;
            FrustumQueries[m_reservedFrusta].Bitmask = bitmask;
            m_reservedFrusta++;
            return bitmask;
        }

        internal void Reset()
        {
            m_reservedFrusta = 0;
        }
    };

    struct MyCullingSmallObjects
    {
        internal Vector3 ProjectionDir;
        internal float ProjectionFactor;
        internal float SkipThreshhold;
    }

    class MyCullingWork : IPrioritizedWork
    {
        MyFrustumCullQuery m_query;

        internal MyCullingWork(MyFrustumCullQuery query)
        {
            m_query = query;
        }

        public WorkPriority Priority
        {
            get { return WorkPriority.Normal; }
        }

        public void DoWork()
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("DoCullWork");

            var frustum = m_query.Frustum;

            if (m_query.SmallObjects.HasValue)
            {
                if (!MyRender11.Settings.DrawOnlyMergedMeshes)
                {
                    MyScene.RenderablesDBVH.OverlapAllFrustum<MyCullProxy>(ref frustum, m_query.List, m_query.IsInsideList,
                        m_query.SmallObjects.Value.ProjectionDir, m_query.SmallObjects.Value.ProjectionFactor, m_query.SmallObjects.Value.SkipThreshhold,
                        0);
                }

                MyScene.GroupsDBVH.OverlapAllFrustum<MyCullProxy_2>(ref frustum, m_query.List2, m_query.IsInsideList2,
                    m_query.SmallObjects.Value.ProjectionDir, m_query.SmallObjects.Value.ProjectionFactor, m_query.SmallObjects.Value.SkipThreshhold,
                    0);
            }
            else
            {
                if (!MyRender11.Settings.DrawOnlyMergedMeshes)
                {
                    MyScene.RenderablesDBVH.OverlapAllFrustum<MyCullProxy>(ref frustum, m_query.List, m_query.IsInsideList, 0);
                }
                MyScene.GroupsDBVH.OverlapAllFrustum<MyCullProxy_2>(ref frustum, m_query.List2, m_query.IsInsideList2, 0);
            }

            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        public WorkOptions Options
        {
            get { return Parallel.DefaultOptions; }
        }
    }

    partial class MyGeometryRenderer
    {
        #region Resources


        internal static DeviceContext Context { get { return MyRender11.ImmediateContext; } }
        internal static ConstantsBufferId m_objectConstants;

        internal static Buffer ObjectCB { get { return m_objectConstants; } }

        internal static readonly string DEFAULT_OPAQUE_PASS = "gbuffer";
        internal static readonly string DEFAULT_DEPTH_PASS = "depth";
        internal static readonly string DEFAULT_FORWARD_PASS = "forward";

        #endregion

        static Queue<CommandList> m_commandListQueue = new Queue<CommandList>();
        static List<MyRenderingPass> m_wavefront = new List<MyRenderingPass>();

        static List<MyRenderingPass> Wavefront { get { return m_wavefront; } }

        static MyCullQuery m_cullQuery = new MyCullQuery();

		internal static void Init()
		{
			m_objectConstants = MyHwBuffers.CreateConstantsBuffer(64);
		}

        static void SendOutputMessages()
        {
            foreach(var q in m_cullQuery.FrustumQueries)
            {
                foreach(var proxy in q.List)
                {
                    // all should have same parent
                    if (proxy.Proxies.Length > 0)
                    {
                        MyRenderProxy.VisibleObjectsWrite.Add(proxy.Proxies[0].Parent.m_owner.ID);
                    }
                }
            }

            // TODO: just for now
            foreach(var h in MyComponentFactory<MyGroupRootComponent>.GetAll())
            {
                if(true)
                {
                    BoundingBoxD bb = BoundingBoxD.CreateInvalid();

                    foreach (var child in h.m_children)
                    {
                        if (child.m_visible)
                        {
                            bb.Include(child.Aabb);
                        }
                    }

                    if(MyEnvironment.ViewFrustumClippedD.Contains(bb) != VRageMath.ContainmentType.Disjoint)
                    {
                        MyRenderProxy.VisibleObjectsWrite.Add(h.m_owner.ID);
                    }
                }
            }

            foreach(var id in MyClipmapFactory.ClipmapByID.Keys)
            {
                MyRenderProxy.VisibleObjectsWrite.Add(id);
            }
        }

        internal static void Render()
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("Preparations");
            PrepareFrame();
            PrepareCulling();
            UpdateEnvironmentProbes();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("DispatchCulling");
            DispatchFrustumCulling();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("ProcessCullResults");
            ProcessFrustumCullingResults();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("DispatchRendering");
            //if (MyRender11.LoopObjectThenPass)
            //    MyRenderingDispatcher.Dispatch_LoopObjectThenPass(Wavefront, m_cullQuery, m_commandListQueue);
            //else 
            Debug.Assert(!MyRender11.LoopObjectThenPass);
            MyRenderingDispatcher.Dispatch_LoopPassThenObject(Wavefront, m_cullQuery, m_commandListQueue);
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("SendOutputMessages");
            SendOutputMessages();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("ExecuteCommandLists");
            while (m_commandListQueue.Count > 0)
            {
                var commandList = m_commandListQueue.Dequeue();
                MyRender11.ImmediateContext.ExecuteCommandList(commandList, false);
                commandList.Dispose();
            }
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            FinalizeEnvProbes();
        }

        internal static void PrepareFrame()
        {
            MyGBuffer.Main.Clear();
            MySceneMaterials.PreFrame();
        }

        internal static void AddCamera(ref MatrixD viewMatrix, ref MatrixD projectionMatrix, MyViewport viewport, MyGBuffer gbuffer)
        {
            var frustumMask = m_cullQuery.AddFrustum(new BoundingFrustumD(MyEnvironment.ViewProjectionD));

            MyGBufferPass pass = new MyGBufferPass();
            pass.Cleanup();
            pass.ProcessingMask = frustumMask;
            pass.ViewProjection = MyEnvironment.ViewProjectionAt0;
            pass.Viewport = viewport;
            pass.GBuffer = gbuffer;

            pass.PerFrame();

            m_wavefront.Add(pass);
        }

        internal static void AddForwardCamera(ref Matrix offsetedViewProjection, ref MatrixD viewProjection, MyViewport viewport, DepthStencilView dsv, RenderTargetView rtv)
        {
            var frustumMask = m_cullQuery.AddFrustum(new BoundingFrustumD(viewProjection));

            MyForwardPass pass = new MyForwardPass();
            pass.Cleanup();
            pass.ProcessingMask = frustumMask;
            pass.ViewProjection = offsetedViewProjection;
            pass.Viewport = viewport;
            pass.DSV = dsv;
            pass.RTV = rtv;

            pass.PerFrame();

            m_wavefront.Add(pass);
        }

        internal static void AddShadowCaster(BoundingFrustumD frustum, Matrix viewProjectionLocal, MyViewport viewport, DepthStencilView depthTarget, bool isCascade, string debugName)
        {
            var frustumMask = m_cullQuery.AddFrustum(frustum);

            MyDepthPass pass = new MyDepthPass();
            pass.DebugName = debugName;
            pass.Cleanup();
            pass.ProcessingMask = frustumMask;
            pass.ViewProjection = viewProjectionLocal;
            pass.Viewport = viewport;

            pass.DSV = depthTarget;
            pass.DefaultRasterizer = isCascade ? MyRender11.m_cascadesRasterizerState : MyRender11.m_shadowRasterizerState;

            pass.PerFrame();

            m_wavefront.Add(pass);
        }

        static List<HashSet<MyCullProxy>> m_shadowCascadeProxies = new List<HashSet<MyCullProxy>>();
        static List<HashSet<MyCullProxy_2>> m_shadowCascadeProxies_2 = new List<HashSet<MyCullProxy_2>>();

        static HashSet<MyCullProxy> m_inCascade0 = new HashSet<MyCullProxy>();
        static HashSet<MyCullProxy> m_inCascade1 = new HashSet<MyCullProxy>();
        static HashSet<MyCullProxy> m_inCascade2 = new HashSet<MyCullProxy>();

        static HashSet<MyCullProxy_2> m_inCascade0_2 = new HashSet<MyCullProxy_2>();
        static HashSet<MyCullProxy_2> m_inCascade1_2 = new HashSet<MyCullProxy_2>();
        static HashSet<MyCullProxy_2> m_inCascade2_2 = new HashSet<MyCullProxy_2>();

		static List<int> m_indicesToRemove;

        internal static void PrepareCulling()
        {
            m_cullQuery.Reset();
            m_wavefront.Clear();

            // add main camera
            AddCamera(ref MyEnvironment.ViewD, ref MyEnvironment.OriginalProjectionD,
                new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y),
                MyGBuffer.Main);
            m_cullQuery.FrustumQueries[m_wavefront.Count - 1].Type = MyFrustumEnum.MainFrustum;

            MyShadows.PrepareShadowmaps();
            foreach (var query in MyShadows.ShadowMapQueries)
            {
                bool isCascade = query.QueryType == MyFrustumEnum.ShadowCascade;
                AddShadowCaster(new BoundingFrustumD(query.ProjectionInfo.WorldToProjection), query.ProjectionInfo.CurrentLocalToProjection, query.Viewport, query.DepthBuffer, isCascade, query.QueryType.ToString());

                if (isCascade)
                {
                    var smallCulling = new MyCullingSmallObjects();
                    smallCulling.ProjectionDir = query.ProjectionDir;
                    smallCulling.ProjectionFactor = query.ProjectionFactor;
                    smallCulling.SkipThreshhold = MyRender11.Settings.ShadowCascadeSmallSkipThresholds[query.CascadeIndex];
                    m_cullQuery.FrustumQueries[m_wavefront.Count - 1].SmallObjects = smallCulling;
                    m_cullQuery.FrustumQueries[m_wavefront.Count - 1].CascadeIndex = query.CascadeIndex;
                }

                m_cullQuery.FrustumQueries[m_wavefront.Count - 1].Type = query.QueryType;
                m_cullQuery.FrustumQueries[m_wavefront.Count - 1].Ignored = query.IgnoredEntities;
            }
        }

        internal static void DispatchFrustumCulling()
        {
            //MyPerformanceCounter.PerCameraDraw11Write.RenderableObjectsNum = MyRenderablesBoundingTree.m_tree.CountLeaves(MyRenderablesBoundingTree.m_tree.GetRoot());

            MyRender11.GetRenderProfiler().StartProfilingBlock("CreateTasksAndWait");
            List<Task> tasks = new List<Task>();
            for (int i = 1; i < m_cullQuery.Size; i++)
            {
                m_cullQuery.FrustumQueries[i].List.Clear();
                m_cullQuery.FrustumQueries[i].IsInsideList.Clear();
                tasks.Add(Parallel.Start(new MyCullingWork(m_cullQuery.FrustumQueries[i])));
            }

            if(m_cullQuery.Size > 0)
            {
                m_cullQuery.FrustumQueries[0].List.Clear();
                m_cullQuery.FrustumQueries[0].IsInsideList.Clear();
                new MyCullingWork(m_cullQuery.FrustumQueries[0]).DoWork();
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }
            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        internal static void ProcessFrustumCullingResults()
        {
            if (m_indicesToRemove == null)
                m_indicesToRemove = new List<int>();

			foreach (MyFrustumCullQuery frustumQuery in m_cullQuery.FrustumQueries)
			{
				var cullProxies = frustumQuery.List;
				bool isShadowFrustum = ((frustumQuery.Type == MyFrustumEnum.ShadowCascade) || (frustumQuery.Type == MyFrustumEnum.ShadowProjection));
				for (int cullProxyIndex = 0; cullProxyIndex < cullProxies.Count; ++cullProxyIndex)
				{
					MyCullProxy cullProxy = cullProxies[cullProxyIndex];
                    if(cullProxy == null)
                    {
                        continue;
                    }
					foreach (MyRenderableProxy proxy in cullProxy.Proxies)
					{
						bool shouldCastShadows = (proxy.Flags & MyRenderableProxyFlags.CastShadows) == MyRenderableProxyFlags.CastShadows;
						if (!proxy.IsInViewDistance() || (isShadowFrustum && !shouldCastShadows))
						{
							m_indicesToRemove.Add(cullProxyIndex);
							break;
						}
						var worldMat = proxy.WorldMatrix;
						worldMat.Translation -= MyEnvironment.CameraPosition;
						proxy.ObjectData.LocalMatrix = worldMat;
						proxy.ObjectData.MaterialIndex = MySceneMaterials.GetDrawMaterialIndex(proxy.PerMaterialIndex);
					}
				}

				for (int removeIndex = m_indicesToRemove.Count - 1; removeIndex >= 0; --removeIndex)
				{
					cullProxies.RemoveAtFast(m_indicesToRemove[removeIndex]);
                    frustumQuery.IsInsideList.RemoveAtFast(m_indicesToRemove[removeIndex]);
				}
				m_indicesToRemove.Clear();

				if (frustumQuery.Type == MyFrustumEnum.MainFrustum)
				{
					MyPerformanceCounter.PerCameraDraw11Write.ViewFrustumObjectsNum = frustumQuery.List.Count;
				}
				else if (frustumQuery.Type == MyFrustumEnum.ShadowCascade)
				{
					while (m_shadowCascadeProxies.Count < MyRenderProxy.Settings.ShadowCascadeCount)
						m_shadowCascadeProxies.Add(new HashSet<MyCullProxy>());
					while (m_shadowCascadeProxies_2.Count < MyRenderProxy.Settings.ShadowCascadeCount)
						m_shadowCascadeProxies_2.Add(new HashSet<MyCullProxy_2>());

					// List 1
					m_shadowCascadeProxies[frustumQuery.CascadeIndex].Clear();
					for (int cullProxyIndex = 0; cullProxyIndex < cullProxies.Count; ++cullProxyIndex)
					{
						var cullProxy = cullProxies[cullProxyIndex];
						bool containedInHigherCascade = false;

						// Cull items if they're fully contained in higher resolution cascades
						for (int hashSetIndex = 0; hashSetIndex < frustumQuery.CascadeIndex; ++hashSetIndex)
						{
							if (m_shadowCascadeProxies[hashSetIndex].Contains(cullProxy))
							{
								cullProxies.RemoveAtFast(cullProxyIndex);
                                frustumQuery.IsInsideList.RemoveAtFast(cullProxyIndex);
								--cullProxyIndex;
								containedInHigherCascade = true;
								break;
							}
						}

						if (!containedInHigherCascade && frustumQuery.IsInsideList[cullProxyIndex])
						{
							m_shadowCascadeProxies[frustumQuery.CascadeIndex].Add(cullProxy);
						}
					}

					// List 2
					var cullProxies_2 = frustumQuery.List2;
					m_shadowCascadeProxies_2[frustumQuery.CascadeIndex].Clear();
					for (int cullProxyIndex = 0; cullProxyIndex < cullProxies_2.Count; ++cullProxyIndex)
					{
						var cullProxy_2 = cullProxies_2[cullProxyIndex];
						bool containedInHigherCascade = false;

						// Cull items if they're fully contained in higher resolution cascades
						for (int hashSetIndex = 0; hashSetIndex < frustumQuery.CascadeIndex; ++hashSetIndex)
						{
							if (m_shadowCascadeProxies_2[hashSetIndex].Contains(cullProxy_2))
							{
								cullProxies_2.RemoveAtFast(cullProxyIndex);
                                frustumQuery.IsInsideList2.RemoveAtFast(cullProxyIndex);
								--cullProxyIndex;
								containedInHigherCascade = true;
								break;
							}
						}

						if (!containedInHigherCascade && frustumQuery.IsInsideList2[cullProxyIndex])
						{
							m_shadowCascadeProxies_2[frustumQuery.CascadeIndex].Add(cullProxy_2);
						}
					}

					MyPerformanceCounter.PerCameraDraw11Write.ShadowCascadeObjectsNum[frustumQuery.CascadeIndex] = frustumQuery.List.Count;
				}

				if (frustumQuery.Ignored != null)
				{
					for (int cullProxyIndex = 0; cullProxyIndex < cullProxies.Count; cullProxyIndex++)
					{
						if (cullProxies[cullProxyIndex].Proxies.Length > 0 && frustumQuery.Ignored.Contains(cullProxies[cullProxyIndex].Proxies[0].Parent.m_owner.ID))
						{
							cullProxies.RemoveAtFast(cullProxyIndex);
							--cullProxyIndex;
						}
					}

				}
			}
        }

        internal struct MyEnvProbe
        {
            const float ProbePositionOffset = 2;

            internal int state;

            internal RwTexId cubemapPrefiltered;

            internal MyTimeSpan lastUpdateTime;
            internal float blendWeight;

            internal RwTexId workCubemap;
            internal RwTexId workCubemapPrefiltered;

            internal RwTexId prevWorkCubemapPrefiltered;

            internal Vector3D position;
            internal MyTimeSpan blendT0;
            internal const float MaxBlendTimeS = 5;

            internal static MyEnvProbe Create()
            {
                var envProbe = new MyEnvProbe();

                envProbe.cubemapPrefiltered = RwTexId.NULL;
                envProbe.workCubemap = RwTexId.NULL;
                envProbe.workCubemapPrefiltered = RwTexId.NULL;
                envProbe.prevWorkCubemapPrefiltered = RwTexId.NULL;

                envProbe.lastUpdateTime = MyTimeSpan.Zero;
                envProbe.state = 0;

                return envProbe;
            }

            internal void ImmediateProbe()
            {
                // reset
                state = 0;

                var prevState = state;
                StepUpdateProbe();
                while(prevState != state) {
                    prevState = state;
                    StepUpdateProbe();
                }
            }

            internal void StepUpdateProbe()
            {
                if (state == 0)
                {
                    position = MyEnvironment.CameraPosition + Vector3.UnitY * 4;
                }

                if (state < 6)
                {
                    int faceId = state;
                    MyImmediateRC.RC.Context.ClearDepthStencilView(m_cubemapDepth.SubresourceDsv(faceId), DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                    MyImmediateRC.RC.Context.ClearRenderTargetView(workCubemap.SubresourceRtv(faceId), new Color4(0, 0, 0, 0));

                    var localViewProj = Matrix.CreateTranslation(MyEnvironment.CameraPosition - position) * PrepareLocalEnvironmentMatrix(Vector3.Zero, new Vector2I(256, 256), faceId, 40.0f);
                    var viewProj = MatrixD.CreateTranslation(-position) * localViewProj;

                    AddForwardCamera(ref localViewProj, ref viewProj, new MyViewport(0, 0, 256, 256), m_cubemapDepth.SubresourceDsv(faceId), workCubemap.SubresourceRtv(faceId));

                    ++state;
                    return;
                }

            }

            internal void ImmediateFiltering()
            {
                Debug.Assert(state == 6);

                var prevState = state;
                StepUpdateFiltering();
                while (prevState != state)
                {
                    prevState = state;
                    StepUpdateFiltering();
                }

                UpdateBlending();
            }

            internal void StepUpdateFiltering()
            {
                if (6 <= state && state < 12)
                {
                    int faceId = state - 6;
                    var matrix = CubeFaceViewMatrix(Vector3.Zero, faceId);
                    MyEnvProbeProcessing.RunForwardPostprocess(workCubemap.SubresourceRtv(faceId), m_cubemapDepth.SubresourceSrv(faceId), ref matrix, null);
                    MyEnvProbeProcessing.BuildMipmaps(workCubemap);
                    MyEnvProbeProcessing.Prefilter(workCubemap, workCubemapPrefiltered);

                    //MyEnvironment.Sk

                    ++state;

                    if(state == 12)
                    {
                        blendT0 = MyRender11.CurrentDrawTime;
                    }

                    return;
                }
            }

            internal void UpdateBlending()
            {
                if (state == 12 && blendWeight < 1)
                {
                    blendWeight = (float)Math.Min((MyRender11.CurrentDrawTime - blendT0).Seconds / MaxBlendTimeS, 1);

                    MyEnvProbeProcessing.Blend(cubemapPrefiltered, prevWorkCubemapPrefiltered, workCubemapPrefiltered, blendWeight);
                    m_envProbe.lastUpdateTime = MyRender11.CurrentDrawTime;
                }

                if (state == 12 && blendWeight == 1)
                {
                    state = 0;
                    MyImmediateRC.RC.Context.CopyResource(workCubemapPrefiltered.Resource, prevWorkCubemapPrefiltered.Resource);
                    blendWeight = 0;
                }
            }
        }

        internal static MyEnvProbe m_envProbe = MyEnvProbe.Create();
        static RwTexId m_cubemapDepth = RwTexId.NULL;

        internal static void UpdateEnvironmentProbes()
        {
            if (MyRender11.IsIntelBrokenCubemapsWorkaround)
                return;

            if (m_cubemapDepth == RwTexId.NULL)
            {
                m_cubemapDepth = MyRwTextures.CreateShadowmapArray(256, 256, 6, Format.R24G8_Typeless, Format.D24_UNorm_S8_UInt, Format.R24_UNorm_X8_Typeless);
            }

            if (m_envProbe.cubemapPrefiltered == RwTexId.NULL)
            {
                m_envProbe.cubemapPrefiltered = MyRwTextures.CreateCubemap(256, Format.R16G16B16A16_Float, "environment prefitlered probe");

                m_envProbe.workCubemap = MyRwTextures.CreateCubemap(256, Format.R16G16B16A16_Float, "environment probe");
                m_envProbe.workCubemapPrefiltered = MyRwTextures.CreateCubemap(256, Format.R16G16B16A16_Float, "environment prefitlered probe");

                m_envProbe.prevWorkCubemapPrefiltered = MyRwTextures.CreateCubemap(256, Format.R16G16B16A16_Float, "environment prefitlered probe");

                m_envProbe.ImmediateProbe();
            }
            else
            {
                m_envProbe.StepUpdateProbe();
            }
        }

        internal static void FinalizeEnvProbes()
        {
            if (MyRender11.IsIntelBrokenCubemapsWorkaround)
                return;

            if (m_envProbe.lastUpdateTime == MyTimeSpan.Zero)
            {
                m_envProbe.ImmediateFiltering();
                MyImmediateRC.RC.Context.CopyResource(m_envProbe.workCubemapPrefiltered.Resource, m_envProbe.prevWorkCubemapPrefiltered.Resource);
            }
            else
            {
                m_envProbe.StepUpdateFiltering();
                m_envProbe.UpdateBlending();
            }
        }

        internal static Matrix CubeFaceViewMatrix(Vector3 pos, int faceId)
        {
            Matrix viewMatrix = Matrix.Identity;
            switch (faceId)
            {
                case 0:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Left, Vector3.Up);
                    break;
                case 1:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Right, Vector3.Up);
                    break;
                case 2:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Up, -Vector3.Backward);
                    break;
                case 3:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Down, -Vector3.Forward);
                    break;
                case 4:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Backward, Vector3.Up);
                    break;
                case 5:
                    viewMatrix = Matrix.CreateLookAt(pos, pos + Vector3.Forward, Vector3.Up);
                    break;
            }



            return viewMatrix;
        }

        internal static Matrix PrepareLocalEnvironmentMatrix(Vector3 pos, Vector2I resolution, int faceId, float farPlane)
        {
            var projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI * 0.5f, 1, 0.5f, farPlane);
            Matrix viewMatrix = CubeFaceViewMatrix(pos, faceId);
            return viewMatrix * projection;
        }
    }
}
