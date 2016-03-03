using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender.Resources;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;

namespace VRageRender
{
    partial class MyRender11
    {
        static MyRCStats m_rcStats;
        static MyPassStats m_passStats;
        static MyPostprocessSettings m_postprocessSettings = MyPostprocessSettings.DefaultGame();

        internal static MyGeometryRenderer DynamicGeometryRenderer;
        internal static MyGeometryRenderer StaticGeometryRenderer;
        internal static MyShadows DynamicShadows;
        internal static MyShadows StaticShadows;
        private static MyFoliageGeneratingPass m_foliageGenerator;
        private static MyFoliageRenderingPass m_foliageRenderer;

        private static Queue<CommandList> m_commandLists = new Queue<CommandList>();

        internal static MyPostprocessSettings Postprocess { get { return m_postprocessSettings; } }
        internal static MyFoliageGeneratingPass FoliageGenerator { get { return m_foliageGenerator; } }

        internal static void Init()
        {
            ResetShadows(MyRenderProxy.Settings.ShadowCascadeCount, RenderSettings.ShadowQuality.ShadowCascadeResolution());
            DynamicGeometryRenderer = new MyGeometryRenderer(MyScene.DynamicRenderablesDBVH, DynamicShadows);
            if (MyScene.SeparateGeometry)
                StaticGeometryRenderer = new MyGeometryRenderer(MyScene.StaticRenderablesDBVH, StaticShadows);

            m_foliageGenerator = new MyFoliageGeneratingPass();
            m_foliageRenderer = new MyFoliageRenderingPass();
        }

        private static void InitShadowCascadeUpdateIntervals(int cascadeCount)
        {
            for (int cascadeIndex = 0; cascadeIndex < cascadeCount; ++cascadeIndex)
            {
                DynamicShadows.ShadowCascades.SetCascadeUpdateInterval(cascadeIndex,
                    MyShadowCascades.DynamicShadowCascadeUpdateIntervals[cascadeIndex].Item1,
                    MyShadowCascades.DynamicShadowCascadeUpdateIntervals[cascadeIndex].Item2);

                if (MyScene.SeparateGeometry)
                {
                    StaticShadows.ShadowCascades.SetCascadeUpdateInterval(cascadeIndex,
                        MyShadowCascades.VoxelShadowCascadeUpdateIntervals[cascadeIndex].Item1,
                        MyShadowCascades.VoxelShadowCascadeUpdateIntervals[cascadeIndex].Item2);
                }
            }
        }

        private static void ResetShadows(int cascadeCount, int cascadeResolution)
        {
            if (DynamicShadows != null)
                DynamicShadows.Reset(cascadeCount, cascadeResolution);
            else
                DynamicShadows = new MyShadows(cascadeCount, cascadeResolution);

            if (StaticShadows != null)
                StaticShadows.Reset(cascadeCount, cascadeResolution);
            else if(MyScene.SeparateGeometry)
                StaticShadows = new MyShadows(cascadeCount, cascadeResolution);

            InitShadowCascadeUpdateIntervals(cascadeCount);
        }

        internal static void ResetStats()
        {
            MyRenderContext.Immediate.Stats.Clear();
            m_rcStats.Clear();
            m_passStats.Clear();
        }

        internal static void GatherStats(MyRCStats stats)
        {
            m_rcStats.Gather(stats);
        }

        internal static void GatherStats(MyPassStats stats)
        {
            m_passStats.Gather(stats);
        }

