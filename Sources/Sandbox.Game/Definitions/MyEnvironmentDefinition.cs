using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;
using Defaults = VRage.Game.MyObjectBuilder_EnvironmentDefinition.Defaults;
using EnvironmentalParticleSettings = VRage.Game.MyObjectBuilder_EnvironmentDefinition.EnvironmentalParticleSettings;

namespace Sandbox.Definitions
{
    /// <summary>
    /// Global (environment) mergeable definitions
    /// </summary>
    [MyDefinitionType(typeof(MyObjectBuilder_EnvironmentDefinition), typeof(Postprocessor))]
    public class MyEnvironmentDefinition : MyDefinitionBase
    {
        public MyEnvironmentDefinition()
        {
            ShadowSettings = new MyShadowsSettings();
            NewPipelineSettings = new MyNewPipelineSettings();
            UserLoddingSettings = new MyNewLoddingSettings();
            LowLoddingSettings = new MyNewLoddingSettings();
            MediumLoddingSettings = new MyNewLoddingSettings();
            HighLoddingSettings = new MyNewLoddingSettings();
            MaterialsSettings = new MyMaterialsSettings();
        }

        public MyFogProperties FogProperties = MyFogProperties.Default;
        public MySunProperties SunProperties = MySunProperties.Default;
        public MyPostprocessSettings PostProcessSettings = MyPostprocessSettings.Default;
        public MySSAOSettings SSAOSettings = MySSAOSettings.Default;
        public MyHBAOData HBAOSettings = MyHBAOData.Default;
        public MyShadowsSettings ShadowSettings { get; private set; }
        public MyNewPipelineSettings NewPipelineSettings { get; private set; }
        public MyNewLoddingSettings UserLoddingSettings { get; private set; }
        public MyNewLoddingSettings LowLoddingSettings { get; private set; }
        public MyNewLoddingSettings MediumLoddingSettings { get; private set; }
        public MyNewLoddingSettings HighLoddingSettings { get; private set; }
        public MyMaterialsSettings MaterialsSettings { get; private set; }

        public float LargeShipMaxSpeed = Defaults.LargeShipMaxSpeed;
        public float SmallShipMaxSpeed = Defaults.SmallShipMaxSpeed;
        public Color ContourHighlightColor = Defaults.ContourHighlightColor;
        public float ContourHighlightThickness = Defaults.ContourHighlightThickness;
        public float HighlightPulseInSeconds = Defaults.HighlightPulseInSeconds;

        public List<EnvironmentalParticleSettings> EnvironmentalParticles = new List<EnvironmentalParticleSettings>();

        private float m_largeShipMaxAngularSpeed = Defaults.LargeShipMaxAngularSpeed;
        private float m_smallShipMaxAngularSpeed = Defaults.SmallShipMaxAngularSpeed;
        private float m_largeShipMaxAngularSpeedInRadians = MathHelper.ToRadians(Defaults.LargeShipMaxAngularSpeed);
        private float m_smallShipMaxAngularSpeedInRadians = MathHelper.ToRadians(Defaults.SmallShipMaxAngularSpeed);

