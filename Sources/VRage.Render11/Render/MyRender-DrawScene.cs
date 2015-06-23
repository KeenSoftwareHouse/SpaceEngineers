using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRage.Import;

using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using Vector2 = VRageMath.Vector2;
using BoundingFrustum = VRageMath.BoundingFrustum;
using System.Diagnostics;
using ParallelTasks;
using System.Text.RegularExpressions;

namespace VRageRender
{
    partial class MyRender11
    {
        static MyRCStats m_rcStats;
        static MyPassStats m_passStats;
        static MyPostprocessSettings m_postprocessSettings = MyPostprocessSettings.DefaultGame();
        internal static MyPostprocessSettings Postprocess { get { return m_postprocessSettings; } }

        internal static void ResetStats()
        {
            MyImmediateRC.RC.Stats.Clear();
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
            var invOriginalProjection = Matrix.CreatePerspectiveFovInv(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.NearPlane, message.FarPlane);
            var complementaryProjection = Matrix.CreatePerspectiveFieldOfView(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.FarPlane, message.NearPlane);
            var invProj = Matrix.CreatePerspectiveFovInv(message.FOV, MyRender11.ResolutionF.X / MyRender11.ResolutionF.Y, message.FarPlane, message.NearPlane);

            var invView = Matrix.Transpose(viewMatrixAt0);
            invView.M41 = (float)message.CameraPosition.X;
            invView.M42 = (float)message.CameraPosition.Y;
            invView.M43 = (float)message.CameraPosition.Z;

            MyEnvironment.ViewAt0 = viewMatrixAt0;
            MyEnvironment.InvViewAt0 = Matrix.Transpose(viewMatrixAt0);
            MyEnvironment.ViewProjectionAt0 = viewMatrixAt0 * complementaryProjection;
            MyEnvironment.InvViewProjectionAt0 = invProj * Matrix.Transpose(viewMatrixAt0);
            message.CameraPosition.AssertIsValid();
            MyEnvironment.CameraPosition = message.CameraPosition;
            MyEnvironment.View = message.ViewMatrix;
            MyEnvironment.InvView = invView;
            MyEnvironment.ViewProjection = message.ViewMatrix * complementaryProjection;
            MyEnvironment.InvViewProjection = invProj * invView;
            MyEnvironment.Projection = complementaryProjection;
            MyEnvironment.InvProjection = invProj;
            
            MyEnvironment.NearClipping = message.NearPlane;
            MyEnvironment.FarClipping = message.FarPlane;
            MyEnvironment.LargeDistanceFarClipping = message.FarPlane;
            MyEnvironment.FovY = message.FOV;
            MyEnvironment.ViewFrustum = new BoundingFrustum(MyEnvironment.ViewProjection);
        }

