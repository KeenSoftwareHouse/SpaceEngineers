using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.DXGI;
using VRage;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Instancing;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.GeometryStage2.Rendering;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRage.Utils;
using VRageMath;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology;

namespace VRageRender
{
    struct MyHighlightDesc
    {
        internal string SectionName;
        internal Color Color;
        internal float Thickness;
        internal float PulseTimeInSeconds;
        internal int InstanceId;
    }

    class MyHighlight : MyImmediateRC
    {
        public const byte HIGHLIGHT_STENCIL_MASK = 0x40;
        static Dictionary<uint, List<MyHighlightDesc>> m_highlights = new Dictionary<uint, List<MyHighlightDesc>>();
        static List<uint> m_keysToRemove = new List<uint>();

        #region Common methods
        static void Add(uint ID, string sectionName, Color outlineColor, float thickness, float pulseTimeInSeconds, int instanceId = -1)
        {
            if (!m_highlights.ContainsKey(ID))
                m_highlights[ID] = new List<MyHighlightDesc>();

            m_highlights[ID].Add(new MyHighlightDesc
            {
                SectionName = sectionName,
                Color = outlineColor,
                Thickness = thickness,
                PulseTimeInSeconds = pulseTimeInSeconds,
                InstanceId = instanceId
            });
        }

        static void WriteHighlightConstants(ref MyHighlightDesc desc)
        {
            HighlightConstantsLayout constants = new HighlightConstantsLayout();
            constants.Color = desc.Color.ToVector4();
            if (desc.PulseTimeInSeconds > 0)
                constants.Color.W *= (float)Math.Pow(Math.Cos(2.0 * Math.PI * MyCommon.TimerMs / desc.PulseTimeInSeconds / 1000), 2.0);

            var mapping = MyMapping.MapDiscard(MyCommon.HighlightConstants);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();
        }

        static void BlendHighlight(IRtvBindable target, ISrvBindable outlined, ICustomTexture fxaaTarget, IDepthStencil depthStencilCopy)
        {
            MyGpuProfiler.IC_BeginBlock("Highlight Blending");
            ProfilerShort.Begin("Highlight Blending");
            if (fxaaTarget != null)
            {
                MyBlendTargets.RunWithStencil(
                    fxaaTarget.SRgb,
                    outlined,
                    MyBlendStateManager.BlendAdditive,
                    MyDepthStencilStateManager.TestHighlightOuterStencil,
                    HIGHLIGHT_STENCIL_MASK,
                    depthStencilCopy);
                MyBlendTargets.RunWithStencil(
                    fxaaTarget.SRgb,
                    outlined,
                    MyBlendStateManager.BlendTransparent,
                    MyDepthStencilStateManager.TestHighlightInnerStencil,
                    HIGHLIGHT_STENCIL_MASK,
                    depthStencilCopy);
            }
            else
            {
                if (MyRender11.MultisamplingEnabled)
                {
                    MyBlendTargets.RunWithPixelStencilTest(target, outlined, MyBlendStateManager.BlendAdditive, false, depthStencilCopy);
                    MyBlendTargets.RunWithPixelStencilTest(target, outlined, MyBlendStateManager.BlendTransparent, true, depthStencilCopy);
                }
                else
                {
                    MyBlendTargets.RunWithStencil(target, outlined, MyBlendStateManager.BlendAdditive,
                        MyDepthStencilStateManager.TestHighlightOuterStencil, HIGHLIGHT_STENCIL_MASK,
                        depthStencilCopy);
                    MyBlendTargets.RunWithStencil(target, outlined, MyBlendStateManager.BlendTransparent,
                        MyDepthStencilStateManager.TestHighlightInnerStencil, HIGHLIGHT_STENCIL_MASK,
                        depthStencilCopy);
                }
            }
            ProfilerShort.End();
            MyGpuProfiler.IC_EndBlock();
        }
        #endregion

        #region The old pipeline
        static void RecordMeshPartCommands(MeshId model, MyActor actor, MyGroupRootComponent group,
            MyCullProxy_2 proxy, MyHighlightDesc desc)
        {
            MeshSectionId sectionId;
            bool found = MyMeshes.TryGetMeshSection(model, 0, desc.SectionName, out sectionId);
            if (!found)
                return;

            WriteHighlightConstants(ref desc);

            MyMeshSectionInfo1 section = sectionId.Info;
            MyMeshSectionPartInfo1[] meshes = section.Meshes;
            for (int idx = 0; idx < meshes.Length; idx++)
            {
                MyMaterialMergeGroup materialGroup;
                found = group.TryGetMaterialGroup(meshes[idx].Material, out materialGroup);
                if (!found)
                {
                    DebugRecordMeshPartCommands(model, desc.SectionName, meshes[idx].Material);
                    return;
                }

                int actorIndex;
                found = materialGroup.TryGetActorIndex(actor, out actorIndex);
                if (!found)
                    return;

                MyHighlightPass.Instance.RecordCommands(ref proxy.Proxies[materialGroup.Index], actorIndex, meshes[idx].PartIndex);
            }
        }