        public string EnvironmentTexture = Defaults.EnvironmentTexture;
        public MyOrientation EnvironmentOrientation = Defaults.EnvironmentOrientation;

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

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyObjectBuilder_EnvironmentDefinition objBuilder = (MyObjectBuilder_EnvironmentDefinition)builder;
            FogProperties = objBuilder.FogProperties;
            SunProperties = objBuilder.SunProperties;
            PostProcessSettings = objBuilder.PostProcessSettings;
            SSAOSettings = objBuilder.SSAOSettings;
            HBAOSettings = objBuilder.HBAOSettings;
            ShadowSettings.CopyFrom(objBuilder.ShadowSettings);
            NewPipelineSettings.CopyFrom(objBuilder.NewPipelineSettings);
            UserLoddingSettings.CopyFrom(objBuilder.UserLoddingSettings);
            LowLoddingSettings.CopyFrom(objBuilder.LowLoddingSettings);
            MediumLoddingSettings.CopyFrom(objBuilder.MediumLoddingSettings);
            HighLoddingSettings.CopyFrom(objBuilder.HighLoddingSettings);
            MaterialsSettings.CopyFrom(objBuilder.MaterialsSettings);
            SmallShipMaxSpeed = objBuilder.SmallShipMaxSpeed;
            LargeShipMaxSpeed = objBuilder.LargeShipMaxSpeed;
            SmallShipMaxAngularSpeed = objBuilder.SmallShipMaxAngularSpeed;
            LargeShipMaxAngularSpeed = objBuilder.LargeShipMaxAngularSpeed;
            ContourHighlightColor = new Color(objBuilder.ContourHighlightColor);
            ContourHighlightThickness = objBuilder.ContourHighlightThickness;
            HighlightPulseInSeconds = objBuilder.HighlightPulseInSeconds;
            EnvironmentTexture = objBuilder.EnvironmentTexture;
            EnvironmentOrientation = objBuilder.EnvironmentOrientation;
            EnvironmentalParticles = objBuilder.EnvironmentalParticles;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var result = new MyObjectBuilder_EnvironmentDefinition();
            result.Id = Id;
            result.FogProperties = FogProperties;
            result.SunProperties = SunProperties;
            result.PostProcessSettings = PostProcessSettings;
            result.SSAOSettings = SSAOSettings;
            result.HBAOSettings = HBAOSettings;
            result.ShadowSettings.CopyFrom(ShadowSettings);
            result.NewPipelineSettings.CopyFrom(NewPipelineSettings);
            result.UserLoddingSettings.CopyFrom(UserLoddingSettings);
            result.LowLoddingSettings.CopyFrom(LowLoddingSettings);
            result.MediumLoddingSettings.CopyFrom(MediumLoddingSettings);
            result.HighLoddingSettings.CopyFrom(HighLoddingSettings);
            result.MaterialsSettings.CopyFrom(MaterialsSettings);
            result.SmallShipMaxSpeed = this.SmallShipMaxSpeed;
            result.LargeShipMaxSpeed = this.LargeShipMaxSpeed;
            result.SmallShipMaxAngularSpeed = this.SmallShipMaxAngularSpeed;
            result.LargeShipMaxAngularSpeed = this.LargeShipMaxAngularSpeed;
            result.ContourHighlightColor = this.ContourHighlightColor.ToVector4();
            result.ContourHighlightThickness = this.ContourHighlightThickness;
            result.HighlightPulseInSeconds = this.HighlightPulseInSeconds;
            result.EnvironmentTexture = this.EnvironmentTexture;
            result.EnvironmentOrientation = this.EnvironmentOrientation;
            result.EnvironmentalParticles = this.EnvironmentalParticles;

            return result;
        }

        public void Merge(MyEnvironmentDefinition src)
        {
            MyEnvironmentDefinition defaults = new MyEnvironmentDefinition();

            // TODO: Find better way to avoid MyDefinitionBase fields to be merged
            defaults.Id = src.Id;
            defaults.DisplayNameEnum = src.DisplayNameEnum;
            defaults.DescriptionEnum = src.DescriptionEnum;
            defaults.DisplayNameString = src.DisplayNameString;
            defaults.DescriptionString = src.DescriptionString;
            defaults.Icons = src.Icons;
            defaults.Enabled = src.Enabled;
            defaults.Public = src.Public;
            defaults.AvailableInSurvival = src.AvailableInSurvival;
            defaults.Context = src.Context;

            MyMergeHelper.Merge(this, src, defaults);
        }

        class Postprocessor : MyDefinitionPostprocessor
        {
            public override void AfterLoaded(ref Bundle definitions)
            { }

            public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
            { }

            public override void OverrideBy(ref Bundle currentDefinitions, ref Bundle overrideBySet)
            {
                foreach (var def in overrideBySet.Definitions)
                {
                    if (def.Value.Enabled)
                    {
                        MyDefinitionBase envDef;
                        if (currentDefinitions.Definitions.TryGetValue(def.Key, out envDef))
                            ((MyEnvironmentDefinition)envDef).Merge((MyEnvironmentDefinition)def.Value);
                        else
                            currentDefinitions.Definitions.Add(def.Key, def.Value);
                    }
                    else
                        currentDefinitions.Definitions.Remove(def.Key);
                }
            }
        }
    }
}
