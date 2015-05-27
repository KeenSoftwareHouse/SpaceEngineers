using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static unsafe void InitSubsystems()
        {
            InitializeBlendStates();
            InitializeRasterizerStates();
            InitilizeSamplerStates();

            MyCommon.Init();
            MyPipelineStates.Init();
            MyTextures.Init();
            MyVertexLayouts.Init();
            MyShaders.Init();
            MyRwTextures.Init();
            MyHwBuffers.Init();
            MyMeshes.Init(); 
            MyMeshTableSRV.Init();
            MyMergeInstancing.Init(); 
            MyGeometryRenderer.Init();
            MyLightRendering.Init();
            MyShadows.Init();
            MyLinesRenderer.Init();
            MySpritesRenderer.Init();
            MyPrimitivesRenderer.Init();
            MyFoliageRenderer.Init();

            MyComponents.Init();

            MyBillboardRenderer.Init(); // hardcoded limits
            MyDebugRenderer.Init();
            MyGPUFoliageGenerating.Init();

            MyScreenDecals.Init();
            MyEnvProbeProcessing.Init();
            MyShadowsResolve.Init();
            MyAAEdgeMarking.Init(); 
            MyScreenPass.Init();
            MyCopyToRT.Init();
            MyBlendTargets.Init();
            MyFXAA.Init();
            MyDepthResolve.Init();
            MyBloom.Init();
            MyLuminanceAverage.Init();
            MyToneMapping.Init();
            MySSAO.Init();
            MyHdrDebugTools.Init();

            MySceneMaterials.Init();
            MyMaterials1.Init();
            MyVoxelMaterials1.Init();
            MyMeshMaterials1.Init();

            //MyShaderFactory.RunCompilation(); // rebuild
        }

        internal static void OnDeviceReset()
        {
            MyHwBuffers.OnDeviceReset();
            MyShaders.OnDeviceReset();
            MyMaterialShaders.OnDeviceReset();
            MyPipelineStates.OnDeviceReset();
            MyTextures.OnDeviceReset();
            MyRwTextures.OnDeviceEnd();
            MyShadows.OnDeviceReset();
            MyBillboardRenderer.OnDeviceRestart();
            MyScreenDecals.OnDeviceEnd();

            MyMeshMaterials1.InvalidateMaterials();
            MyVoxelMaterials1.InvalidateMaterials();


            MyRenderableComponent.MarkAllDirty();
            foreach (var f in MyComponentFactory<MyFoliageComponent>.GetAll())
            {
                f.Dispose();
            }

            foreach (var c in MyComponentFactory<MyGroupRootComponent>.GetAll())
            {
                c.OnDeviceReset();
            }

            MyBigMeshTable.Table.OnDeviceReset();
            MySceneMaterials.OnDeviceReset();
            MyMeshes.OnDeviceReset();
            MyInstancing.OnDeviceReset();
            MyScreenDecals.OnDeviceReset();
        }

        internal static void OnDeviceEnd()
        {
            MyScreenDecals.OnDeviceEnd();
            MyShaders.OnDeviceEnd();
            MyMaterialShaders.OnDeviceEnd();
            MyVoxelMaterials1.OnDeviceEnd();
            MyTextures.OnDeviceEnd();
            MyRwTextures.OnDeviceEnd();
            MyHwBuffers.OnDeviceEnd();
            MyPipelineStates.OnDeviceEnd();
        }

        internal static void OnSessionEnd()
        {
            UnloadData();
        }

        #region Content load

        internal static void UnloadData()
        {
            MyActorFactory.RemoveAll();
            // many-to-one relation, can live withput owners, deallocated separately
            // MyComponentFactory<MyInstancingComponent>.RemoveAll();

            //MyVoxelMesh.RemoveAll();
            //MyDynamicMesh.RemoveAll();


            MyRender11.Log.WriteLine("Unloading session data");

            MyScene.RenderablesDBVH.Clear();
            MyScene.GroupsDBVH.Clear();
            MyClipmapFactory.RemoveAll();

            MyInstancing.OnSessionEnd();
            MyMeshes.OnSessionEnd();
            MyLights.OnSessionEnd();

            MyMaterials1.OnSessionEnd();
            MyVoxelMaterials1.OnSessionEnd();
            MyMeshMaterials1.OnSessionEnd();
            MyScreenDecals.OnSessionEnd();
            
            MyTextures.OnSessionEnd();
            MyBigMeshTable.Table.OnSessionEnd();
            MyScreenDecals.OnSessionEnd();

            //MyAssetsLoader.ClearMeshes();
        }

        internal static void QueryTexturesFromEntities()
        {
            MyMeshMaterials1.OnResourcesRequesting();
            MyVoxelMaterials1.OnResourcesRequesting();
            MyScreenDecals.OnResourcesRequesting();
        }

        internal static void GatherTextures()
        {
            MyMeshMaterials1.OnResourcesGathering();
            MyVoxelMaterials1.OnResourcesGather();
        }

        #endregion

        #region Fonts

        static SortedDictionary<int, MyRenderFont> m_fontsById = new SortedDictionary<int, MyRenderFont>();
        static MyRenderFont m_debugFont;
        internal static MyRenderFont DebugFont { get { return m_debugFont; } }

        internal static void AddFont(int id, MyRenderFont font, bool isDebugFont)
        {
            Debug.Assert(!m_fontsById.ContainsKey(id), "Adding font with ID that already exists.");
            if (isDebugFont)
            {
                Debug.Assert(m_debugFont == null, "Debug font was already specified and it will be overwritten.");
                m_debugFont = font;
            }
            m_fontsById[id] = font;
        }

        internal static MyRenderFont GetDebugFont()
        {
            return m_debugFont;
        }

        internal static MyRenderFont GetFont(int id)
        {
            return m_fontsById[id];
        }

        #endregion

        internal static MyRenderTarget m_resolvedLight;
        internal static MyUnorderedAccessTexture m_reduce0;
        internal static MyUnorderedAccessTexture m_reduce1;
        internal static MyUnorderedAccessTexture m_uav3;
        internal static MyUnorderedAccessTexture m_prevLum;
        internal static MyUnorderedAccessTexture m_localLum;

        internal static MyUnorderedAccessTexture m_div2;
        internal static MyUnorderedAccessTexture m_div4;
        internal static MyUnorderedAccessTexture m_div8;
        internal static MyUnorderedAccessTexture m_div8_1;

        internal static MyUnorderedAccessTexture m_rgba8_linear;
        internal static MyCustomTexture m_rgba8_0;
        internal static MyRenderTarget m_rgba8_1;

        internal static RwTexId m_shadowsHelper = RwTexId.NULL;
        internal static RwTexId m_shadowsHelper1 = RwTexId.NULL;
        internal static RwTexId m_gbuffer1Copy = RwTexId.NULL;

        internal static void RemoveScreenResources()
        {
            if (m_resolvedLight != null)
            {
                m_resolvedLight.Release();
                m_reduce0.Release();
                m_reduce1.Release();
                m_uav3.Release();
                m_localLum.Release();
                m_div2.Release();
                m_div4.Release();
                m_div8.Release();
                m_div8_1.Release();
                m_rgba8_linear.Release();
                m_rgba8_0.Release();
                m_rgba8_1.Release();
                m_prevLum.Release();

                MyRwTextures.Destroy(ref m_shadowsHelper);
                MyRwTextures.Destroy(ref m_shadowsHelper1);
                MyRwTextures.Destroy(ref m_gbuffer1Copy);
            }
        }

        internal static void CreateScreenResources()
        {
            var width = m_resolution.X;
            var height = m_resolution.Y;
            var samples = RenderSettings.AntialiasingMode.SamplesCount();

            if(MyGBuffer.Main == null)
            {
                MyGBuffer.Main = new MyGBuffer();
            }
            MyGBuffer.Main.Resize(width, height, samples, 0);

            MyScreenDependants.Resize(width, height, samples, 0);

            RemoveScreenResources();

            m_resolvedLight = new MyRenderTarget(width, height, Format.R11G11B10_Float, 1, 0);
            m_reduce0 = new MyUnorderedAccessTexture(width, height, Format.R32G32_Float);
            m_reduce0.SetDebugName("reduce0");
            m_reduce1 = new MyUnorderedAccessTexture(width, height, Format.R32G32_Float);
            m_reduce1.SetDebugName("reduce1");
            m_uav3 = new MyUnorderedAccessTexture(width, height, Format.R11G11B10_Float);

            m_localLum = new MyUnorderedAccessTexture(
                (width + MyLuminanceAverage.NumThreads - 1) / MyLuminanceAverage.NumThreads,
                (height + MyLuminanceAverage.NumThreads - 1) / MyLuminanceAverage.NumThreads,
                Format.R32_Float);

            m_div2 = new MyUnorderedAccessTexture(width / 2, height / 2, Format.R11G11B10_Float);
            m_div4 = new MyUnorderedAccessTexture(width / 4, height / 4, Format.R11G11B10_Float);
            m_div8 = new MyUnorderedAccessTexture(width / 8, height / 8, Format.R11G11B10_Float);
            m_div8_1 = new MyUnorderedAccessTexture(width / 8, height / 8, Format.R11G11B10_Float);

            m_rgba8_linear = new MyUnorderedAccessTexture(width, height, Format.R8G8B8A8_UNorm);

            m_rgba8_0 = new MyCustomTexture(width, height, BindFlags.RenderTarget | BindFlags.ShaderResource, Format.R8G8B8A8_Typeless);
            m_rgba8_0.AddView(new MyViewKey { Fmt = Format.R8G8B8A8_UNorm, View = MyViewEnum.RtvView });
            m_rgba8_0.AddView(new MyViewKey { Fmt = Format.R8G8B8A8_UNorm_SRgb, View = MyViewEnum.SrvView });

            m_rgba8_1 = new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm_SRgb, 1, 0);
            m_prevLum = new MyUnorderedAccessTexture(1, 1, Format.R32G32_Float);

            Debug.Assert(m_shadowsHelper == RwTexId.NULL);
            m_shadowsHelper = MyRwTextures.CreateUav2D(width, height, Format.R8_UNorm, "cascade shadows gather");
            m_shadowsHelper1 = MyRwTextures.CreateUav2D(width, height, Format.R8_UNorm, "cascade shadows gather 2");

            m_gbuffer1Copy = MyRwTextures.CreateScratch2D(width, height, Format.R8G8B8A8_UNorm, samples, 0, "gbuffer 1 copy");
        }

        internal static void CopyGbufferToScratch()
        {
            MyImmediateRC.RC.Context.CopyResource(MyGBuffer.Main.m_resources[(int)MyGbufferSlot.GBuffer1].m_resource, m_gbuffer1Copy.Resource);
        }
    }
}
