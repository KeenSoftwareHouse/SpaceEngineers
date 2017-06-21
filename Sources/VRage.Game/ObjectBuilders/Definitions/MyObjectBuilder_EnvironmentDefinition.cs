using System.Collections.Generic;
using ProtoBuf;
using VRage.Data;
using VRage.ObjectBuilders;
using VRageMath;
using System.Xml.Serialization;
using VRageRender;
using VRageRender.Messages;

namespace VRage.Game
{
    /// <summary>
    /// Global (environment) mergeable definitions
    /// </summary>
    [MyObjectBuilderDefinition]
    [XmlType("EnvironmentDefinition")]
    public class MyObjectBuilder_EnvironmentDefinition : MyObjectBuilder_DefinitionBase
    {
        [XmlElement(Type = typeof(MyStructXmlSerializer<MyFogProperties>))]
        public MyFogProperties FogProperties = MyFogProperties.Default;

        [XmlElement(Type = typeof(MyStructXmlSerializer<MySunProperties>))]
        public MySunProperties SunProperties = MySunProperties.Default;

        [XmlElement(Type = typeof(MyStructXmlSerializer<MyPostprocessSettings>))]
        public MyPostprocessSettings PostProcessSettings = MyPostprocessSettings.Default;

        [XmlElement(Type = typeof(MyStructXmlSerializer<MySSAOSettings>))]
        public MySSAOSettings SSAOSettings = MySSAOSettings.Default;

        [XmlElement(Type = typeof(MyStructXmlSerializer<MyHBAOData>))]
        public MyHBAOData HBAOSettings = MyHBAOData.Default;

        public MyShadowsSettings ShadowSettings = new MyShadowsSettings();
        public MyNewPipelineSettings NewPipelineSettings = new MyNewPipelineSettings();
        public MyNewLoddingSettings UserLoddingSettings = new MyNewLoddingSettings();
        public MyNewLoddingSettings LowLoddingSettings = new MyNewLoddingSettings();
        public MyNewLoddingSettings MediumLoddingSettings = new MyNewLoddingSettings();
        public MyNewLoddingSettings HighLoddingSettings = new MyNewLoddingSettings();
        public MyMaterialsSettings MaterialsSettings = new MyMaterialsSettings();

        [ProtoContract]
        public struct EnvironmentalParticleSettings
        {
            [ProtoMember]
            public SerializableDefinitionId Id;

            [ProtoMember]
            public string Material;

            [ProtoMember]
            public Vector4 Color;

            [ProtoMember]
            public float MaxSpawnDistance;

            [ProtoMember]
            public float DespawnDistance;

            [ProtoMember]
            public float Density;

            [ProtoMember]
            public int MaxLifeTime;

            [ProtoMember]
            public int MaxParticles;
        }

        [ProtoMember, XmlArrayItem("ParticleType")]
        public List<EnvironmentalParticleSettings> EnvironmentalParticles = new List<EnvironmentalParticleSettings>();

        public float SmallShipMaxSpeed = Defaults.SmallShipMaxSpeed;
        public float LargeShipMaxSpeed = Defaults.LargeShipMaxSpeed;
        public float SmallShipMaxAngularSpeed = Defaults.SmallShipMaxAngularSpeed;
        public float LargeShipMaxAngularSpeed = Defaults.LargeShipMaxAngularSpeed;
        public Vector4 ContourHighlightColor = Defaults.ContourHighlightColor;
        public float ContourHighlightThickness = Defaults.ContourHighlightThickness;
        public float HighlightPulseInSeconds = Defaults.HighlightPulseInSeconds;

        [ModdableContentFile("dds")]
        public string EnvironmentTexture = Defaults.EnvironmentTexture;
        public MyOrientation EnvironmentOrientation = Defaults.EnvironmentOrientation;

        public static class Defaults
        {
            public const float SmallShipMaxSpeed = 100;
            public const float LargeShipMaxSpeed = 100;
            public const float SmallShipMaxAngularSpeed = 36000;
            public const float LargeShipMaxAngularSpeed = 18000;
            public static readonly Vector4 ContourHighlightColor = new Vector4(1.0f, 1.0f, 0.0f, 0.05f);
            public const float ContourHighlightThickness = 5;
            public const float HighlightPulseInSeconds = 0;
            public const string EnvironmentTexture = @"Textures\BackgroundCube\Final\BackgroundCube.dds";
            public static readonly MyOrientation EnvironmentOrientation = new MyOrientation(MathHelper.ToRadians(60.3955536f), MathHelper.ToRadians(-61.1861954f), MathHelper.ToRadians(90.90578f));
        }
    }
}