        static void RecordMeshPartCommands(MeshId model, LodMeshId lodModelId,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyHighlightDesc desc)
        {
            WriteHighlightConstants(ref desc);

            var submeshCount = lodModelId.Info.PartsNum;
            for (int submeshIndex = 0; submeshIndex < submeshCount; ++submeshIndex)
            {
                var part = MyMeshes.GetMeshPart(model, rendercomp.CurrentLod, submeshIndex);

                MyRenderUtils.BindShaderBundle(RC, renderLod.RenderableProxies[submeshIndex].HighlightShaders);
                MyHighlightPass.Instance.RecordCommands(renderLod.RenderableProxies[submeshIndex], -1, desc.InstanceId);
            }
        }

        /// <returns>True if the section was found</returns>
        static void RecordMeshSectionCommands(MeshId model,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyHighlightDesc desc)
        {
            MeshSectionId sectionId;
            bool found = MyMeshes.TryGetMeshSection(model, rendercomp.CurrentLod, desc.SectionName, out sectionId);
            if (!found)
                return;

            WriteHighlightConstants(ref desc);

            MyMeshSectionInfo1 section = sectionId.Info;
            MyMeshSectionPartInfo1[] meshes = section.Meshes;
            for (int idx = 0; idx < meshes.Length; idx++)
            {
                MyMeshSectionPartInfo1 sectionInfo = meshes[idx];
                if (renderLod.RenderableProxies.Length <= sectionInfo.PartIndex)
                {
                    DebugRecordMeshPartCommands(model, desc.SectionName, rendercomp, renderLod, meshes, idx);
                    return;
                }

                MyRenderableProxy proxy = renderLod.RenderableProxies[sectionInfo.PartIndex];
                MyHighlightPass.Instance.RecordCommands(proxy, sectionInfo.PartSubmeshIndex, desc.InstanceId);
            }
        }

        static void DrawRenderableComponent(MyActor actor, MyRenderableComponent renderableComponent, List<MyHighlightDesc> highlightDescs)
        {
            var renderLod = renderableComponent.Lods[renderableComponent.CurrentLod];
            var model = renderableComponent.GetModel();

            LodMeshId currentModelId;
            if (!MyMeshes.TryGetLodMesh(model, renderableComponent.CurrentLod, out currentModelId))
            {
                Debug.Fail("Mesh for outlining not found!");
                return;
            }

            foreach (MyHighlightDesc descriptor in highlightDescs)
            {
                if (!renderableComponent.IsRenderedStandAlone)
                {
                    MyGroupLeafComponent leafComponent = actor.GetGroupLeaf();
                    MyGroupRootComponent groupComponent = leafComponent.RootGroup;
                    if (groupComponent != null)
                        RecordMeshPartCommands(model, actor, groupComponent, groupComponent.m_proxy, descriptor);

                    continue;
                }

                if (!string.IsNullOrEmpty(descriptor.SectionName))
                    RecordMeshSectionCommands(model, renderableComponent, renderLod, descriptor);
                else
                    RecordMeshPartCommands(model, currentModelId, renderableComponent, renderLod, descriptor);
            }
        }
        #endregion The old pipeline

        #region The new pipeline
        static void DrawHighlightedPart(MyRenderContext RC, MyPart part, MyInstanceLodState state)
        {
            // settings per part (using MyPart.cs):

            MyShaderBundle shaderBundle = part.GetShaderBundle(state);
            RC.SetInputLayout(shaderBundle.InputLayout);
            RC.VertexShader.Set(shaderBundle.VertexShader);
            RC.PixelShader.Set(shaderBundle.PixelShader);

            // (using MyHighlightPass.cs):
            RC.SetRasterizerState(null);

            RC.DrawIndexed(part.IndicesCount, part.StartIndex, part.StartVertex);
        }

