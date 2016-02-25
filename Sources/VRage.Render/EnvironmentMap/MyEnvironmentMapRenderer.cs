using System;
using System.Collections.Generic;
using VRageMath;

using SharpDX;
using SharpDX.Direct3D9;



namespace VRageRender
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using VRageRender.Effects;
    using VRageRender.Graphics;

    public class MyEnvironmentMapRenderer 
    {
        //const float NEAR_CLIP_FOR_INSTANT = 12;

        CubeTexture m_environmentRT;
        CubeTexture m_ambientRT;
        Texture m_fullSizeRT;

        MyRender.MyRenderSetup m_setup;
        MyRender.MyRenderSetup m_backup = new MyRender.MyRenderSetup();

        BaseTexture[] m_bindings = new BaseTexture[1];

        float NearClip
        {
            get
            {
                // This gets near from projection matrix
                return MyRenderCamera.ProjectionMatrix.M43 / MyRenderCamera.ProjectionMatrix.M33;
            }
        }

        public MyEnvironmentMapRenderer()
        {
            SetRenderSetup();
            MyRenderConstants.OnRenderQualityChange += new EventHandler(MyRenderConstants_OnRenderQualityChange);
        }

        void MyRenderConstants_OnRenderQualityChange(object sender, EventArgs e)
        {
            SetRenderSetup();
        }

        public void SetRenderTarget(CubeTexture environmentRT, CubeTexture ambientRT, Texture fullSizeRT)
        {
            m_setup.RenderTargets[0] = fullSizeRT;

            m_environmentRT = environmentRT;
            m_ambientRT = ambientRT;
            m_fullSizeRT = fullSizeRT;
        }

        MatrixD CreateViewMatrix(CubeMapFace cubeMapFace, Vector3D position)
        {
            MatrixD viewMatrix = MatrixD.Identity;
            Vector3D pos = position;
            switch (cubeMapFace)
            {
                // Face index 0
                case CubeMapFace.PositiveX:
                    viewMatrix = MatrixD.CreateLookAt(pos, pos + Vector3.Left, -Vector3.Up);
                    break;

                // Face index 1
                case CubeMapFace.NegativeX:
                    viewMatrix = MatrixD.CreateLookAt(pos, pos + Vector3.Right, -Vector3.Up);
                    break;

                // Face index 2
                case CubeMapFace.PositiveY:
                    viewMatrix = MatrixD.CreateLookAt(pos, pos + Vector3.Down, Vector3.Backward);
                    break;

                // Face index 3
                case CubeMapFace.NegativeY:
                    viewMatrix = MatrixD.CreateLookAt(pos, pos + Vector3.Up, Vector3.Forward);
                    break;

                // Face index 4
                case CubeMapFace.PositiveZ:
                    viewMatrix = MatrixD.CreateLookAt(pos, pos + Vector3.Forward, -Vector3.Up);
                    break;

                // Face index 5
                case CubeMapFace.NegativeZ:
                    viewMatrix = MatrixD.CreateLookAt(pos, pos + Vector3.Backward, -Vector3.Up);
                    break;
            }
            return viewMatrix;
        }

        public CubeTexture Environment
        {
            get
            {
                return m_environmentRT;
            }
        }

        public CubeTexture Ambient
        {
            get
            {
                return m_ambientRT;
            }
        }

        int m_currentIndex = -1;
        Vector3D m_position;

        // If render now is true, all face are rendered instantly
        public void StartUpdate(Vector3D position, bool renderNow = false)
        {
            this.m_position = position;
            m_currentIndex = 0;

            if (renderNow)
            {
                // When rendering all in one frame, make sure no close objects bother us
                //var old = NearClip;
                //NearClip = NEAR_CLIP_FOR_INSTANT;

                for (int i = 0; i < 12; i++)
                {
                    ContinueUpdate();
                }

                //NearClip = old;
            }
        }

        public void ContinueUpdate()
        {
            if (m_currentIndex >= 0 && m_currentIndex <= 5)
            {
                // We use rendered scene in cube map for both environment and ambient;
                if (MyRender.Settings.EnableEnvironmentMapReflection || MyRender.Settings.EnableEnvironmentMapAmbient)
                {
                    UpdateFace(m_position, m_currentIndex);
                }
                m_currentIndex++;
                //currentIndex = 6; // Only render one side
            }
            else if (m_currentIndex >= 6 && m_currentIndex <= 11)
            {
                // Precalculate ambient to be used as lookup texture (cumulation)
                if (MyRender.Settings.EnableEnvironmentMapAmbient)
                {
                    UpdateAmbient(m_currentIndex - 6);
                }
                m_currentIndex++;
                //currentIndex = 12; // Only blur one side
            }
        }

        public bool IsDone()
        {
            return m_currentIndex == 12;
        }

        private void SetRenderSetup()
        {
            if (m_setup == null)
                m_setup = new MyRender.MyRenderSetup();

            m_setup.CallerID = MyRenderCallerEnum.EnvironmentMap;

            if (m_setup.RenderTargets == null)
                m_setup.RenderTargets = new Texture[1];
           

            m_setup.EnabledModules = new HashSet<MyRenderModuleEnum>();
            m_setup.EnabledModules.Add(MyRenderModuleEnum.SunGlow);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.SectorBorder);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.DrawSectorBBox);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.DrawCoordSystem);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.Explosions);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.BackgroundCube);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.GPS);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.TestField);
            m_setup.EnabledModules.Add(MyRenderModuleEnum.AnimatedParticles);
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

            m_setup.EnabledRenderStages = new HashSet<MyRenderStage>();
            m_setup.EnabledRenderStages.Add(MyRenderStage.PrepareForDraw);
            m_setup.EnabledRenderStages.Add(MyRenderStage.Background);
            m_setup.EnabledRenderStages.Add(MyRenderStage.LODDrawStart);
            m_setup.EnabledRenderStages.Add(MyRenderStage.LODDrawEnd);

            m_setup.EnabledPostprocesses = new HashSet<MyPostProcessEnum>();
            m_setup.EnabledPostprocesses.Add(MyPostProcessEnum.VolumetricFog);
            m_setup.FogMultiplierMult = 1.0f; //increases fog to imitate missing particle dust

            m_setup.EnableHDR = false;
            m_setup.EnableSun = false;
            m_setup.EnableSmallLights = false;
            m_setup.EnableDebugHelpers = false;
            m_setup.EnableEnvironmentMapping = false;
            m_setup.EnableOcclusionQueries = false;
            m_setup.EnableNear = false;
        }

        public void UpdateFace(Vector3D position, int faceIndex)
        {
            //SetRenderSetup();

            CubeMapFace face = (CubeMapFace)faceIndex;

            // New setup
            m_setup.CameraPosition = position;
            m_setup.AspectRatio = 1.0f;
            m_setup.Viewport = new Viewport(0, 0, (int)m_environmentRT.GetLevelDescription(0).Width, (int)m_environmentRT.GetLevelDescription(0).Width);
            m_setup.ViewMatrix = CreateViewMatrix(face, position);
            m_setup.Fov = MathHelper.PiOver2;
            m_setup.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(m_setup.Fov.Value, m_setup.AspectRatio.Value, NearClip, MyRenderCamera.NEAR_PLANE_FOR_BACKGROUND);
            m_setup.DepthToAlpha = true;

            MyRender.GetRenderProfiler().StartProfilingBlock("Draw environmental maps");

            MyRender.PushRenderSetupAndApply(m_setup, ref m_backup);
            MyRender.Draw3D(false);

            MyRender.GetRenderProfiler().EndProfilingBlock();
                                        
            Surface cubeSurface = m_environmentRT.GetCubeMapSurface(face, 0);                 
            MyRender.GraphicsDevice.SetRenderTarget(0, cubeSurface);

            var screenEffect = MyRender.GetEffect(MyEffects.Screenshot) as MyEffectScreenshot;
            screenEffect.SetTechnique(MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
            screenEffect.SetSourceTexture(m_fullSizeRT);
            screenEffect.SetScale(new Vector2(m_environmentRT.GetLevelDescription(0).Width / (float)m_fullSizeRT.GetLevelDescription(0).Width, m_environmentRT.GetLevelDescription(0).Height / (float)m_fullSizeRT.GetLevelDescription(0).Height));
            MyRender.GetFullscreenQuad().Draw(screenEffect);
            screenEffect.SetScale(new Vector2(1, 1));

            //Texture.ToFile(m_fullSizeRT, "C:\\fullSizeRT.dds", ImageFileFormat.Dds);

            cubeSurface.Dispose();    

            MyRender.PopRenderSetupAndRevert(m_backup);
        }

        public void UpdateAmbient(int index)
        {                             
            CubeMapFace face = (CubeMapFace)index;
            Surface cubeSurface = m_ambientRT.GetCubeMapSurface(face, 0);
            MyRender.GraphicsDevice.SetRenderTarget(0, cubeSurface);
            BlendState.Opaque.Apply();

            MyEffectAmbientPrecalculation precalc = MyRender.GetEffect(MyEffects.AmbientMapPrecalculation) as MyEffectAmbientPrecalculation;
            precalc.SetEnvironmentMap(this.m_environmentRT);
            precalc.SetFaceMatrix((Matrix)CreateViewMatrix(face, Vector3D.Zero));
            precalc.SetRandomTexture(MyRender.GetRandomTexture());
            precalc.SetIterationCount(14);
            precalc.SetMainVectorWeight(1.0f);
            precalc.SetBacklightColorAndIntensity(new Vector3(MyRender.Sun.BackColor.X, MyRender.Sun.BackColor.Y, MyRender.Sun.BackColor.Z), MyRender.Sun.BackIntensity);
            MyRender.GetFullscreenQuad().Draw(precalc);

            MyRender.SetRenderTarget(null, null);
            cubeSurface.Dispose();  
        }
    }
}
