using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if !XB1 // XB1_NOOPENVRWRAPPER
using Valve.VR;
#endif // !XB1
using System.IO;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender.Resources;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using VRage.OpenVRWrapper;

namespace VRageRender
{
    partial class MyRender11
    {
        static MyRCStats m_rcStats;
        static MyPassStats m_passStats;
        static MyPostprocessSettings m_postprocessSettings = MyPostprocessSettings.DefaultGame();
        private static MyRenderDebugOverrides m_debugOverrides = new MyRenderDebugOverrides();
        internal static MyRenderDebugOverrides DebugOverrides { get { return m_debugOverrides; } }

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
        private static void InitShadows(int cascadeCount, int cascadeResolution)
        {
            DynamicShadows = new MyShadows(cascadeCount, cascadeResolution);
            StaticShadows = new MyShadows(cascadeCount, cascadeResolution);
        }
        private static void ResetShadows(int cascadeCount, int cascadeResolution)
        {
            if(DynamicShadows != null)
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
            SetupCameraMatricesInternal(message, MyRender11.Environment, MyStereoRegion.FULLSCREEN);
            if (MyStereoRender.Enable)
            {
                SetupCameraMatricesInternal(message, MyStereoRender.EnvMatricesLeftEye, MyStereoRegion.LEFT);
                SetupCameraMatricesInternal(message, MyStereoRender.EnvMatricesRightEye, MyStereoRegion.RIGHT);
            }
        }

        private static Matrix GetMatrixEyeTranslation(bool isLeftEye, Matrix view)
        {
            float Ipd_2 = 0.2f;
            if (MyOpenVR.Static != null)
                Ipd_2 = MyOpenVR.Ipd_2;

            var invViewMatrix = Matrix.Transpose(view);
            var eyePosition = (!isLeftEye ? invViewMatrix.Left : invViewMatrix.Right) * Ipd_2;
            return Matrix.CreateTranslation(eyePosition);
        }
        
