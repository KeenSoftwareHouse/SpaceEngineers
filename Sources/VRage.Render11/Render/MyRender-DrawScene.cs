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
using SharpDX.DXGI;
using VRage;
using VRage.Utils;
using VRageMath;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using VRage.OpenVRWrapper;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.LightingStage.Shadows;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageRender.Messages;
using Resource = SharpDX.Direct3D11.Resource;
using ImageFileFormat = SharpDX.Direct3D9.ImageFileFormat;

namespace VRageRender
{
    partial class MyRender11
    {
        static MyPassStats m_passStats;
        private static MyRenderDebugOverrides m_debugOverrides = new MyRenderDebugOverrides();
        internal static MyRenderDebugOverrides DebugOverrides { get { return m_debugOverrides; } }

        internal static MyPostprocessSettings Postprocess = MyPostprocessSettings.Default;
        internal static MyGeometryRenderer DynamicGeometryRenderer;
        internal static MyGeometryRenderer StaticGeometryRenderer;
        internal static MyShadows DynamicShadows;
        internal static MyShadows StaticShadows;
        private static MyFoliageGeneratingPass m_foliageGenerator;
        private static MyFoliageRenderingPass m_foliageRenderer;

        private static Queue<CommandList> m_commandLists = new Queue<CommandList>();

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
            m_passStats.Clear();
        }

        internal static void GatherStats(MyPassStats stats)
        {
            m_passStats.Gather(stats);
        }

