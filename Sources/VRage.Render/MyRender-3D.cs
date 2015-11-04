#region Using

using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D9;
using VRage.Generics;
using VRageMath;

#endregion

namespace VRageRender
{
    using VRage;
    using VRage.Import;
    using VRageRender.Lights;
    using VRageRender.Profiler;
    using VRageRender.Shadows;
    using VRageRender.Textures;
    using VRageRender.Utils;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using BoundingSphere = VRageMath.BoundingSphere;
    using Color = VRageMath.Color;
    using Matrix = VRageMath.Matrix;
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using VRage.Utils;


    #region Enums
    public enum MyRenderTargets
    {
        Normals,
        Diffuse,
        Depth,
        DepthHalf,

        SSAO,
        SSAOBlur,

        Auxiliary0,
        Auxiliary1,
        Auxiliary2,
        AuxiliaryHalf0,
        AuxiliaryQuarter0,
        AuxiliaryHalf1010102,

        HDR4,
        HDRAux,
        HDR4Threshold,

        ShadowMap,
        ShadowMapZBuffer,
        SecondaryShadowMap,
        SecondaryShadowMapZBuffer,

        // Environment map 2D texture - for rendering one face of cube texture
        EnvironmentMap,

        // Environment map cube texture - rendered cube texture (small size 6x128x128 or similar)
        EnvironmentCube,
        EnvironmentCubeAux,

        // Ambient map cube texture - precalculated ambient from cube (small size 6x128x128 or similar)
        AmbientCube,
        AmbientCubeAux,

        // Environment map aux texture (size of one face - to apply effects)
        EnvironmentFaceAux,
        EnvironmentFaceAux2,

        SecondaryCamera,
        SecondaryCameraZBuffer,
    }

    
    public enum MyRenderStage
    {
        PrepareForDraw,
        Background,
        LODDrawStart,
        LODDrawEnd,
        AllGeometryRendered,
        AlphaBlendPreHDR,
        AlphaBlend,
        DebugDraw
    }

    /// <summary>
    /// This enum should contain an identificator for anything that uses the MyRender pipeline.
    /// </summary>
    public enum MyRenderCallerEnum
    {
        Main,
        EnvironmentMap,
        SecondaryCamera,
        GUIPreview,
    }
    public enum MyPostProcessEnum
    {
        VolumetricSSAO2,
        HDR,
        Contrast,
        SSAO,
        VolumetricFog,
        FXAA,
        GodRays,
        Vignetting,
        ColorMapping,
        ChromaticAberration,
    }
    #endregion

    internal static partial class MyRender
    {


        #region Delegates

        internal delegate void DrawEventHandler();

        #endregion

        #region Nested classes


        internal class MyRenderSetup
        {
            /// <summary>
            /// Holds information about who is calling the MyRender.Draw() method.
            /// This information is mandatory.
            /// </summary>
            public MyRenderCallerEnum? CallerID;

            public Texture[] RenderTargets;
            public Texture DepthTarget;

            public Vector3D? CameraPosition;

            MatrixD? m_viewMatrix;

            public MatrixD? ViewMatrix
            {
                get { return m_viewMatrix; }

                set
                {
                    m_viewMatrix = value;
                    if (m_viewMatrix != null)
                    {
                        MyUtils.AssertIsValid(m_viewMatrix.Value);
                    }
                }
            }
            public Matrix? ProjectionMatrix;
            public float? AspectRatio;
            public float? Fov;
            public Viewport? Viewport;

            public float? LodTransitionNear; // Used
            public float? LodTransitionFar; // Used
            public float? LodTransitionBackgroundStart; // Used
            public float? LodTransitionBackgroundEnd; // Used

            public bool? EnableHDR; // Used
            public bool? EnableLights; // Used
            public bool? EnableSun; // Used
            public MyShadowRenderer ShadowRenderer; // Used - null for no shadows
            public bool? EnableShadowInterleaving;
            public bool? EnableSmallLights; // Used
            public bool? EnableSmallLightShadows; // Used
            public bool? EnableDebugHelpers; // Used
            public bool? EnableEnvironmentMapping;
            public bool? EnableNear;
            public bool EnableOcclusionQueries; //Used
            public float FogMultiplierMult; //Used
            public bool DepthToAlpha; //Will copy depth to diffuse alpha
            public bool DepthCopy;//Will copy depth to diffuse 