        private static void SetupCameraMatrices(MyRenderMessageSetCameraViewMatrix message)
        {
            var viewMatrixAt0 = message.ViewMatrix;
            viewMatrixAt0.M14 = 0;
            viewMatrixAt0.M24 = 0;
            viewMatrixAt0.M34 = 0;
            viewMatrixAt0.M41 = 0;
            viewMatrixAt0.M42 = 0;
            viewMatrixAt0.M43 = 0;
            viewMatrixAt0.M44 = 1;

            var originalProjection = message.ProjectionMatrix;
            //var invOriginalProjection = Matrix.CreatePerspectiveFovRhInverse(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.NearPlane, message.FarPlane);
            var renderProjection = Matrix.CreatePerspectiveFieldOfView(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.FarPlane, message.NearPlane);
            var invProj = Matrix.CreatePerspectiveFovRhInverse(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.FarPlane, message.NearPlane);

            renderProjection = Matrix.CreatePerspectiveFovRhInfiniteComplementary(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.NearPlane);
            invProj = Matrix.CreatePerspectiveFovRhInfiniteComplementaryInverse(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.NearPlane);

            var invView = Matrix.Transpose(viewMatrixAt0);
            invView.M41 = (float)message.CameraPosition.X;
            invView.M42 = (float)message.CameraPosition.Y;
            invView.M43 = (float)message.CameraPosition.Z;

            MyEnvironment.ViewAt0 = viewMatrixAt0;
            MyEnvironment.InvViewAt0 = Matrix.Transpose(viewMatrixAt0);
            MyEnvironment.ViewProjectionAt0 = viewMatrixAt0 * renderProjection;
            MyEnvironment.InvViewProjectionAt0 = invProj * Matrix.Transpose(viewMatrixAt0);
            message.CameraPosition.AssertIsValid();
            MyEnvironment.CameraPosition = message.CameraPosition;
            MyEnvironment.View = message.ViewMatrix;
            MyEnvironment.ViewD = message.ViewMatrix;
            MyEnvironment.OriginalProjectionD = originalProjection;
            MyEnvironment.InvView = invView;
            MyEnvironment.ViewProjection = message.ViewMatrix * renderProjection;
            MyEnvironment.InvViewProjection = invProj * invView;
            MyEnvironment.Projection = renderProjection;
            MyEnvironment.InvProjection = invProj;

            MyEnvironment.ViewProjectionD = MyEnvironment.ViewD * (MatrixD)renderProjection;
            
            MyEnvironment.NearClipping = message.NearPlane;
            MyEnvironment.FarClipping = message.FarPlane;
            MyEnvironment.LargeDistanceFarClipping = message.FarPlane*500.0f;
            MyEnvironment.FovY = message.FOV;

            MyUtils.Init(ref MyEnvironment.ViewFrustumD);
            MyEnvironment.ViewFrustumD.Matrix = MyEnvironment.ViewProjectionD;

            MyUtils.Init(ref MyEnvironment.ViewFrustumClippedD);
            MyEnvironment.ViewFrustumClippedD.Matrix = MyEnvironment.ViewD * MyEnvironment.OriginalProjectionD;
        }

        private static void TransferPerformanceStats()
        {
            m_rcStats.Gather(MyRenderContext.Immediate.Stats);

            MyPerformanceCounter.PerCameraDraw11Write.MeshesDrawn = m_passStats.Meshes;
            MyPerformanceCounter.PerCameraDraw11Write.SubmeshesDrawn = m_passStats.Submeshes;
            MyPerformanceCounter.PerCameraDraw11Write.BillboardsDrawn = m_passStats.Billboards;
            MyPerformanceCounter.PerCameraDraw11Write.ObjectConstantsChanges = m_passStats.ObjectConstantsChanges;
            MyPerformanceCounter.PerCameraDraw11Write.MaterialConstantsChanges = m_passStats.MaterialConstantsChanges;
            MyPerformanceCounter.PerCameraDraw11Write.TrianglesDrawn = m_passStats.Triangles;
            MyPerformanceCounter.PerCameraDraw11Write.InstancesDrawn = m_passStats.Instances;

            MyPerformanceCounter.PerCameraDraw11Write.Draw = m_rcStats.Draw;
            MyPerformanceCounter.PerCameraDraw11Write.DrawInstanced = m_rcStats.DrawInstanced;
            MyPerformanceCounter.PerCameraDraw11Write.DrawIndexed = m_rcStats.DrawIndexed;
            MyPerformanceCounter.PerCameraDraw11Write.DrawIndexedInstanced = m_rcStats.DrawIndexedInstanced;
            MyPerformanceCounter.PerCameraDraw11Write.DrawAuto = m_rcStats.DrawAuto;
            MyPerformanceCounter.PerCameraDraw11Write.ShadowDrawIndexed = m_rcStats.ShadowDrawIndexed;
            MyPerformanceCounter.PerCameraDraw11Write.ShadowDrawIndexedInstanced = m_rcStats.ShadowDrawIndexedInstanced;
            MyPerformanceCounter.PerCameraDraw11Write.BillboardDrawCalls = m_rcStats.BillboardDrawIndexed;
            MyPerformanceCounter.PerCameraDraw11Write.SetVB = m_rcStats.SetVB;
            MyPerformanceCounter.PerCameraDraw11Write.SetIB = m_rcStats.SetIB;
            MyPerformanceCounter.PerCameraDraw11Write.SetIL = m_rcStats.SetIL;
            MyPerformanceCounter.PerCameraDraw11Write.SetVS = m_rcStats.SetVS;
            MyPerformanceCounter.PerCameraDraw11Write.SetPS = m_rcStats.SetPS;
            MyPerformanceCounter.PerCameraDraw11Write.SetGS = m_rcStats.SetGS;
            MyPerformanceCounter.PerCameraDraw11Write.SetCB = m_rcStats.SetCB;
            MyPerformanceCounter.PerCameraDraw11Write.SetRasterizerState = m_rcStats.SetRasterizerState;
            MyPerformanceCounter.PerCameraDraw11Write.SetBlendState = m_rcStats.SetBlendState;
            MyPerformanceCounter.PerCameraDraw11Write.BindShaderResources = m_rcStats.BindShaderResources;
        }