        private static void SetupCameraMatrices(MyRenderMessageSetCameraViewMatrix message)
        {
            SetupCameraMatricesInternal(message, MyRender11.Environment.Matrices, MyStereoRegion.FULLSCREEN);
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
        {
            var originalProjection = message.ProjectionMatrix; 
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
 
            float aspectRatio = MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y;
            if (typeofEnv != MyStereoRegion.FULLSCREEN)
                aspectRatio /= 2;
            Matrix projMatrix = Matrix.CreatePerspectiveFovRhInfiniteComplementary(message.FOV, aspectRatio, message.NearPlane);

            cameraPosition.AssertIsValid(); 
            
            envMatrices.ViewAt0 = viewMatrixAt0;
            envMatrices.InvViewAt0 = Matrix.Invert(viewMatrixAt0);
            envMatrices.ViewProjectionAt0 = viewMatrixAt0 * projMatrix;
            envMatrices.InvViewProjectionAt0 = Matrix.Invert(viewMatrixAt0 * projMatrix);
            envMatrices.CameraPosition = cameraPosition;
            envMatrices.View = viewMatrix;
            envMatrices.ViewD = viewMatrix;
            envMatrices.OriginalProjectionD = originalProjection;
            envMatrices.InvView = Matrix.Invert(viewMatrix);
            envMatrices.ViewProjection = viewMatrix * projMatrix;
            envMatrices.InvViewProjection = Matrix.Invert(viewMatrix * projMatrix);
            envMatrices.Projection = projMatrix;
            envMatrices.InvProjection = Matrix.Invert(projMatrix);
            envMatrices.ViewProjectionD = envMatrices.ViewD * (MatrixD)projMatrix;       
            envMatrices.NearClipping = message.NearPlane;
            envMatrices.FarClipping = message.FarPlane;
            envMatrices.LargeDistanceFarClipping = message.FarPlane*500.0f;
            envMatrices.FovY = message.FOV;

            MyUtils.Init(ref envMatrices.ViewFrustumD);
            envMatrices.ViewFrustumD.Matrix = envMatrices.ViewProjectionD;

            MyUtils.Init(ref envMatrices.ViewFrustumClippedD);
            envMatrices.ViewFrustumClippedD.Matrix = envMatrices.ViewD * envMatrices.OriginalProjectionD;
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
                MyRender11.RC.ExecuteCommandList(commandList, false);
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

                    if (MyRender11.Environment.Matrices.ViewFrustumClippedD.Contains(bb) != VRageMath.ContainmentType.Disjoint)
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
        private static IRtvTexture DrawGameScene(IRtvBindable renderTarget)
        {
            MyGpuProfiler.IC_BeginBlock("Clear & Geometry Render");
                       
            PrepareGameScene();


            // todo: shouldn't be necessary
            if (true)
            {
                ProfilerShort.Begin("Clear");
                MyRender11.RC.ClearState();
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
            MyManagers.EnvironmentProbe.FinalizeEnvProbes();
#endif

            // cleanup context atfer deferred lists
            if (true)
            {
                ProfilerShort.Begin("Clear3");
                MyRender11.RC.ClearState();
                ProfilerShort.End();
            }
            
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.Begin("Render decals - Opaque");
            MyGpuProfiler.IC_BeginBlock("Render decals - Opaque");
            MyImmediateRC.RC.CopyResource(MyGBuffer.Main.GBuffer1.Resource, MyGlobalResources.Gbuffer1Copy.Resource); // copy gbuffer1
            MyScreenDecals.Draw(false);
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Render foliage");
            MyGpuProfiler.IC_BeginBlock("Render foliage");
            m_foliageRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("GBuffer Resolve");
            ProfilerShort.BeginNextBlock("MySceneMaterials.MoveToGPU");
            MySceneMaterials.MoveToGPU();

            MyRender11.RC.ResetTargets();

            int nPasses = MyStereoRender.Enable ? 2 : 1;
            for (int i = 0; i < nPasses; i++)
            {
                if (MyStereoRender.Enable)
                    MyStereoRender.RenderRegion = i == 0 ? MyStereoRegion.LEFT : MyStereoRegion.RIGHT;

                if (MultisamplingEnabled)
                {
                    MyRender11.RC.ClearDsv(MyScreenDependants.m_resolvedDepth, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                    MyGpuProfiler.IC_BeginBlock("MarkAAEdges");
                    MyAAEdgeMarking.Run();
                    MyGpuProfiler.IC_EndBlock();
                    MyDepthResolve.Run(MyScreenDependants.m_resolvedDepth, MyGBuffer.Main.DepthStencil);
                }

                ProfilerShort.BeginNextBlock("Shadows");
                MyGpuProfiler.IC_BeginBlock("Shadows");
                IBorrowedUavTexture postProcessedShadows;
                if (MyScene.SeparateGeometry)
                {
                    MyShadowCascadesPostProcess.Combine(MyShadowCascades.CombineShadowmapArray, DynamicShadows.ShadowCascades, StaticShadows.ShadowCascades);
                    postProcessedShadows = DynamicShadows.ShadowCascades.PostProcess(MyShadowCascades.CombineShadowmapArray);
                    //MyShadowCascadesPostProcess.Combine(MyShadowCascades.CombineShadowmapArray,
                    //    DynamicShadows.ShadowCascades, StaticShadows.ShadowCascades);
                    //postProcessedShadows =
                    //    DynamicShadows.ShadowCascades.PostProcess(MyShadowCascades.CombineShadowmapArray);
                }
                else
                {
                    postProcessedShadows = DynamicShadows.ShadowCascades.PostProcess(DynamicShadows.ShadowCascades.CascadeShadowmapArray);
                    //postProcessedShadows = MyManagers.Shadow.Evaluate();
                }
                MyGpuProfiler.IC_EndBlock();

                if (MySSAO.Params.Enabled && m_debugOverrides.Postprocessing && m_debugOverrides.SSAO)
                {
                    ProfilerShort.BeginNextBlock("SSAO");
                    MyGpuProfiler.IC_BeginBlock("SSAO");
                    MySSAO.Run(MyScreenDependants.m_ambientOcclusion, MyGBuffer.Main, MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.SrvDepth : MyGBuffer.Main.DepthStencil.SrvDepth);

                    if (MySSAO.Params.UseBlur)
                        MyBlur.Run(MyScreenDependants.m_ambientOcclusion, MyScreenDependants.m_ambientOcclusionHelper, MyScreenDependants.m_ambientOcclusion, clearColor : Color4.White);
                    MyGpuProfiler.IC_EndBlock();
                }
                else if (MyHBAO.Params.Enabled && m_debugOverrides.Postprocessing && m_debugOverrides.SSAO)
                {
                    ProfilerShort.BeginNextBlock("HBAO");
                    MyGpuProfiler.IC_BeginBlock("HBAO");
                    MyHBAO.Run(MyScreenDependants.m_ambientOcclusion, MyGBuffer.Main, 
                        MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.SrvDepth : MyGBuffer.Main.DepthStencil.SrvDepth);
                    MyGpuProfiler.IC_EndBlock();
                }
                else
                {
                    MyRender11.RC.ClearRtv(MyScreenDependants.m_ambientOcclusion, Color4.White);
                }

                ProfilerShort.BeginNextBlock("Lights");
                MyGpuProfiler.IC_BeginBlock("Lights");
                if (m_debugOverrides.Lighting)
                    MyLightRendering.Render(postProcessedShadows);
                MyGpuProfiler.IC_EndBlock();
                postProcessedShadows.Release();

                if (MyRender11.DebugOverrides.Flares)
                    MyLightRendering.DrawFlares();
            }
            MyStereoRender.RenderRegion = MyStereoRegion.FULLSCREEN;
            MyGpuProfiler.IC_EndBlock();

            // Rendering for VR is solved inside of Transparent rendering
            ProfilerShort.BeginNextBlock("Transparent Pass");
            MyGpuProfiler.IC_BeginBlock("Transparent Pass");
            if (m_debugOverrides.Transparent)
                MyTransparentRendering.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("PostProcess");
            MyGpuProfiler.IC_BeginBlock("PostProcess");
            MyGpuProfiler.IC_BeginBlock("Luminance reduction");
            IBorrowedUavTexture avgLum = null;

            if (MyRender11.Postprocess.EnableEyeAdaptation)
            {
                if (m_resetEyeAdaptation)
                {
                    MyLuminanceAverage.Reset();
                    m_resetEyeAdaptation = false;
                }

                avgLum = MyLuminanceAverage.Run(MyGBuffer.Main.LBuffer);
            }
            else
            {
                avgLum = MyLuminanceAverage.Skip();
            }

            MyGpuProfiler.IC_EndBlock();

            IBorrowedUavTexture histogram = null;
            if (MyRender11.Settings.DisplayHistogram)
                histogram = MyHdrDebugTools.CreateHistogram(MyGBuffer.Main.LBuffer, MyGBuffer.Main.SamplesCount);
            if (MyRender11.Settings.DisplayHdrIntensity)
            {
                MyHdrDebugTools.DisplayHdrIntensity(MyGBuffer.Main.LBuffer);
            }

            MyGpuProfiler.IC_BeginBlock("Bloom");
            IBorrowedUavTexture bloom;
            if (m_debugOverrides.Postprocessing && m_debugOverrides.Bloom)
            {
                bloom = MyBloom.Run(MyGBuffer.Main.LBuffer, MyGBuffer.Main.GBuffer2,
                    MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.SrvDepth : MyGBuffer.Main.DepthStencil.SrvDepth);
            }
            else
            {
                bloom = MyManagers.RwTexturesPool.BorrowUav("bloom_EightScreenUavHDR", MyRender11.ResolutionI.X / 8, MyRender11.ResolutionI.Y / 8, MyGBuffer.LBufferFormat);
                MyRender11.RC.ClearRtv(bloom, Color4.Black);
            }
            MyGpuProfiler.IC_EndBlock();

            
            MyGpuProfiler.IC_BeginBlock("Tone mapping");
            IBorrowedUavTexture tonemapped = MyToneMapping.Run(MyGBuffer.Main.LBuffer, avgLum, bloom, Postprocess.EnableTonemapping && m_debugOverrides.Postprocessing && m_debugOverrides.Tonemapping);
            bloom.Release();
            MyGpuProfiler.IC_EndBlock();

            IRtvTexture renderedImage;

            IBorrowedCustomTexture rgba8_0 = null;
            bool fxaa = MyRender11.FxaaEnabled; 
            if (fxaa)
            {
                rgba8_0 = MyManagers.RwTexturesPool.BorrowCustom("MyRender11.Rgb8_0");
                MyGpuProfiler.IC_BeginBlock("FXAA");
                MyFXAA.Run(rgba8_0.Linear, tonemapped);
                MyGpuProfiler.IC_EndBlock();

                renderedImage = rgba8_0.SRgb;
            }
            else
            {
                renderedImage = tonemapped;
            }
            ProfilerShort.Begin("Outline");
            if (MyOutline.AnyOutline())
            {
                IBorrowedRtvTexture outlined = MyOutline.Run();

                MyGpuProfiler.IC_BeginBlock("Outline Blending");
                ProfilerShort.Begin("Outline Blending");
                if (fxaa)
                {
                    MyBlendTargets.RunWithStencil(
                        rgba8_0.SRgb,
                        outlined,
                        MyBlendStateManager.BlendAdditive,
                        MyDepthStencilStateManager.TestOutlineMeshStencil,
                        0x40);
                    MyBlendTargets.RunWithStencil(
                        rgba8_0.SRgb,
                        outlined,
                        MyBlendStateManager.BlendTransparent,
                        MyDepthStencilStateManager.TestHighlightMeshStencil,
                        0x40);
                }
                else
                {
                    if (MyRender11.MultisamplingEnabled)
                    {
                        MyBlendTargets.RunWithPixelStencilTest(tonemapped, outlined, MyBlendStateManager.BlendAdditive);
                        MyBlendTargets.RunWithPixelStencilTest(tonemapped, outlined, MyBlendStateManager.BlendTransparent, true);
                    }
                    else
                    {
                        MyBlendTargets.RunWithStencil(tonemapped, outlined, MyBlendStateManager.BlendAdditive, MyDepthStencilStateManager.TestOutlineMeshStencil, 0x40);
                        MyBlendTargets.RunWithStencil(tonemapped, outlined, MyBlendStateManager.BlendTransparent, MyDepthStencilStateManager.TestHighlightMeshStencil, 0x40);
                    }
                }
                ProfilerShort.End();
                MyGpuProfiler.IC_EndBlock();

                outlined.Release();
            }
            ProfilerShort.End();

            if (renderTarget != null)
            {
                MyCopyToRT.Run(renderTarget, renderedImage);
            }

            if (MyRender11.Settings.DisplayHistogram)
            {
                if (renderTarget != null && avgLum != null)
                    MyHdrDebugTools.DisplayHistogram(renderTarget, avgLum, histogram);
            }
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            if (rgba8_0 != null)
                rgba8_0.Release();
            if (histogram != null)
                histogram.Release();
            avgLum.Release();
            tonemapped.Release();

            // HOTFIX: MyDebugTextureDisplay uses borrowed textures. If we place MyDebugTextureDisplay to the different location, we will have problem with borrowed textures (comment by Michal)
            ProfilerShort.Begin("MyDebugTextureDisplay.Draw");
            MyGpuProfiler.IC_BeginBlock("MyDebugTextureDisplay.Draw");
            MyDebugTextureDisplay.Draw(MyRender11.Backbuffer);
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            return renderedImage;
        }

        private static void TakeCustomSizedScreenshot(Vector2 rescale)
        {
            IRtvTexture m_finalImage;

            var resCpy = m_resolution;

            m_resolution = new Vector2I(resCpy * rescale);
            CreateScreenResources();

            m_finalImage = DrawGameScene(null);
            m_resetEyeAdaptation = true;

            MyRwTextureManager texManager = MyManagers.RwTextures;
            var surface = texManager.CreateRtv("MyRender11.TakeCustomSizedScreenshot",
                m_finalImage.Size.X, m_finalImage.Size.Y, SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb, 1, 0);
            MyCopyToRT.Run(surface, m_finalImage);
            MyCopyToRT.ClearAlpha(surface);
            SaveScreenshotFromResource(surface.Resource);
            texManager.DisposeTex(ref surface);

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
            MyManagers.FileTextures.LoadAllRequested();
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

        private static MyBackbuffer m_lastScreenDataResource = null;
        private static Stream m_lastDataStream = null;

        private unsafe static byte[] GetScreenData(Resource res, byte[] screenData, ImageFileFormat fmt)
        {
            return MyTextureData.ToData(res, screenData, fmt);
        }
    }
}
