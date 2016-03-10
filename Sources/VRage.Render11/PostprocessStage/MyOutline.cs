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
        internal static void HandleOutline(uint ID, string materialName, int sectionIndex, Color? outlineColor, float thickness)
        {
            if (thickness > 0)
                Add(ID, materialName, sectionIndex, outlineColor.Value, thickness);
            else
                Remove(ID);
        }

        /// <param name="sectionIndex">-1 for all the mesh</param>
        /// <param name="thickness">Zero or negative remove the outline</param>
        internal static void HandleOutline(uint ID, string materialName, int[] sectionIndices, Color? outlineColor, float thickness)
        {
            if (thickness > 0)
            {
                if (sectionIndices == null)
                {
                    Add(ID, materialName, -1, outlineColor.Value, thickness);
                }
                else
                {
                    foreach (int index in sectionIndices)
                        Add(ID, materialName, index, outlineColor.Value, thickness);
                }
            }
            else
            {
                Remove(ID);
            }
        }

        private static void Add(uint ID, string materialName, int sectionIndex, Color outlineColor, float thickness)
        {
            if (!m_outlines.ContainsKey(ID))
                m_outlines[ID] = new List<MyOutlineDesc>();

            m_outlines[ID].Add(new MyOutlineDesc { Material = MyStringId.GetOrCompute(materialName), SectionIndex = sectionIndex, Color = outlineColor, Thickness = thickness });
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

            MyOutlinePass.Instance.ViewProjection = MyEnvironment.ViewProjectionAt0;
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

            OutlineConstantsLayout constants = new OutlineConstantsLayout();
            float maxThickness = 0f;

            foreach (var pair in m_outlines)
            {
                MyActor actor = MyIDTracker<MyActor>.FindByID(pair.Key);
                if (actor == null)  // If an actor has been removed without removing outlines, just remove the outlines too
                {
                    m_keysToRemove.Add(pair.Key);
                    continue;
                }

                var renderableComponent = actor.GetRenderable();
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
                    if (descriptor.SectionIndex == -1)
                    {
                        RecordMeshPartCommands(model, currentModelId, renderableComponent, renderLod, descriptor, ref constants, ref maxThickness);
                    }
                    else
                    {
                        RecordMeshSectionCommands(model, currentModelId, renderableComponent, renderLod, descriptor, ref constants, ref maxThickness);
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
                    null, MyFoliageRenderingPass.GrassStencilMask);
            }

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
        }

        private static void RecordMeshPartCommands(MeshId model, LodMeshId lodModelId,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyOutlineDesc desc, ref OutlineConstantsLayout constants, ref float maxThickness)
        {
            var submeshCount = lodModelId.Info.PartsNum;
            for (int submeshIndex = 0; submeshIndex < submeshCount; ++submeshIndex)
            {
                var part = MyMeshes.GetMeshPart(model, rendercomp.CurrentLod, submeshIndex);

                maxThickness = Math.Max(desc.Thickness, maxThickness);
                constants.Color = desc.Color.ToVector4();

                var mapping = MyMapping.MapDiscard(MyCommon.OutlineConstants);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                RC.BindShaders(renderLod.HighlightShaders[submeshIndex]);
                MyOutlinePass.Instance.RecordCommands(renderLod.RenderableProxies[submeshIndex]);
            }
        }

        /// <returns>True if the section was found</returns>
        private static bool RecordMeshSectionCommands(MeshId model, LodMeshId lodModelId,
            MyRenderableComponent rendercomp, MyRenderLod renderLod,
            MyOutlineDesc desc, ref OutlineConstantsLayout constants, ref float maxThickness)
        {
            MeshSectionId sectionId;
            bool found = MyMeshes.TryGetMeshSection(model, rendercomp.CurrentLod, desc.SectionIndex, out sectionId);
            if (!found)
                return false;

            MyMeshSectionInfo1 section = sectionId.Info;
            MyMeshSectionPartInfo1[] meshes = section.Meshes;
            for (int idx = 0; idx < meshes.Length; idx++)
            {
                maxThickness = Math.Max(desc.Thickness, maxThickness);
                constants.Color = desc.Color.ToVector4();

                var mapping = MyMapping.MapDiscard(MyCommon.OutlineConstants);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                RC.BindShaders(renderLod.HighlightShaders[meshes[idx].PartIndex]);

                MyRenderableProxy proxy = renderLod.RenderableProxies[meshes[idx].PartIndex];
                MyOutlinePass.Instance.RecordCommands(proxy, meshes[idx].PartSubmeshIndex);
            }

            return true;
        }
    }
}