        internal static readonly HashSet<MyRenderableComponent> PendingComponentsToUpdate = new HashSet<MyRenderableComponent>();
        private static readonly List<MyRenderableComponent> m_pendingComponentsToRemove = new List<MyRenderableComponent>();
        
        static void UpdateActors()
        {
            ProfilerShort.Begin("UpdateActors");
            ProfilerShort.Begin("MyRenderableComponent rebuild dirty");
            Debug.Assert(m_pendingComponentsToRemove.Count == 0, "Temporary list not cleared after use");
            foreach (var renderableComponent in PendingComponentsToUpdate)
            {
                renderableComponent.RebuildRenderProxies();

                if(!renderableComponent.Owner.RenderDirty)
                    m_pendingComponentsToRemove.Add(renderableComponent);
            }

            if (PendingComponentsToUpdate.Count != m_pendingComponentsToRemove.Count)
            {
                foreach (var renderableComponent in m_pendingComponentsToRemove)
                {
                    PendingComponentsToUpdate.Remove(renderableComponent);
                }
                m_pendingComponentsToRemove.Clear();
            }
            else
            {
                PendingComponentsToUpdate.Clear();
                m_pendingComponentsToRemove.Clear();
            }

            ProfilerShort.BeginNextBlock("MyInstanceLodComponent OnFrameUpdate");
            foreach (var instanceLodComponent in MyComponentFactory<MyInstanceLodComponent>.GetAll())
            {
                instanceLodComponent.OnFrameUpdate();
            }
            ProfilerShort.End();
            ProfilerShort.End();
        }

        static bool m_resetEyeAdaptation = false;

        private static void PrepareGameScene()
        {
            ProfilerShort.Begin("PrepareGameScene");

            ProfilerShort.Begin("Stats");
            ResetStats();

            ProfilerShort.BeginNextBlock("GBuffer clear");
            MyGBuffer.Main.Clear();

            ProfilerShort.BeginNextBlock("Constants");
            MySceneMaterials.PreFrame();
            MyCommon.UpdateFrameConstants();
            ProfilerShort.End();

            ProfilerShort.End();
        }

        private static void ExecuteCommandLists(Queue<CommandList> commandLists)
        {
            ProfilerShort.Begin("Execute command lists");
            while (commandLists.Count > 0)
            {
                var commandList = commandLists.Dequeue();
                MyRender11.DeviceContext.ExecuteCommandList(commandList, false);
                commandList.Dispose();
            }
            ProfilerShort.End();
        }

        private static void SendGlobalOutputMessages()
        {
            ProfilerShort.Begin("SendGlobalOutputMessages");
            ProfilerShort.Begin("Root");
            foreach (var groupRootComponent in MyComponentFactory<MyGroupRootComponent>.GetAll())
            {
                if (true)
                {
                    BoundingBoxD bb = BoundingBoxD.CreateInvalid();

                    foreach (var child in groupRootComponent.m_children)
                    {
                        if (child.IsVisible)
                        {
                            bb.Include(child.Aabb);
                        }
                    }

                    if (MyEnvironment.ViewFrustumClippedD.Contains(bb) != VRageMath.ContainmentType.Disjoint)
                    {
                        MyRenderProxy.VisibleObjectsWrite.Add(groupRootComponent.Owner.ID);
                    }
                }
            }

            ProfilerShort.BeginNextBlock("Clipmap");
            foreach (var id in MyClipmapFactory.ClipmapByID.Keys)
            {
                MyRenderProxy.VisibleObjectsWrite.Add(id);
            }
            ProfilerShort.End();
            ProfilerShort.End();
        }

