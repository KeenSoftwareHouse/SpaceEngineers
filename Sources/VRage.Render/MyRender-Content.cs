#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SharpDX;
using SharpDX.Direct3D9;

using VRage.Utils;

using VRageRender.Effects;
using VRageRender.Graphics;
using VRageRender.Shadows;
using VRageRender.Textures;

using BoundingBox = VRageMath.BoundingBox;
using Vector3 = VRageMath.Vector3;
using VRage;
using VRageMath;

#endregion

namespace VRageRender
{
    public enum SetDepthTargetEnum
    {
        NoChange,
        RestoreDefault,
    }

    public enum MyEffects
    {
        //Rendering
        PointLight,
        DirectionalLight,
        BlendLights,
        //LodTransition2,
        ClearGBuffer,
        ShadowMap,
        //ShadowOcclusion,
        TransparentGeometry,
        //HudSectorBorder,
        Decals,

        //Post process effects
        SSAOBlur,
        SSAO,
        VolumetricSSAO,
        GaussianBlur,
        DownsampleForSSAO,
        AntiAlias,
        Screenshot,
        Scale,
        Threshold,
        HDR,
        Luminance,
        Contrast,
        VolumetricFog,
        GodRays,
        Vignetting,
        ColorMapping,
        ChromaticAberration,

        //Model effects
        ModelDNS,
        ModelDiffuse,
        VoxelDebrisMRT,
        VoxelsMRT,
        Gizmo,

        //Occlusion queries
        OcclusionQueryDrawMRT,

        //Sprites
        SpriteBatch,

        //Ambient map
        AmbientMapPrecalculation,

        //Background
        DistantImpostors,
        BackgroundCube,
        //DebugDrawBillboards,

        //HUD
        HudRadar,
        Hud,
        CockpitGlass,
    }

    internal enum MyRenderComponentID
    {
        RenderCamera,
        ShadowRendererBase,
        OcclusionQueries,
        VertexFormats,
        DepthStencilState,
        SamplerState,
        RasterizerState,
        BlendState,
        DebugDraw,
        RenderVoxelMaterials,
        BackgroundCube,
        TransparentGeometry,
        Decals,
        CockpitGlass,
        CockpitGlassDecals,
        DistantImpostors,
        SunGlare,
        TextureManager,
        Models,
        CustomMaterial,
    }

    internal static partial class MyRender
    {
        static readonly MyEffectBase[] m_effects = new MyEffectBase[Enum.GetValues(typeof(MyEffects)).GetLength(0)];
        static Texture m_randomTexture = null;
        static SortedDictionary<int, MyRenderFont> m_fontsById = new SortedDictionary<int, MyRenderFont>();
        static MyRenderFont m_debugFont;
        static Dictionary<int, MyRenderComponentBase> m_renderComponents = new Dictionary<int, MyRenderComponentBase>();

        #region Component registration

        static void RegisterComponent(MyRenderComponentBase renderComponent)
        {
            m_renderComponents.Add(renderComponent.GetID(), renderComponent);
        }


        #endregion


        #region Content load

        internal static void LoadContent(MyRenderQualityEnum quality)
        {
            MyRender.Log.WriteLine("MyRender.LoadContent(...) - START");

            GraphicsDevice = Device;
            MyRenderConstants.SwitchRenderQuality(quality);
            MyRenderTexturePool.RenderQualityChanged(quality);
            //Log.WriteLine("Viewport: " + GraphicsDevice.Viewport.ToString());

            LoadContent();

            MyRender.Log.WriteLine("MyRender.LoadContent(...) - END");
        }

        private static void DumpSettingsToLog()
        {
            var objects = new object[] { 
                m_settings.BackBufferWidth, m_settings.BackBufferHeight, 
                m_settings.AdapterOrdinal, m_settings.RefreshRate, m_settings.VSync, 
                m_settings.WindowMode
            };
            MyRender.Log.WriteLine(String.Format("Settings - resolution: {0}x{1}, adapter id: {2}, refresh rate: {3}, VSync: {4}, mode: {5}",
                objects));
        }