        private static void TransferPerformanceStats()
        {
            m_rcStats.Gather(MyImmediateRC.RC.Stats);

            MyPerformanceCounter.PerCameraDraw11Write.MeshesDrawn = m_passStats.Meshes;
            MyPerformanceCounter.PerCameraDraw11Write.SubmeshesDrawn = m_passStats.Submeshes;
            MyPerformanceCounter.PerCameraDraw11Write.ObjectConstantsChanges = m_passStats.ObjectConstantsChanges;
            MyPerformanceCounter.PerCameraDraw11Write.MaterialConstantsChanges = m_passStats.ObjectConstantsChanges;
            MyPerformanceCounter.PerCameraDraw11Write.TrianglesDrawn = m_passStats.Triangles;
            MyPerformanceCounter.PerCameraDraw11Write.InstancesDrawn = m_passStats.Instances;

            MyPerformanceCounter.PerCameraDraw11Write.Draw = m_rcStats.Draw;
            MyPerformanceCounter.PerCameraDraw11Write.DrawInstanced = m_rcStats.DrawInstanced;
            MyPerformanceCounter.PerCameraDraw11Write.DrawIndexed = m_rcStats.DrawIndexed;
            MyPerformanceCounter.PerCameraDraw11Write.DrawIndexedInstanced = m_rcStats.DrawIndexedInstanced;
            MyPerformanceCounter.PerCameraDraw11Write.DrawAuto = m_rcStats.DrawAuto;
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

        static void UpdateActors()
        {
            foreach (var renderable in MyComponentFactory<MyRenderableComponent>.GetAll())
            {
                if (renderable.IsVisible)
                {
                    renderable.OnFrameUpdate();
                }
            }
        }

        static bool m_resetEyeAdaptation = false;

        private static void DrawGameScene(bool blitToBackbuffer)
        {
            ResetStats();
            MyCommon.UpdateFrameConstants();

            // todo: shouldn't be necessary
            if (true)
            {
                MyImmediateRC.RC.Clear();
                MyImmediateRC.RC.Context.ClearState();
            }

            MyRender11.GetRenderProfiler().StartProfilingBlock("MyGeometryRenderer.Render");
            MyGpuProfiler.IC_BeginBlock("MyGeometryRenderer.Render");
            MyGeometryRenderer.Render();
            MyGpuProfiler.IC_EndBlock();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            // cleanup context atfer deferred lists
            if (MyRender11.DeferredContextsEnabled)
            {
                MyImmediateRC.RC.Clear();
            }

            // todo: shouldn't be necessary
            if(true)
            {
                MyImmediateRC.RC.Clear();
                MyImmediateRC.RC.Context.ClearState();
            }

            MyRender11.GetRenderProfiler().StartProfilingBlock("Render decals");
            MyGpuProfiler.IC_BeginBlock("Render decals");
            MyRender11.CopyGbufferToScratch();
            MyScreenDecals.Draw();
            MyGpuProfiler.IC_EndBlock();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("Render foliage");
            MyGpuProfiler.IC_BeginBlock("Render foliage");
            MyFoliageRenderer.Render();
            MyGpuProfiler.IC_EndBlock();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MySceneMaterials.MoveToGPU();

            MyRender11.GetRenderProfiler().StartProfilingBlock("Postprocessing");
            MyGpuProfiler.IC_BeginBlock("Postprocessing");
            if (MultisamplingEnabled)
            {
                MyRender11.Context.ClearDepthStencilView(MyScreenDependants.m_resolvedDepth.m_DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                MyGpuProfiler.IC_BeginBlock("MarkAAEdges");
                MyAAEdgeMarking.Run();
                MyGpuProfiler.IC_EndBlock();
                MyDepthResolve.Run(MyScreenDependants.m_resolvedDepth, MyGBuffer.Main.DepthStencil.Depth);
            }

            MyGpuProfiler.IC_BeginBlock("MarkCascades");
            MyShadows.MarkCascadesInStencil();
            MyGpuProfiler.IC_EndBlock();


            MyGpuProfiler.IC_BeginBlock("Shadows resolve");
            MyShadowsResolve.Run();
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("SSAO");
            if (Postprocess.EnableSsao)
            {
                MySSAO.Run(MyScreenDependants.m_ambientOcclusion, MyGBuffer.Main, MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.Depth : MyGBuffer.Main.DepthStencil.Depth);
            }
            else
            {
                MyRender11.Context.ClearRenderTargetView(MyScreenDependants.m_ambientOcclusion.m_RTV, Color4.White);
            }
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("Lights");
            MyLightRendering.Render();
            MyGpuProfiler.IC_EndBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("Billboards");
            MyGpuProfiler.IC_BeginBlock("Billboards");
            MyRender11.Context.ClearRenderTargetView((MyScreenDependants.m_particlesRT as IRenderTargetBindable).RTV, new Color4(0, 0, 0, 0));
            if (MyRender11.MultisamplingEnabled)
            {
                MyBillboardRenderer.Render(MyScreenDependants.m_particlesRT, MyScreenDependants.m_resolvedDepth, MyScreenDependants.m_resolvedDepth.Depth);
            }
            else
            {
                MyBillboardRenderer.Render(MyScreenDependants.m_particlesRT, MyGBuffer.Main.DepthStencil, MyGBuffer.Main.DepthStencil.Depth);
            }

            MyBlendTargets.Run(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), MyScreenDependants.m_particlesRT);
            MyGpuProfiler.IC_EndBlock();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyGpuProfiler.IC_BeginBlock("Luminance reduction");
            MyBindableResource avgLum = null;
             
            if (MyRender11.MultisamplingEnabled)
            {
                //MyLBufferResolve.Run(MyGBuffer.Main.Get(MyGbufferSlot.LBufferResolved), MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), MyGBuffer.Main.DepthStencil.Stencil);

                MyImmediateRC.RC.Context.ResolveSubresource(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer).m_resource, 0, MyGBuffer.Main.Get(MyGbufferSlot.LBufferResolved).m_resource, 0, SharpDX.DXGI.Format.R11G11B10_Float);
            }
            if (m_resetEyeAdaptation)
            {
                MyImmediateRC.RC.Context.ClearUnorderedAccessView(m_prevLum.m_UAV, Int4.Zero);
                m_resetEyeAdaptation = false;
            }
            avgLum = MyLuminanceAverage.Run(m_reduce0, m_reduce1, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), m_prevLum, m_localLum);
            
            MyGpuProfiler.IC_EndBlock();

            if(MyRender11.Settings.DispalyHdrDebug)
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
            MyToneMapping.Run(tonemapped, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), avgLum, bloom, MyRender11.Settings.EnableTonemapping && Postprocess.EnableTonemapping && MyRender11.RenderSettings.TonemappingEnabled);
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