        static unsafe IConstantBuffer GetObjectCB(MyRenderContext RC, MyInstanceComponent instance, float stateData)
        {
            Vector4 col0, col1, col2;
            instance.GetMatrixCols(0, out col0, out col1, out col2);
            Matrix matrix = Matrix.Identity;
            matrix.SetRow(0, col0);
            matrix.SetRow(1, col1);
            matrix.SetRow(2, col2);
            matrix = Matrix.Transpose(matrix);

            int cbSize = sizeof(MyObjectDataCommon);
            cbSize += sizeof(MyObjectDataNonVoxel);

            IConstantBuffer cb = MyCommon.GetObjectCB(cbSize);
            var mapping = MyMapping.MapDiscard(RC, cb);
            MyObjectDataNonVoxel nonVoxelData = new MyObjectDataNonVoxel();
            mapping.WriteAndPosition(ref nonVoxelData);
            MyObjectDataCommon commonData = new MyObjectDataCommon();
            commonData.LocalMatrix = matrix;
            commonData.ColorMul = Vector3.One;
            commonData.KeyColor = new Vector3(0, -1f, 0f);
            commonData.CustomAlpha = stateData;

            mapping.WriteAndPosition(ref commonData);
            mapping.Unmap();
            return cb;
        }

        static void DrawInstanceComponent(MyInstanceComponent instanceComponent, List<MyHighlightDesc> highlightDescs)
        {
            MyRenderContext RC = MyRender11.RC;

            // common settings (combination of MyHighlightPass.cs and MyRenderingPass.cs):
            MyMapping mapping = MyMapping.MapDiscard(MyCommon.ProjectionConstants);
            Matrix matrix = MyRender11.Environment.Matrices.ViewProjectionAt0;
            matrix = Matrix.Transpose(matrix);
            mapping.WriteAndPosition(ref matrix);
            mapping.Unmap();

            RC.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.VertexShader.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
            RC.PixelShader.SetSrv(MyCommon.DITHER_8X8_SLOT, MyGeneratedTextureManager.Dithering8x8Tex);
            //RC.AllShaderStages.SetConstantBuffer(MyCommon.ALPHAMASK_VIEWS_SLOT, MyCommon.AlphamaskViewsConstants); // not used! Maybe impostors?
            RC.SetDepthStencilState(MyDepthStencilStateManager.WriteHighlightStencil, MyHighlight.HIGHLIGHT_STENCIL_MASK);
            RC.SetBlendState(null);
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            RC.SetScreenViewport();

            RC.PixelShader.SetConstantBuffer(4, MyCommon.HighlightConstants);


            MyLod lod = instanceComponent.GetHighlightLod();
            MyInstanceLodState stateId = MyInstanceLodState.Solid;
            float stateData = 0;

            RC.SetIndexBuffer(lod.IB);
            RC.SetVertexBuffer(0, lod.VB0);

            IConstantBuffer objectCB = GetObjectCB(RC, instanceComponent, stateData);

            RC.VertexShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, objectCB);
            RC.PixelShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, objectCB);