        private static void LoadContent()
        {
            MyRender.Log.WriteLine("MyRender.LoadContent() - START");

            MyRender.GetRenderProfiler().StartProfilingBlock("MyRender::LoadContent");

            m_screenshot = null;

            DumpSettingsToLog();

            UpdateScreenSize();

            CreateRenderTargets();
            CreateEnvironmentMapsRT(MyRenderConstants.ENVIRONMENT_MAP_SIZE);

            DefaultSurface = GraphicsDevice.GetRenderTarget(0);
            DefaultSurface.DebugName = "DefaultSurface";
            DefaultDepth = GraphicsDevice.DepthStencilSurface;
            DefaultDepth.DebugName = "DefaultDepth";

            m_randomTexture = CreateRandomTexture();

            LoadEffects();

            if (m_shadowRenderer == null)
            {
                m_shadowRenderer = new MyShadowRenderer(GetShadowCascadeSize(), MyRenderTargets.ShadowMap, MyRenderTargets.ShadowMapZBuffer, true);
            }

            if (m_spotShadowRenderer == null)
                m_spotShadowRenderer = new MySpotShadowRenderer();


            foreach (var renderComponent in m_renderComponents)
            {
                renderComponent.Value.LoadContent(GraphicsDevice);
            }

            m_spriteBatch = new Graphics.SpriteBatch(GraphicsDevice, "SpriteBatch");
            m_fullscreenQuad = new MyFullScreenQuad();

            BlankTexture = MyTextureManager.GetTexture<MyTexture2D>("Textures\\GUI\\Blank.dds", flags: TextureFlags.IgnoreQuality);

            MyEnvironmentMap.Reset();

            foreach (var ro in m_renderObjects)
            {
                ro.Value.LoadContent();
            }

            LoadContent_Video();

            MyRender.GetRenderProfiler().EndProfilingBlock();

            MyRender.Log.WriteLine("MyRender.LoadContent() - END");
        }

        internal static void UnloadContent(bool removeObjects = true)
        {
            MyRender.Log.WriteLine("MyRender.UnloadContent - START");
            MyRender.Log.IncreaseIndent();

            UnloadContent_Video();

            foreach (var ro in m_renderObjects)
            {
                ro.Value.UnloadContent();
            }

            foreach (var renderComponent in m_renderComponents.Reverse())
            {
                renderComponent.Value.UnloadContent();
            }

            for (int i = 0; i < m_renderTargets.GetLength(0); i++)
            {
                DisposeRenderTarget((MyRenderTargets)i);
            }

            DisposeSpotShadowRT();

            if (m_randomTexture != null)
            {
                m_randomTexture.Dispose();
                m_randomTexture = null;
            }

            UnloadEffects();

            Clear();

            if (m_fullscreenQuad != null)
            {
                m_fullscreenQuad.Dispose();
                m_fullscreenQuad = null;
            }

            MyTextureManager.UnloadTexture(BlankTexture);
            MyRenderTexturePool.ReleaseResources();

            if (m_spriteBatch != null)
            {
                m_spriteBatch.Dispose();
                m_spriteBatch = null;
            }

            if (DefaultSurface != null)
            {
                DefaultSurface.Dispose();
                DefaultSurface = null;
            }
            if (DefaultDepth != null)
            {
                DefaultDepth.Dispose();
                DefaultDepth = null;
            }

            try
            {
                if (GraphicsDevice != null)
                {
                    GraphicsDevice.SetStreamSource(0, null, 0, 0);
                    GraphicsDevice.Indices = null;
                    GraphicsDevice.PixelShader = null;
                    GraphicsDevice.VertexShader = null;
                    for (int i = 0; i < 16; i++)
                    {
                        GraphicsDevice.SetTexture(i, null);
                    }

                    GraphicsDevice.EvictManagedResources();
                }
                GraphicsDevice = null;
            }
            catch (Exception e)
            {
                //Because of crash on Win8
                Log.WriteLine(e);
            }

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyRender.UnloadContent - END");
        }

        internal static int GetShadowCascadeSize()
        {
            var maxTextureSize = m_adaptersList[m_settings.AdapterOrdinal].MaxTextureSize;
            try
            {
                return System.Math.Min(maxTextureSize / MyShadowRenderer.NumSplits, MyRenderConstants.RenderQualityProfile.ShadowMapCascadeSize);
            }
            catch (Exception e)
            {
                MyRender.Log.WriteLine("Exception in GetShadowCascadeSize(): ");
                MyRender.Log.WriteLine(e);

                // Use fallback 2K x 2K
                return System.Math.Min(2048 / MyShadowRenderer.NumSplits, MyRenderConstants.RenderQualityProfile.ShadowMapCascadeSize);
            }
        }

