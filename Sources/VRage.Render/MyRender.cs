using SharpDX.Direct3D9;
using System.Collections.Generic;
using VRageRender.Textures;
using VRageRender.Graphics;
using VRageMath;
using VRage.Utils;
using VRageRender.Profiler;
using System.Threading;
using System;
using SharpDX;
using VRageRender.Shadows;
using VRage;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.FileSystem;

namespace VRageRender
{
    internal static partial class MyRender
    {
        internal static MyTimeSpan m_previousDrawTime;
        internal static MyTimeSpan m_previousUpdateTime;
        internal static MyTimeSpan m_currentDrawTime;
        internal static MyTimeSpan m_currentUpdateTime;
        internal static MyTimeSpan m_currentLag = MyTimeSpan.FromMiliseconds(MyRender.Settings.InterpolationLagMs);
        internal static MyTimeSpan m_lagConstant = MyTimeSpan.FromMiliseconds(MyRender.Settings.InterpolationLagMs);

        internal static MyTimeSpan CurrentDrawTime { get { return m_currentDrawTime; } set { m_previousDrawTime = m_currentDrawTime; m_currentDrawTime = value; } }
        internal static MyTimeSpan CurrentUpdateTime { get { return m_currentUpdateTime; } set { m_previousUpdateTime = m_currentUpdateTime; m_currentUpdateTime = value; } }
        internal static MyTimeSpan InterpolationTime { 
            get {
                float factor = (float)((m_currentDrawTime - m_currentLag - m_previousUpdateTime).Seconds / (m_currentUpdateTime - m_previousUpdateTime).Seconds);

                if (factor < 3)
                    return m_currentDrawTime - m_currentLag;
                else
                    return m_currentUpdateTime + m_currentUpdateTime + m_currentUpdateTime - m_previousUpdateTime - m_previousUpdateTime;
            } 
        }

        internal static MySharedData SharedData = new MySharedData();

        internal static Device GraphicsDevice { get; private set; }
        internal static bool SupportsHDR { get; private set; }

        internal static MyLog Log = new MyLog();

        internal static MyTexture2D BlankTexture;

        public static Vector2I ScreenSize;
        private static Vector2I ScreenSizeHalf;
        private static float m_safeScreenScale;

        internal static string RootDirectory = MyFileSystem.ContentPath;
        internal static string RootDirectoryEffects = MyFileSystem.ContentPath;
        internal static string RootDirectoryDebug = MyFileSystem.ContentPath;

        static MyFullScreenQuad m_fullscreenQuad;
        internal static Surface DefaultSurface { get; private set; }
        internal static Surface DefaultDepth { get; private set; }

        internal static uint GlobalMessageCounter = 0;

        internal static MyScreenshot m_screenshot = null;

        internal static List<renderColoredTextureProperties> m_texturesToRender = new List<renderColoredTextureProperties>();
        public static MyScreenshot GetScreenshot()
        {
            return m_screenshot;
        }

        internal class MyFogProperties
        {
            // Fog
            public float FogNear;
            public float FogFar;
            public float FogMultiplier;
            public float FogBacklightMultiplier;
            public VRageMath.Vector3 FogColor;
        }

        internal static MyFogProperties FogProperties = new MyFogProperties();