            foreach (MyHighlightDesc desc in highlightDescs)
            {
                MyHighlightDesc descRef = desc;
                WriteHighlightConstants(ref descRef);

                if (string.IsNullOrEmpty(desc.SectionName))
                {
                    foreach (var part in lod.Parts)
                        DrawHighlightedPart(RC, part, stateId);
                }
                else
                {
                    if (lod.HighlightSections != null && lod.HighlightSections.ContainsKey(desc.SectionName))
                        foreach (var part in lod.HighlightSections[desc.SectionName].Parts)
                            DrawHighlightedPart(RC, part, stateId);
                }
            }
        }
        #endregion

        /// <param name="sectionIndices">null for all the mesh</param>
        /// <param name="thickness">Zero or negative remove the outline</param>
        public static void AddObjects(uint ID, string[] sectionNames, Color? outlineColor, float thickness, float pulseTimeInSeconds, int instanceId)
        {
            MyRenderProxy.Assert(thickness > 0); // this behaviour was required by the prev implementation
            if (thickness > 0)
            {
                if (sectionNames == null)
                {
                    Add(ID, null, outlineColor.Value, thickness, pulseTimeInSeconds, instanceId);
                }
                else
                {
                    foreach (string sectionName in sectionNames)
                        Add(ID, sectionName, outlineColor.Value, thickness, pulseTimeInSeconds, instanceId);
                }
            }
        }

        public static void RemoveObjects(uint ID, string[] sectionNames)
        {
             m_highlights.Remove(ID);
        }

        public static bool HasHighlights
        {
            get { return m_highlights.Count > 0; }
        }

        public static void Run(IRtvBindable target, ICustomTexture fxaaTarget, IDepthStencil depthStencilCopy)
        {
            if (!HasHighlights)
                return;

            ProfilerShort.Begin("MyHighlight.Run");
            MyGpuProfiler.IC_BeginBlock("MyHighlight.Run");
            // set resolved depth/ stencil
            // render all with proper depth-stencil state
            // blur
            // blend to main target testing with stencil again

            MyHighlightPass.Instance.ViewProjection = MyRender11.Environment.Matrices.ViewProjectionAt0;
            MyHighlightPass.Instance.Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);

            MyHighlightPass.Instance.PerFrame();
            MyHighlightPass.Instance.Begin();

            int samples = MyRender11.Settings.User.AntialiasingMode.SamplesCount();
            IBorrowedRtvTexture rgba8_1 = MyManagers.RwTexturesPool.BorrowRtv("MyHighlight.Rgba8_1", Format.R8G8B8A8_UNorm_SRgb, samples);
            RC.ClearRtv(rgba8_1, new SharpDX.Color4(0, 0, 0, 0));
            RC.SetRtv(depthStencilCopy, MyDepthStencilAccess.DepthReadOnly, rgba8_1);

            foreach (var pair in m_highlights)
            {
                MyActor actor = MyIDTracker<MyActor>.FindByID(pair.Key);
                if (actor == null)
                {
                    MyRenderProxy.Fail("The actor cannot be found for highlight. This bug is outside of the renderer.");
                    continue;
                }
                MyRenderableComponent renderableComponent = actor.GetRenderable();
                MyInstanceComponent instanceComponent = actor.GetInstance();
                if (renderableComponent != null)
                    DrawRenderableComponent(actor, renderableComponent, pair.Value);
                else if (instanceComponent != null)
                    DrawInstanceComponent(instanceComponent, pair.Value);
                else
                {
                    // If an actor has been removed without removing outlines, just remove the outlines too
                    m_keysToRemove.Add(pair.Key);
                    MyRenderProxy.Fail("The actor has been removed, but the highligh is still active. This bug is caused by the issue out of the renderer.");
                }
            }

            MyHighlightPass.Instance.End();
            RC.SetBlendState(null);
            foreach (var outlineKey in m_keysToRemove)
                m_highlights.Remove(outlineKey);
            m_keysToRemove.Clear();

            ISrvBindable initialSourceView = rgba8_1;
            IRtvBindable renderTargetview = rgba8_1;

            float maxThickness = 0f;
            foreach (var pair in m_highlights)
                foreach (MyHighlightDesc descriptor in pair.Value)
                    maxThickness = Math.Max(maxThickness, descriptor.Thickness);

            if (maxThickness > 0)
            {
                IBorrowedRtvTexture rgba8_2 = MyManagers.RwTexturesPool.BorrowRtv("MyHighlight.Rgba8_2", Format.R8G8B8A8_UNorm_SRgb);
                MyBlur.Run(renderTargetview, rgba8_2, initialSourceView,
                    (int)Math.Round(maxThickness), MyBlur.MyBlurDensityFunctionType.Exponential, 0.25f,
                    MyDepthStencilStateManager.IgnoreDepthStencil);
                rgba8_2.Release();
            }

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            BlendHighlight(target, rgba8_1, fxaaTarget, depthStencilCopy);
        }

        static void DebugRecordMeshPartCommands(MeshId model, string sectionName, MyRenderableComponent render,
            MyRenderLod renderLod, MyMeshSectionPartInfo1[] meshes, int index)
        {
            MyRenderProxy.Error("DebugRecordMeshPartCommands1: Call Francesco");
            MyLog.Default.WriteLine("DebugRecordMeshPartCommands1");
            MyLog.Default.WriteLine("sectionName: " + sectionName);
            MyLog.Default.WriteLine("model.Info.Name: " + model.Info.Name);
            MyLog.Default.WriteLine("render.CurrentLod: " + render.CurrentLod);
            MyLog.Default.WriteLine("renderLod.RenderableProxies.Length: " + renderLod.RenderableProxies.Length);
            MyLog.Default.WriteLine("meshes.Length: " + meshes.Length);
            MyLog.Default.WriteLine("Mesh index: " + index);
            MyLog.Default.WriteLine("Mesh part index: " + meshes[index].PartIndex);
        }

        static void DebugRecordMeshPartCommands(MeshId model, string sectionName, MyMeshMaterialId material)
        {
            MyRenderProxy.Error("DebugRecordMeshPartCommands2: Call Francesco");
            MyLog.Default.WriteLine("DebugRecordMeshPartCommands2");
            MyLog.Default.WriteLine("sectionName: " + sectionName);
            MyLog.Default.WriteLine("model.Info.Name: " + model.Info.Name);
            MyLog.Default.WriteLine("material.Info.Name: " + material.Info.Name);
        }
    }
}