        internal static void UnloadData()
        {
            MyRenderTexturePool.ReleaseResources();
            AssertStructuresEmpty();

            m_prunningStructure.Clear();
            m_cullingStructure.Clear();
            m_manualCullingStructure.Clear();
            m_shadowPrunningStructure.Clear();
            m_farObjectsPrunningStructure.Clear();
            m_nearObjects.Clear();

            Clear();

            //Remove all empty cull objects (they could be already added empty)
            foreach (var ro in m_renderObjects)
            {
                MyCullableRenderObject cullObject = ro.Value as MyCullableRenderObject;
                if (cullObject != null && cullObject.EntitiesContained == 0)
                {
                    m_renderObjectsToDraw.Add(cullObject);
                }
            }
            foreach (var cullObject in m_renderObjectsToDraw)
            {
                m_renderObjects.Remove(cullObject.ID);
                cullObject.UnloadContent();
            }
            m_renderObjectsToDraw.Clear();

            System.Diagnostics.Debug.Assert(m_renderObjects.Count == 0);
        }

        internal static void ClearIntermediateLists()
        {
            m_renderOcclusionQueries.Clear();

            m_spotLightRenderElements.Clear();
            m_spotLightsPool.DeallocateAll();

            m_renderObjectsToDraw.Clear();
            m_renderObjectsToDebugDraw.Clear();

            m_renderElements.Clear();
            m_transparentRenderElements.Clear();
            m_cullObjectListForDraw.Clear();
            m_manualCullObjectListForDraw.Clear();
            m_renderObjectListForDraw.Clear();
            m_renderObjectListForIntersections.Clear();
        }

        internal static void Clear()
        {
            ClearElementPool();
            ClearIntermediateLists();
        }

        [Conditional("DEBUG")]
        internal static void AssertStructuresEmpty()
        {
            BoundingBoxD testAABB = new BoundingBoxD(new Vector3D(double.MinValue), new Vector3D(double.MaxValue));
            m_prunningStructure.OverlapAllBoundingBox(ref testAABB, m_renderObjectListForDraw);
            Debug.Assert(m_renderObjectListForDraw.Count == 0, "There are some objects in render prunning structure which are not removed on unload!");

            m_shadowPrunningStructure.OverlapAllBoundingBox(ref testAABB, m_renderObjectListForDraw);
            Debug.Assert(m_renderObjectListForDraw.Count == 0, "There are some objects in shadow prunning structure which are not removed on unload!");

            m_cullingStructure.OverlapAllBoundingBox(ref testAABB, m_cullObjectListForDraw);
            int count = 0;
            foreach (var obj in m_cullObjectListForDraw)
            {
                count += ((MyCullableRenderObject)obj).EntitiesContained;
                Debug.Assert(((MyCullableRenderObject)obj).EntitiesContained == 0, "There are some objects in culling structure which are not removed on unload!");

                // ((MyCullableRenderObject)obj).CulledObjects.OverlapAllBoundingBox(ref testAABB, m_renderObjectListForDraw);

            }

            m_manualCullingStructure.OverlapAllBoundingBox(ref testAABB, m_manualCullObjectListForDraw);
            count = 0;
            foreach (var obj in m_manualCullObjectListForDraw)
            {
                count += ((MyCullableRenderObject)obj).EntitiesContained;
                Debug.Assert(((MyCullableRenderObject)obj).EntitiesContained == 0, "There are some objects in manual culling structure which are not removed on unload!");

                // ((MyCullableRenderObject)obj).CulledObjects.OverlapAllBoundingBox(ref testAABB, m_renderObjectListForDraw);

            }

            m_farObjectsPrunningStructure.OverlapAllBoundingBox(ref testAABB, m_farCullObjectListForDraw);
            count = 0;
            foreach (var obj in m_farCullObjectListForDraw)
            {
                count += ((MyCullableRenderObject)obj).EntitiesContained;
                Debug.Assert(((MyCullableRenderObject)obj).EntitiesContained == 0, "There are some objects in manual culling structure which are not removed on unload!");

                // ((MyCullableRenderObject)obj).CulledObjects.OverlapAllBoundingBox(ref testAABB, m_renderObjectListForDraw);

            }

        }

