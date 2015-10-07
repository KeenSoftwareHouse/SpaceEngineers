using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    struct MyOutlineDesc {
        internal MyStringId Material;
        internal Color Color;
        internal Matrix? WorldToVolume;
    }

    class MyOutline : MyImmediateRC
    {
        internal static Dictionary<uint, List<MyOutlineDesc>> m_outlines = new Dictionary<uint, List<MyOutlineDesc>>();


        static PixelShaderId m_blurH;
        static PixelShaderId m_blurV;

        internal static void Init()
        {
            //m_copyPs = MyShaders.CreatePs("postprocess.hlsl", "copy");
            //m_clearAlphaPs = MyShaders.CreatePs("postprocess.hlsl", "clear_alpha");

            m_blurH = MyShaders.CreatePs("blur.hlsl", "blur_h", MyShaderHelpers.FormatMacros(MyRender11.ShaderSampleFrequencyDefine()));
            m_blurV = MyShaders.CreatePs("blur.hlsl", "blur_v");
        }

        internal static void Add(uint ID, string materialName, Color outlineColor, Matrix? outlineVolume)
        {
            if (!m_outlines.ContainsKey(ID))
            {
                m_outlines[ID] = new List<MyOutlineDesc>();
            }

            m_outlines[ID].Add(new MyOutlineDesc { Material = X.TEXT(materialName), Color = outlineColor, WorldToVolume = outlineVolume });
        }

        internal static void Remove(uint ID, string materialName)
        {
            if (!m_outlines.ContainsKey(ID))
            {
                return;
            }

            int index = -1;
            for (int i = 0; i < m_outlines[ID].Count; ++i)
            {
                if( m_outlines[ID][i].Material.ToString() == materialName ) {
                    index = i;
                    break;
                }
            }

            if(index != -1) {
                m_outlines[ID].RemoveAtFast(index);
            }
        }

        internal static bool AnyOutline()
        {
            return m_outlines.Count > 0;
        }

        internal static void Run()
        {
            // set resolved depth/ stencil
            // render all with proper depth-stencil state
            // blur
            // blend to main target testing with stencil again

            MyOutlinePass.Instance.ViewProjection = MyEnvironment.ViewProjectionAt0;
            MyOutlinePass.Instance.Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);

            MyOutlinePass.Instance.PerFrame();
            MyOutlinePass.Instance.Begin();

            RC.Clear();
            RC.Context.VertexShader.SetShaderResources(0, null, null, null, null, null, null);
            RC.Context.GeometryShader.SetShaderResources(0, null, null, null, null, null, null);
            RC.Context.PixelShader.SetShaderResources(0, null, null, null, null, null, null);
            RC.Context.ComputeShader.SetShaderResources(0, null, null, null, null, null, null);

            if (MyRender11.MultisamplingEnabled)
            {
                RC.Context.ClearRenderTargetView(MyRender11.m_rgba8_ms.m_RTV, new SharpDX.Color4(0, 0, 0, 0));

                RC.Context.OutputMerger.SetTargets(MyGBuffer.Main.DepthStencil.m_DSV, MyRender11.m_rgba8_ms.m_RTV);
            }
            else
            {
                RC.Context.ClearRenderTargetView(MyRender11.m_rgba8_1.m_RTV, new SharpDX.Color4(0, 0, 0, 0));

                RC.Context.OutputMerger.SetTargets(MyGBuffer.Main.DepthStencil.m_DSV, MyRender11.m_rgba8_1.m_RTV);
            }

            OutlineConstantsLayout constants;

            foreach (var kv in m_outlines)
            {
                var r = MyIDTracker<MyActor>.FindByID(kv.Key).GetRenderable();
                var renderLod = r.m_lods[r.m_lod];

                var submeshes = MyMeshes.GetLodMesh(r.GetModel(), r.m_lod).Info.PartsNum;
                for (int i = 0; i < submeshes; i++)
                {
                    var part = MyMeshes.GetMeshPart(r.GetModel(), r.m_lod, i);

                    for (int j = 0; j < kv.Value.Count; ++j)
                    {
                        if (part.Info.Material.Info.Name == kv.Value[j].Material)
                        {
                            constants.Color = kv.Value[j].Color.ToVector4();
                            constants.WorldToVolume = kv.Value[j].WorldToVolume.HasValue ? kv.Value[j].WorldToVolume.Value : Matrix.Zero;

                            var mapping = MyMapping.MapDiscard(MyCommon.OutlineConstants);
                            mapping.stream.Write(constants);
                            mapping.Unmap();

                            RC.BindShaders(renderLod.HighlightShaders[i]);
                            MyOutlinePass.Instance.RecordCommands(renderLod.RenderableProxies[i]);
                        }
                    }
                }
            }

            MyOutlinePass.Instance.End();
            RC.Context.OutputMerger.SetTargets(null as DepthStencilView, null as RenderTargetView);
            RC.SetBS(null);
            if (MyRender11.MultisamplingEnabled)
            {
                RC.Context.PixelShader.SetShaderResource(0, MyRender11.m_rgba8_ms.m_SRV);
            }
            else
            {
                RC.Context.PixelShader.SetShaderResource(0, MyRender11.m_rgba8_1.m_SRV);
            }
            RC.Context.OutputMerger.SetTargets(null as DepthStencilView, MyRender11.m_rgba8_2.m_RTV);
            RC.SetPS(m_blurH);
            MyScreenPass.DrawFullscreenQuad();

            RC.Context.PixelShader.SetShaderResource(0, null);
            RC.Context.OutputMerger.SetTargets(null as DepthStencilView, MyRender11.m_rgba8_1.m_RTV);
            RC.Context.PixelShader.SetShaderResource(0, MyRender11.m_rgba8_2.m_SRV);
            RC.SetPS(m_blurV);
            MyScreenPass.DrawFullscreenQuad();

            RC.Context.OutputMerger.SetTargets(null as DepthStencilView, null as RenderTargetView);
            RC.Context.PixelShader.SetShaderResource(0, null);
        }


    }
}