        private static void SetupCameraMatricesInternal(MyRenderMessageSetCameraViewMatrix message, MyEnvironmentMatrices envMatrices, MyStereoRegion typeofEnv)
        {//uses m_leftEye to handle HMD images

            var viewMatrix = message.ViewMatrix;
            var cameraPosition = message.CameraPosition;

            if (MyStereoRender.Enable)
            {
                if (MyOpenVR.Static != null && message.LastMomentUpdateIndex != 0)
                {
                    MatrixD origin = MatrixD.Identity;
                    MyOpenVR.LMUMatrixGetOrigin(ref origin, message.LastMomentUpdateIndex);
                    viewMatrix = MatrixD.Invert(origin);
                }
            }

            var viewMatrixAt0 = viewMatrix;
            viewMatrixAt0.M14 = 0;
            viewMatrixAt0.M24 = 0;
            viewMatrixAt0.M34 = 0;
            viewMatrixAt0.M41 = 0;
            viewMatrixAt0.M42 = 0;
            viewMatrixAt0.M43 = 0;
            viewMatrixAt0.M44 = 1;

            if (MyStereoRender.Enable)
            {
                if (MyOpenVR.Static != null)
                {
                    if (message.LastMomentUpdateIndex != 0)
                    {
                        var tViewMatrix = Matrix.Transpose(viewMatrix);
                        var viewHMDat0 = MyOpenVR.ViewHMD;
                        viewHMDat0.M14 = 0;
                        viewHMDat0.M24 = 0;
                        viewHMDat0.M34 = 0;
                        viewHMDat0.M41 = 0;
                        viewHMDat0.M42 = 0;
                        viewHMDat0.M43 = 0;
                        viewHMDat0.M44 = 1;

                        //cameraPosition += tViewMatrix.Up * MyOpenVR.ViewHMD.Translation.Y;
                        //cameraPosition += tViewMatrix.Backward * MyOpenVR.ViewHMD.Translation.X;
                        //cameraPosition += tViewMatrix.Right * MyOpenVR.ViewHMD.Translation.Z;

                        viewMatrixAt0 = viewMatrixAt0 * viewHMDat0;
                        viewMatrix = viewMatrix * viewHMDat0;

                        if (!MyOpenVR.Debug2DImage && typeofEnv == MyStereoRegion.LEFT)
                        {
                            viewMatrixAt0 = GetMatrixEyeTranslation(true, viewMatrixAt0) * viewMatrixAt0;
                            viewMatrix = GetMatrixEyeTranslation(true, viewMatrix) * viewMatrix;
                        }
                        else if (!MyOpenVR.Debug2DImage && typeofEnv == MyStereoRegion.RIGHT)
                        {
                            viewMatrixAt0 = GetMatrixEyeTranslation(false, viewMatrixAt0) * viewMatrixAt0;
                            viewMatrix = GetMatrixEyeTranslation(false, viewMatrix) * viewMatrix;
                        }
                    }
                }
                else
                {
                    if (!MyOpenVR.Debug2DImage && typeofEnv == MyStereoRegion.LEFT)
                    {
                        viewMatrixAt0 = GetMatrixEyeTranslation(true, viewMatrixAt0) * viewMatrixAt0;
                        viewMatrix = GetMatrixEyeTranslation(true, viewMatrix) * viewMatrix;
                    }
                    else if (!MyOpenVR.Debug2DImage && typeofEnv == MyStereoRegion.RIGHT)
                    {
                        viewMatrixAt0 = GetMatrixEyeTranslation(false, viewMatrixAt0) * viewMatrixAt0;
                        viewMatrix = GetMatrixEyeTranslation(false, viewMatrix) * viewMatrix;
                    }
                }
            }

            var originalProjection = message.ProjectionMatrix;
            //var invOriginalProjection = Matrix.CreatePerspectiveFovRhInverse(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.NearPlane, message.FarPlane);

            float aspectRatio = MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y;
            if (typeofEnv != MyStereoRegion.FULLSCREEN)
                aspectRatio /= 2;
            var renderProjection = Matrix.CreatePerspectiveFieldOfView(message.FOV, aspectRatio, message.FarPlane, message.NearPlane);
            var invProj = Matrix.CreatePerspectiveFovRhInverse(message.FOV, aspectRatio, message.FarPlane, message.NearPlane);

            renderProjection = Matrix.CreatePerspectiveFovRhInfiniteComplementary(message.FOV, aspectRatio, message.NearPlane);
            invProj = Matrix.CreatePerspectiveFovRhInfiniteComplementaryInverse(message.FOV, aspectRatio, message.NearPlane);

            var invView = Matrix.Transpose(viewMatrixAt0);
            invView.M41 = (float)cameraPosition.X;
            invView.M42 = (float)cameraPosition.Y;
            invView.M43 = (float)cameraPosition.Z;

            envMatrices.ViewAt0 = viewMatrixAt0;
            envMatrices.InvViewAt0 = Matrix.Transpose(viewMatrixAt0);
            envMatrices.ViewProjectionAt0 = viewMatrixAt0 * renderProjection;
            envMatrices.InvViewProjectionAt0 = invProj * Matrix.Transpose(viewMatrixAt0);
            cameraPosition.AssertIsValid();
            envMatrices.CameraPosition = cameraPosition;
            envMatrices.View = viewMatrix;
            envMatrices.ViewD = viewMatrix;
            envMatrices.OriginalProjectionD = originalProjection;
            envMatrices.InvView = invView;
            envMatrices.ViewProjection = viewMatrix * renderProjection;
            envMatrices.InvViewProjection = invProj * invView;
            envMatrices.Projection = renderProjection;
            envMatrices.InvProjection = invProj;

            envMatrices.ViewProjectionD = envMatrices.ViewD * (MatrixD)renderProjection;
            
            envMatrices.NearClipping = message.NearPlane;
            envMatrices.FarClipping = message.FarPlane;
            envMatrices.LargeDistanceFarClipping = message.FarPlane*500.0f;
            envMatrices.FovY = message.FOV;

            MyUtils.Init(ref envMatrices.ViewFrustumD);
            envMatrices.ViewFrustumD.Matrix = envMatrices.ViewProjectionD;

            MyUtils.Init(ref envMatrices.ViewFrustumClippedD);
            envMatrices.ViewFrustumClippedD.Matrix = envMatrices.ViewD * envMatrices.OriginalProjectionD;
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
            MyGBuffer.Main.Clear(VRageMath.Color.Black);
            //TODO: Find out why clearing to White affects result image
            //MyGBuffer.Main.Clear(MyEnvironment.BackgroundColor);

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

                    if (MyRender11.Environment.ViewFrustumClippedD.Contains(bb) != VRageMath.ContainmentType.Disjoint)
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
            MyGpuProfiler.IC_BeginBlock("Clear & Geometry Render");
                       
            PrepareGameScene();

            // todo: shouldn't be necessary
            if (true)
            {
                ProfilerShort.Begin("Clear");
                MyRenderContext.Immediate.Clear();
                MyRenderContext.Immediate.DeviceContext.ClearState();
                ProfilerShort.End();
            }

            if (MyStereoRender.Enable && MyStereoRender.EnableUsingStencilMask)
            {
                ProfilerShort.Begin("MyStereoStencilMask.Draw");
                MyGpuProfiler.IC_BeginBlock("MyStereoStencilMask.Draw");
                MyStereoStencilMask.Draw();
                MyGpuProfiler.IC_EndBlock();
                ProfilerShort.End();
            }

            MyGpuProfiler.IC_BeginBlock("MyGeometryRenderer.Render"); 
            Debug.Assert(m_commandLists.Count == 0, "Not all command lists executed last frame!");
            ProfilerShort.Begin("DynamicGeometryRenderer");
            DynamicGeometryRenderer.Render(m_commandLists, true);
            ProfilerShort.End();    // End function block
            if (MyScene.SeparateGeometry)
            {
                ProfilerShort.Begin("StaticGeometryRenderer");
                StaticGeometryRenderer.Render(m_commandLists, false);
                ProfilerShort.End();    // End function block
            }

            SendGlobalOutputMessages();
            ExecuteCommandLists(m_commandLists);
            MyGpuProfiler.IC_EndBlock();

#if !UNSHARPER_TMP
            MyEnvironmentProbe.FinalizeEnvProbes();
#endif

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
            
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.Begin("Render decals - Opaque");
            MyGpuProfiler.IC_BeginBlock("Render decals - Opaque");
            MyRender11.CopyGbufferToScratch();
            MyScreenDecals.Draw(false);
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Render foliage");
            MyGpuProfiler.IC_BeginBlock("Render foliage");
            m_foliageRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("GBuffer Resolve");
            ProfilerShort.BeginNextBlock("MySceneMaterials.MoveToGPU");
            MySceneMaterials.MoveToGPU();

            int nPasses = MyStereoRender.Enable ? 2 : 1;
            for (int i = 0; i < nPasses; i++)
            {
                if (MyStereoRender.Enable)
                    MyStereoRender.RenderRegion = i == 0 ? MyStereoRegion.LEFT : MyStereoRegion.RIGHT;

                if (MultisamplingEnabled)
                {
                    MyRender11.DeviceContext.ClearDepthStencilView(MyScreenDependants.m_resolvedDepth.m_DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                    MyGpuProfiler.IC_BeginBlock("MarkAAEdges");
                    MyAAEdgeMarking.Run();
                    MyGpuProfiler.IC_EndBlock();
                    MyDepthResolve.Run(MyScreenDependants.m_resolvedDepth, MyGBuffer.Main.DepthStencil.Depth);
                }

                ProfilerShort.BeginNextBlock("Shadows");
                MyGpuProfiler.IC_BeginBlock("Shadows");
                if (MyScene.SeparateGeometry)
                {
                    MyShadowCascadesPostProcess.Combine(MyShadowCascades.CombineShadowmapArray, DynamicShadows.ShadowCascades, StaticShadows.ShadowCascades);
                    DynamicShadows.ShadowCascades.PostProcess(MyRender11.PostProcessedShadows, MyShadowCascades.CombineShadowmapArray);
                }
                else
                    DynamicShadows.ShadowCascades.PostProcess(MyRender11.PostProcessedShadows, DynamicShadows.ShadowCascades.CascadeShadowmapArray);
                MyGpuProfiler.IC_EndBlock();

                ProfilerShort.BeginNextBlock("SSAO");
                MyGpuProfiler.IC_BeginBlock("SSAO");
                if (Postprocess.EnableSsao && m_debugOverrides.Postprocessing && m_debugOverrides.SSAO)
                {
                    MySSAO.Run(MyScreenDependants.m_ambientOcclusion, MyGBuffer.Main, MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.Depth : MyGBuffer.Main.DepthStencil.Depth);

                    if (MySSAO.UseBlur)
                        MyBlur.Run(MyScreenDependants.m_ambientOcclusion, MyScreenDependants.m_ambientOcclusionHelper, MyScreenDependants.m_ambientOcclusion);
                }
                else
                {
                    MyRender11.DeviceContext.ClearRenderTargetView(MyScreenDependants.m_ambientOcclusion.m_RTV, Color4.White);
                }
                MyGpuProfiler.IC_EndBlock();

                ProfilerShort.BeginNextBlock("Lights");
                MyGpuProfiler.IC_BeginBlock("Lights");
                if (m_debugOverrides.Lighting)
                    MyLightRendering.Render();
                MyGpuProfiler.IC_EndBlock();

                if (MyRender11.DebugOverrides.Flares)
                    MyLightRendering.DrawFlares();
            }
            MyStereoRender.RenderRegion = MyStereoRegion.FULLSCREEN;
            MyGpuProfiler.IC_EndBlock();

            // Rendering for VR is solved inside of Transparent rendering
            ProfilerShort.BeginNextBlock("Transparent Pass");
            MyGpuProfiler.IC_BeginBlock("Transparent Pass");
            if (m_debugOverrides.Transparent)
                MyTransparentRendering.Render(m_transparencyAccum, m_transparencyCoverage);
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("PostProcess");
            MyGpuProfiler.IC_BeginBlock("PostProcess");
            MyGpuProfiler.IC_BeginBlock("Luminance reduction");
            MyBindableResource avgLum = null;

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
            MyBindableResource bloom;
            if (m_debugOverrides.Postprocessing && m_debugOverrides.Bloom)
            {
                bloom = MyBloom.Run(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), avgLum);
            }
            else
            {
                MyRender11.DeviceContext.ClearRenderTargetView(MyRender11.EighthScreenUavHDR.m_RTV, Color4.Black);
                bloom = MyRender11.EighthScreenUavHDR;
            }
            MyGpuProfiler.IC_EndBlock();

            MyBindableResource tonemapped;
            bool fxaa = MyRender11.FxaaEnabled;
            if (fxaa)
            {
                tonemapped = m_rgba8_linear;
            }
            else
            {
                tonemapped = m_uav3;
            }

            MyGpuProfiler.IC_BeginBlock("Tone mapping");
            if (m_debugOverrides.Postprocessing && m_debugOverrides.Tonemapping)
            {
                MyToneMapping.Run(tonemapped, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), avgLum, bloom, Postprocess.EnableTonemapping);
            }
            else
            {
                tonemapped = MyGBuffer.Main.Get(MyGbufferSlot.LBuffer);
                //MyRender11.DeviceContext.CopyResource(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer).m_resource, tonemapped.m_resource);
            }
            MyGpuProfiler.IC_EndBlock();

            MyBindableResource renderedImage;

            if (fxaa)
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
                if (fxaa)
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
                var target = renderTarget as IRenderTargetBindable;
                var lum = avgLum as IShaderResourceBindable;
                if (target != null && lum != null)
                    MyHdrDebugTools.DisplayHistogram(target.RTV, lum.SRV);
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

        private static void SaveScreenshotFromResource(Resource res)
        {
            bool result = MyTextureData.ToFile(res, m_screenshot.Value.SavePath, m_screenshot.Value.Format);
            MyRenderProxy.ScreenshotTaken(result, m_screenshot.Value.SavePath, m_screenshot.Value.ShowNotification);
            m_screenshot = null;
        }

        private static MyCustomTexture m_backbufferCopyResource = null;
        private static MyBindableResource m_lastScreenDataResource = null;
        private static Stream m_lastDataStream = null;

        private unsafe static byte[] GetScreenData(Resource res, byte[] screenData, ImageFileFormat fmt)
        {
            return MyTextureData.ToData(res, screenData, fmt);
        }
    }
}
