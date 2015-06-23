#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Import;
using SharpDX.Direct3D9;
using VRageRender.Effects;
using VRageRender.Graphics;
using System.Diagnostics;
using SharpDX;
using VRageRender.Textures;
using VRageRender.Lights;
using VRage;

using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using Rectangle = VRageMath.Rectangle;
using Matrix = VRageMath.Matrix;
using Color = VRageMath.Color;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Stats;
using VRage.Library.Utils;

#endregion

namespace VRageRender
{
    internal static partial class MyRender
    {
        #region Draw

        internal static void DrawScene()
        {
            GetRenderProfiler().StartProfilingBlock("Draw scene Part1");

            m_transparentRenderElements.Clear();
            ModelTrianglesCountStats = 0;

            GetRenderProfiler().StartProfilingBlock("Background");
            // Renders into aux0
            DrawBackground(m_aux0Binding);
            GetRenderProfiler().EndProfilingBlock();

            m_sortedElements.Clear();
            m_sortedTransparentElements.Clear();
        
            GetRenderProfiler().StartProfilingBlock("PrepareRenderObjectsForDraw");
            // Prepare entities for draw
            PrepareRenderObjectsForDraw(false);
            GetRenderProfiler().EndProfilingBlock();

            //  Draw LOD0 scene, objects
            DrawScene_OneLodLevel(MyLodTypeEnum.LOD0);
         
            GetRenderProfiler().StartProfilingBlock("AllGeometryRendered");
            DrawRenderModules(MyRenderStage.AllGeometryRendered);
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("DrawScene_Transparent");
            // Draw transparent models (non LODed, to be fully lit, not sorted)
            DrawScene_Transparent();
            GetRenderProfiler().EndProfilingBlock();

            MyRenderStats.Generic.Write("Model triangles", ModelTrianglesCountStats, MyStatTypeEnum.CurrentValue, 250, 0);

            // Issue occlusion queries
            IssueOcclusionQueries();

            // Render post processes, there's no source and target in this stage
            RenderPostProcesses(PostProcessStage.LODBlend, null, null, GetRenderTarget(MyRenderTargets.Auxiliary1), false);
       
            // Take screenshots if required
            //TakeLODScreenshots();

            GetRenderProfiler().EndProfilingBlock();   //Draw scene Part1

            GetRenderProfiler().StartProfilingBlock("Draw scene Part2");

            if (EnableLights && Settings.EnableLightsRuntime && MyRender.CurrentRenderSetup.EnableLights.Value && !Settings.ShowBlendedScreens)
            {
                //Render shadows to Lod0Diffuse
                if (Settings.EnableSun && Settings.EnableShadows && CurrentRenderSetup.EnableSun.Value)
                {
                    GetRenderProfiler().StartProfilingBlock("Render shadows");

                    GetShadowRenderer().Render();

                    GetRenderProfiler().EndProfilingBlock();
                }
                else
                {
                    // Set our targets
                    MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.ShadowMap), MyRender.GetRenderTarget(MyRenderTargets.ShadowMapZBuffer));
                    GraphicsDevice.Clear(ClearFlags.ZBuffer, new ColorBGRA(1.0f), 1.0f, 0);

                    MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.SecondaryShadowMap), MyRender.GetRenderTarget(MyRenderTargets.SecondaryShadowMapZBuffer));
                    GraphicsDevice.Clear(ClearFlags.ZBuffer, new ColorBGRA(1.0f), 1.0f, 0);
                }

                //Render all lights to Lod0Depth (LDR or HDR-part1) and Lod0Diffuse (HDR-part2)
                RenderLights();
            }
            else
            {
                MyRender.SetRenderTarget(GetRenderTarget(MyRenderTargets.Auxiliary1), null);
                BlendState.Opaque.Apply();
                RasterizerState.CullNone.Apply();
                DepthStencilState.None.Apply();
                Blit(GetRenderTarget(MyRenderTargets.Diffuse), false, MyEffectScreenshot.ScreenshotTechniqueEnum.Color);
            }

            RenderPostProcesses(PostProcessStage.PostLighting, null, null, GetRenderTarget(MyRenderTargets.EnvironmentMap), false);

            SetupAtmosphereShader();
            DrawNearPlanetSurfaceFromSpace();
            DrawNearPlanetSurfaceFromAtmosphere();

            GetRenderProfiler().StartProfilingBlock("PrepareRenderObjectsForFarDraw");
            // Prepare entities for draw
            PrepareRenderObjectsForDraw(true);
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("Draw far objects");
            DrawScene_BackgroundObjects(MyLodTypeEnum.LOD_BACKGROUND);
            GetRenderProfiler().EndProfilingBlock();

            DrawAtmosphere(false);
            DrawAtmosphere(true);
          

            GetRenderProfiler().StartProfilingBlock("AlphaBlendPreHDR");
            DrawRenderModules(MyRenderStage.AlphaBlendPreHDR);
            GetRenderProfiler().EndProfilingBlock();

            TakeScreenshot("Blended_lights", GetRenderTarget(MyRenderTargets.Auxiliary1), MyEffectScreenshot.ScreenshotTechniqueEnum.Color);

            GetRenderProfiler().EndProfilingBlock();   //Draw scene part 2

            GetRenderProfiler().StartProfilingBlock("Draw scene Part3 ");

            //MySandboxGameDX.SetRenderTarget(null, null);

            // Render post processes
            RenderPostProcesses(PostProcessStage.HDR, GetRenderTarget(MyRenderTargets.Auxiliary1), m_aux0Binding, GetRenderTarget(MyRenderTargets.Auxiliary2), true, true);
            TakeScreenshot("HDR_down4_blurred", MyRender.GetRenderTarget(MyRenderTargets.HDR4Threshold), MyEffectScreenshot.ScreenshotTechniqueEnum.HDR);

            GetRenderProfiler().EndProfilingBlock();   //Draw scene Part2

            GetRenderProfiler().StartProfilingBlock("Draw scene Part4");
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //  Only one render target - where we draw final LOD scene
            MyRender.SetRenderTargets(m_aux0Binding, null);
            SetCorrectViewportSize();


            if (Settings.ShowBlendedScreens && CurrentRenderSetup.EnableDebugHelpers.Value)
            {     
                DrawDebugBlendedRenderTargets();
            }        

            GetRenderProfiler().StartProfilingBlock("Alphablend");
            DrawRenderModules(MyRenderStage.AlphaBlend);
            GetRenderProfiler().EndProfilingBlock();

         
            // Render post processes
            RenderPostProcesses(PostProcessStage.AlphaBlended, GetRenderTarget(MyRenderTargets.Auxiliary0), CurrentRenderSetup.RenderTargets, GetRenderTarget(MyRenderTargets.Auxiliary1), true, true);

            MyRender.SetRenderTargets(CurrentRenderSetup.RenderTargets, CurrentRenderSetup.DepthTarget);
            SetCorrectViewportSize();

            if (m_currentSetup.DepthToAlpha)
            {
                Blit(GetRenderTarget(MyRenderTargets.Depth), true, MyEffectScreenshot.ScreenshotTechniqueEnum.DepthToAlpha);
            }
            if (m_currentSetup.DepthCopy)
            {
                Blit(GetRenderTarget(MyRenderTargets.Depth), true, MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
            }

            if (Settings.ShowEnvironmentScreens && CurrentRenderSetup.EnableDebugHelpers.Value)
            {
                DrawDebugEnvironmentRenderTargets();
            }

            GetRenderProfiler().EndProfilingBlock();   //Draw scene Part3
        }

   
        #region Screenshot

        internal static void TakeScreenshot(string name, BaseTexture target, MyEffectScreenshot.ScreenshotTechniqueEnum technique)
        {
            if (ScreenshotOnlyFinal && name != "FinalScreen" && name != "test")
                return;

            //  Screenshot object survives only one DRAW after created. We delete it immediatelly. So if 'm_screenshot'
            //  is not null we know we have to take screenshot and set it to null.
            if (m_screenshot != null)
            {
                try
                {
                    if (target is Texture)
                    {
                        Texture renderTarget = target as Texture;
                        Texture rt = new Texture(MyRender.GraphicsDevice, renderTarget.GetLevelDescription(0).Width, renderTarget.GetLevelDescription(0).Height, 0, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                        MyRender.SetRenderTarget(rt, null);

                        BlendState.NonPremultiplied.Apply();
                        DepthStencilState.None.Apply();
                        RasterizerState.CullNone.Apply();

                        MyEffectScreenshot ssEffect = GetEffect(MyEffects.Screenshot) as MyEffectScreenshot;
                        ssEffect.SetSourceTexture(renderTarget);
                        ssEffect.SetTechnique(technique);
                        ssEffect.SetScale(Vector2.One);
                        MyRender.GetFullscreenQuad().Draw(ssEffect);

                        MyRender.SetRenderTarget(null, null);

                        string filename = m_screenshot.SaveTexture2D(rt, name);
                        rt.Dispose();

                        MyRenderProxy.ScreenshotTaken(filename != null, filename, m_screenshot.ShowNotification);
                    }
                    else if (target is CubeTexture)
                    {
                        string filename = m_screenshot.GetFilename(name + ".dds");
                        CubeTexture.ToFile(target, filename, ImageFileFormat.Dds);

                        MyRenderProxy.ScreenshotTaken(true, filename, m_screenshot.ShowNotification);
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine("Error while taking screenshot.");
                    Log.WriteLine(e.ToString());
                    MyRenderProxy.ScreenshotTaken(false, null, m_screenshot.ShowNotification);
                }
            }    
        }

        #endregion

        #region Draw

        private static void DrawModelsLod(MyLodTypeEnum lodTypeEnum, bool drawUsingStencil, bool collectTransparentElements)
        {
            m_renderElements.Clear();

            // Call draw for all entities, they push wanted render models to the list
            m_renderObjectsToDraw.Clear();

            GetRenderProfiler().StartProfilingBlock("LODDrawStart");
            DrawRenderModules(MyRenderStage.LODDrawStart);
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("entity.Draw");

            if (lodTypeEnum == MyLodTypeEnum.LOD_NEAR)
            {
                foreach (MyRenderObject renderObject in m_nearObjects)
                {
                    renderObject.Draw();
                }
            }
            else
            {
                //  Iterate over all objects that intersect the frustum (objects and voxel maps)
                foreach (MyRenderObject renderObject in m_renderObjectListForDraw)
                {
                    renderObject.Draw();
                }
            }

            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("CollectElements", m_renderObjectsToDraw.Count);
            CollectElements(lodTypeEnum, collectTransparentElements);
            GetRenderProfiler().EndProfilingBlock();

            //Sort render elements by their model and drawtechnique. We spare a lot of device state changes   
            GetRenderProfiler().StartProfilingBlock("m_renderElements.Sort");
            m_sortedElements.Add(lodTypeEnum, m_renderElements);
            GetRenderProfiler().EndProfilingBlock();

            //Draw sorted render elements
            int ibChangesStats;
            GetRenderProfiler().StartProfilingBlock("DrawRenderElements");
            DrawRenderElementsAlternative(m_sortedElements, lodTypeEnum, out ibChangesStats);
            MyPerformanceCounter.PerCameraDrawWrite.RenderElementsIBChanges += ibChangesStats;
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("DrawRenderElements Near");
            if (lodTypeEnum == MyLodTypeEnum.LOD_NEAR)  //We cannot render near transparent elements later
            {
                if (!drawUsingStencil)
                {   //Only near objects while LOD0 rendering
                    int ibStateChanges;

                    m_sortedTransparentElements.Add(lodTypeEnum, m_transparentRenderElements);
                    DrawRenderElementsAlternative(m_sortedTransparentElements, lodTypeEnum, out ibStateChanges);
                    m_transparentRenderElements.Clear();
                }
            }
            MyPerformanceCounter.PerCameraDrawWrite.RenderElementsInFrustum += m_renderElements.Count;
            GetRenderProfiler().EndProfilingBlock();
        }

        internal static void AddRenderObjectToDraw(MyRenderObject renderObject)
        {
            m_renderObjectsToDraw.Add(renderObject);
            m_renderObjectsToDebugDraw.Add(renderObject);
            MyRenderProxy.VisibleObjectsWrite.Add(renderObject.ID);
        }

        private static void CollectElements(MyLodTypeEnum lodTypeEnum, bool collectTransparentElements)
        {
            if (m_renderObjectsToDraw.Count <= 0)
                return;

            // Gather all render elements from entities, sort them later
            for (int i = 0; i < m_renderObjectsToDraw.Count; i++)
            {
                MyRenderObject renderObject = m_renderObjectsToDraw[i];
                renderObject.GetRenderElements(lodTypeEnum, m_renderElements, collectTransparentElements ? m_transparentRenderElements : null);
            }
        }


        internal static void AllocateRenderElement(out MyRenderElement renderElement)
        {
            lock (m_renderElementsPool)
            {
                renderElement = m_renderElementCounter >= MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT ? null : m_renderElementsPool[m_renderElementIndex++ % MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT];
                m_renderElementCounter++;
            }            
            IsRenderOverloaded = m_renderElementCounter >= (MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT - 4096);
        }

        private static MyRenderMeshMaterial m_emptyMaterial = new MyRenderMeshMaterial("", "", "", null, null);

        [Conditional("DEVELOP")]
        public static void CheckTextures(MyEffectBase shader, Texture normalTexture, bool isHolo)
        {
            if (Settings.CheckDiffuseTextures)
            {
                if (!shader.IsTextureDiffuseSet())
                {
                    LazyLoadDebugTextures();

                    shader.SetTextureDiffuse(m_debugTexture);
                    shader.SetDiffuseColor(Vector3.One);
                    shader.SetEmissivity(1);
                }
                else
                {
                    if (!isHolo)
                    {
                        shader.SetEmissivity(0);
                    }
                }
            }
            if (Settings.CheckNormalTextures)
            {
                if (!shader.IsTextureNormalSet())
                {
                    LazyLoadDebugTextures();

                    shader.SetTextureDiffuse(m_debugTexture);
                    shader.SetEmissivity(1);
                }
                else
                {
                    shader.SetTextureDiffuse(normalTexture);
                    //shader.SetTextureDiffuse(m_debugNormalTexture);
                    shader.SetEmissivity(0);
                }
            }

            if (!shader.IsTextureNormalSet())
            {
                LazyLoadDebugTextures();
                shader.SetTextureNormal(m_debugTexture);
            }
        }

        internal static MyTexture2D GetDebugTexture()
        {
            LazyLoadDebugTextures();
            return m_debugTexture;
        }

        internal static MyTexture2D GetDebugNormalTexture()
        {
            LazyLoadDebugTextures();
            return m_debugNormalTexture;
        }

        internal static MyTexture2D GetDebugNormalTextureBump()
        {
            LazyLoadDebugTextures();
            return m_debugNormalTextureBump;
        }

        static void LazyLoadDebugTextures()
        {
            if (m_debugTexture == null)
            {
                m_debugTexture = MyTextureManager.GetTexture<MyTexture2D>("Textures2\\Models\\Debug\\debug_d");
            }
            if (m_debugNormalTexture == null)
            {
                m_debugNormalTexture = MyTextureManager.GetTexture<MyTexture2D>("Textures2\\Models\\fake_ns");
            }
            if (m_debugNormalTextureBump == null)
            {
                m_debugNormalTextureBump = MyTextureManager.GetTexture<MyTexture2D>("Textures2\\Models\\Debug\\debug_n");
            }
        }


        static MyRenderSetup m_renderSetup = new MyRenderSetup();
        static MyRenderSetup m_backupRenderSetup = new MyRenderSetup();

        static VRageMath.Vector2 m_sizeMultiplierForStrings = Vector2.One;

        internal static void UpdateInterpolationLag()
        {
            var predictedUpdateTime = m_currentUpdateTime + m_currentUpdateTime - m_previousUpdateTime;
            var predictedDrawTime = m_currentDrawTime + m_currentDrawTime - m_previousDrawTime;
            var correction = (predictedDrawTime - predictedUpdateTime + MyTimeSpan.FromMiliseconds(MyRender.Settings.InterpolationLagMs) - m_currentLag).Miliseconds * MyRender.Settings.LagFeedbackMult;
            var lag = m_currentLag.Miliseconds + correction;
            lag = Math.Max(lag, MyRender.Settings.InterpolationLagMs);
            lag = Math.Min(lag, 100);
            m_currentLag = MyTimeSpan.FromMiliseconds(lag);

            MyRenderStats.Generic.Write("interpolation lag", (float)m_currentLag.Miliseconds, MyStatTypeEnum.CurrentValue, 100, 2);
        }

        private static MyTexture2D GetTextureForName(string name)
        {
            foreach (var item in m_sortedElements.Models)
            {
                foreach (var material in item.Models)
                {
                    if (material.Key.DiffuseName == name)
                    {
                        return material.Key.DiffuseTexture;
                    }
                }
            }

            foreach (var item in m_sortedTransparentElements.Models)
            {
                foreach (var material in item.Models)
                {
                    if (material.Key.DiffuseName == name)
                    {
                        return material.Key.DiffuseTexture;
                    }
                }
            }
            return null;
        }

        public static void Draw(bool draw = true)
        {
            RenderTimeInMS += MyRenderConstants.RENDER_STEP_IN_MILLISECONDS;

            MyPerformanceCounter.PerCameraDrawWrite.Reset();

            UpdateInterpolationLag();
            
            GetRenderProfiler().StartProfilingBlock("ProcessMessages");
            ProcessMessageQueue();
            GetRenderProfiler().EndProfilingBlock();

            if (draw)
            {
                Texture rt = null;
                Texture dt = null;

                if (m_screenshot != null)
                {
                    m_renderSetup.CallerID = MyRenderCallerEnum.Main;

                    Viewport viewport = MyRenderCamera.Viewport;
                    m_sizeMultiplierForStrings = m_screenshot.SizeMultiplier;

                    UpdateScreenSize();
                    MyEnvironmentMap.Reset();

                    MyRenderCamera.ChangeFov(MyRenderCamera.FieldOfView); // Refresh projection
                    MyRenderCamera.UpdateCamera();
                    MyRender.SetDeviceViewport(MyRenderCamera.Viewport);

                    CreateRenderTargets();
                    m_renderSetup.ShadowRenderer = MyRender.GetShadowRenderer();
                    //We need depth n stencil because stencil is used for drawing hud
                    rt = new Texture(GraphicsDevice, (int)(MyRenderCamera.Viewport.Width), (int)(MyRenderCamera.Viewport.Height), 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                    dt = new Texture(GraphicsDevice, (int)(MyRenderCamera.Viewport.Width), (int)(MyRenderCamera.Viewport.Height), 1, Usage.DepthStencil, Format.D24S8, Pool.Default);
                    m_renderSetup.RenderTargets = new Texture[] { rt };
                    m_renderSetup.AspectRatio = MyRenderCamera.AspectRatio;
                    m_renderSetup.ProjectionMatrix = MyRenderCamera.ProjectionMatrix;

                    m_screenshot.DefaultSurface = rt.GetSurfaceLevel(0);
                    m_screenshot.DefaultDepth = dt.GetSurfaceLevel(0);

                    // To have render target with large size on device (screen.Draw calls MyCamera.EnableForward, which sets Viewport on device)
                    SetRenderTargets(m_renderSetup.RenderTargets, dt);
                    PushRenderSetupAndApply(m_renderSetup, ref m_backupRenderSetup);

                    m_backupRenderSetup.Viewport = viewport;
                }

                GetRenderProfiler().StartProfilingBlock("DrawMessageQueue");
                DrawMessageQueue();
                GetRenderProfiler().EndProfilingBlock();

                if (null != m_texturesToRender && m_texturesToRender.Count > 0)
                {
                    RenderColoredTextures();
                }

                if (m_screenshot != null)
                {                 
                    MyRender.PopRenderSetupAndRevert(m_backupRenderSetup);
                    
                    MyRender.TakeScreenshot("FinalScreen", GetScreenshotTexture(), Effects.MyEffectScreenshot.ScreenshotTechniqueEnum.Color);

                    var screen = m_screenshot;
                    m_screenshot = null; // Need to clear before SetRenderTarget(null, null)

                    SetRenderTarget(null, null);
                    MyRender.Blit(GetScreenshotTexture(), true, MyEffectScreenshot.ScreenshotTechniqueEnum.Color);

                    GetScreenshotTexture().Dispose();

                    screen.DefaultSurface.Dispose();
                    screen.DefaultDepth.Dispose();

                    rt.Dispose();
                    dt.Dispose();

                    MyRender.GraphicsDevice.Viewport = m_backupRenderSetup.Viewport.Value;
                    UpdateScreenSize();
                    MyRender.CreateRenderTargets();
                    MyEnvironmentMap.Reset();
                    RestoreDefaultTargets();
                    m_sizeMultiplierForStrings = Vector2.One;
                } 
            }

            GetRenderProfiler().StartProfilingBlock("ClearDrawMessages");
            ClearDrawMessages();
            GetRenderProfiler().EndProfilingBlock();

            if (m_spriteBatch != null)
                Debug.Assert(m_spriteBatch.ScissorStack.Empty);
        }

        private static void RenderColoredTextures()
        {
            const int RENDER_TEXTURE_RESOLUTION = 512;

            BlendState.NonPremultiplied.Apply();
            DepthStencilState.None.Apply();
            RasterizerState.CullNone.Apply();

            MyEffectScreenshot ssEffect = GetEffect(MyEffects.Screenshot) as MyEffectScreenshot;
            ssEffect.SetTechnique(Effects.MyEffectScreenshot.ScreenshotTechniqueEnum.ColorizeTexture);
            ssEffect.SetScale(Vector2.One);

            Dictionary<int, Texture> createdRenderTextureTargets = new Dictionary<int, Texture>();

            foreach (var texture in m_texturesToRender)
            {
                MyTexture2D tex = MyRender.GetTextureForName(texture.TextureName);
                if (null == tex)
                {
                    tex = MyTextureManager.GetTexture<MyTexture2D>(texture.TextureName, "", null, LoadingMode.Immediate);
                }
             
                int textureHeight = RENDER_TEXTURE_RESOLUTION;
                int textureWidth = RENDER_TEXTURE_RESOLUTION;
                if (null != tex)
                {
                    textureWidth *= (int)(tex.Width / (float)tex.Height);
                    ssEffect.SetColorMaskHSV(texture.ColorMaskHSV);
                    ssEffect.SetSourceTexture(tex);
                }
                Texture renderTexture = null;
                if (false == createdRenderTextureTargets.TryGetValue(textureWidth, out renderTexture))
                {
                    renderTexture = new Texture(MyRender.GraphicsDevice, textureWidth, textureHeight, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                    createdRenderTextureTargets[textureWidth] = renderTexture;
                }

                MyRender.SetRenderTarget(renderTexture, null);
                MyRender.GetFullscreenQuad().Draw(ssEffect);            
                MyScreenshot.SaveScreenshot(renderTexture, texture.PathToSave);
            }            
            MyRender.SetRenderTarget(null, null);
            foreach (var texture in createdRenderTextureTargets)
            {
                texture.Value.Dispose();
            }
            createdRenderTextureTargets.Clear();
            m_texturesToRender.Clear();
        }


        private static Texture RenderTextToTexture(MyRenderTextureId objectId, string text, float scale, Color fontColor, Color backgroundColor, int resolution, int aspectRatio)
        {
            if (m_screenshot != null)
            {
                return null;
            }
            Texture renderTexture = MyRenderTexturePool.GetRenderTexture(objectId, resolution, aspectRatio);
            if (renderTexture != null)
            {
                MyRender.SetRenderTarget(renderTexture, null);

                var surfaceDesc = renderTexture.GetLevelDescription(0);
                MyRender.GraphicsDevice.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(backgroundColor.R, backgroundColor.G, backgroundColor.B, 0), 1, 0);
                MyRender.SetDeviceViewport(new SharpDX.Viewport(0, 0, surfaceDesc.Width, surfaceDesc.Height));

                MyDebugDraw.DrawText(Vector2.Zero, new StringBuilder(text), fontColor, scale * MyRenderTexturePool.RenderQualityScale(), false, blendState:BlendState.EmissiveTexture);

                MyRender.SetRenderTarget(null, null);
            }
            return renderTexture;
        }

        public static Texture GetScreenshotTexture()
        {
            return m_renderSetup.RenderTargets[0];
        }

        static void ClearElementPool()
        {
            for (int i = 0; i < m_renderElementsPool.Length; i++)
            {
                m_renderElementsPool[i].Clear();
            }
            m_renderElementIndex = 0;
        }

        internal static void Draw3D(bool applyBackupStack = true)
        {
            if (!Enabled)
            {
                return;
            }

            ApplySetupStack(m_backupSetup);

            GetRenderProfiler().StartProfilingBlock("Draw total");

            if (m_currentSetup.CallerID.Value == MyRenderCallerEnum.Main)
                m_renderCounter++;

            m_renderElementCounter = 0;

            GetShadowRenderer().UpdateFrustumCorners();
            if (EnableLights && Settings.EnableLightsRuntime && MyRender.CurrentRenderSetup.EnableLights.Value && Settings.EnableSun && Settings.EnableShadows && CurrentRenderSetup.EnableSun.Value && !Settings.ShowBlendedScreens)
                GetShadowRenderer().PrepareFrame();

            //Updates dependent on draw
            GetRenderProfiler().StartProfilingBlock("PrepareForDraw");
            DrawRenderModules(MyRenderStage.PrepareForDraw);
            GetRenderProfiler().EndProfilingBlock();

            //Scene rendering
            DrawScene();

            if (MyRender.CurrentRenderSetup.EnableDebugHelpers.Value)
            {
                //Debug draw
                DrawDebug();

                if (IsRenderOverloaded)
                    MyDebugDraw.DrawText(Vector2.Zero, new StringBuilder("WARNING: Render is overloaded"), Color.Red, 0.7f, false);

                GetRenderProfiler().StartProfilingBlock("DebugDraw");
                DrawRenderModules(MyRenderStage.DebugDraw);
                GetRenderProfiler().EndProfilingBlock();
            }

            if (applyBackupStack)
            {
                ApplySetup(m_backupSetup);
            }

            if (m_currentSetup.CallerID.Value == MyRenderCallerEnum.Main)
            { 
                GraphicsDevice.Clear(ClearFlags.Stencil, new ColorBGRA(0), 1, 0);
            }

            //Profiling data
            GetRenderProfiler().EndProfilingBlock();
         }

        /// <summary>
        /// Renders the source from parameter to the current device's render target.
        /// It just copies one RT into another (but by a rendering pass - so it's probably redundant).
        /// </summary>
        /// <param name="source"></param>
        internal static void Blit(Texture source, bool scaleToTarget, MyEffectScreenshot.ScreenshotTechniqueEnum technique = MyEffectScreenshot.ScreenshotTechniqueEnum.Default)
        {
            var screenEffect = m_effects[(int)MyEffects.Screenshot] as MyEffectScreenshot;
            screenEffect.SetSourceTexture(source);

            //For case that source is bigger then camera viewport (back camera etc.)
            Vector2 scale = Vector2.One;

            if (scaleToTarget)
            {
                scale = GetScaleForViewport(source);
            }


            screenEffect.SetScale(scale);
            screenEffect.SetTechnique(technique);


            BlendState bs = BlendState.Current;
            if (technique == MyEffectScreenshot.ScreenshotTechniqueEnum.DepthToAlpha)
            {
                MyStateObjects.AlphaChannels_BlendState.Apply();
            }

            GetFullscreenQuad().Draw(screenEffect);

            if (technique == MyEffectScreenshot.ScreenshotTechniqueEnum.DepthToAlpha)
            {
                bs.Apply();
            }
        }

        internal static Vector2 GetScaleForViewport(Texture source)
        {
            return m_scaleToViewport;
        }

        internal static void RenderPostProcesses(PostProcessStage postProcessStage, Texture source, Texture[] target, Texture availableRT, bool copyToTarget = true, bool scaleToTarget = false)
        {
            Texture lastSurface = source;

            GetRenderProfiler().StartProfilingBlock("Render Post process: " + postProcessStage.ToString());

            {
                (MyRender.GetEffect(MyEffects.BlendLights) as MyEffectBlendLights).DefaultTechnique = MyEffectBlendLights.Technique.LightsEnabled;
                (MyRender.GetEffect(MyEffects.BlendLights) as MyEffectBlendLights).CopyEmissivityTechnique = MyEffectBlendLights.Technique.CopyEmissivity;

                MyEffectDirectionalLight directionalLight = MyRender.GetEffect(MyEffects.DirectionalLight) as MyEffectDirectionalLight;
                directionalLight.DefaultTechnique = MyEffectDirectionalLight.Technique.Default;
                directionalLight.DefaultWithoutShadowsTechnique = MyEffectDirectionalLight.Technique.WithoutShadows;
                directionalLight.DefaultNoLightingTechnique = MyEffectDirectionalLight.Technique.NoLighting;

                MyEffectPointLight pointLight = MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight;
                pointLight.PointTechnique = MyEffectPointLight.MyEffectPointLightTechnique.Point;
                pointLight.PointWithShadowsTechnique = MyEffectPointLight.MyEffectPointLightTechnique.PointShadows;
                pointLight.HemisphereTechnique = MyEffectPointLight.MyEffectPointLightTechnique.Point;
                pointLight.SpotTechnique = MyEffectPointLight.MyEffectPointLightTechnique.Spot;
                pointLight.SpotShadowTechnique = MyEffectPointLight.MyEffectPointLightTechnique.SpotShadows;
            }


            foreach (MyPostProcessBase postProcess in m_postProcesses)
            {
                if (postProcess.Enabled && (MyRender.CurrentRenderSetup.EnabledPostprocesses == null || MyRender.CurrentRenderSetup.EnabledPostprocesses.Contains(postProcess.Name)))
                {
                    var currSurface = postProcess.Render(postProcessStage, lastSurface, availableRT);

                    // Effect used availableRT as target, so lastSurface is available now
                    if (currSurface != lastSurface && lastSurface != null)
                    {
                        availableRT = lastSurface;
                    }
                    lastSurface = currSurface;
                }
            }

            GetRenderProfiler().EndProfilingBlock();

            if (lastSurface != null && copyToTarget)
            {
                MyRender.SetRenderTargets(target, null);

                if (scaleToTarget)
                    SetCorrectViewportSize();

                BlendState.Opaque.Apply();

                Blit(lastSurface, scaleToTarget);
            }
        }

        internal static void SetCorrectViewportSize()
        {
            if (((Texture)MyRender.GetRenderTarget(MyRenderTargets.Depth)).GetLevelDescription(0).Width != MyRenderCamera.Viewport.Width)
            {   //missile camera, remote camera, back camera etc
                GraphicsDevice.Viewport = MyRenderCamera.Viewport;
            }
        }


        /// <summary>
        /// Draw background of the scene
        /// </summary>
        internal static void DrawBackground(Texture[] targets)
        {
            GetRenderProfiler().StartProfilingBlock("Draw background");

            if (targets != null)
            {
                var rt = targets[0];
                var targetWidth = rt.GetLevelDescription(0).Width;
                var targetHeight = rt.GetLevelDescription(0).Height;
                m_scaleToViewport = new Vector2(((float)MyRenderCamera.Viewport.Width / targetWidth), ((float)MyRenderCamera.Viewport.Height / targetHeight));
            }
            else
            {
                m_scaleToViewport = Vector2.One;
            }

            //Render background
            MyRender.SetRenderTargets(targets, null);
            SetDeviceViewport(MyRenderCamera.Viewport);

            RasterizerState.CullNone.Apply();
            DepthStencilState.None.Apply();
            BlendState.Opaque.Apply();

            if (Settings.ShowGreenBackground)
            {
                GraphicsDevice.Clear(ClearFlags.Target, new ColorBGRA(0, 1, 0, 1), 1, 0);
            }
            else
            {
                GetRenderProfiler().StartProfilingBlock("Background");
                DrawRenderModules(MyRenderStage.Background);
                GetRenderProfiler().EndProfilingBlock();
            }

            GetRenderProfiler().EndProfilingBlock();
        }

        /// <summary>
        /// Draw one LOD level of the scene
        /// </summary>
        /// <param name="currentLodDrawPass"></param>
        /// <param name="drawCockpitInterior"></param>
        internal static void DrawScene_OneLodLevel(MyLodTypeEnum currentLodDrawPass)
        {
            GetRenderProfiler().StartProfilingBlock(MyEnum<MyLodTypeEnum>.GetName(currentLodDrawPass));

            m_currentLodDrawPass = currentLodDrawPass;

            switch (currentLodDrawPass)
            {
                case MyLodTypeEnum.LOD0:
                    MyRender.SetRenderTargets(m_GBufferDefaultBinding, null);
                    break;
            }

            SetDeviceViewport(MyRenderCamera.Viewport);                        
            BlendState.Opaque.Apply();

            if (currentLodDrawPass == MyLodTypeEnum.LOD0)
            {
                //  We don't need depth buffer for clearing Gbuffer
                DepthStencilState.None.Apply();
                RasterizerState.CullCounterClockwise.Apply();

                GraphicsDevice.Clear(ClearFlags.All, new ColorBGRA(1.0f, 0, 0, 1), 1, 0);
                //  Clear Gbuffer
                GetFullscreenQuad().Draw(MyRender.GetEffect(MyEffects.ClearGBuffer));
            }

            //  This compare function "less" is better when drawing normal and LOD1 cells, then z-fighting isn't so visible.
            DepthStencilState.Default.Apply();

            if (!Settings.Wireframe)
                RasterizerState.CullCounterClockwise.Apply();
            else
                MyStateObjects.WireframeClockwiseRasterizerState.Apply();

            GetRenderProfiler().StartProfilingBlock("MyRenderStage.LODDrawStart");
            // Render registered modules for this stage
            GetRenderProfiler().EndProfilingBlock();

            bool drawNear = !Settings.SkipLOD_NEAR && m_currentSetup.EnableNear.HasValue && m_currentSetup.EnableNear.Value;

            if (currentLodDrawPass == MyLodTypeEnum.LOD0 || currentLodDrawPass == MyLodTypeEnum.LOD_BACKGROUND) // LOD0
            {
                GetRenderProfiler().StartProfilingBlock("DrawNearObjects");
                if (drawNear && currentLodDrawPass == MyLodTypeEnum.LOD0)
                {
                    DrawScene_OneLodLevel_DrawNearObjects(Settings.ShowStencilOptimization, true);
                }
                GetRenderProfiler().EndProfilingBlock();

                GetRenderProfiler().StartProfilingBlock("Draw(false);");
                m_currentLodDrawPass = MyLodTypeEnum.LOD0;
                if (!Settings.SkipLOD_0)
                {
                    DrawScene_OneLodLevel_Draw(false, true, currentLodDrawPass);
                }

                GetRenderProfiler().EndProfilingBlock();
            }

            RasterizerState.CullCounterClockwise.Apply();

            // Render registered modules for this stage
            GetRenderProfiler().StartProfilingBlock("MyRenderStage.LODDrawEnd");
            DrawRenderModules(MyRenderStage.LODDrawEnd);
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().EndProfilingBlock();
        }

        internal static void DrawScene_BackgroundObjects(MyLodTypeEnum currentLodDrawPass)
        {
            GetRenderProfiler().StartProfilingBlock(MyEnum<MyLodTypeEnum>.GetName(currentLodDrawPass));
            m_currentLodDrawPass = currentLodDrawPass;
            SetDeviceViewport(MyRenderCamera.Viewport);
            BlendState.Opaque.Apply();

            //  This compare function "less" is better when drawing normal and LOD1 cells, then z-fighting isn't so visible.
            DepthStencilState.BackgroundObjects.Apply();

            if (!Settings.Wireframe)
                RasterizerState.CullCounterClockwise.Apply();
            else
                MyStateObjects.WireframeClockwiseRasterizerState.Apply();

            GetRenderProfiler().StartProfilingBlock("Draw(false);");
            m_currentLodDrawPass = MyLodTypeEnum.LOD_BACKGROUND;     
            DrawScene_OneLodLevel_Draw(false, true, currentLodDrawPass);
            GetRenderProfiler().EndProfilingBlock();

            RasterizerState.CullCounterClockwise.Apply();

            GetRenderProfiler().EndProfilingBlock();
        }

        internal static void DrawScene_OneLodLevel_Forward(MyLodTypeEnum currentLodDrawPass)
        {
            GetRenderProfiler().StartProfilingBlock(MyEnum<MyLodTypeEnum>.GetName(currentLodDrawPass));

            m_currentLodDrawPass = currentLodDrawPass;

            SetDeviceViewport(MyRenderCamera.Viewport);

            BlendState.Opaque.Apply();
            DepthStencilState.Default.Apply();

            if (!Settings.Wireframe)
                RasterizerState.CullCounterClockwise.Apply();
            else
                MyStateObjects.WireframeClockwiseRasterizerState.Apply();

            // Render registered modules for this stage
            GetRenderProfiler().StartProfilingBlock("MyRenderStage.LODDrawStart");
            DrawRenderModules(MyRenderStage.LODDrawStart);
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("DrawNearObjects");
            if (!Settings.SkipLOD_NEAR && m_currentSetup.EnableNear.HasValue && m_currentSetup.EnableNear.Value)
            {
                DrawScene_OneLodLevel_DrawNearObjects(Settings.ShowStencilOptimization, true);
            }
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("Draw(false);");
            m_currentLodDrawPass = currentLodDrawPass;
            if (!Settings.SkipLOD_0)
            {
                DrawScene_OneLodLevel_Draw(false, true, currentLodDrawPass);
            }
            GetRenderProfiler().EndProfilingBlock();

            RasterizerState.CullCounterClockwise.Apply();

            // Render registered modules for this stage
            GetRenderProfiler().StartProfilingBlock("MyRenderStage.LODDrawEnd");
            DrawRenderModules(MyRenderStage.LODDrawEnd);
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().EndProfilingBlock();
        }

        private static void DrawScene_OneLodLevel_DrawNearObjects(bool drawStencilTechnique, bool collectTransparentElements)
        {
            if (Settings.EnableStencilOptimization)
            {
                MyStateObjects.DepthStencil_WriteNearObject.Apply();
            }
            else if (Settings.ShowStencilOptimization)
            {
                DepthStencilState.None.Apply();
            }

            m_currentLodDrawPass = MyLodTypeEnum.LOD_NEAR;

            MyRenderCamera.SetNearObjectsClipPlanes(true);

            DrawScene_OneLodLevel_Draw(drawStencilTechnique, collectTransparentElements,MyLodTypeEnum.LOD_NEAR);

            MyRenderCamera.ResetClipPlanes(true);

            // Need to clear only depth
            GraphicsDevice.Clear(ClearFlags.ZBuffer, new ColorBGRA(0), 1, 0);
            MyStateObjects.DepthStencil_WriteFarObject.Apply();
        }

        private static void DrawScene_OneLodLevel_Draw(bool drawStencilTechnique, bool collectTransparentElements,MyLodTypeEnum LOD)
        {
            if (CurrentRenderSetup.RenderElementsToDraw != null)
            {
                //Draw render elements
                GetRenderProfiler().StartProfilingBlock("CurrentRenderSetup.RenderElementsToDraw");
                int ibChangesStats;

                m_sortedElements.Add(m_currentLodDrawPass, CurrentRenderSetup.RenderElementsToDraw);
                DrawRenderElementsAlternative(m_sortedElements, LOD, out ibChangesStats);
                GetRenderProfiler().EndProfilingBlock();
            }
            else
            {
                // Render are models listed for draw
                GetRenderProfiler().StartProfilingBlock("DrawModels()");
                DrawModelsLod(LOD, drawStencilTechnique, collectTransparentElements);
                GetRenderProfiler().EndProfilingBlock();
            }
            MyStateObjects.DepthStencil_TestFarObject.Apply();
        }

        internal static void DrawScene_Transparent()
        {
            int ibChangesStats;

            //Draw sorted render elements
            m_sortedTransparentElements.Add(m_currentLodDrawPass, CurrentRenderSetup.TransparentRenderElementsToDraw ?? m_transparentRenderElements);
            DrawRenderElementsAlternative(m_sortedTransparentElements, m_currentLodDrawPass, out ibChangesStats);
        }

        internal static void DrawAtmosphere(bool backgroundObjects)
        {
            m_sortedElements.Clear();
            m_renderObjectListForDraw.Clear();
            if (backgroundObjects)
            {
                DepthStencilState.BackgroundAtmosphereObjects.Apply();
            }
            else
            {
               MyStateObjects.DepthStencil_WriteNearAtmosphere.Apply();
            }

            GetAtmosphereRenderObjects(backgroundObjects, m_renderAtmospheresForNearPlanetSurface);

            foreach (MyRenderObject renderObject in m_renderAtmospheresForNearPlanetSurface)
            {
                MyRenderAtmosphere atmosphereObject = (renderObject as MyRenderAtmosphere);
                if (atmosphereObject.IsSurface == false)
                {
                    m_renderObjectListForDraw.Add(renderObject);
                    renderObject.BeforeDraw();
                }
            }

            DrawModelsLod(backgroundObjects? MyLodTypeEnum.LOD_BACKGROUND:MyLodTypeEnum.LOD0, false, false);

            BlendState.Opaque.Apply();
        }

        internal static void DrawNearPlanetSurfaceFromSpace()
        {
            m_renderObjectListForDraw.Clear();
            m_sortedElements.Clear();
            m_renderAtmospheresForNearPlanetSurface.Clear();

            MyStateObjects.DepthStencil_RenderNearPlanetSurfaceInAtmosphere.Apply();

            GetAtmosphereRenderObjects(false, m_renderAtmospheresForNearPlanetSurface);

            foreach (MyRenderObject renderObject in m_renderAtmospheresForNearPlanetSurface)
            {
                MyRenderAtmosphere atmosphereObject = (renderObject as MyRenderAtmosphere);
                if (false == atmosphereObject.IsInside(MyRenderCamera.Position) && atmosphereObject.IsSurface)
                {
                    m_renderObjectListForDraw.Add(renderObject);
                    renderObject.BeforeDraw();
                }
            }
            if (m_renderObjectListForDraw.Count > 0)
            {
                DrawModelsLod(MyLodTypeEnum.LOD0, false, false);
            }

        }

        internal static void DrawNearPlanetSurfaceFromAtmosphere()
        {
            m_renderObjectListForDraw.Clear();
            m_sortedElements.Clear();
            m_renderAtmospheresForNearPlanetSurface.Clear();

            DepthStencilState.BackgroundAtmospherePlanetSurfaceState.Apply();

            Matrix optProjection = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio, MyRenderCamera.NEAR_PLANE_DISTANCE, MyRenderCamera.FAR_PLANE_FOR_BACKGROUND);
            m_cameraFrustum.Matrix = MyRenderCamera.ViewMatrix * optProjection;
            m_cameraPosition = MyRenderCamera.Position;

            m_atmospherePurunnigStructure.OverlapAllFrustum(ref m_cameraFrustum, m_renderAtmospheresForNearPlanetSurface);

            foreach (MyRenderObject renderObject in m_renderAtmospheresForNearPlanetSurface)
            {
                MyRenderAtmosphere atmosphereObject = (renderObject as MyRenderAtmosphere);
                if (atmosphereObject.IsInside(MyRenderCamera.Position) && atmosphereObject.IsSurface)
                {
                    m_renderObjectListForDraw.Add(renderObject);
                    renderObject.BeforeDraw();
                }
            }

            if (m_renderObjectListForDraw.Count > 0)
            {
                DrawModelsLod( MyLodTypeEnum.LOD0, false, false);
            }
        }

        private static void SetupAtmosphereShader()
        {
            MyEffectAtmosphere effectPointLight = (MyEffectAtmosphere)MyRender.GetEffect(MyEffects.Atmosphere);
            var invViewMatrix = Matrix.Invert(MyRenderCamera.ViewMatrixAtZero);
            effectPointLight.SetInvViewMatrix(invViewMatrix);
            SharpDX.Direct3D9.Texture diffuseRT = MyRender.GetRenderTarget(MyRenderTargets.Diffuse);
            effectPointLight.SetDepthsRT(MyRender.GetRenderTarget(MyRenderTargets.Depth));
            effectPointLight.SetSourceRT(MyRender.GetRenderTarget(MyRenderTargets.Auxiliary1));

            effectPointLight.SetHalfPixel(diffuseRT.GetLevelDescription(0).Width, diffuseRT.GetLevelDescription(0).Height);
            effectPointLight.SetScale(GetScaleForViewport(diffuseRT));
        }

        private static void GetAtmosphereRenderObjects(bool backgroundObjects,List<MyElement> renderObjects)
        {
            Matrix optProjection = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio, backgroundObjects ? MyRenderCamera.FAR_PLANE_DISTANCE : MyRenderCamera.NEAR_PLANE_DISTANCE, backgroundObjects ? MyRenderCamera.FAR_PLANE_FOR_BACKGROUND : MyRenderCamera.FAR_PLANE_DISTANCE);
            m_cameraFrustum.Matrix = MyRenderCamera.ViewMatrix * optProjection;
            m_cameraPosition = MyRenderCamera.Position;

            m_atmospherePurunnigStructure.OverlapAllFrustum(ref m_cameraFrustum, renderObjects);
        }
        #endregion

        #region Modules

        /// <summary>
        /// Register renderer event handler to make specific behaviour in several render stage
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="handler"></param>
        /// <param name="renderStage"></param>
        /// <param name="priority"></param>
        internal static void RegisterRenderModule(MyRenderModuleEnum module, string displayName, DrawEventHandler handler, MyRenderStage renderStage)
        {
            RegisterRenderModule(module, displayName, handler, renderStage, MyRenderConstants.DEFAULT_RENDER_MODULE_PRIORITY, true);
        }

        internal static void RegisterRenderModule(MyRenderModuleEnum module, string displayName, DrawEventHandler handler, MyRenderStage renderStage, bool enabled)
        {
            RegisterRenderModule(module, displayName, handler, renderStage, MyRenderConstants.DEFAULT_RENDER_MODULE_PRIORITY, enabled);
        }

        /// <summary>
        /// Register renderer event handler to make specific behaviour in several render stage
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="handler"></param>
        /// <param name="renderStage"></param>
        /// <param name="priority">0 - first item, higher number means lower priority</param>
        internal static void RegisterRenderModule(MyRenderModuleEnum module, string displayName, DrawEventHandler handler, MyRenderStage renderStage, int priority, bool enabled)
        {
            Debug.Assert(!m_renderModules[(int)renderStage].Any(x => x.Name == module));

            m_renderModules[(int)renderStage].Add(new MyRenderModuleItem { Name = module, DisplayName = displayName, Priority = priority, Handler = handler, Enabled = enabled });
            m_renderModules[(int)renderStage].Sort((p1, p2) => p1.Priority.CompareTo(p2.Priority));
        }

        /// <summary>
        /// Removes render module from the list
        /// </summary>
        /// <param name="name"></param>
        internal static void UnregisterRenderModule(MyRenderModuleEnum name)
        {
            for (int i = 0; i < m_renderModules.Length; i++)
            {
                List<MyRenderModuleItem> modules = m_renderModules[i];
                foreach (MyRenderModuleItem module in modules)
                {
                    if (module.Name == name)
                    {
                        modules.Remove(module);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Returns enumerator for render modules of current render stage
        /// </summary>
        /// <param name="renderStage"></param>
        /// <returns></returns>
        internal static List<MyRenderModuleItem> GetRenderModules(MyRenderStage renderStage)
        {
            return m_renderModules[(int)renderStage];
        }


        internal static bool IsModuleEnabled(MyRenderStage stage, MyRenderModuleEnum module)
        {
            if (!(CurrentRenderSetup.EnabledRenderStages == null || CurrentRenderSetup.EnabledRenderStages.Contains(stage)))
                return false;

            List<MyRenderModuleItem> renderModules = m_renderModules[(int)stage];
            if (!(CurrentRenderSetup.EnabledModules == null || CurrentRenderSetup.EnabledModules.Contains(module)))
                return false;

            foreach (var moduleItem in renderModules)
            {
                if (moduleItem.Name == module)
                {
                    return moduleItem.Enabled;
                }
            }
            return false;
        }

        private static void DrawRenderModules(MyRenderStage renderStage)
        {
            if (CurrentRenderSetup.EnabledRenderStages == null || CurrentRenderSetup.EnabledRenderStages.Contains(renderStage))
            {
                List<MyRenderModuleItem> renderModules = m_renderModules[(int)renderStage];
                foreach (MyRenderModuleItem moduleItem in renderModules)
                {
                    if (moduleItem.Enabled && (CurrentRenderSetup.EnabledModules == null || CurrentRenderSetup.EnabledModules.Contains(moduleItem.Name)))
                    {
                        GetRenderProfiler().StartProfilingBlock(moduleItem.DisplayName);
                        moduleItem.Handler();
                        GetRenderProfiler().EndProfilingBlock();
                    }
                }
            }
        }

        static void EnableRenderModule(MyRenderModuleEnum module, bool enable)
        {
            foreach (MyRenderStage stage in Enum.GetValues(typeof(MyRenderStage)))
            {
                MyRender.MyRenderModuleItem renderModule = MyRender.GetRenderModules(stage).Find((x) => x.Name == module);
                if (renderModule != null)
                    renderModule.Enabled = enable;
            }
        }

        #endregion

        #region Prepare for draw

        static int OCCLUSION_INTERVAL = 4;

        internal static void PrepareRenderObjectsForDraw(bool backgroundObjects)
        {
            if (CurrentRenderSetup.RenderElementsToDraw != null)
                return;

            m_sortedElements.Clear();
            m_renderObjectsToDebugDraw.Clear();

            Matrix optProjection = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio, backgroundObjects ? MyRenderCamera.NEAR_PLANE_FOR_BACKGROUND : MyRenderCamera.NEAR_PLANE_DISTANCE, backgroundObjects ? MyRenderCamera.FAR_PLANE_FOR_BACKGROUND : MyRenderCamera.FAR_PLANE_DISTANCE);
            m_cameraFrustum.Matrix = MyRenderCamera.ViewMatrix * optProjection;
            m_cameraPosition = MyRenderCamera.Position;

            MyPerformanceCounter.PerCameraDrawWrite.EntitiesOccluded = 0;
            if (backgroundObjects)
            {
                m_renderLightsForDraw.Clear();
                m_manualCullObjectListForDraw.Clear();
                m_cullObjectListForDraw.Clear();
                m_renderOcclusionQueries.Clear();
                PrepareEntitiesForLargeDraw(ref m_cameraFrustum, m_cameraPosition, MyOcclusionQueryID.MAIN_RENDER, m_renderObjectListForDraw, ref MyPerformanceCounter.PerCameraDrawWrite.EntitiesOccluded);
            }
            else
            {
                PrepareEntitiesForDraw(ref m_cameraFrustum, m_cameraPosition, MyOcclusionQueryID.MAIN_RENDER, m_renderObjectListForDraw, m_renderLightsForDraw, m_cullObjectListForDraw, m_manualCullObjectListForDraw, m_renderOcclusionQueries, ref MyPerformanceCounter.PerCameraDrawWrite.EntitiesOccluded);
                //  Iterate first draw (because of linked objects)
                foreach (MyRenderObject renderObject in m_nearObjects)
                {
                    renderObject.BeforeDraw();
                }
            }
            
            foreach (MyRenderObject renderObject in m_renderObjectListForDraw)
            {
                renderObject.BeforeDraw();
            }
        }

        internal static void PrepareEntitiesForDraw(ref BoundingBoxD box, MyOcclusionQueryID queryID, List<MyElement> renderObjectListForDraw, List<MyRenderLight> renderLightsForDraw, List<MyOcclusionQueryIssue> renderOcclusionQueries, ref int occludedItemsStats)
        {
            GetRenderProfiler().StartProfilingBlock("PrepareEntitiesForDraw()");

            //Process only big cull object for queries
            renderOcclusionQueries.Clear();

            m_cullingStructure.OverlapAllBoundingBox(ref box, m_cullObjectListForDraw);

            PrepareObjectQueries(queryID, m_cullObjectListForDraw, renderOcclusionQueries, ref occludedItemsStats);

            renderObjectListForDraw.Clear();

            GetRenderProfiler().StartProfilingBlock("m_prunningStructure.OverlapAllBoundingBox");
            m_prunningStructure.OverlapAllBoundingBox(ref box, renderObjectListForDraw);

            foreach (MyCullableRenderObject cullableObject in m_cullObjectListForDraw)
            {
                cullableObject.CulledObjects.OverlapAllBoundingBox(ref box, renderObjectListForDraw, 0, false);
            }

            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().EndProfilingBlock();
        }

        internal static void PrepareEntitiesForDraw(ref BoundingFrustumD frustum, Vector3D cameraPosition, MyOcclusionQueryID queryID, List<MyElement> renderObjectListForDraw, List<MyRenderLight> renderLightsForDraw, List<MyElement> cullObjectListForDraw, List<MyElement> manualCullObjectListForDraw, List<MyOcclusionQueryIssue> renderOcclusionQueries, ref int occludedItemsStats)
        {
            GetRenderProfiler().StartProfilingBlock("PrepareEntitiesForDrawFr()");

            if (queryID != MyOcclusionQueryID.MAIN_RENDER)
            {
                m_shadowPrunningStructure.OverlapAllFrustum(ref frustum, renderObjectListForDraw);

                GetRenderProfiler().StartProfilingBlock("m_manualCullingStructure.OverlapAllFrustum");
                m_manualCullingStructure.OverlapAllFrustum(ref frustum, manualCullObjectListForDraw);
                GetRenderProfiler().EndProfilingBlock();

                GetRenderProfiler().StartProfilingBlock("Get from manual cullobjects");
                foreach (MyCullableRenderObject cullableObject in manualCullObjectListForDraw)
                {
                    cullableObject.CulledObjects.GetAll(renderObjectListForDraw, false);
                }
                GetRenderProfiler().EndProfilingBlock();

                foreach (var nearObject in m_nearObjects)
                {
                    renderObjectListForDraw.Add(nearObject);
                }

                GetRenderProfiler().EndProfilingBlock();
                return;
            }

            GetRenderProfiler().StartProfilingBlock("m_cullingStructure.OverlapAllFrustum");
            m_cullingStructure.OverlapAllFrustum(ref frustum, cullObjectListForDraw);
            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("m_manualCullingStructure.OverlapAllFrustum");
            m_manualCullingStructure.OverlapAllFrustum(ref frustum, manualCullObjectListForDraw);
            GetRenderProfiler().EndProfilingBlock();

            
            if (renderOcclusionQueries != null)
            {
                //Process only big cull object for queries
                renderOcclusionQueries.Clear();

                GetRenderProfiler().StartProfilingBlock("PrepareObjectQueries");
                PrepareObjectQueries(queryID, cullObjectListForDraw, renderOcclusionQueries, ref occludedItemsStats);
                GetRenderProfiler().EndProfilingBlock();

                GetRenderProfiler().StartProfilingBlock("PrepareObjectQueries 2");
                PrepareObjectQueries(queryID, manualCullObjectListForDraw, renderOcclusionQueries, ref occludedItemsStats);
                GetRenderProfiler().EndProfilingBlock();
            }

            renderObjectListForDraw.Clear();
            renderLightsForDraw.Clear();

            GetRenderProfiler().StartProfilingBlock("m_prunningStructure.OverlapAllFrustum");
            m_prunningStructure.OverlapAllFrustum(ref frustum, renderObjectListForDraw);

            //AssertRenderObjects(renderObjectListForDraw);
            GetRenderProfiler().EndProfilingBlock();


            GetRenderProfiler().StartProfilingBlock("Get from cullobjects - part 1");

            foreach (MyCullableRenderObject cullableObject in cullObjectListForDraw)
            {
                if (frustum.Contains(cullableObject.WorldAABB) == VRageMath.ContainmentType.Contains)
                {
                    cullableObject.CulledObjects.GetAll(renderObjectListForDraw, false);
                }
                else
                {
                    cullableObject.CulledObjects.OverlapAllFrustum(ref frustum, renderObjectListForDraw, false);
                }
            }

            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("Get from manual cullobjects");

            foreach (MyCullableRenderObject cullableObject in manualCullObjectListForDraw)
            {
                cullableObject.CulledObjects.GetAll(renderObjectListForDraw, false);
                MyRenderProxy.VisibleObjectsWrite.Add(cullableObject.ID);
            }

            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().StartProfilingBlock("Get from cullobjects - part 2");

            int c = 0;

            if (queryID == MyOcclusionQueryID.MAIN_RENDER)
            {
                //int ii = 0;
                while (c < renderObjectListForDraw.Count)
                {
                    MyRenderLight renderLight = renderObjectListForDraw[c] as MyRenderLight;
                    if (renderLight != null && ((renderLight.LightOn || renderLight.GlareOn)))
                    {
                        renderLightsForDraw.Add(renderLight);
                        renderObjectListForDraw.RemoveAtFast(c);
                        continue;
                    }

                    MyRenderObject ro = renderObjectListForDraw[c] as MyRenderObject;

                    if (ro.NearFlag)
                    {
                        renderObjectListForDraw.RemoveAtFast(c);
                        continue;
                    }


                    if (!ro.SkipIfTooSmall)
                    {
                        c++;
                        continue;
                    }

            
                    Vector3D entityPosition = ro.WorldVolume.Center;

                    Vector3D.Distance(ref cameraPosition, ref entityPosition, out ro.Distance);
                    
                    float cullRatio = MyRenderConstants.DISTANCE_CULL_RATIO;

                    if (ro.WorldVolume.Radius < ro.Distance / cullRatio)
                    {
                        renderObjectListForDraw.RemoveAtFast(c);
                        continue;
                    }
                    

                    c++;
                }


                MyLights.UpdateLightsForEffect(m_renderLightsForDraw);
            }

            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().EndProfilingBlock();
        }

        internal static void PrepareEntitiesForLargeDraw(ref BoundingFrustumD frustum, Vector3D cameraPosition, MyOcclusionQueryID queryID, List<MyElement> renderObjectListForDraw, ref int occludedItemsStats)
        {
            GetRenderProfiler().StartProfilingBlock("PrepareEntitiesForDrawFr()");

            if (queryID != MyOcclusionQueryID.MAIN_RENDER)
            {             
                return;
            }

            renderObjectListForDraw.Clear();

            GetRenderProfiler().StartProfilingBlock("m_prunningStructure.OverlapAllFrustum");
            m_farObjectsPrunningStructure.OverlapAllFrustum(ref frustum, renderObjectListForDraw);

            //AssertRenderObjects(renderObjectListForDraw);
            GetRenderProfiler().EndProfilingBlock();
          
            GetRenderProfiler().StartProfilingBlock("Get from cullobjects - part 2");

            int c = 0;

            if (queryID == MyOcclusionQueryID.MAIN_RENDER)
            {
                //int ii = 0;
                while (c < renderObjectListForDraw.Count)
                {
                    MyRenderLight renderLight = renderObjectListForDraw[c] as MyRenderLight;
                    if (renderLight != null && ((renderLight.LightOn || renderLight.GlareOn)))
                    {
                        continue;
                    }

                    MyRenderObject ro = renderObjectListForDraw[c] as MyRenderObject;

                    if (ro.NearFlag)
                    {
                        renderObjectListForDraw.RemoveAtFast(c);
                        continue;
                    }


                    if (!ro.SkipIfTooSmall)
                    {
                        c++;
                        continue;
                    }

                    Vector3D entityPosition = ro.WorldVolume.Center;

                    Vector3D.Distance(ref cameraPosition, ref entityPosition, out ro.Distance);

                    float cullRatio = MyRenderConstants.DISTANCE_CULL_RATIO;

                    if (ro.WorldVolume.Radius < ro.Distance / cullRatio)
                    {
                        renderObjectListForDraw.RemoveAtFast(c);
                        continue;
                    }
                    c++;
                }
            }

            GetRenderProfiler().EndProfilingBlock();

            GetRenderProfiler().EndProfilingBlock();
        }

        [Conditional("DEBUG")]
        static void AssertRenderObjects(List<MyElement> elements)
        {      /*  //todo
            foreach (var ro in elements)
            {
                Debug.Assert(!(ro as MyRenderObject).Entity.NearFlag);
            }    */
        }

        static void PrepareObjectQueries(MyOcclusionQueryID queryID, List<MyElement> cullObjectListForDraw, List<MyOcclusionQueryIssue> renderOcclusionQueries, ref int occludedItemsStats)
        {
            if (queryID != MyOcclusionQueryID.MAIN_RENDER)
                return;

            if (!Settings.EnableHWOcclusionQueries)
                return;

            if (!m_currentSetup.EnableOcclusionQueries)
                return;

            int c = 0;
            while (c < cullObjectListForDraw.Count)
            {
                MyCullableRenderObject cullableRenderObject = (MyCullableRenderObject)cullObjectListForDraw[c];

                bool isVisibleFromQuery = false;
                MyOcclusionQueryIssue query = cullableRenderObject.GetQuery(queryID);
                if (query.OcclusionQueryIssued)
                {
                    isVisibleFromQuery = query.OcclusionQueryVisible;

                    bool isComplete = query.OcclusionQuery.IsComplete;

                    if (isComplete)
                    {
                        query.OcclusionQueryIssued = false;

                        isVisibleFromQuery = query.OcclusionQuery.PixelCount > 0;

                        //Holy ATI shit
                        if (query.OcclusionQuery.PixelCount < 0)
                        {
                            isVisibleFromQuery = true;
                        }

                        query.OcclusionQueryVisible = isVisibleFromQuery;


                        //if (m_renderCounter % OCCLUSION_INTERVAL == cullableRenderObject.RenderCounter)
                        if (!query.OcclusionQueryVisible)
                        {
                            renderOcclusionQueries.Add(query);
                        }
                    }

                    if (!isVisibleFromQuery)
                    {
                        occludedItemsStats += cullableRenderObject.EntitiesContained;
                        cullObjectListForDraw.RemoveAtFast(c);
                        continue;
                    }
                }
                else
                {
                    if (query.OcclusionQueryVisible && (m_renderCounter % OCCLUSION_INTERVAL == cullableRenderObject.RenderCounter || ShowHWOcclusionQueries))
                    {
                        renderOcclusionQueries.Add(cullableRenderObject.GetQuery(queryID));
                    }

                    if (!query.OcclusionQueryVisible)
                    {
                        renderOcclusionQueries.Add(cullableRenderObject.GetQuery(queryID));

                        occludedItemsStats += cullableRenderObject.EntitiesContained;
                        cullObjectListForDraw.RemoveAtFast(c);
                        continue;
                    }
                }

                c++;
            }
        }

        #endregion

        #endregion
    }
}