            // If background color is set, background cube is replaced by color
            public Color? BackgroundColor;

            public HashSet<MyRenderModuleEnum> EnabledModules; // Used
            public HashSet<MyPostProcessEnum> EnabledPostprocesses; // Used
            public HashSet<MyRenderStage> EnabledRenderStages; // Used

          //  public List<MyLight> LightsToUse; // if null, MyLights.GetLights() will be used
            public List<MyRenderElement> RenderElementsToDraw; // if null, MyEntities.Draw() will be used
            public List<MyRenderElement> TransparentRenderElementsToDraw; // if null, MyEntities.Draw() will be used

            public void Clear()
            {
                CallerID = null;

                RenderTargets = null;
                CameraPosition = null;
                ViewMatrix = null;
                ProjectionMatrix = null;
                AspectRatio = null;
                Fov = null;
                Viewport = null;

                LodTransitionNear = null;
                LodTransitionFar = null;
                LodTransitionBackgroundStart = null;
                LodTransitionBackgroundEnd = null;

                EnableHDR = null;
                EnableLights = null;
                EnableSun = null;
                ShadowRenderer = null;
                EnableShadowInterleaving = null;
                EnableSmallLights = null;
                EnableSmallLightShadows = null;
                EnableDebugHelpers = null;
                EnableEnvironmentMapping = null;
                EnableNear = null;

                BackgroundColor = null;

                EnableOcclusionQueries = true;
                FogMultiplierMult = 1.0f;
                DepthToAlpha = false;
                DepthCopy = false;

                EnabledModules = null;
                EnabledPostprocesses = null;
                EnabledRenderStages = null;
            }
        }

        internal class MyRenderModuleItem
        {
            public string DisplayName;
            public MyRenderModuleEnum Name;
            public int Priority; // Lower is higher priority
            public DrawEventHandler Handler;
            public bool Enabled;


            public override string ToString()
            {
                return DisplayName;
            }
        }

        internal class MyRenderElement
        {
            public MyMeshDrawTechnique DrawTechnique;

            public MyRenderMeshMaterial Material;

            //Debug
            //public string DebugName;

            public MyRenderObject RenderObject;

            //Element members
            public VertexBuffer VertexBuffer;
            public VertexBuffer InstanceBuffer;
            public int InstanceStart;
            public int InstanceCount;
            public IndexBuffer IndexBuffer;
            public int IndexStart;
            public int TriCount;
            public int VertexCount;
            public VertexDeclaration VertexDeclaration;
            public int VertexStride;
            public int[] BonesUsed;

            public int InstanceStride;

            public MatrixD WorldMatrix;
            public MatrixD WorldMatrixForDraw;

            public Vector3 Color;
            public float Dithering;

            public Vector3 ColorMaskHSV; // Hue is absolute, Saturation and value is relative to texture

            public MyRenderVoxelBatch VoxelBatch;

            public override string ToString()
            {
                return "DrawTechnique: " + DrawTechnique.ToString() + " IB:" + IndexBuffer.GetHashCode().ToString();
            }

            internal void Clear()
            {
                RenderObject = null;
                Material = null;
                IndexBuffer = null;
                VertexBuffer = null;
                InstanceBuffer = null;
            }
        }

        internal class MyLightRenderElement
        {
            public class MySpotComparer : IComparer<MyLightRenderElement>
            {
                public int Compare(MyLightRenderElement x, MyLightRenderElement y)
                {
                    var result = x.RenderShadows.CompareTo(y.RenderShadows);
                    if (result == 0)
                    {
                        //var xHash = x.Light.ReflectorTexture != null ? x.Light.ReflectorTexture.GetHashCode() : 0;
                        //var yHash = y.Light.ReflectorTexture != null ? y.Light.ReflectorTexture.GetHashCode() : 0;
                        var xHash = x.Light.QueryPixels;
                        var yHash = y.Light.QueryPixels;
                        result = yHash.CompareTo(xHash);
                    }
                    return result;
                }
            }