        internal static void ReloadContent(MyRenderQualityEnum quality)
        {
            UnloadContent();
            LoadContent(quality);
        }

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

        #endregion

        #region Render targets

        internal static void CreateRenderTargets()
        {
            Viewport forwardViewport = GraphicsDevice.Viewport;
            Viewport backwardViewport = GetBackwardViewport();

            int forwardRTWidth = (int)(forwardViewport.Width);
            int forwardRTHeight = (int)(forwardViewport.Height);
            int forwardRTHalfWidth = (int)(forwardViewport.Width / 2);
            int forwardRTHalfHeight = (int)(forwardViewport.Height / 2);
            int forwardRT4Width = (int)(forwardViewport.Width / 4);
            int forwardRT4Height = (int)(forwardViewport.Height / 4);
            int forwardRT8Width = (int)(forwardViewport.Width / 8);
            int forwardRT8Height = (int)(forwardViewport.Height / 8);

            int secondaryShadowMapSize = MyRenderConstants.RenderQualityProfile.SecondaryShadowMapCascadeSize;

            //Largest RT
#if COLOR_SHADOW_MAP_FORMAT
            CreateRenderTarget(MyRenderTargets.ShadowMap, MyShadowRenderer.NumSplits * GetShadowCascadeSize(), GetShadowCascadeSize(), SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            CreateRenderTarget(MyRenderTargets.SecondaryShadowMap, MyShadowRenderer.NumSplits * secondaryShadowMapSize, secondaryShadowMapSize, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
#else
            CreateRenderTarget(MyRenderTargets.ShadowMap, MyShadowRenderer.NumSplits * GetShadowCascadeSize(), GetShadowCascadeSize(), Format.R32F, Usage.RenderTarget, false);

            // number of splits should be power of 2 (works in most cases)
            var maxSize = m_adaptersList[m_settings.AdapterOrdinal].MaxTextureSize;
            var shadowmapZbufferWidth = Math.Min(MyShadowRenderer.NumSplits*GetShadowCascadeSize(), maxSize);
            CreateRenderTarget(MyRenderTargets.ShadowMapZBuffer, shadowmapZbufferWidth, shadowmapZbufferWidth / MyShadowRenderer.NumSplits, Format.D24S8, Usage.DepthStencil, false);

            CreateRenderTarget(MyRenderTargets.SecondaryShadowMap, MyShadowRenderer.NumSplits * secondaryShadowMapSize, secondaryShadowMapSize, Format.R32F);
            CreateRenderTarget(MyRenderTargets.SecondaryShadowMapZBuffer, MyShadowRenderer.NumSplits * secondaryShadowMapSize, secondaryShadowMapSize, Format.D24S8, Usage.DepthStencil);
#endif

            const bool ENABLE_RT_MIPMAPS = false;

            //Full viewport RTs
            CreateRenderTarget(MyRenderTargets.Auxiliary0, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            CreateRenderTarget(MyRenderTargets.Auxiliary1, forwardRTWidth, forwardRTHeight, Format.A16B16G16R16F, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            CreateRenderTarget(MyRenderTargets.Auxiliary2, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);

            CreateRenderTarget(MyRenderTargets.Normals, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            CreateRenderTarget(MyRenderTargets.Diffuse, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            CreateRenderTarget(MyRenderTargets.Depth, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);

            CreateRenderTarget(MyRenderTargets.EnvironmentMap, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);

            //Half viewport RTs
            CreateRenderTarget(MyRenderTargets.AuxiliaryHalf0, forwardRTHalfWidth, forwardRTHalfHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);

            CreateRenderTarget(MyRenderTargets.DepthHalf, forwardRTHalfWidth, forwardRTHalfHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            CreateRenderTarget(MyRenderTargets.SSAO, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            CreateRenderTarget(MyRenderTargets.SSAOBlur, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);

            //Quarter viewport RTs
            CreateRenderTarget(MyRenderTargets.AuxiliaryQuarter0, forwardRT4Width, forwardRT4Height, Format.A8R8G8B8, Usage.RenderTarget, false);


            if (MyPostProcessHDR.RenderHDR())
            {
                CreateRenderTarget(MyRenderTargets.HDR4, forwardRT4Width, forwardRT4Height, Format.A2R10G10B10, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
                CreateRenderTarget(MyRenderTargets.HDRAux, forwardRTWidth, forwardRTHeight, Format.A8R8G8B8, Usage.RenderTarget | Usage.AutoGenerateMipMap);
                CreateRenderTarget(MyRenderTargets.HDR4Threshold, forwardRT4Width, forwardRT4Height, Format.A2R10G10B10, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
                CreateRenderTarget(MyRenderTargets.AuxiliaryHalf1010102, forwardRTHalfWidth, forwardRTHalfHeight, Format.A2R10G10B10, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            }

            //Low size RTs
            CreateRenderTarget(MyRenderTargets.SecondaryCamera, backwardViewport.Width, backwardViewport.Height, Format.A8R8G8B8, Usage.RenderTarget, ENABLE_RT_MIPMAPS);
            CreateRenderTarget(MyRenderTargets.SecondaryCameraZBuffer, backwardViewport.Width, backwardViewport.Height, Format.D24S8, Usage.DepthStencil, ENABLE_RT_MIPMAPS);
            CreateSpotShadowRT();

            SetEnvironmentRenderTargets();


            m_GBufferDefaultBinding = new Texture[] { (Texture)MyRender.GetRenderTarget(MyRenderTargets.Normals), (Texture)MyRender.GetRenderTarget(MyRenderTargets.Diffuse), (Texture)MyRender.GetRenderTarget(MyRenderTargets.Depth) };
            m_aux0Binding = new Texture[] { (Texture)MyRender.GetRenderTarget(MyRenderTargets.Auxiliary0) };

        }

        internal static void CreateSpotShadowRT()
        {
            DisposeSpotShadowRT();

            for (int i = 0; i < MyRenderConstants.SPOT_SHADOW_RENDER_TARGET_COUNT; i++)
            {
                m_spotShadowRenderTargets[i] = new Texture(GraphicsDevice, MySpotShadowRenderer.SpotShadowMapSize, MySpotShadowRenderer.SpotShadowMapSize, 0, Usage.RenderTarget, Format.R32F, Pool.Default);
                m_spotShadowRenderTargets[i].DebugName = "SpotShadowRT" + i;
                m_spotShadowRenderTargetsZBuffers[i] = new Texture(GraphicsDevice, MySpotShadowRenderer.SpotShadowMapSize, MySpotShadowRenderer.SpotShadowMapSize, 0, Usage.DepthStencil, Format.D24S8, Pool.Default);
                m_spotShadowRenderTargetsZBuffers[i].DebugName = "SpotShadowDepthRT" + i;
            }
        }

        // Create environment map render targets for both cube textures
        internal static void CreateEnvironmentMapsRT(int environmentMapSize)
        {
            CreateRenderTargetCube(MyRenderTargets.EnvironmentCube, environmentMapSize, Format.A8R8G8B8);
            CreateRenderTargetCube(MyRenderTargets.EnvironmentCubeAux, environmentMapSize, Format.A8R8G8B8);

            CreateRenderTargetCube(MyRenderTargets.AmbientCube, environmentMapSize, Format.A8R8G8B8);
            CreateRenderTargetCube(MyRenderTargets.AmbientCubeAux, environmentMapSize, Format.A8R8G8B8);

            CreateRenderTarget(MyRenderTargets.EnvironmentFaceAux, environmentMapSize, environmentMapSize, Format.A8R8G8B8);
            CreateRenderTarget(MyRenderTargets.EnvironmentFaceAux2, environmentMapSize, environmentMapSize, Format.A8R8G8B8);

            SetEnvironmentRenderTargets();
        }

        /// <summary>
        /// Sets the environment render targets.
        /// </summary>
        private static void SetEnvironmentRenderTargets()
        {
            var rt1 = MyRender.GetRenderTargetCube(MyRenderTargets.EnvironmentCube);
            var rt2 = MyRender.GetRenderTargetCube(MyRenderTargets.EnvironmentCubeAux);
            var rt3 = MyRender.GetRenderTargetCube(MyRenderTargets.AmbientCube);
            var rt4 = MyRender.GetRenderTargetCube(MyRenderTargets.AmbientCubeAux);
            var rt5 = MyRender.GetRenderTarget(MyRenderTargets.EnvironmentMap);

            MyEnvironmentMap.SetRenderTargets((CubeTexture)rt1, (CubeTexture)rt2, (CubeTexture)rt3, (CubeTexture)rt4, (Texture)rt5);
        }

        static void CreateRenderTargetCube(MyRenderTargets renderTarget, int size, Format surfaceFormat)
        {
            DisposeRenderTarget(renderTarget);
            if (size <= 0)
            {
                return;
            }

            m_renderTargets[(int)renderTarget] = new CubeTexture(GraphicsDevice, size, 0, Usage.RenderTarget | Usage.AutoGenerateMipMap, surfaceFormat, Pool.Default);
            m_renderTargets[(int)renderTarget].DebugName = renderTarget.ToString();
        }

        static void CreateRenderTarget(MyRenderTargets renderTarget, int width, int height, Format preferredFormat, Usage usage = Usage.RenderTarget, bool mipmaps = true)
        {
            try
            {
                //  Dispose render target - this happens e.g. after video resolution change
                DisposeRenderTarget(renderTarget);
                if (width <= 0 || height <= 0) // may happen when creatting in load content
                    return;

                //  Create new render target, no anti-aliasing
                m_renderTargets[(int)renderTarget] = new Texture(GraphicsDevice, width, height, mipmaps ? 0 : 1, usage, preferredFormat, Pool.Default);
                m_renderTargets[(int)renderTarget].DebugName = renderTarget.ToString();
            }
            catch (Exception )
            {
                string formatStr = "Creating render target failed, target: {0}({1}), width: {2}, height: {3}, format: {4}, usage: {5}, mipmaps: {6}";
                string str = String.Format(formatStr, renderTarget, (int)renderTarget, width, height, preferredFormat, usage, mipmaps);

                if(m_adaptersList[m_settings.AdapterOrdinal].DeviceName.Contains("Microsoft Basic Display Adapter"))
                {
                    throw new MyRenderException(str, MyRenderExceptionEnum.DriverNotInstalled);
                }
                else
                {
                    throw new MyRenderException(str, MyRenderExceptionEnum.GpuNotSupported);
                }
                
                //throw new ApplicationException(str, e);
            }
        }

        static void DisposeRenderTarget(MyRenderTargets renderTarget)
        {
            if (m_renderTargets[(int)renderTarget] != null)
            {
                m_renderTargets[(int)renderTarget].Dispose();
                m_renderTargets[(int)renderTarget] = null;
            }
        }

        static void DisposeSpotShadowRT()
        {
            for (int i = 0; i < MyRenderConstants.SPOT_SHADOW_RENDER_TARGET_COUNT; i++)
            {
                if (m_spotShadowRenderTargets[i] != null)
                {
                    m_spotShadowRenderTargets[i].Dispose();
                    m_spotShadowRenderTargets[i] = null;
                }

                if (m_spotShadowRenderTargetsZBuffers[i] != null)
                {
                    m_spotShadowRenderTargetsZBuffers[i].Dispose();
                    m_spotShadowRenderTargetsZBuffers[i] = null;
                }
            }
        }

        internal static Texture GetRenderTarget(MyRenderTargets renderTarget)
        {
            return (Texture)m_renderTargets[(int)renderTarget];
        }

        internal static CubeTexture GetRenderTargetCube(MyRenderTargets renderTarget)
        {
            return (CubeTexture)m_renderTargets[(int)renderTarget];
        }


        static int m_renderTargetsCount = 0;

        internal static void SetDeviceViewport(Viewport viewport)
        {
            GetRenderProfiler().StartProfilingBlock("MySandboxGame::SetDeviceViewport");

            if (m_renderTargetsCount == 0)
            {
                if ((DefaultSurface.Description.Height >= (viewport.Height + viewport.Y))
                    && (DefaultSurface.Description.Width >= (viewport.Width + viewport.X))
                    ||
                    m_screenshot != null)
                {
                    GetRenderProfiler().StartProfilingBlock("set viewport");
                    GraphicsDevice.Viewport = viewport;
                    GetRenderProfiler().EndProfilingBlock();
                }
                else
                {
                    GetRenderProfiler().StartProfilingBlock("change screen size");
                    UpdateScreenSize();
                    GetRenderProfiler().EndProfilingBlock();
                }
            }
            else
            {
                GetRenderProfiler().StartProfilingBlock("set viewport");
                GraphicsDevice.Viewport = viewport;
                GetRenderProfiler().EndProfilingBlock();
            }
            GetRenderProfiler().EndProfilingBlock();

        }


        private static void RestoreDefaultTargets()
        {
            if (m_screenshot != null)
            {
                GraphicsDevice.SetRenderTarget(0, m_screenshot.DefaultSurface);
                GraphicsDevice.SetRenderTarget(1, null);
                GraphicsDevice.SetRenderTarget(2, null);
                GraphicsDevice.DepthStencilSurface = m_screenshot.DefaultDepth;
            }
            else
            {
                GraphicsDevice.SetRenderTarget(0, DefaultSurface);
                GraphicsDevice.SetRenderTarget(1, null);
                GraphicsDevice.SetRenderTarget(2, null);
                GraphicsDevice.DepthStencilSurface = DefaultDepth;
            }
            m_renderTargetsCount = 0;
        }

        internal static void SetRenderTarget(Texture rt, Texture depth, SetDepthTargetEnum depthOp = SetDepthTargetEnum.NoChange)
        {
            if (rt == null)
            {
                RestoreDefaultTargets();
            }
            else
            {
                GraphicsDevice.SetRenderTarget(0, rt, 0);
                GraphicsDevice.SetRenderTarget(1, null);
                GraphicsDevice.SetRenderTarget(2, null);
                if (depth != null)
                {
                    GraphicsDevice.SetDepthStencil(depth, 0);
                }
                else if (depthOp == SetDepthTargetEnum.RestoreDefault)
                {
                    if (m_screenshot != null)
                        GraphicsDevice.DepthStencilSurface = m_screenshot.DefaultDepth;
                    else
                        GraphicsDevice.DepthStencilSurface = DefaultDepth;
                }

                m_renderTargetsCount = 1;
            }
        }

        internal static void SetRenderTargets(Texture[] rts, Texture depth, SetDepthTargetEnum depthOp = SetDepthTargetEnum.NoChange)
        {
            if (rts == null)
            {
                RestoreDefaultTargets();
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < rts.Length)
                    {
                        GraphicsDevice.SetRenderTarget(i, rts[i], 0);
                    }
                    else
                        GraphicsDevice.SetRenderTarget(i, null);
                }
                if (depth != null)
                {
                    GraphicsDevice.SetDepthStencil(depth, 0);
                }       /* todo
                else if (depthOp == SetDepthTargetEnum.RestoreDefault)
                {
                    GraphicsDevice.DepthStencilSurface = DefaultDepth;
                }              */

                m_renderTargetsCount = rts.Length;
            }
        }

        #endregion

        #region Effects

        internal static void LoadEffects()
        {
            if (!m_settings.DebugDrawOnly)
            {
                //Post process effects
                //m_effects[(int)MyEffects.LodTransition2] = new MyEffectLodTransition2();
                m_effects[(int)MyEffects.ClearGBuffer] = new MyEffectClearGbuffer();
                m_effects[(int)MyEffects.ShadowMap] = new MyEffectShadowMap();
                //m_effects[(int)MyEffects.ShadowOcclusion] = new MyEffectShadowOcclusion();
                m_effects[(int)MyEffects.TransparentGeometry] = new MyEffectTransparentGeometry();
                //m_effects[(int)MyEffects.HudSectorBorder] = new MyEffectHudSectorBorder();
                m_effects[(int)MyEffects.Decals] = new MyEffectDecals();
                m_effects[(int)MyEffects.PointLight] = new MyEffectPointLight();
                m_effects[(int)MyEffects.DirectionalLight] = new MyEffectDirectionalLight();
                m_effects[(int)MyEffects.BlendLights] = new MyEffectBlendLights();
                m_effects[(int)MyEffects.SSAO] = new MyEffectSSAO3();
                m_effects[(int)MyEffects.VolumetricSSAO] = new MyEffectVolumetricSSAO2();
                m_effects[(int)MyEffects.SSAOBlur] = new MyEffectSSAOBlur2();
                m_effects[(int)MyEffects.GaussianBlur] = new MyEffectGaussianBlur();
                m_effects[(int)MyEffects.DownsampleForSSAO] = new MyEffectDownsampleDepthForSSAO();
                m_effects[(int)MyEffects.AntiAlias] = new MyEffectAntiAlias();
                m_effects[(int)MyEffects.Screenshot] = new MyEffectScreenshot();
                m_effects[(int)MyEffects.Scale] = new MyEffectScale();
                m_effects[(int)MyEffects.Threshold] = new MyEffectThreshold();
                m_effects[(int)MyEffects.HDR] = new MyEffectHDR();
                m_effects[(int)MyEffects.Luminance] = new MyEffectLuminance();
                m_effects[(int)MyEffects.Contrast] = new MyEffectContrast();
                m_effects[(int)MyEffects.VolumetricFog] = new MyEffectVolumetricFog();
                m_effects[(int)MyEffects.GodRays] = new MyEffectGodRays();
                m_effects[(int)MyEffects.Vignetting] = new MyEffectVignetting();
                m_effects[(int)MyEffects.ColorMapping] = new MyEffectColorMapping();
                m_effects[(int)MyEffects.ChromaticAberration] = new MyEffectChromaticAberration();

                //Model effects
                m_effects[(int)MyEffects.Gizmo] = new MyEffectRenderGizmo();
                m_effects[(int)MyEffects.ModelDNS] = new MyEffectModelsDNS();
                m_effects[(int)MyEffects.ModelDiffuse] = new MyEffectModelsDiffuse();
                m_effects[(int)MyEffects.VoxelDebrisMRT] = new MyEffectVoxelsDebris();
                m_effects[(int)MyEffects.VoxelsMRT] = new MyEffectVoxels();
                m_effects[(int)MyEffects.OcclusionQueryDrawMRT] = new MyEffectOcclusionQueryDraw();
                m_effects[(int)MyEffects.SpriteBatch] = new MyEffectSpriteBatchShader();//prejmenovat enum..
                m_effects[(int)MyEffects.AmbientMapPrecalculation] = new MyEffectAmbientPrecalculation();

                //Background
                m_effects[(int)MyEffects.DistantImpostors] = new MyEffectDistantImpostors();
                m_effects[(int)MyEffects.BackgroundCube] = new MyEffectBackgroundCube();

                //HUD
                m_effects[(int)MyEffects.HudRadar] = new MyEffectHudRadar();
                m_effects[(int)MyEffects.Hud] = new MyEffectHud();
                m_effects[(int)MyEffects.CockpitGlass] = new MyEffectCockpitGlass();

                Debug.Assert(m_effects.All(effect => effect != null));
            }
            else
            {
                m_effects[(int)MyEffects.SpriteBatch] = new MyEffectSpriteBatchShader();
                m_effects[(int)MyEffects.ModelDiffuse] = new MyEffectModelsDiffuse();
                m_effects[(int)MyEffects.ClearGBuffer] = new MyEffectClearGbuffer();
                m_effects[(int)MyEffects.Screenshot] = new MyEffectScreenshot();
            }

        }

        static void UnloadEffects()
        {
            for (int i = 0; i < Enum.GetValues(typeof(MyEffects)).GetLength(0); i++)
            {
                MyEffectBase effect = m_effects[i];
                if (effect != null)
                {
                    effect.Dispose();
                    m_effects[i] = null;
                }
            }
        }

        static Texture CreateRandomTexture()
        {
            Random r = new Random();
            int size = 256;
            float[] rnd = new float[size];
            for (int i = 0; i < size; i++)
            {
                rnd[i] = MyUtils.GetRandomFloat(-1, 1);
            }

            var result = new Texture(GraphicsDevice, size, 1, 0, Usage.None, Format.R32F, Pool.Managed);
            DataStream ds;
            result.LockRectangle(0, LockFlags.None, out ds);
            ds.WriteRange(rnd);
            result.UnlockRectangle(0);

            return result;
        }

        internal static Texture GetRandomTexture()
        {
            return m_randomTexture;
        }

        internal static MyEffectBase GetEffect(MyEffects effect)
        {
            return m_effects[(int)effect];
        }

        internal static MyPostProcessBase GetPostProcess(MyPostProcessEnum name)
        {
            foreach (var p in m_postProcesses)
            {
                if (p.Name == name)
                    return p;
            }
            return null;
        }

        internal static IEnumerable<MyPostProcessBase> GetPostProcesses()
        {
            return m_postProcesses;
        }

        internal static MyRenderFont GetDebugFont()
        {
            return m_debugFont;
        }

        internal static MyRenderFont GetFont(int id)
        {
            return m_fontsById[id];
        }

        internal static MyRenderFont TryGetFont(int id)
        {
            MyRenderFont font;
            m_fontsById.TryGetValue(id, out font);
            return font;
        }

        #endregion
    }
}