        // Returns the final image and copies it to renderTarget if non-null
        private static MyBindableResource DrawGameScene(MyBindableResource renderTarget)
        {
            ProfilerShort.Begin("DrawGameScene");
           
            PrepareGameScene();

            // todo: shouldn't be necessary
            if (true)
            {
                ProfilerShort.Begin("Clear");
                MyRenderContext.Immediate.Clear();
                MyRenderContext.Immediate.DeviceContext.ClearState();
                ProfilerShort.End();
            }

            Debug.Assert(m_commandLists.Count == 0, "Not all command lists executed last frame!");
            DynamicGeometryRenderer.Render(m_commandLists, true);
            if (MyScene.SeparateGeometry)
                StaticGeometryRenderer.Render(m_commandLists, false);

            SendGlobalOutputMessages();
            ExecuteCommandLists(m_commandLists);
            MyEnvironmentProbe.FinalizeEnvProbes();

            // cleanup context atfer deferred lists
            if (MyRender11.DeferredContextsEnabled)
            {
                ProfilerShort.Begin("Clear2");
                MyRenderContext.Immediate.Clear();
                ProfilerShort.End();
            }

            // todo: shouldn't be necessary
            if (true)
            {
                ProfilerShort.Begin("Clear3");
                MyRenderContext.Immediate.Clear();
                MyRenderContext.Immediate.DeviceContext.ClearState();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Render decals");
            MyGpuProfiler.IC_BeginBlock("Render decals");
            MyRender11.CopyGbufferToScratch();
            MyScreenDecals.Draw();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Render foliage");
            MyGpuProfiler.IC_BeginBlock("Render foliage");
            m_foliageRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("MySceneMaterials.MoveToGPU");
            MySceneMaterials.MoveToGPU();

            ProfilerShort.BeginNextBlock("Postprocessing");
            MyGpuProfiler.IC_BeginBlock("Postprocessing");
            if (MultisamplingEnabled)
            {
                MyRender11.DeviceContext.ClearDepthStencilView(MyScreenDependants.m_resolvedDepth.m_DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                MyGpuProfiler.IC_BeginBlock("MarkAAEdges");
                MyAAEdgeMarking.Run();
                MyGpuProfiler.IC_EndBlock();
                MyDepthResolve.Run(MyScreenDependants.m_resolvedDepth, MyGBuffer.Main.DepthStencil.Depth);
            }

            if(MyScene.SeparateGeometry)
            {
                MyShadowCascadesPostProcess.Combine(MyShadowCascades.CombineShadowmapArray, DynamicShadows.ShadowCascades, StaticShadows.ShadowCascades);
                DynamicShadows.ShadowCascades.PostProcess(MyRender11.PostProcessedShadows, MyShadowCascades.CombineShadowmapArray);
            }
            else
                DynamicShadows.ShadowCascades.PostProcess(MyRender11.PostProcessedShadows, DynamicShadows.ShadowCascades.CascadeShadowmapArray);

            MyGpuProfiler.IC_BeginBlock("SSAO");
            if (Postprocess.EnableSsao)
            {
                MySSAO.Run(MyScreenDependants.m_ambientOcclusion, MyGBuffer.Main, MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.Depth : MyGBuffer.Main.DepthStencil.Depth);

                if(MySSAO.UseBlur)
                    MyBlur.Run(MyScreenDependants.m_ambientOcclusion, MyScreenDependants.m_ambientOcclusionHelper, MyScreenDependants.m_ambientOcclusion);
            }
            else
            {
                MyRender11.DeviceContext.ClearRenderTargetView(MyScreenDependants.m_ambientOcclusion.m_RTV, Color4.White);
            }
            MyGpuProfiler.IC_EndBlock();


            MyGpuProfiler.IC_BeginBlock("Lights");
            MyLightRendering.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Billboards");
            MyGpuProfiler.IC_BeginBlock("Billboards");
            if (MyRender11.MultisamplingEnabled)
            {
                MyBillboardRenderer.Render(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), MyScreenDependants.m_resolvedDepth, MyScreenDependants.m_resolvedDepth.Depth);
            }
            else
            {
                MyBillboardRenderer.Render(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), MyGBuffer.Main.DepthStencil, MyGBuffer.Main.DepthStencil.Depth);
            }

            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Atmosphere");
            MyGpuProfiler.IC_BeginBlock("Atmosphere");
            MyAtmosphereRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Clouds");
            MyGpuProfiler.IC_BeginBlock("Clouds");
            MyCloudRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.End();

            MyGpuProfiler.IC_BeginBlock("Luminance reduction");
            MyBindableResource avgLum = null;

            if (MyRender11.MultisamplingEnabled)
            {
                MyRenderContext.Immediate.DeviceContext.ResolveSubresource(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer).m_resource, 0, MyGBuffer.Main.Get(MyGbufferSlot.LBufferResolved).m_resource, 0, SharpDX.DXGI.Format.R11G11B10_Float);
            }

