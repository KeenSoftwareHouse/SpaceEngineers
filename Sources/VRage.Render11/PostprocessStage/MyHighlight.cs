using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.DXGI;
using VRage;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    struct MyHighlightDesc
    {
        internal int SectionIndex;
        internal Color Color;
        internal float Thickness;
        internal ulong PulseTimeInFrames;
        internal int InstanceId;
    }

    class MyHighlight : MyImmediateRC
    {
        public const byte HIGHLIGHT_STENCIL_MASK = 0x40;
        private static Dictionary<uint, List<MyHighlightDesc>> m_highlights = new Dictionary<uint, List<MyHighlightDesc>>();
        private static List<uint> m_keysToRemove = new List<uint>();

        internal static void Init()
        {
        }

        /// <param name="sectionIndex">-1 for all the mesh</param>
        /// <param name="thickness">Zero or negative remove the outline</param>
        internal static void HandleHighlight(uint ID, int sectionIndex, Color? outlineColor, float thickness, ulong pulseTimeInFrames)
        {
            if (thickness > 0)
                Add(ID, sectionIndex, outlineColor.Value, thickness, pulseTimeInFrames);
            else
                Remove(ID);
        }

        /// <param name="sectionIndices">null for all the mesh</param>
        /// <param name="thickness">Zero or negative remove the outline</param>
        internal static void HandleHighlight(uint ID, int[] sectionIndices, Color? outlineColor, float thickness, ulong pulseTimeInFrames, int instanceId)
        {
            if (thickness > 0)
            {
                if (sectionIndices == null)
                {
                    Add(ID, -1, outlineColor.Value, thickness, pulseTimeInFrames, instanceId);
                }
                else
                {
                    foreach (int index in sectionIndices)
                        Add(ID, index, outlineColor.Value, thickness, pulseTimeInFrames, instanceId);
                }
            }
            else
            {
                Remove(ID);
            }
        }

        private static void Add(uint ID, int sectionIndex, Color outlineColor, float thickness, ulong pulseTimeInFrames, int instanceId = -1)
        {
            if (!m_highlights.ContainsKey(ID))
                m_highlights[ID] = new List<MyHighlightDesc>();

            m_highlights[ID].Add(new MyHighlightDesc
            {
                SectionIndex = sectionIndex,
                Color = outlineColor,
                Thickness = thickness,
                PulseTimeInFrames = pulseTimeInFrames,
                InstanceId = instanceId
            });
        }

        private static void Remove(uint ID)
        {
            m_highlights.Remove(ID);
        }

        internal static void Run(IRtvBindable target, ICustomTexture fxaaTarget, IDepthStencil depthStencilCopy)
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

            RC.VertexShader.SetSrvs(0, null, null, null, null, null, null);
            RC.GeometryShader.SetSrvs(0, null, null, null, null, null, null);
            RC.PixelShader.SetSrvs(0, null, null, null, null, null, null);
            RC.ComputeShader.SetSrvs(0, null, null, null, null, null, null);

            int samples = MyRender11.RenderSettings.AntialiasingMode.SamplesCount();
            IBorrowedRtvTexture rgba8_1 = MyManagers.RwTexturesPool.BorrowRtv("MyHighlight.Rgba8_1", Format.R8G8B8A8_UNorm_SRgb, samples);
            RC.ClearRtv(rgba8_1, new SharpDX.Color4(0, 0, 0, 0));
            RC.SetRtv(depthStencilCopy, MyDepthStencilAccess.DepthReadOnly, rgba8_1);

            float maxThickness = 0f;

            foreach (var pair in m_highlights)
            {
                MyActor actor = MyIDTracker<MyActor>.FindByID(pair.Key);
                MyRenderableComponent renderableComponent;
                if (actor == null || (renderableComponent = actor.GetRenderable()) == null)
                {
                    // If an actor has been removed without removing outlines, just remove the outlines too
                    m_keysToRemove.Add(pair.Key);
                    continue;
                }

                var renderLod = renderableComponent.Lods[renderableComponent.CurrentLod];
                var model = renderableComponent.GetModel();

                LodMeshId currentModelId;
                if (!MyMeshes.TryGetLodMesh(model, renderableComponent.CurrentLod, out currentModelId))
                {
                    Debug.Fail("Mesh for outlining not found!");
                    continue;
                }

                foreach (MyHighlightDesc descriptor in pair.Value)
                {

                    if (!renderableComponent.IsRenderedStandAlone)
                    {
                        MyGroupLeafComponent leafComponent = actor.GetGroupLeaf();
                        MyGroupRootComponent groupComponent = leafComponent.RootGroup;
                        if (groupComponent != null)
                            RecordMeshPartCommands(model, actor, groupComponent, groupComponent.m_proxy, descriptor, ref maxThickness);

                        continue;

                    }

                    if (descriptor.SectionIndex == -1)
                    {
                        RecordMeshPartCommands(model, currentModelId, renderableComponent, renderLod, descriptor, ref maxThickness);
                    }
                    else
                    {
                        RecordMeshSectionCommands(model, currentModelId, renderableComponent, renderLod, descriptor, ref maxThickness);
                    }
                }
            }

            MyHighlightPass.Instance.End();
            RC.SetBlendState(null);

            foreach (var outlineKey in m_keysToRemove)
                m_highlights.Remove(outlineKey);

            m_keysToRemove.SetSize(0);

            ISrvBindable initialSourceView = rgba8_1;
            IRtvBindable renderTargetview = rgba8_1;

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

        public static bool HasHighlights
        {
            get { return m_highlights.Count > 0; }
        }

        private static void BlendHighlight(IRtvBindable target, ISrvBindable outlined, ICustomTexture fxaaTarget, IDepthStencil depthStencilCopy)
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

        private static void RecordMeshPartCommands(MeshId model, MyActor actor, MyGroupRootComponent group,
            MyCullProxy_2 proxy, MyHighlightDesc desc, ref float maxThickness)
        {
            MeshSectionId sectionId;
            bool found = MyMeshes.TryGetMeshSection(model, 0, desc.SectionIndex, out sectionId);
            if (!found)
                return;

            WriteHighlightConstants(ref desc, ref maxThickness);

            MyMeshSectionInfo1 section = sectionId.Info;
            MyMeshSectionPartInfo1[] meshes = section.Meshes;
            for (int idx = 0; idx < meshes.Length; idx++)
            {
                MyMaterialMergeGroup materialGroup;
                found = group.TryGetMaterialGroup(meshes[idx].Material, out materialGroup);
                if (!found)
                {
                    DebugRecordMeshPartCommands(model, desc.SectionIndex, meshes[idx].Material);
                    return;
                }

                int actorIndex;
                found = materialGroup.TryGetActorIndex(actor, out actorIndex);
                if (!found)
                    return;

                MyHighlightPass.Instance.RecordCommands(ref proxy.Proxies[materialGroup.Index], actorIndex, meshes[idx].PartIndex);
            }
        }

        private static void RecordMeshPartCommands(MeshId model, LodMeshId lodModelId,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyHighlightDesc desc, ref float maxThickness)
        {
            WriteHighlightConstants(ref desc, ref maxThickness);

            var submeshCount = lodModelId.Info.PartsNum;
            for (int submeshIndex = 0; submeshIndex < submeshCount; ++submeshIndex)
            {
                var part = MyMeshes.GetMeshPart(model, rendercomp.CurrentLod, submeshIndex);

                MyRenderUtils.BindShaderBundle(RC, renderLod.RenderableProxies[submeshIndex].HighlightShaders);
                MyHighlightPass.Instance.RecordCommands(renderLod.RenderableProxies[submeshIndex], -1, desc.InstanceId);
            }
        }

        /// <returns>True if the section was found</returns>
        private static void RecordMeshSectionCommands(MeshId model, LodMeshId lodModelId,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyHighlightDesc desc, ref float maxThickness)
        {
            MeshSectionId sectionId;
            bool found = MyMeshes.TryGetMeshSection(model, rendercomp.CurrentLod, desc.SectionIndex, out sectionId);
            if (!found)
                return;

            WriteHighlightConstants(ref desc, ref maxThickness);

            MyMeshSectionInfo1 section = sectionId.Info;
            MyMeshSectionPartInfo1[] meshes = section.Meshes;
            for (int idx = 0; idx < meshes.Length; idx++)
            {
                MyMeshSectionPartInfo1 sectionInfo = meshes[idx];
                if (renderLod.RenderableProxies.Length <= sectionInfo.PartIndex)
                {
                    DebugRecordMeshPartCommands(model, desc.SectionIndex, rendercomp, renderLod, meshes, idx);
                    return;
                }

                MyRenderableProxy proxy = renderLod.RenderableProxies[sectionInfo.PartIndex];
                MyHighlightPass.Instance.RecordCommands(proxy, sectionInfo.PartSubmeshIndex, desc.InstanceId);
            }

            return;
        }

        private static void WriteHighlightConstants(ref MyHighlightDesc desc, ref float maxThickness)
        {
            maxThickness = Math.Max(desc.Thickness, maxThickness);
            HighlightConstantsLayout constants = new HighlightConstantsLayout();
            constants.Color = desc.Color.ToVector4();
            if (desc.PulseTimeInFrames > 0)
                constants.Color.W *= (float)Math.Pow((float)Math.Cos(2.0 * Math.PI * (float)MyRender11.GameplayFrameCounter / (float)desc.PulseTimeInFrames), 2.0);

            var mapping = MyMapping.MapDiscard(MyCommon.HighlightConstants);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();
        }

        static void DebugRecordMeshPartCommands(MeshId model, int sectionIndex, MyRenderableComponent render,
            MyRenderLod renderLod, MyMeshSectionPartInfo1[] meshes, int index)
        {
            Debug.Assert(false, "DebugRecordMeshPartCommands1: Call Francesco");
            MyLog.Default.WriteLine("DebugRecordMeshPartCommands1");
            MyLog.Default.WriteLine("sectionIndex: " + sectionIndex);
            MyLog.Default.WriteLine("model.Info.Name: " + model.Info.Name);
            MyLog.Default.WriteLine("render.CurrentLod: " + render.CurrentLod);
            MyLog.Default.WriteLine("renderLod.RenderableProxies.Length: " + renderLod.RenderableProxies.Length);
            MyLog.Default.WriteLine("meshes.Length: " + meshes.Length);
            MyLog.Default.WriteLine("Mesh index: " + index);
            MyLog.Default.WriteLine("Mesh part index: " + meshes[index].PartIndex);
        }

        static void DebugRecordMeshPartCommands(MeshId model, int sectionIndex, MyMeshMaterialId material)
        {
            Debug.Assert(false, "DebugRecordMeshPartCommands2: Call Francesco");
            MyLog.Default.WriteLine("DebugRecordMeshPartCommands2");
            MyLog.Default.WriteLine("sectionIndex: " + sectionIndex);
            MyLog.Default.WriteLine("model.Info.Name: " + model.Info.Name);
            MyLog.Default.WriteLine("material.Info.Name: " + material.Info.Name);
        }
    }
}