            public static MySpotComparer SpotComparer = new MySpotComparer();

            public MyRenderLight Light;
            public MatrixD World;
            public MatrixD ViewAtZero;
            public Matrix Projection;
            public MatrixD ShadowLightViewProjection;
            public Matrix ShadowLightViewProjectionAtZero;
            public Texture ShadowMap;
            public bool RenderShadows;
        }

        //  Used to sort render elements by their properties to spare switching render states
        class MyRenderElementsComparer : IComparer<MyRenderElement>
        {
            public int Compare(MyRenderElement x, MyRenderElement y)
            {
                MyMeshDrawTechnique xDrawTechnique = x.DrawTechnique;
                MyMeshDrawTechnique yDrawTechnique = y.DrawTechnique;

                if (xDrawTechnique == yDrawTechnique)
                {
                    if (x.VoxelBatch != null && y.VoxelBatch != null)
                    {
                        return ((short)x.VoxelBatch.SortOrder).CompareTo((short)y.VoxelBatch.SortOrder);
                    }

                    int xMat = x.Material.GetHashCode();
                    int yMat = y.Material.GetHashCode();

                    if (xMat == yMat)
                    {
                        // This is right and slightly faster, static get hash code returns instance identifier
                        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(x.VertexBuffer).CompareTo(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(y.VertexBuffer));
                    }
                    else
                    {
                        return xMat.CompareTo(yMat);
                    }
                }
                    
                return ((int)xDrawTechnique).CompareTo((int)yDrawTechnique);
            }
        }

        #endregion

        #region Internal Members

        //These settings can be changed from game
        internal static MyRenderSettings Settings = new MyRenderSettings();

        internal static Vector3 AmbientColor;
        internal static float AmbientMultiplier;
        internal static float EnvAmbientIntensity;

        internal static bool ScreenshotOnlyFinal = true;
        internal static bool ShowHWOcclusionQueries = false;
        internal static bool EnableHWOcclusionQueriesForShadows = false;

        internal static uint RenderCounter { get { return m_renderCounter; } }

        internal static readonly Vector3D PrunningExtension = new Vector3D(10, 10, 10);
        internal static bool EnableLights = true;

        internal static bool EnableSpectatorReflector = true;
        internal static bool DrawSpectatorReflector = false;
        
        internal static bool DrawPlayerLightShadow = false;

        internal static byte? OverrideVoxelMaterial = null;


        // When no render setup on stack, we're rendering main fullscreen scene (no mirror, env.map etc)
        internal static bool MainRendering
        {
            get
            {
                return m_renderSetupStack.Count == 0;
            }
        }

        /// <summary>
        /// Current render setup, it's safe to get value from any field, all nullable fields has value.
        /// Do not write to this, use Push/Pop functions
        /// </summary>
        internal static MyRenderSetup CurrentRenderSetup
        {
            get
            {
                return m_currentSetup;
            }
        }

        //Temporary debug for occ queris
        internal static bool RenderOcclusionsImmediatelly = false;

        //Too much render elements
        internal static bool IsRenderOverloaded = false;


        #endregion

        #region Members


        private static MyRenderSetup m_currentSetup = new MyRenderSetup();
        private static List<MyRenderSetup> m_renderSetupStack = new List<MyRenderSetup>(10);
        private static MyRenderSetup m_backupSetup = new MyRenderSetup();

        private static List<MyTexture2D> m_textures = new List<MyTexture2D>(5);
        private static List<MyRenderLight> m_lightsToRender = new List<MyRenderLight>(128);
        private static List<MyRenderLight> m_pointLights = new List<MyRenderLight>(128); // just references
        private static List<MyRenderLight> m_hemiLights = new List<MyRenderLight>(128); // just references


        private static bool m_enableEnvironmentMapAmbient = true;
        private static bool m_enableEnvironmentMapReflection = true;

