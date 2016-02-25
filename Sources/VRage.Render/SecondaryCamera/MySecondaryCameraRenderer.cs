using System;
using VRageMath;
using VRageRender.Effects;
using VRageRender.Shadows;


namespace VRageRender
{
    /// <summary>
    /// Singleton class for rendering game to texture, for things such as missile camera, drone camera, rear mirror.
    /// </summary>
    internal class MySecondaryCameraRenderer
    {
        private readonly MyRender.MyRenderSetup m_setup = new MyRender.MyRenderSetup();
        MyRender.MyRenderSetup m_backup = new MyRender.MyRenderSetup();

        public MatrixD ViewMatrix { get; set; }
        public Matrix ProjectionMatrix { get; set; }

        private static MySecondaryCameraRenderer m_instance;
        public static MySecondaryCameraRenderer Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new MySecondaryCameraRenderer();
                }
                return m_instance;
            }
        }

        private MySecondaryCameraRenderer()
        {
            SetRenderSetup();
        }

        public SharpDX.Direct3D9.Texture GetRenderedTexture()
        {
            return MyRender.GetRenderTarget(MyRenderTargets.SecondaryCamera);
        }

        private void SetRenderSetup()
        {       
            m_setup.CallerID = MyRenderCallerEnum.SecondaryCamera;

            m_setup.RenderTargets = new SharpDX.Direct3D9.Texture[1];
            m_setup.EnabledModules = new System.Collections.Generic.HashSet<MyRenderModuleEnum>();
            m_setup.EnabledModules.Add(MyRenderModuleEnum.SunGlareAndLensFlare);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.UpdateOcclusions);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.AnimatedParticlesPrepare);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.TransparentGeometry);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.SunGlow);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.SectorBorder);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.DrawSectorBBox);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.DrawCoordSystem);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.Explosions);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.BackgroundCube);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.GPS);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.TestField);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.AnimatedParticles);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.AnimatedParticlesPrepare);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.Lights);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.Projectiles);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.DebrisField);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.ThirdPerson);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.Editor);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.PrunningStructure);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.SunWind);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.IceStormWind);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.PrefabContainerManager);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.PhysicsPrunningStructure);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.ParticlesDustField);

            m_setup.EnabledPostprocesses = new System.Collections.Generic.HashSet<MyPostProcessEnum>();
            m_setup.EnabledPostprocesses.Add(MyPostProcessEnum.SSAO);
            m_setup.EnabledPostprocesses.Add(MyPostProcessEnum.FXAA);
            m_setup.EnabledPostprocesses.Add(MyPostProcessEnum.VolumetricFog);

            m_setup.EnableDebugHelpers = false;
            m_setup.EnableOcclusionQueries = false;
            m_setup.EnableHDR = false;
            m_setup.EnableNear = false;

            m_setup.EnableSun = true;
            m_setup.ShadowRenderer = new MyShadowRenderer(MyRenderConstants.RenderQualityProfile.SecondaryShadowMapCascadeSize, MyRenderTargets.SecondaryShadowMap, MyRenderTargets.SecondaryCameraZBuffer, false);
            m_setup.EnableLights = true;

            m_setup.FogMultiplierMult = 2.5f; //increases fog in back camera to imitate missing particle dust

            MyRenderConstants.OnRenderQualityChange += MyRenderConstants_OnRenderQualityChange;
        }

        void MyRenderConstants_OnRenderQualityChange(object sender, EventArgs e)
        {
            m_setup.ShadowRenderer.ChangeSize(MyRenderConstants.RenderQualityProfile.SecondaryShadowMapCascadeSize);
        }

        public void Render()
        {
            var cameraRT = MyRender.GetRenderTarget(MyRenderTargets.SecondaryCamera);
            m_setup.RenderTargets[0] = cameraRT;

            //SetRenderSetup();

            // Adjust render setup
            m_setup.AspectRatio = cameraRT.GetLevelDescription(0).Width / (float)cameraRT.GetLevelDescription(0).Height;
            m_setup.Viewport = new SharpDX.Viewport(0, 0, cameraRT.GetLevelDescription(0).Width, cameraRT.GetLevelDescription(0).Height);
            m_setup.ViewMatrix = ViewMatrix;
            m_setup.CameraPosition = ViewMatrix.Translation;
            m_setup.Fov = MathHelper.ToRadians(MySecondaryCameraConstants.FIELD_OF_VIEW);

            m_setup.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(m_setup.Fov.Value,
                                                                           m_setup.AspectRatio.Value,
                                                                           MySecondaryCameraConstants.NEAR_PLANE_DISTANCE,
                                                                           //m_setup.LodTransitionBackgroundEnd.Value);
                                                                           MyRenderCamera.FAR_PLANE_DISTANCE);
            ProjectionMatrix = m_setup.ProjectionMatrix.Value;
            MyRenderCamera.Viewport = m_setup.Viewport.Value;

            // render to fullsize texture
            MyRender.PushRenderSetupAndApply(m_setup, ref m_backup);
            MyRender.Draw3D(false);
            //MyRender.GraphicsDevice.Clear(ClearFlags.All, new ColorBGRA(1.0f, 0, 0.0f, 1), 1, 0);
            MyRender.PopRenderSetupAndRevert(m_backup);


            MyRender.TakeScreenshot("BackCamera", cameraRT, MyEffectScreenshot.ScreenshotTechniqueEnum.Color);

            //Texture.ToFile(cameraRT, "c:\\__cam.dds", ImageFileFormat.Dds);
        }
    }
}
