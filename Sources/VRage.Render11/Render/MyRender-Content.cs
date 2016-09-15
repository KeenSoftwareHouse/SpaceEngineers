using SharpDX.DXGI;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.OpenVRWrapper;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRage.Utils;
using VRageRender.Voxels;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static unsafe void InitSubsystemsOnce()
        {
        }

        internal static unsafe void InitSubsystems()
        {
            MyManagers.OnDeviceInit();
            //MyRwTextures.Init();
            MyHwBuffers.Init();
            ResetShadows(MyShadowCascades.Settings.NewData.CascadesCount, RenderSettings.ShadowQuality.ShadowCascadeResolution());
            MyRender11.Init();
            MyCommon.Init();
            MyVertexLayouts.Init();
            MyShaders.Init();
            MyMeshes.Init(); 
            MyMeshTableSrv.Init();
            MyLightRendering.Init();
            MyLinesRenderer.Init();
            MySpritesRenderer.Init();
            MyPrimitivesRenderer.Init();
            MyOutline.Init();
            MyBlur.Init();
            MyTransparentRendering.Init();

            MyFoliageComponents.Init();

            MyBillboardRenderer.Init(); // hardcoded limits
            MyDebugRenderer.Init();

            MyScreenDecals.Init();
            MyEnvProbeProcessing.Init();
            MyAtmosphereRenderer.Init();
			MyCloudRenderer.Init();
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

            MyHBAO.Init();

            try
            {
                if (m_settings.UseStereoRendering)
                {
                    var openVR = new MyOpenVR();
                    MyStereoStencilMask.InitUsingOpenVR();
                }
            }
            catch (System.Exception e)
            {
                if (!VRage.MyCompilationSymbols.DX11ForceStereo)
                    throw;
                MyStereoStencilMask.InitUsingUndefinedMask();
            }
        }

        internal static void OnDeviceReset()
        {
            MyManagers.OnDeviceReset();

            MyHwBuffers.OnDeviceReset();
            MyShaders.OnDeviceReset();
            MyMaterialShaders.OnDeviceReset();
            //MyRwTextures.OnDeviceReset();

            MyTransparentRendering.OnDeviceReset();

            ResetShadows(MyShadowCascades.Settings.NewData.CascadesCount, RenderSettings.ShadowQuality.ShadowCascadeResolution());

            MyBillboardRenderer.OnDeviceRestart();
            MyScreenDecals.OnDeviceReset();

            MyMeshMaterials1.OnDeviceReset();
            MyVoxelMaterials1.OnDeviceReset();

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
            MyManagers.OnDeviceEnd();

            MyScreenDecals.OnDeviceEnd();
            MyShaders.OnDeviceEnd();
            MyMaterialShaders.OnDeviceEnd();
            MyVoxelMaterials1.OnDeviceEnd();
            //MyRwTextures.OnDeviceEnd();
            MyHwBuffers.OnDeviceEnd();
            MyTransparentRendering.OnDeviceEnd();
        }

        #region Content load

        internal static void UnloadData()
        {
            MyManagers.OnUnloadData();

            MyActorFactory.RemoveAll();
            // many-to-one relation, can live withput owners, deallocated separately
            // MyComponentFactory<MyInstancingComponent>.RemoveAll();

            //MyVoxelMesh.RemoveAll();
            //MyDynamicMesh.RemoveAll();


            MyRender11.Log.WriteLine("Unloading session data");

            // Remove leftover persistent debug draw messages
            m_debugDrawMessages.Clear();

            MyScene.DynamicRenderablesDBVH.Clear();
            if (MyScene.SeparateGeometry)
                MyScene.StaticRenderablesDBVH.Clear();
            MyScene.GroupsDBVH.Clear();
            MyScene.FoliageDBVH.Clear();
            MyClipmapFactory.RemoveAll();
            MyClipmap.UnloadCache();

            MyInstancing.OnSessionEnd();
            MyFoliageComponents.OnSessionEnd();
            MyMeshes.OnSessionEnd();
            MyLights.OnSessionEnd();

            MyMaterials1.OnSessionEnd();
            MyVoxelMaterials1.OnSessionEnd();
            MyMeshMaterials1.OnSessionEnd();
            MyScreenDecals.OnSessionEnd();
            
            MyBigMeshTable.Table.OnSessionEnd();

            MyPrimitivesRenderer.Unload();

            MyTransparentRendering.OnSessionEnd();

            //MyAssetsLoader.ClearMeshes();
        }

        internal static void QueryTexturesFromEntities()
        {
            MyMeshMaterials1.OnResourcesRequesting();
            MyVoxelMaterials1.OnResourcesRequesting();
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

        internal static void ReloadFonts()
        {
            foreach (var fontIt in m_fontsById)
            {
                fontIt.Value.LoadContent();
            }
            m_debugFont.LoadContent();
        }

        #endregion
        
        internal static void RemoveScreenResources()
        {
            MyRwTextureManager texManager = MyManagers.RwTextures;

            MyManagers.GlobalResources.Destroy();

            if(m_lastScreenDataResource != null && m_lastScreenDataResource != Backbuffer)
            {
                m_lastScreenDataResource.Release();
                m_lastScreenDataResource = null;
            }

            if(m_lastDataStream != null)
            {
                m_lastDataStream.Dispose();
                m_lastDataStream = null;
            }

        }

        internal static void CreateScreenResources()
        {
            var width = m_resolution.X;
            var height = m_resolution.Y;
            var samples = RenderSettings.AntialiasingMode.SamplesCount();

            MyUtils.Init(ref MyGBuffer.Main);
            MyGBuffer.Main.Resize(width, height, samples, 0);

            MyScreenDependants.Resize(width, height, samples, 0);

            RemoveScreenResources();

            MyRwTextureManager texManager = MyManagers.RwTextures;
            MyManagers.GlobalResources.Create();

            MyHBAO.InitScreenResources();
        }

    }
}
