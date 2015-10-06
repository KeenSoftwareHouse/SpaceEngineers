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
using VRage.ObjectBuilders;

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

        public static MySunProperties SunProperties;
        public static MyFogProperties FogProperties;
        internal static MyParticleDustProperties ParticleDustProperties;
        internal static MyGodRaysProperties GodRaysProperties;
        public static VRageRender.MyImpostorProperties[] ImpostorProperties;
        public static string BackgroundTexture;
        public static Quaternion BackgroundOrientation;
        public static bool UseGenerator = false;
        public static List<int> PrimaryMaterials;
        public static List<int> SecondaryMaterials;

        public static MyCamera MainCamera;


        public static void SetDefaults()
        {
            SunProperties = new MySunProperties(MySunProperties.Default);
            FogProperties = MyFogProperties.Default;
            ImpostorProperties = new VRageRender.MyImpostorProperties[1];
            ParticleDustProperties = new MyParticleDustProperties();
            GodRaysProperties = new MyGodRaysProperties();
            BackgroundTexture = "BackgroundCube";
        }

        public static Vector3 DirectionToSunNormalized
        {
            get
            {
                return SunProperties.SunDirectionNormalized;
            }
        }

        public static float DistanceToSun;
        public static float DayTime;
        public static bool ResetEyeAdaptation;

        public static void InitEnvironmentSettings(MyObjectBuilder_EnvironmentSettings environmentBuilder = null)
        {
            var environment = MyDefinitionManager.Static.EnvironmentDefinition;
            var o = environment.BackgroundOrientation;
            BackgroundTexture        = environment.BackgroundTexture;
            BackgroundOrientation    = Quaternion.CreateFromYawPitchRoll(o.Yaw, o.Pitch, o.Roll);
            DistanceToSun            = environment.DistanceToSun;

            SunProperties = new MySunProperties(environment.SunProperties);
            FogProperties = environment.FogProperties;

            if (environmentBuilder != null)
            {
                Vector3 sunDirection;
                Vector3.CreateFromAzimuthAndElevation(environmentBuilder.SunAzimuth, environmentBuilder.SunElevation, out sunDirection);

                SunProperties.BaseSunDirectionNormalized = sunDirection;
                
                SunProperties.SunDirectionNormalized = sunDirection;
                SunProperties.SunIntensity = environmentBuilder.SunIntensity;

                FogProperties.FogMultiplier = environmentBuilder.FogMultiplier;
                FogProperties.FogDensity = environmentBuilder.FogDensity;
                FogProperties.FogColor = new Color(environmentBuilder.FogColor);
                FogProperties.FogColor.A = 255;
            }
        }

        public static MyObjectBuilder_EnvironmentSettings GetEnvironmentSettings()
        {
            if (SunProperties.Equals(MyDefinitionManager.Static.EnvironmentDefinition.SunProperties) && FogProperties.Equals(MyDefinitionManager.Static.EnvironmentDefinition.FogProperties))
            {
                return null;
            }
            
            var objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_EnvironmentSettings>();

            float azimuth, elevation;
            Vector3.GetAzimuthAndElevation(SunProperties.BaseSunDirectionNormalized, out azimuth, out elevation);
            objectBuilder.SunAzimuth = azimuth;
            objectBuilder.SunElevation = elevation;
            objectBuilder.SunIntensity = SunProperties.SunIntensity;

            objectBuilder.FogMultiplier = FogProperties.FogMultiplier;
            objectBuilder.FogDensity = FogProperties.FogDensity;
            objectBuilder.FogColor = FogProperties.FogColor.ToVector3();

            return objectBuilder;
        }

        public override void LoadData()
        {
            MainCamera = new MyCamera();
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
            MainCamera.Update();
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