            m_finalImage = renderedImage;

            if(blitToBackbuffer)
            {
                MyCopyToRT.Run(Backbuffer, renderedImage);
            }

            if(MyRender11.Settings.DispalyHdrDebug)
            {
                MyHdrDebugTools.DisplayHistogram(Backbuffer.m_RTV, (avgLum as IShaderResourceBindable).SRV);
            }

            MyGpuProfiler.IC_EndBlock();
            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        static MyBindableResource m_finalImage;

        private static void TakeCustomSizedScreenshot(Vector2 rescale)
        {
            var resCpy = m_resolution;

            m_resolution = new Vector2I(resCpy * rescale);
            CreateScreenResources();

            DrawGameScene(false);
            m_resetEyeAdaptation = true;

            // uav3 stores final colors
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
            var desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.IsFrontCounterClockwise = true;

            desc.DepthBias = 25000;
            desc.DepthBiasClamp = 2;
            desc.SlopeScaledDepthBias = 1;

            MyPipelineStates.Modify(m_shadowRasterizerState, desc);


            MyMeshes.Load();
            QueryTexturesFromEntities();
            MyTextures.Load();
            GatherTextures();
            MyComponents.UpdateCullProxies();
            MyComponents.ProcessEntities();
            MyComponents.SendVisible();

            MyBillboardRenderer.OnFrameStart();

            MyRender11.GetRenderProfiler().StartProfilingBlock("RebuildProxies");
            foreach (var renderable in MyComponentFactory<MyRenderableComponent>.GetAll())
            {
                renderable.RebuildRenderProxies();
            }
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("UpdateProxies");
            UpdateActors();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyBigMeshTable.Table.MoveToGPU();

            MyRender11.GetRenderProfiler().StartProfilingBlock("Update merged groups");
            MyRender11.GetRenderProfiler().StartProfilingBlock("UpdateBeforeDraw");
            foreach (var r in MyComponentFactory<MyGroupRootComponent>.GetAll())
            {
                r.UpdateBeforeDraw();
            }
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("MoveToGPU");
            foreach (var r in MyComponentFactory<MyGroupRootComponent>.GetAll())
            {
                foreach (var val in r.m_materialGroups.Values)
                {
                    // optimize: keep list+set for updating
                    val.MoveToGPU();
                }
            }
            MyRender11.GetRenderProfiler().EndProfilingBlock();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyRender11.GetRenderProfiler().StartProfilingBlock("Fill foliage streams");
            MyGpuProfiler.IC_BeginBlock("Fill foliage streams");
            MyGPUFoliageGenerating.GetInstance().PerFrame();
            MyGPUFoliageGenerating.GetInstance().Begin();
            foreach (var foliage in MyComponentFactory<MyFoliageComponent>.GetAll())
            {
                if (foliage.m_owner.CalculateCameraDistance() < MyRender11.RenderSettings.FoliageDetails.GrassDrawDistance())
                {
                    foliage.FillStreams();
                }
                else
                {
                    foliage.InvalidateStreams();
                }
            }
            MyGPUFoliageGenerating.GetInstance().End();
            MyGpuProfiler.IC_EndBlock();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            MyCommon.MoveToNextFrame();
        }

        //private static void DrawScene()
        //{
        //    DrawGameScene(true, true);

        //    if (m_screenshot.HasValue)
        //    { 
        //        if(m_screenshot.Value.SizeMult == Vector2.One)
        //        {
        //            SaveScreenshotFromResource(Backbuffer.m_resource);
        //        }
        //        else
        //        {
        //            TakeCustomSizedScreenshot(m_screenshot.Value.SizeMult);
        //        }
        //    }

        //    TransferPerformanceStats();
        //}

        static void SaveResourceToFile(Resource res, string path, ImageFileFormat fmt)
        {
            try
            {
                Resource.ToFile(MyRender11.Context, res, fmt, path);

                MyRenderProxy.ScreenshotTaken(true, path, false);
            }
            catch (SharpDX.SharpDXException e)
            {
                MyRender11.Log.WriteLine("SaveResourceToFile()");
                MyRender11.Log.IncreaseIndent();
                    MyRender11.Log.WriteLine(String.Format("Failed to save screenshot {0}: {1}", path, e));
                MyRender11.Log.DecreaseIndent();

                MyRenderProxy.ScreenshotTaken(false, path, false);
            }
        }

        private static void SaveScreenshotFromResource(Resource res)
        {
            SaveResourceToFile(res, m_screenshot.Value.SavePath, m_screenshot.Value.Format);
            m_screenshot = null;
        }
    }
}