        static readonly BaseTexture[] m_renderTargets = new BaseTexture[Enum.GetValues(typeof(MyRenderTargets)).GetLength(0)];
        static readonly BaseTexture[] m_spotShadowRenderTargets = new BaseTexture[MyRenderConstants.SPOT_SHADOW_RENDER_TARGET_COUNT];
        static readonly BaseTexture[] m_spotShadowRenderTargetsZBuffers = new BaseTexture[MyRenderConstants.SPOT_SHADOW_RENDER_TARGET_COUNT];

        static readonly List<MyLightRenderElement> m_spotLightRenderElements = new List<MyLightRenderElement>(MyRenderConstants.SPOT_SHADOW_RENDER_TARGET_COUNT);
        static MyObjectsPool<MyLightRenderElement> m_spotLightsPool = new MyObjectsPool<MyLightRenderElement>(400); // Maximum number of spotlights allowed

        static readonly List<MyRenderModuleItem>[] m_renderModules = new List<MyRenderModuleItem>[Enum.GetValues(typeof(MyRenderStage)).GetLength(0)];

        static MyLodTypeEnum m_currentLodDrawPass;
        static List<MyRenderObject> m_renderObjectsToDraw = new List<MyRenderObject>(4000);
        static HashSet<MyRenderObject> m_renderObjectsToDebugDraw = new HashSet<MyRenderObject>();
        static List<MyPostProcessBase> m_postProcesses = new List<MyPostProcessBase>();

        //static MyObjectsPool<MyRenderElement> m_renderElementsPool = new MyObjectsPool<MyRenderElement>(MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT);
        static MyRenderElement[] m_renderElementsPool = new MyRenderElement[MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT];
        static uint m_renderElementIndex = 0;
        static uint m_renderElementCounter = 0;
        static MyRenderElementsComparer m_renderElementsComparer = new MyRenderElementsComparer();
        static List<MyRenderElement> m_renderElements = new List<MyRenderElement>(MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT);
        static List<MyRenderElement> m_transparentRenderElements = new List<MyRenderElement>(MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT);
        static Vector2 m_scaleToViewport = Vector2.One;

        static Texture[] m_GBufferDefaultBinding;
        //static RenderTargetBinding[] m_GBufferLOD0Binding;
        //static RenderTargetBinding[] m_GBufferLOD1Binding;
        //static RenderTargetBinding[] m_GBufferLOD0ExBinding;
        //static RenderTargetBinding[] m_GBufferAux1Lod1DiffBinding;
        static Texture[] m_aux0Binding;

        //Texture for debug rendering
        static MyTexture2D m_debugTexture;
        static MyTexture2D m_debugNormalTexture;
        static MyTexture2D m_debugNormalTextureBump;
        //static RenderTarget2D m_screenshot;

        // Struct for getting statistics of render object
        internal static long ModelTrianglesCountStats;

        //Shadows rendering
        static MyShadowRenderer m_shadowRenderer;

        // Spol light shadows 
        static MySpotShadowRenderer m_spotShadowRenderer;

        //Enabled renderer
        private static bool m_enabled = true;
        private static uint m_renderCounter = 0;


        static HashSet<MyRenderObject> m_nearObjects = new HashSet<MyRenderObject>();
        internal static Dictionary<uint, MyRenderObject> m_renderObjects = new Dictionary<uint, MyRenderObject>();
        
