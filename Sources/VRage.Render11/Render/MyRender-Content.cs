using System.Collections.Generic;
using System.Diagnostics;
using VRage.OpenVRWrapper;
using VRage.Render11.Common;
using VRage.Render11.LightingStage;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageRender.Voxels;

namespace VRageRender
{
    partial class MyRender11
    {
        private static unsafe void InitSubsystemsOnce()
        {
            MyManagers.GlobalResources.CreateOnStartup();
        }

        private static unsafe void InitSubsystems()
        {
            MyManagers.OnDeviceInit();
            ResetShadows(MyShadowCascades.Settings.NewData.CascadesCount, Settings.User.ShadowQuality.ShadowCascadeResolution());
            MyRender11.Init();
            MyCommon.Init();
            MyVertexLayouts.Init();
            MyShaders.Init();
            MyMeshes.Init(); 
            MyMeshTableSrv.Init();
            MyLightsRendering.Init();
            MyLinesRenderer.Init();
            MySpritesRenderer.Init();
            MyPrimitivesRenderer.Init();
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
            MyOcclusionQueryRenderer.Init();
            
            OnSessionStart();

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

        private static void OnDeviceReset()
        {
            MyManagers.OnDeviceReset();

            MyShaders.OnDeviceReset();
            MyMaterialShaders.OnDeviceReset();

            MyTransparentRendering.OnDeviceReset();

            ResetShadows(MyShadowCascades.Settings.NewData.CascadesCount, Settings.User.ShadowQuality.ShadowCascadeResolution());

            MyBillboardRenderer.OnDeviceReset();
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

        private static void OnDeviceEnd()
        {
            // Reversed order of calling End -- Managers last
            MyScreenDecals.OnDeviceEnd();
            MyShaders.OnDeviceEnd();
            MyMaterialShaders.OnDeviceEnd();
            MyVoxelMaterials1.OnDeviceEnd();
            MyTransparentRendering.OnDeviceEnd();

            MyManagers.OnDeviceEnd();
        }

        #region Content load

        private static void OnSessionStart()
        {
            MyAtmosphereRenderer.OnSessionStart();
        }
        private static void OnSessionEnd()
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

            MyScene.Clear();

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
            MyBillboardRenderer.OnSessionEnd();

            //MyAssetsLoader.ClearMeshes();
        }

        private static void QueryTexturesFromEntities()
        {
            MyMeshMaterials1.OnResourcesRequesting();
            MyVoxelMaterials1.OnResourcesRequesting();
        }

        private static void GatherTextures()
        {
            MyMeshMaterials1.OnResourcesGathering();
            MyVoxelMaterials1.OnResourcesGather();
        }

        #endregion

        #region Fonts

        private static readonly SortedDictionary<int, MyRenderFont> m_fontsById = new SortedDictionary<int, MyRenderFont>();
        internal static MyRenderFont DebugFont { get; private set; }

        private static void AddFont(int id, MyRenderFont font, bool isDebugFont)
        {
            Debug.Assert(!m_fontsById.ContainsKey(id), "Adding font with ID that already exists.");
            if (isDebugFont)
            {
                Debug.Assert(DebugFont == null, "Debug font was already specified and it will be overwritten.");
                DebugFont = font;
            }
            m_fontsById[id] = font;
        }

        internal static MyRenderFont GetDebugFont()
        {
            return DebugFont;
        }

        private static MyRenderFont GetFont(int id)
        {
            MyRenderFont font;
            if (m_fontsById.TryGetValue(id, out font))
                return font;
            Debug.Assert(false, "Font " + id + " was not loaded into renderer. Call MyRenderProxy.CreateFont first.");
            return DebugFont;
        }

        private static void ReloadFonts()
        {
            foreach (var fontIt in m_fontsById)
            {
                fontIt.Value.LoadContent();
            }
            DebugFont.LoadContent();
        }

        #endregion

        private static void RemoveScreenResources()
        {
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

        private static void CreateScreenResources()
        {
            var width = m_resolution.X;
            var height = m_resolution.Y;
            var samples = Settings.User.AntialiasingMode.SamplesCount();

            MyUtils.Init(ref MyGBuffer.Main);
            MyGBuffer.Main.Resize(width, height, samples, 0);

            MyLightsRendering.Resize(width, height);

            RemoveScreenResources();

            MyHBAO.InitScreenResources();
        }
    }
}
