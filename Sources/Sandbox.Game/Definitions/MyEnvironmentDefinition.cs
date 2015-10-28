using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage;
using VRageMath;
using EnvironmentalParticleSettings = Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_EnvironmentDefinition.EnvironmentalParticleSettings;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_EnvironmentDefinition))]
    public class MyEnvironmentDefinition : MyDefinitionBase
    {
        public string BackgroundTexture = @"Textures\BackgroundCube\Final\BackgroundCube.dds";
        public MyOrientation BackgroundOrientation = new MyOrientation(MathHelper.ToRadians(60.3955536f), MathHelper.ToRadians(-61.1861954f), MathHelper.ToRadians(90.90578f));
        public float DistanceToSun = 1620.18518f;

        public MyFogProperties FogProperties = new MyFogProperties();
        public MySunProperties SunProperties = new MySunProperties()
        {
            SunIntensity = 1.456f,
            SunDiffuse = new VRageMath.Color(0.784313738f),
            SunSpecular = new VRageMath.Color(0.784313738f),
            BackSunDiffuse = new VRageMath.Color(0.784313738f),
            BackSunIntensity = 0.239f,
            AmbientColor = new Color(0.141176477f),
            AmbientMultiplier = 0.969f,
            EnvironmentAmbientIntensity = 0.5f,
            BackgroundColor = new Color(1.0f),
            SunMaterial = "SunDisk",
            SunSizeMultiplier = 200.0f,
            SunDirectionNormalized = new Vector3(0.339467347f, 0.709795356f, -0.617213368f),
            BaseSunDirectionNormalized = new Vector3(0.339467347f, 0.709795356f, -0.617213368f)
        };

        public float LargeShipMaxSpeed = 100;
        public float SmallShipMaxSpeed = 100;

		public List<EnvironmentalParticleSettings> EnvironmentalParticles = new List<EnvironmentalParticleSettings>();

        private float m_largeShipMaxAngularSpeed = 18000;
        private float m_smallShipMaxAngularSpeed = 36000;
        private float m_largeShipMaxAngularSpeedInRadians = MathHelper.ToRadians(18000);
        private float m_smallShipMaxAngularSpeedInRadians = MathHelper.ToRadians(36000);

        public float LargeShipMaxAngularSpeed
        {
            get { return m_largeShipMaxAngularSpeed; }
            private set
            {
                m_largeShipMaxAngularSpeed = value;
                m_largeShipMaxAngularSpeedInRadians = MathHelper.ToRadians(m_largeShipMaxAngularSpeed);
            }
        }
        public float SmallShipMaxAngularSpeed
        {
            get { return m_smallShipMaxAngularSpeed; }
            private set
            {
                m_smallShipMaxAngularSpeed = value;
                m_smallShipMaxAngularSpeedInRadians = MathHelper.ToRadians(m_smallShipMaxAngularSpeed);
            }
        }
        public float LargeShipMaxAngularSpeedInRadians
        {
            get { return m_largeShipMaxAngularSpeedInRadians; }
        }
        public float SmallShipMaxAngularSpeedInRadians
        {
            get { return m_smallShipMaxAngularSpeedInRadians; }
        }

        static MyEnvironmentDefinition m_defaults = new MyEnvironmentDefinition();
        const float DELTA = 0.001f;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyObjectBuilder_EnvironmentDefinition objBuilder = (MyObjectBuilder_EnvironmentDefinition)builder;

            BackgroundTexture = objBuilder.EnvironmentTexture;
            BackgroundOrientation = new MyOrientation(
                MathHelper.ToRadians(objBuilder.EnvironmentOrientation.Yaw),
                MathHelper.ToRadians(objBuilder.EnvironmentOrientation.Pitch),
                MathHelper.ToRadians(objBuilder.EnvironmentOrientation.Roll));

            SmallShipMaxSpeed = objBuilder.SmallShipMaxSpeed;
            LargeShipMaxSpeed = objBuilder.LargeShipMaxSpeed;
			EnvironmentalParticles = objBuilder.EnvironmentalParticles;
            SmallShipMaxAngularSpeed = objBuilder.SmallShipMaxAngularSpeed;
            LargeShipMaxAngularSpeed = objBuilder.LargeShipMaxAngularSpeed;
            FogProperties.Deserialize(objBuilder);
            SunProperties.Deserialize(objBuilder);
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var result = new MyObjectBuilder_EnvironmentDefinition()
            {
                EnvironmentTexture = this.BackgroundTexture,
                SmallShipMaxSpeed = this.SmallShipMaxSpeed,
                LargeShipMaxSpeed = this.LargeShipMaxSpeed,
				EnvironmentalParticles = this.EnvironmentalParticles,
                SmallShipMaxAngularSpeed = this.m_smallShipMaxAngularSpeed,
                LargeShipMaxAngularSpeed = this.m_largeShipMaxAngularSpeed,
                EnvironmentOrientation = new MyOrientation(
                    MathHelper.ToDegrees(BackgroundOrientation.Yaw),
                    MathHelper.ToDegrees(BackgroundOrientation.Pitch),
                    MathHelper.ToDegrees(BackgroundOrientation.Roll)),
            };
            FogProperties.Serialize(result);
            SunProperties.Serialize(result);
            return result;
        }

		#region Merge

		public void Merge(MyEnvironmentDefinition src)
		{
			if (src.BackgroundTexture != m_defaults.BackgroundTexture)
			{
				BackgroundTexture = src.BackgroundTexture;
			}

			if (Math.Abs(src.BackgroundOrientation.Yaw - m_defaults.BackgroundOrientation.Yaw) > DELTA ||
				Math.Abs(src.BackgroundOrientation.Pitch - m_defaults.BackgroundOrientation.Pitch) > DELTA ||
				Math.Abs(src.BackgroundOrientation.Roll - m_defaults.BackgroundOrientation.Roll) > DELTA)
			{
				BackgroundOrientation = src.BackgroundOrientation;
			}

			if (src.DistanceToSun != m_defaults.DistanceToSun)
			{
				DistanceToSun = src.DistanceToSun;
			}

			MergeSunProperties(src);
			MergeFogProperties(src);

			if(!src.EnvironmentalParticles.Equals(m_defaults.EnvironmentalParticles))
			{
				foreach(var particleEffect in src.EnvironmentalParticles)
				{
					if (EnvironmentalParticles.Contains(particleEffect))
						continue;
					EnvironmentalParticles.Add(particleEffect);
				}
			}
				
			

			if (src.LargeShipMaxSpeed != m_defaults.LargeShipMaxSpeed)
			{
				LargeShipMaxSpeed = src.LargeShipMaxSpeed;
			}
			if (src.SmallShipMaxSpeed != m_defaults.SmallShipMaxSpeed)
			{
				SmallShipMaxSpeed = src.SmallShipMaxSpeed;
			}
			if (src.m_smallShipMaxAngularSpeed != m_defaults.m_smallShipMaxAngularSpeed)
			{
				SmallShipMaxAngularSpeed = src.m_smallShipMaxAngularSpeed;
			}
			if (src.m_largeShipMaxAngularSpeed != m_defaults.m_largeShipMaxAngularSpeed)
			{
				LargeShipMaxAngularSpeed = src.m_largeShipMaxAngularSpeed;
			}
		}
		private void MergeSunProperties(MyEnvironmentDefinition src)
		{
			if (src.SunProperties.AmbientColor != m_defaults.SunProperties.AmbientColor)
			{
				SunProperties.AmbientColor = src.SunProperties.AmbientColor;
			}

			if (src.SunProperties.AmbientMultiplier != m_defaults.SunProperties.AmbientMultiplier)
			{
				SunProperties.AmbientMultiplier = src.SunProperties.AmbientMultiplier;
			}

			if (src.SunProperties.BackgroundColor != m_defaults.SunProperties.BackgroundColor)
			{
				SunProperties.BackgroundColor = src.SunProperties.BackgroundColor;
			}

			if (src.SunProperties.BackSunDiffuse != m_defaults.SunProperties.BackSunDiffuse)
			{
				SunProperties.BackSunDiffuse = src.SunProperties.BackSunDiffuse;
			}

			if (src.SunProperties.BackSunIntensity != m_defaults.SunProperties.BackSunIntensity)
			{
				SunProperties.BackSunIntensity = src.SunProperties.BackSunIntensity;
			}

			if (src.SunProperties.EnvironmentAmbientIntensity != m_defaults.SunProperties.EnvironmentAmbientIntensity)
			{
				SunProperties.EnvironmentAmbientIntensity = src.SunProperties.EnvironmentAmbientIntensity;
			}

			if (src.SunProperties.SunDiffuse != m_defaults.SunProperties.SunDiffuse)
			{
				SunProperties.SunDiffuse = src.SunProperties.SunDiffuse;
			}

			if (src.SunProperties.SunIntensity != m_defaults.SunProperties.SunIntensity)
			{
				SunProperties.SunIntensity = src.SunProperties.SunIntensity;
			}

			if (src.SunProperties.SunMaterial != m_defaults.SunProperties.SunMaterial)
			{
				SunProperties.SunMaterial = src.SunProperties.SunMaterial;
			}

			if (src.SunProperties.SunSizeMultiplier != m_defaults.SunProperties.SunSizeMultiplier)
			{
				SunProperties.SunSizeMultiplier = src.SunProperties.SunSizeMultiplier;
			}

			if (src.SunProperties.SunSpecular != m_defaults.SunProperties.SunSpecular)
			{
				SunProperties.SunSpecular = src.SunProperties.SunSpecular;
			}

			if (src.SunProperties.SunDirectionNormalized != m_defaults.SunProperties.SunDirectionNormalized)
			{
				SunProperties.SunDirectionNormalized = src.SunProperties.SunDirectionNormalized;
			}
		}
		private void MergeFogProperties(MyEnvironmentDefinition src)
		{
			if (src.FogProperties.EnableFog != m_defaults.FogProperties.EnableFog)
			{
				FogProperties.EnableFog = src.FogProperties.EnableFog;
			}

			if (src.FogProperties.FogNear != m_defaults.FogProperties.FogNear)
			{
				FogProperties.FogNear = src.FogProperties.FogNear;
			}

			if (src.FogProperties.FogFar != m_defaults.FogProperties.FogFar)
			{
				FogProperties.FogFar = src.FogProperties.FogFar;
			}

			if (src.FogProperties.FogMultiplier != m_defaults.FogProperties.FogMultiplier)
			{
				FogProperties.FogMultiplier = src.FogProperties.FogMultiplier;
			}

			if (src.FogProperties.FogBacklightMultiplier != m_defaults.FogProperties.FogBacklightMultiplier)
			{
				FogProperties.FogBacklightMultiplier = src.FogProperties.FogBacklightMultiplier;
			}

			if (src.FogProperties.FogColor != m_defaults.FogProperties.FogColor)
			{
				FogProperties.FogColor = src.FogProperties.FogColor;
			}

            if (src.FogProperties.FogDensity != m_defaults.FogProperties.FogDensity)
            {
                FogProperties.FogDensity = src.FogProperties.FogDensity;
            }
		}
		#endregion
    }
}