            if (m_resetEyeAdaptation)
            {
                MyRenderContext.Immediate.DeviceContext.ClearUnorderedAccessView(m_prevLum.m_UAV, Int4.Zero);
                m_resetEyeAdaptation = false;
            }

            avgLum = MyLuminanceAverage.Run(m_reduce0, m_reduce1, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), m_prevLum);

            MyGpuProfiler.IC_EndBlock();

            if (MyRender11.Settings.DisplayHdrDebug)
            {
                var src = MyGBuffer.Main.Get(MyGbufferSlot.LBuffer) as MyRenderTarget;
                MyHdrDebugTools.CreateHistogram(src.m_SRV, src.m_resolution, src.m_samples.X);
            }


            MyGpuProfiler.IC_BeginBlock("Bloom");
            var bloom = MyBloom.Run(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), avgLum);
            MyGpuProfiler.IC_EndBlock();

            MyBindableResource tonemapped;
            if (MyRender11.FxaaEnabled)
            {
                tonemapped = m_rgba8_linear;
            }
            else
            {
                tonemapped = m_uav3;
            }

            MyGpuProfiler.IC_BeginBlock("Tone mapping");
            MyToneMapping.Run(tonemapped, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), avgLum, bloom, Postprocess.EnableTonemapping);
            MyGpuProfiler.IC_EndBlock();

            MyBindableResource renderedImage;

            if (MyRender11.FxaaEnabled)
            {
                MyGpuProfiler.IC_BeginBlock("FXAA");
                MyFXAA.Run(m_rgba8_0.GetView(new MyViewKey { Fmt = SharpDX.DXGI.Format.R8G8B8A8_UNorm, View = MyViewEnum.RtvView }), tonemapped);
                MyGpuProfiler.IC_EndBlock();

                renderedImage = m_rgba8_0.GetView(new MyViewKey { Fmt = SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, View = MyViewEnum.SrvView });
            }
            else
            {
                //renderedImage = (tonemapped as MyCustomTexture).GetView(new MyViewKey { Fmt = SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, View = MyViewEnum.SrvView });
                renderedImage = tonemapped;
            }
            ProfilerShort.Begin("Outline");
            if (MyOutline.AnyOutline())
            {
                MyOutline.Run();

                MyGpuProfiler.IC_BeginBlock("Outline Blending");
                ProfilerShort.Begin("Outline Blending");
                if (MyRender11.FxaaEnabled)
                {
                    MyBlendTargets.RunWithStencil(
                        m_rgba8_0.GetView(new MyViewKey { Fmt = SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, View = MyViewEnum.RtvView }),
                        MyRender11.m_rgba8_1,
                        MyRender11.BlendAdditive,
                        MyDepthStencilState.TestOutlineMeshStencil,
                        0x40);
                    MyBlendTargets.RunWithStencil(
                        m_rgba8_0.GetView(new MyViewKey { Fmt = SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, View = MyViewEnum.RtvView }),
                        MyRender11.m_rgba8_1,
                        MyRender11.BlendTransparent,
                        MyDepthStencilState.TestHighlightMeshStencil,
                        0x40);
                }
                else
                {
                    if (MyRender11.MultisamplingEnabled)
                    {
                        MyBlendTargets.RunWithPixelStencilTest(tonemapped, MyRender11.m_rgba8_ms, MyRender11.BlendAdditive);
                        MyBlendTargets.RunWithPixelStencilTest(tonemapped, MyRender11.m_rgba8_ms, MyRender11.BlendTransparent, true);
                    }
                    else
                    {
                        MyBlendTargets.RunWithStencil(tonemapped, MyRender11.m_rgba8_1, MyRender11.BlendAdditive, MyDepthStencilState.TestOutlineMeshStencil, 0x40);
                        MyBlendTargets.RunWithStencil(tonemapped, MyRender11.m_rgba8_1, MyRender11.BlendTransparent, MyDepthStencilState.TestHighlightMeshStencil, 0x40);
                    }
                }
                ProfilerShort.End();
                MyGpuProfiler.IC_EndBlock();
            }
            ProfilerShort.End();

            if (renderTarget != null)
            {
                MyCopyToRT.Run(renderTarget, renderedImage);
            }

            if (MyRender11.Settings.DisplayHdrDebug)
            {
                MyHdrDebugTools.DisplayHistogram((renderTarget as IRenderTargetBindable).RTV, (avgLum as IShaderResourceBindable).SRV);
            }

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
            return renderedImage;
        }

        static MyBindableResource m_finalImage;

        private static void TakeCustomSizedScreenshot(Vector2 rescale)
        {
            var resCpy = m_resolution;

            m_resolution = new Vector2I(resCpy * rescale);
            CreateScreenResources();

            m_finalImage = DrawGameScene(null);
            m_resetEyeAdaptation = true;

            var surface = new MyRenderTarget(m_finalImage.GetSize().X, m_finalImage.GetSize().Y, SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, 1, 0);
            MyCopyToRT.Run(surface, m_finalImage);
            MyCopyToRT.ClearAlpha(surface);
            SaveScreenshotFromResource(surface.m_resource);
            surface.Release();

            m_resolution = resCpy;
            CreateScreenResources();
        }

        private static void UpdateSceneFrame()
        {
            ProfilerShort.Begin("LoadMeshes");
            MyMeshes.Load();
            ProfilerShort.End();

            ProfilerShort.Begin("QueryTexturesFromEntities");
            QueryTexturesFromEntities();
            ProfilerShort.End();
            ProfilerShort.Begin("MyTextures.Load");
            MyTextures.Load();
            ProfilerShort.End();
            ProfilerShort.Begin("GatherTextures");
            GatherTextures();
            ProfilerShort.End();

            MyBillboardRenderer.OnFrameStart();

            UpdateActors();

            MyBigMeshTable.Table.MoveToGPU();

            ProfilerShort.Begin("Update merged groups");
            ProfilerShort.Begin("UpdateBeforeDraw");
            foreach (var r in MyComponentFactory<MyGroupRootComponent>.GetAll())
            {
                r.UpdateBeforeDraw();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("MoveToGPU");
            foreach (var r in MyComponentFactory<MyGroupRootComponent>.GetAll())
            {
                foreach (var val in r.m_materialGroups.Values)
                {
                    // optimize: keep list+set for updating
                    val.MoveToGPU();
                }
            }
            ProfilerShort.End();
            ProfilerShort.End();

            ProfilerShort.Begin("Fill foliage streams");
            MyGpuProfiler.IC_BeginBlock("Fill foliage streams");
            m_foliageGenerator.PerFrame();
            m_foliageGenerator.Begin();
            MyFoliageComponents.Update();
            m_foliageGenerator.End();
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            MyCommon.MoveToNextFrame();
        }

        static void SaveResourceToFile(Resource res, string path, ImageFileFormat fmt)
        {
            try
            {
                Resource.ToFile(MyRender11.DeviceContext, res, fmt, path);

                MyRenderProxy.ScreenshotTaken(true, path, m_screenshot.Value.ShowNotification);
            }
            catch (SharpDX.SharpDXException e)
            {
                MyRender11.Log.WriteLine("SaveResourceToFile()");
                MyRender11.Log.IncreaseIndent();
                    MyRender11.Log.WriteLine(String.Format("Failed to save screenshot {0}: {1}", path, e));
                MyRender11.Log.DecreaseIndent();

				MyRenderProxy.ScreenshotTaken(false, path, m_screenshot.Value.ShowNotification);
            }
        }

        private static void SaveScreenshotFromResource(Resource res)
        {
            SaveResourceToFile(res, m_screenshot.Value.SavePath, m_screenshot.Value.Format);
            m_screenshot = null;
        }

        private static MyBindableResource m_lastScreenDataResource = null;
        private static Stream m_lastDataStream = null;

        private unsafe static byte[] GetScreenData(Vector2I resolution, byte[] screenData = null)
        {
            const uint headerPadding = 256; // Need to allocate some space for the bitmap headers
            const uint bytesPerPixel = 4;
            uint imageSizeInBytes = (uint)(resolution.Size() * bytesPerPixel);
            uint dataSizeInBytes = imageSizeInBytes + headerPadding;

            byte[] returnData = null;
            if(screenData == null)
                screenData = new byte[imageSizeInBytes];
            else if(screenData.Length != imageSizeInBytes)
            {
                Debug.Fail("Preallocated buffer for GetScreenData incorrect size: " + imageSizeInBytes.ToString() + " expected, " + screenData.Length * bytesPerPixel + " received");
                return returnData;
            }

            MyBindableResource imageSource = Backbuffer;
            if (imageSource == null)
                return returnData;

            if(imageSizeInBytes > int.MaxValue)
            {
                Debug.Fail("Image size too large to read!");
                return returnData;
            }

            MyBindableResource imageResource = imageSource;
            if (resolution.X != imageResource.GetSize().X || resolution.Y != imageResource.GetSize().Y)
            {
                imageResource = m_lastScreenDataResource;
                if (imageResource == null || (imageResource.GetSize().X != resolution.X || imageResource.GetSize().Y != resolution.Y))
                {
                    if (m_lastScreenDataResource != null && m_lastScreenDataResource != Backbuffer)
                    {
                        m_lastScreenDataResource.Release();
                    }
                    m_lastScreenDataResource = null;

                    imageResource = new MyRenderTarget(resolution.X, resolution.Y, MyRender11Constants.DX11_BACKBUFFER_FORMAT, 0, 0);
                }

                MyCopyToRT.Run(imageResource, imageSource, new MyViewport(resolution));
            }

            m_lastScreenDataResource = imageResource;

            Stream dataStream = m_lastDataStream;
            if (m_lastDataStream == null || m_lastDataStream.Length != dataSizeInBytes)
            {
                if (m_lastDataStream != null)
                {
                    m_lastDataStream.Dispose();
                    m_lastDataStream = null;
                }
                dataStream = new DataStream((int)dataSizeInBytes, true, true);
            }

            m_lastDataStream = dataStream;

            Resource.ToStream(MyRenderContext.Immediate.DeviceContext, imageResource.m_resource, ImageFileFormat.Bmp, dataStream);

            if (!(dataStream.CanRead && dataStream.CanSeek))
            {
                Debug.Fail("Screen data stream does not support the necessary operations to get the data");
                return returnData;
            }

            fixed (byte* dataPointer = screenData)
            {
                GetBmpDataFromStream(dataStream, dataPointer, imageSizeInBytes);
            }

            returnData = screenData;

            if (m_lastDataStream != null)
                m_lastDataStream.Seek(0, SeekOrigin.Begin);

            return returnData;
        }

        // TODO: Still probably needs some more work to read the pixel array properly in case of non 4-byte aligned rows
        private unsafe static void GetBmpDataFromStream(Stream dataStream, byte* dataPointer, uint imageSizeInBytes)
        {
            // Start reading from the position of the pixel array offset information in the bitmap header
            const int startOffset = 10;
            dataStream.Seek(startOffset, SeekOrigin.Begin);

            int pixelArrayOffset = dataStream.ReadInt32();

            // Read in some data from the dib header
            int dibHeaderSize = dataStream.ReadInt32();
            int bitmapWidth = dataStream.ReadInt32();
            int bitmapHeight = dataStream.ReadInt32();
            int colorPlaneCount = dataStream.ReadInt16();
            int bitsPerPixel = dataStream.ReadInt16();

            Debug.Assert(colorPlaneCount == 1);

            // Everything ok, time to read the actual pixel data
            //dataStream.Seek(pixelArrayOffset, SeekOrigin.Begin);

            dataStream.ReadNoAlloc(dataPointer, pixelArrayOffset, (int)imageSizeInBytes);
        }
    }
}