        static MyDynamicAABBTreeD m_farObjectsPrunningStructure = new MyDynamicAABBTreeD(PrunningExtension);
        static MyDynamicAABBTreeD m_prunningStructure = new MyDynamicAABBTreeD(PrunningExtension);
        static MyDynamicAABBTreeD m_cullingStructure = new MyDynamicAABBTreeD(PrunningExtension);
        static MyDynamicAABBTreeD m_manualCullingStructure = new MyDynamicAABBTreeD(PrunningExtension);
        static MyDynamicAABBTreeD m_shadowPrunningStructure = new MyDynamicAABBTreeD(PrunningExtension);
        static List<MyElement> m_renderObjectListForDraw = new List<MyElement>(16384); // for draw method, using separate list, so that it cannot be impacted by concurrent modification
        static List<MyElement> m_cullObjectListForDraw = new List<MyElement>(16384); // for draw method, using separate list, so that it cannot be impacted by concurrent modification
        static List<MyElement> m_manualCullObjectListForDraw = new List<MyElement>(16384); // for draw method, using separate list, so that it cannot be impacted by concurrent modification
        static List<MyElement> m_farCullObjectListForDraw = new List<MyElement>(128); // for draw method, using separate list, so that it cannot be impacted by concurrent modification
        static List<MyElement> m_renderObjectListForIntersections = new List<MyElement>(128);
        static List<MyElement> m_cullObjectListForIntersections = new List<MyElement>(128);
        static List<MyOcclusionQueryIssue> m_renderOcclusionQueries = new List<MyOcclusionQueryIssue>(256); // for draw method, using separate list, so that it cannot be impacted by concurrent modification
        static List<MyRenderLight> m_renderLightsForDraw = new List<MyRenderLight>(16384); //Not multithreaded!         
        static BoundingFrustumD m_cameraFrustum = new BoundingFrustumD(MatrixD.Identity);
        static Vector3D m_cameraPosition;
      
        //Sun
        static MySunLight m_sun = new MySunLight();
        static Vector3[] frustumCorners = new Vector3[8];

        internal static long RenderTimeInMS = 0; //time in MS for internal render animations

        #endregion

        #region Init

        static void MyRenderConstants_OnRenderQualityChange(object sender, EventArgs e)
        {
            //Recreate render targets with size depending on render quality
            if (m_shadowRenderer != null)
            {   //Test if content was already loaded
                m_shadowRenderer.ChangeSize(GetShadowCascadeSize());
                //CreateRenderTargets();
            }
        }

        /// <summary>
        /// Resets the render states.
        /// </summary>
        internal static void ResetStates()
        {
            /*
            //m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.MagFilter, TextureFilter.Point);
            m_device.VertexSamplerStates[0] = SamplerState.PointClamp;
            m_device.VertexSamplerStates[1] = SamplerState.PointClamp;
            m_device.VertexSamplerStates[2] = SamplerState.PointClamp;
            m_device.VertexSamplerStates[3] = SamplerState.PointClamp;

            m_device.SamplerStates[0] = MyStateObjects.PointTextureFilter;
            m_device.SamplerStates[1] = MyStateObjects.PointTextureFilter;
            m_device.SamplerStates[2] = MyStateObjects.PointTextureFilter;
            m_device.SamplerStates[3] = MyStateObjects.PointTextureFilter;
            m_device.SamplerStates[4] = MyStateObjects.PointTextureFilter;
            m_device.SamplerStates[5] = MyStateObjects.PointTextureFilter;
            m_device.SamplerStates[6] = MyStateObjects.PointTextureFilter;
            m_device.SamplerStates[7] = MyStateObjects.PointTextureFilter;
             */
        }

        #endregion

        #region Properties


        internal static MyShadowRenderer GetShadowRenderer()
        {
            return m_currentSetup.ShadowRenderer != null ? m_currentSetup.ShadowRenderer : m_shadowRenderer;
        }

        internal static MySunLight Sun
        {
            get { return m_sun; }
        }
               /*
        internal static VRageRender.Profiler.MyRenderProfiler GetRenderProfiler()
        {
            return VRageRender.MyRender.GetRenderProfiler();
        }
                 */
        internal static bool Enabled
        {
            get { return m_enabled; }
            set
            {
                m_enabled = value;
            }
        }

        //  Resolve back buffer into texture. This method doesn't belong to GUI manager but I don't know about better place. It doesn't belong
        //  to utils too, because it's too XNA specific.

        internal static Texture GetBackBufferAsTexture()
        {
            //TODO
            //return (new MyResolveBackBuffer(m_device)).RenderTarget;
            return null;
        }

        #endregion


