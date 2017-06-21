using System.Collections.Generic;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.Entities;
using Sandbox.Definitions;
using System.Diagnostics;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.Utils;
using VRageRender;
using System.IO;
using VRage.FileSystem;
using VRageRender.Messages;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.World
{
    public class MySectorLodding
    {
        public MyNewLoddingSettings UserSettings = new MyNewLoddingSettings();
        MyNewLoddingSettings m_lowSettings = new MyNewLoddingSettings();
        MyNewLoddingSettings m_mediumSettings = new MyNewLoddingSettings();
        MyNewLoddingSettings m_highSettings = new MyNewLoddingSettings();

        bool m_isActiveUserSettings = false;
        MyRenderQualityEnum m_selectedModelQuality = MyRenderQualityEnum.HIGH;
        public MyNewLoddingSettings LowSettings { get { return m_lowSettings; } }
        public MyNewLoddingSettings MediumSettings { get { return m_mediumSettings; } }
        public MyNewLoddingSettings HighSettings { get { return m_highSettings; } }

        void Apply()
        {
            if (m_isActiveUserSettings)
                MyRenderProxy.UpdateNewLoddingSettings(UserSettings);
            else
            {
                MyNewLoddingSettings sel;
                switch (m_selectedModelQuality)
                {
                    case MyRenderQualityEnum.LOW:
                        sel = LowSettings;
                        break;
                    case MyRenderQualityEnum.NORMAL:
                        sel = MediumSettings;
                        break;
                    case MyRenderQualityEnum.HIGH:
                        sel = HighSettings;
                        break;
                    default:
                        Debug.Fail("Unresolved condition");
                        return;
                }
                MyRenderProxy.UpdateNewLoddingSettings(sel);
            }
        }

        public void ApplyUserSettings()
        {
            m_isActiveUserSettings = true;
            Apply();
        }

        public void UpdatePreset(MyNewLoddingSettings userSettings, MyNewLoddingSettings lowLoddingSettings, MyNewLoddingSettings mediumLoddingSettings,
            MyNewLoddingSettings highLoddingSettings)
        {
            UserSettings.CopyFrom(userSettings);
            m_lowSettings.CopyFrom(lowLoddingSettings);
            m_mediumSettings.CopyFrom(mediumLoddingSettings);
            m_highSettings.CopyFrom(highLoddingSettings);
            if (!m_isActiveUserSettings)
                Apply();
        }

        public void SelectQuality(MyRenderQualityEnum quality)
        {
            m_isActiveUserSettings = false;
            m_selectedModelQuality = quality;
            Apply();
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation, 800)]
    public class MySector : MySessionComponentBase
    {
        static MySector()
        {
            // Set defaults - that's for example for menu animations, trailers etc...
            SetDefaults();
        }

        public static Vector3 SunRotationAxis;
        public static MySunProperties SunProperties;
        public static MyFogProperties FogProperties;
        public static MySSAOSettings SSAOSettings;
        public static MyHBAOData HBAOSettings;
        public static MyShadowsSettings ShadowSettings = new MyShadowsSettings();
        public static MyNewPipelineSettings NewPipelineSettings = new MyNewPipelineSettings();
        public static MySectorLodding Lodding = new MySectorLodding();
        
        internal static MyParticleDustProperties ParticleDustProperties;
        public static VRageRender.MyImpostorProperties[] ImpostorProperties;
        public static bool UseGenerator = false;
        public static List<int> PrimaryMaterials;
        public static List<int> SecondaryMaterials;

        public static MyEnvironmentDefinition EnvironmentDefinition;

        private static Lights.MyLight m_sunFlare; 

        private static MyCamera m_camera;
        public static MyCamera MainCamera
        {
            get { return m_camera;  }
            set
            {
                m_camera = value;
                Graphics.MyGuiManager.SetCamera(MainCamera);
                MyTransparentGeometry.SetCamera(MainCamera);
            }
        }

        public static void SetDefaults()
        {
            SunProperties = MySunProperties.Default;
            FogProperties = MyFogProperties.Default;
            ImpostorProperties = new VRageRender.MyImpostorProperties[1];
            ParticleDustProperties = new MyParticleDustProperties();
        }

        public static Vector3 DirectionToSunNormalized
        {
            get { return SunProperties.SunDirectionNormalized; }
        }

        public static float DayTime;
        public static bool ResetEyeAdaptation;

        public static void InitEnvironmentSettings(MyObjectBuilder_EnvironmentSettings environmentBuilder = null)
        {
            if (environmentBuilder != null)
            {
                EnvironmentDefinition = MyDefinitionManager.Static.GetDefinition<MyEnvironmentDefinition>(environmentBuilder.EnvironmentDefinition);
            }
            else if (EnvironmentDefinition == null)
            {
                // Fallback
                EnvironmentDefinition = MyDefinitionManager.Static.GetDefinition<MyEnvironmentDefinition>(MyStringHash.GetOrCompute("Default"));
            }

            var environment = EnvironmentDefinition;
            SunProperties = environment.SunProperties;
            FogProperties = environment.FogProperties;
            SSAOSettings = environment.SSAOSettings;
            HBAOSettings = environment.HBAOSettings;
            ShadowSettings.CopyFrom(environment.ShadowSettings);
            NewPipelineSettings.CopyFrom(environment.NewPipelineSettings);
            SunRotationAxis = SunProperties.SunRotationAxis;

            MyRenderProxy.UpdateShadowsSettings(ShadowSettings);
            MyRenderProxy.UpdateNewPipelineSettings(NewPipelineSettings);
            MySector.Lodding.UpdatePreset(environment.UserLoddingSettings, environment.LowLoddingSettings, environment.MediumLoddingSettings, environment.HighLoddingSettings);

            MyMaterialsSettings materialsSettings = new MyMaterialsSettings();
            materialsSettings.CopyFrom(environment.MaterialsSettings);
            MyRenderProxy.UpdateMaterialsSettings(materialsSettings);

            // TODO: Delete MyPostprocessSettingsWrapper and move to have bundled
            // settings in MySector and change all references to point here
            MyPostprocessSettingsWrapper.Settings = environment.PostProcessSettings;

            if (environmentBuilder != null)
            {
                Vector3 sunDirection;
                Vector3.CreateFromAzimuthAndElevation(environmentBuilder.SunAzimuth, environmentBuilder.SunElevation, out sunDirection);
                sunDirection.Normalize();

                SunProperties.BaseSunDirectionNormalized = sunDirection;
                SunProperties.SunDirectionNormalized = sunDirection;
                //SunProperties.SunIntensity = environmentBuilder.SunIntensity;

                FogProperties.FogMultiplier = environmentBuilder.FogMultiplier;
                FogProperties.FogDensity = environmentBuilder.FogDensity;
                FogProperties.FogColor = new Color(environmentBuilder.FogColor);
            }
        }

        public static void SaveEnvironmentDefinition()
        {
            EnvironmentDefinition.SunProperties = SunProperties;
            EnvironmentDefinition.FogProperties = FogProperties;
            EnvironmentDefinition.SSAOSettings = SSAOSettings;
            EnvironmentDefinition.HBAOSettings = HBAOSettings;
            EnvironmentDefinition.PostProcessSettings = MyPostprocessSettingsWrapper.Settings;
            EnvironmentDefinition.ShadowSettings.CopyFrom(ShadowSettings);
            EnvironmentDefinition.NewPipelineSettings.CopyFrom(NewPipelineSettings);
            EnvironmentDefinition.UserLoddingSettings.CopyFrom(Lodding.UserSettings);
            EnvironmentDefinition.LowLoddingSettings.CopyFrom(Lodding.LowSettings);
            EnvironmentDefinition.MediumLoddingSettings.CopyFrom(Lodding.MediumSettings);
            EnvironmentDefinition.HighLoddingSettings.CopyFrom(Lodding.HighSettings);

            var save = new MyObjectBuilder_Definitions();
            save.Environments = new MyObjectBuilder_EnvironmentDefinition[] { (MyObjectBuilder_EnvironmentDefinition)EnvironmentDefinition.GetObjectBuilder() };
            save.Save(Path.Combine(MyFileSystem.ContentPath, "Data", "Environment.sbc"));
        }

        public static MyObjectBuilder_EnvironmentSettings GetEnvironmentSettings()
        {
            if (SunProperties.Equals(EnvironmentDefinition.SunProperties) && FogProperties.Equals(EnvironmentDefinition.FogProperties))
            {
                return null;
            }
            
            var objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_EnvironmentSettings>();

            float azimuth, elevation;
            Vector3.GetAzimuthAndElevation(SunProperties.BaseSunDirectionNormalized, out azimuth, out elevation);
            objectBuilder.SunAzimuth = azimuth;
            objectBuilder.SunElevation = elevation;
            //objectBuilder.SunIntensity = SunProperties.SunIntensity;

            objectBuilder.FogMultiplier = FogProperties.FogMultiplier;
            objectBuilder.FogDensity = FogProperties.FogDensity;
            objectBuilder.FogColor = FogProperties.FogColor;

            objectBuilder.EnvironmentDefinition = EnvironmentDefinition.Id;

            return objectBuilder;
        }

        public override void LoadData()
        {
            MainCamera = new MyCamera(MySandboxGame.Config.FieldOfView, MySandboxGame.ScreenViewport)
            {
                FarPlaneDistance = MySession.Static.Settings.ViewDistance
            };
            MyEntities.LoadData();

            Debug.Assert(m_sunFlare == null);
            m_sunFlare = Lights.MyLights.AddLight();
            m_sunFlare.Start(Lights.MyLight.LightTypeEnum.None, 1.0f);
            m_sunFlare.LightOwner = Lights.MyLight.LightOwnerEnum.LargeShip;
            m_sunFlare.GlareOn = MyFakes.SUN_GLARE;
            m_sunFlare.GlareIntensity = 1;
            m_sunFlare.GlareSize = 25000.0f;
            m_sunFlare.GlareQuerySize = 100000.0f;
            m_sunFlare.GlareQueryFreqMinMs = 0;
            m_sunFlare.GlareQueryFreqRndMs = 0;
            m_sunFlare.GlareType = VRageRender.Lights.MyGlareTypeEnum.Distant;
            m_sunFlare.GlareMaterial = "SunGlareMain";
            m_sunFlare.GlareMaxDistance = 2000000;
            UpdateSunLight();
        }

        public static void UpdateSunLight()
        {
            m_sunFlare.Position = MainCamera.Position + SunProperties.SunDirectionNormalized * 1000000;
            m_sunFlare.UpdateLight();
        }

        protected override void UnloadData()
        {
            MyEntities.UnloadData();
            MainCamera = null;
            base.UnloadData();

            if (m_sunFlare != null)
                Lights.MyLights.RemoveLight(m_sunFlare);
            m_sunFlare = null;
        }

        public override void UpdateBeforeSimulation()
        {
            MyEntities.UpdateBeforeSimulation();

            base.UpdateBeforeSimulation();
        }

        public override void UpdateAfterSimulation()
        {
            MyEntities.UpdateAfterSimulation();

            base.UpdateAfterSimulation();
        }

        public override void UpdatingStopped()
        {
            MyEntities.UpdatingStopped();

            base.UpdatingStopped();
        }

        public override void Draw()
        {
            base.Draw();

            MyEntities.Draw();
        }

        public override void BeforeStart()
        {
            base.BeforeStart();
        }
    }
}