        static MyRender()
        {
            const string logName = "VRageRender.log";
            Log.Init(logName, new System.Text.StringBuilder("Version unknown"));
            Log.WriteLine("VRage renderer started");

            RegisterPostProcesses();

            //Initialize for event registration
            for (int i = 0; i < Enum.GetValues(typeof(MyRenderStage)).GetLength(0); i++)
            {
                m_renderModules[i] = new List<MyRenderModuleItem>();
            }

            for (int i = 0; i < MyRenderConstants.MAX_RENDER_ELEMENTS_COUNT; i++)
            {
                m_renderElementsPool[i] = new MyRenderElement();
            }

            RegisterComponents();

            m_sun.Start();

            InitDrawTechniques();

            //m_prepareEntitiesEvent = new AutoResetEvent(false);
            //Task.Factory.StartNew(PrepareEntitiesForDrawBackground, TaskCreationOptions.PreferFairness);

            MyRender.RegisterRenderModule(MyRenderModuleEnum.PrunningStructure, "Prunning structure", DebugDrawPrunning, MyRenderStage.DebugDraw, 250, false);
            MyRender.RegisterRenderModule(MyRenderModuleEnum.Atmosphere, "Draw atmosphere", DrawAtmosphere, MyRenderStage.AlphaBlendPreHDR, 1, false);
            //MyRender.RegisterRenderModule(MyRenderModuleEnum.PhysicsPrunningStructure, "Physics prunning structure", MyPhysics.DebugDrawPhysicsPrunning, MyRenderStage.DebugDraw, 250, false);

            MyRenderConstants.OnRenderQualityChange += new EventHandler(MyRenderConstants_OnRenderQualityChange);
        }

        internal static float AddAndInterpolateObjectMatrix(MyInterpolationQueue<VRageMath.MatrixD> queue, ref VRageMath.MatrixD matrix)
        {
            if (Settings.EnableObjectInterpolation)
            {
                queue.AddSample(ref matrix, MyRender.CurrentUpdateTime);
                return queue.Interpolate(MyRender.InterpolationTime, out matrix);
            }
            return 0.0f;
        }

        private static void RegisterPostProcesses()
        {
            //Initialize post processes                        
            m_postProcesses.Add(new MyPostProcessHDR());
            m_postProcesses.Add(new MyPostProcessAntiAlias());
            m_postProcesses.Add(new MyPostProcessVolumetricSSAO2());
            m_postProcesses.Add(new MyPostProcessContrast(false));
            m_postProcesses.Add(new MyPostProcessVolumetricFog(false));
            m_postProcesses.Add(new MyPostProcessGodRays(false));
            m_postProcesses.Add(new MyPostProcessVignetting(false));
            m_postProcesses.Add(new MyPostProcessColorMapping(false));
            m_postProcesses.Add(new MyPostProcessChromaticAberration(false));
        }

        private static void RegisterComponents()
        {
            RegisterComponent(new MyRenderCamera());
            RegisterComponent(new MyShadowRendererBase());
            RegisterComponent(new MyOcclusionQueries());
            RegisterComponent(new MyVertexFormats());
            RegisterComponent(new DepthStencilState());
            RegisterComponent(new VRageRender.Graphics.SamplerState());
            RegisterComponent(new RasterizerState());
            RegisterComponent(new BlendState());
            RegisterComponent(new MyDebugDraw());
            RegisterComponent(new MyRenderVoxelMaterials());
            RegisterComponent(new MyBackgroundCube());
            RegisterComponent(new MyTransparentGeometry());
            //RegisterComponent(new MySolarMapRenderer());
            RegisterComponent(new MyDecals());
            RegisterComponent(new MyCockpitGlass());
            //RegisterComponent(new MyDistantImpostors());
            RegisterComponent(new MySunGlare());
            RegisterComponent(new MyTextureManager());
            RegisterComponent(new MyRenderModels());
        }

        internal static void EnqueueMessage(MyRenderMessageBase message, bool limitMaxQueueSize)
        {
            SharedData.CurrentUpdateFrame.RenderInput.Add(message);
        }

        internal static void EnqueueOutputMessage(MyRenderMessageBase message)
        {
            SharedData.RenderOutputMessageQueue.Enqueue(message);
        }

        internal static MyMessageQueue OutputQueue
        {
            get { return SharedData.RenderOutputMessageQueue; }
        }

        //  This is for HUD, therefore not GUI normalized coordinates
        public static VRageMath.Vector2 GetHudPixelCoordFromNormalizedCoord(VRageMath.Vector2 normalizedCoord)
        {
            return new VRageMath.Vector2(
                normalizedCoord.X * (float)m_safeFullscreenRectangle.Width,
                normalizedCoord.Y * (float)m_safeFullscreenRectangle.Height);
        }
    }
}
