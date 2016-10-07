using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Common;
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
using Sandbox.Game.SessionComponents;

namespace Sandbox.Game.World
{

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

        internal static MyParticleDustProperties ParticleDustProperties;
        public static VRageRender.MyImpostorProperties[] ImpostorProperties;
        public static bool UseGenerator = false;
        public static List<int> PrimaryMaterials;
        public static List<int> SecondaryMaterials;

        public static MyEnvironmentDefinition EnvironmentDefinition;

        public static MyCamera MainCamera;

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
            SunRotationAxis = SunProperties.SunRotationAxis;

            MyRenderProxy.UpdateShadowsSettings(ShadowSettings);

            // TODO: Delete MyPostprocessSettingsWrapper and move to have bundled
            // settings in MySector and change all references to point here
            MyPostprocessSettingsWrapper.Settings = environment.PostProcessSettings;
            MyPostprocessSettingsWrapper.PlanetSettings = environment.PostProcessSettings;

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
            MainCamera = new MyCamera(MySandboxGame.Config.FieldOfView, MySandboxGame.ScreenViewport);
            MainCamera.FarPlaneDistance = MySession.Static.Settings.ViewDistance;
            MyEntities.LoadData();
        }

        protected override void UnloadData()
        {
            MyEntities.UnloadData();
            MainCamera = null;
            base.UnloadData();
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