        internal static void UpdateScreenSize()
        {
            MyRender.Log.WriteLine("MyRender.UpdateScreenSize() - START");

            //VRageRender.MyRender.GetRenderProfiler().StartProfilingBlock("MySandboxGame::UpdateScreenSize");

            ScreenSize = new Vector2I((int)GraphicsDevice.Viewport.Width, (int)GraphicsDevice.Viewport.Height);
            ScreenSizeHalf = new Vector2I(ScreenSize.X / 2, ScreenSize.Y / 2);

            if (m_screenshot != null)
            {
                ScreenSize.X = (int)(ScreenSize.X * m_screenshot.SizeMultiplier.X);
                ScreenSize.Y = (int)(ScreenSize.Y * m_screenshot.SizeMultiplier.Y);
                ScreenSizeHalf = new Vector2I(ScreenSize.X / 2, ScreenSize.Y / 2);
            }

            MyRender.Log.WriteLine("ScreenSize: " + ScreenSize.ToString());

            int safeGuiSizeY = ScreenSize.Y;
            int safeGuiSizeX = (int)(safeGuiSizeY * 1.3333f);     //  This will mantain same aspect ratio for GUI elements

            int safeFullscreenSizeX = ScreenSize.X;
            int safeFullscreenSizeY = ScreenSize.Y;

            m_fullscreenRectangle = new VRageMath.Rectangle(0, 0, ScreenSize.X, ScreenSize.Y);

            //  Triple head is drawn on three monitors, so we will draw GUI only on the middle one
            m_safeGuiRectangle = new VRageMath.Rectangle(ScreenSize.X / 2 - safeGuiSizeX / 2, 0, safeGuiSizeX, safeGuiSizeY);

            //if (MyVideoModeManager.IsTripleHead() == true)
            //m_safeGuiRectangle.X += MySandboxGame.ScreenSize.X / 3;

            m_safeFullscreenRectangle = new VRageMath.Rectangle(ScreenSize.X / 2 - safeFullscreenSizeX / 2, 0, safeFullscreenSizeX, safeFullscreenSizeY);

            //  This will help as maintain scale/ratio of images, texts during in different resolution
            m_safeScreenScale = (float)safeGuiSizeY / MyRenderGuiConstants.REFERENCE_SCREEN_HEIGHT;

            MyRenderCamera.UpdateScreenSize();

            MyRender.Log.WriteLine("MyRender.UpdateScreenSize() - END");
        }

        internal static Viewport GetBackwardViewport()
        {
            Viewport ret = GraphicsDevice.Viewport;
            ret.Height = (int)(m_safeFullscreenRectangle.Height * MyHudConstants.BACK_CAMERA_HEIGHT);
            ret.Y = (int)(m_safeFullscreenRectangle.Height * MyRenderGuiConstants.HUD_FREE_SPACE.Y);
            ret.Width = (int)(ret.Height * MyHudConstants.BACK_CAMERA_ASPECT_RATIO);
            ret.X = m_safeFullscreenRectangle.Left + m_safeFullscreenRectangle.Width - ret.Width - (int)(m_safeFullscreenRectangle.Width * MyRenderGuiConstants.HUD_FREE_SPACE.X);
            return ret;
        }

        internal static Viewport GetHudViewport()
        {
            Viewport ret = GraphicsDevice.Viewport;
            ret.X = m_safeFullscreenRectangle.Left;
            ret.Width = m_safeFullscreenRectangle.Width;
            return ret;
        }

        internal static Viewport GetFullscreenHudViewport()
        {
            // it's the same as Forward viewport
            Viewport ret = GraphicsDevice.Viewport;
            return ret;
        }


        // Profiling
        static MyRenderProfiler m_renderProfiler = new MyRenderProfilerDX9();

        public static MyRenderProfiler GetRenderProfiler()
        {
            return m_renderProfiler;
        }

        internal static MyFullScreenQuad GetFullscreenQuad()
        {
            return m_fullscreenQuad;
        }

        public static MyDynamicAABBTreeD ShadowPrunning
        {
            get { return m_shadowPrunningStructure; }
        }

        internal static List<MyRenderLight> RenderLightsForDraw
        {
            get { return m_renderLightsForDraw; }
        }
    }
}