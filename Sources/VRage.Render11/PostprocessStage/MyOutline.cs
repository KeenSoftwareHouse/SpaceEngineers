using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    struct MyOutlineDesc {
        internal MyStringId Material;
        internal int SectionIndex;
        internal Color Color;
        internal float Thickness;
        internal ulong PulseTimeInFrames;
        internal int InstanceId;
    }

    class MyOutline : MyImmediateRC
    {
        private static Dictionary<uint, List<MyOutlineDesc>> m_outlines = new Dictionary<uint, List<MyOutlineDesc>>();
        private static List<uint> m_keysToRemove = new List<uint>();

        internal static void Init()
        {
        }

        /// <param name="sectionIndex">-1 for all the mesh</param>
        /// <param name="thickness">Zero or negative remove the outline</param>
        internal static void HandleOutline(uint ID, int sectionIndex, Color? outlineColor, float thickness, ulong pulseTimeInFrames)
        {
            if (thickness > 0)
                Add(ID, sectionIndex, outlineColor.Value, thickness, pulseTimeInFrames);
            else
                Remove(ID);
        }

        /// <param name="sectionIndices">null for all the mesh</param>
        /// <param name="thickness">Zero or negative remove the outline</param>
        internal static void HandleOutline(uint ID, int[] sectionIndices, Color? outlineColor, float thickness, ulong pulseTimeInFrames, int instanceId)
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
            if (!m_outlines.ContainsKey(ID))
                m_outlines[ID] = new List<MyOutlineDesc>();

            m_outlines[ID].Add(new MyOutlineDesc
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
            m_outlines.Remove(ID);
        }

        internal static bool AnyOutline()
        {
            return m_outlines.Count > 0;
        }

        internal static void Run()
        {
            ProfilerShort.Begin("MyOutline.Run");
            MyGpuProfiler.IC_BeginBlock("MyOutline.Run");
            // set resolved depth/ stencil
            // render all with proper depth-stencil state
            // blur
            // blend to main target testing with stencil again

            MyOutlinePass.Instance.ViewProjection = MyRender11.Environment.ViewProjectionAt0;
            MyOutlinePass.Instance.Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);

            MyOutlinePass.Instance.PerFrame();
            MyOutlinePass.Instance.Begin();

            RC.Clear();
            RC.DeviceContext.VertexShader.SetShaderResources(0, null, null, null, null, null, null);
            RC.DeviceContext.GeometryShader.SetShaderResources(0, null, null, null, null, null, null);
            RC.DeviceContext.PixelShader.SetShaderResources(0, null, null, null, null, null, null);
            RC.DeviceContext.ComputeShader.SetShaderResources(0, null, null, null, null, null, null);

            if (MyRender11.MultisamplingEnabled)
            {
                RC.DeviceContext.ClearRenderTargetView(MyRender11.m_rgba8_ms.m_RTV, new SharpDX.Color4(0, 0, 0, 0));

                RC.DeviceContext.OutputMerger.SetTargets(MyGBuffer.Main.DepthStencil.m_DSV, MyRender11.m_rgba8_ms.m_RTV);
            }
            else
            {
                RC.DeviceContext.ClearRenderTargetView(MyRender11.m_rgba8_1.m_RTV, new SharpDX.Color4(0, 0, 0, 0));

                RC.DeviceContext.OutputMerger.SetTargets(MyGBuffer.Main.DepthStencil.m_DSV, MyRender11.m_rgba8_1.m_RTV);
            }

            float maxThickness = 0f;

            foreach (var pair in m_outlines)
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

                foreach (MyOutlineDesc descriptor in pair.Value)
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

            MyOutlinePass.Instance.End();
            RC.SetBS(null);

            foreach (var outlineKey in m_keysToRemove)
                m_outlines.Remove(outlineKey);

            m_keysToRemove.SetSize(0);

            ShaderResourceView initialSourceView = MyRender11.MultisamplingEnabled ? MyRender11.m_rgba8_ms.m_SRV : MyRender11.m_rgba8_1.m_SRV;
            RenderTargetView renderTargetview = MyRender11.MultisamplingEnabled ? MyRender11.m_rgba8_ms.m_RTV : MyRender11.m_rgba8_1.m_RTV;

            if (maxThickness > 0)
            {
                MyBlur.Run(renderTargetview, MyRender11.m_rgba8_2.m_RTV, MyRender11.m_rgba8_2.m_SRV, initialSourceView,
                    (int)Math.Round(5 * maxThickness),
                    MyBlur.MyBlurDensityFunctionType.Exponential, 0.25f,
                    null, MyFoliageRenderingPass.GrassStencilMask, MyRender11.Settings.BlurCopyOnDepthStencilFail);
            }

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
        }

        private static void RecordMeshPartCommands(MeshId model, MyActor actor, MyGroupRootComponent group,
            MyCullProxy_2 proxy, MyOutlineDesc desc, ref float maxThickness)
        {
            MeshSectionId sectionId;
            bool found = MyMeshes.TryGetMeshSection(model, 0, desc.SectionIndex, out sectionId);
            if (!found)
                return;

            OutlineConstantsLayout constants = new OutlineConstantsLayout();
            maxThickness = Math.Max(desc.Thickness, maxThickness);
            constants.Color = desc.Color.ToVector4();
            if (desc.PulseTimeInFrames > 0)
                constants.Color.W *= (float)Math.Pow((float)Math.Cos(2.0 * Math.PI * (float)MyRender11.Settings.GameplayFrame / (float)desc.PulseTimeInFrames), 2.0);

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

                var mapping = MyMapping.MapDiscard(MyCommon.OutlineConstants);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                MyOutlinePass.Instance.RecordCommands(ref proxy.Proxies[materialGroup.Index], actorIndex, meshes[idx].PartIndex);
            }
        }

        private static void RecordMeshPartCommands(MeshId model, LodMeshId lodModelId,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyOutlineDesc desc, ref float maxThickness)
        {
            OutlineConstantsLayout constants = new OutlineConstantsLayout();
            var submeshCount = lodModelId.Info.PartsNum;
            for (int submeshIndex = 0; submeshIndex < submeshCount; ++submeshIndex)
            {
                var part = MyMeshes.GetMeshPart(model, rendercomp.CurrentLod, submeshIndex);

                maxThickness = Math.Max(desc.Thickness, maxThickness);
                constants.Color = desc.Color.ToVector4();
                if (desc.PulseTimeInFrames > 0)
                    constants.Color.W *= (float)Math.Pow((float)Math.Cos(2.0 * Math.PI * (float)MyRender11.Settings.GameplayFrame / (float)desc.PulseTimeInFrames), 2.0);

                var mapping = MyMapping.MapDiscard(MyCommon.OutlineConstants);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                RC.BindShaders(renderLod.HighlightShaders[submeshIndex]);
                MyOutlinePass.Instance.RecordCommands(renderLod.RenderableProxies[submeshIndex], -1, desc.InstanceId);
            }
        }

        /// <returns>True if the section was found</returns>
        private static void RecordMeshSectionCommands(MeshId model, LodMeshId lodModelId,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyOutlineDesc desc, ref float maxThickness)
        {
            MeshSectionId sectionId;
            bool found = MyMeshes.TryGetMeshSection(model, rendercomp.CurrentLod, desc.SectionIndex, out sectionId);
            if (!found)
                return;

            OutlineConstantsLayout constants = new OutlineConstantsLayout();
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

                maxThickness = Math.Max(desc.Thickness, maxThickness);
                constants.Color = desc.Color.ToVector4();
                if (desc.PulseTimeInFrames > 0)
                    constants.Color.W *= (float)Math.Pow((float)Math.Cos(2.0 * Math.PI * (float)MyRender11.Settings.GameplayFrame / (float)desc.PulseTimeInFrames), 2.0);

                var mapping = MyMapping.MapDiscard(MyCommon.OutlineConstants);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                RC.BindShaders(renderLod.HighlightShaders[sectionInfo.PartIndex]);

                MyRenderableProxy proxy = renderLod.RenderableProxies[sectionInfo.PartIndex];
                MyOutlinePass.Instance.RecordCommands(proxy, sectionInfo.PartSubmeshIndex, desc.InstanceId);
            }

            return;
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
            MyLog.Default.WriteLine("renderLod.HighlightShaders.Length: " + renderLod.HighlightShaders.Length);
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