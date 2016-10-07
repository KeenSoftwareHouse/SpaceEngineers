using ProtoBuf;
using VRageMath;

namespace VRageRender.Messages
{
    [ProtoContract]
    public struct MyAtmosphereSettings
    {
        [ProtoMember]
        public Vector3 RayleighScattering;
        [ProtoMember]
        public float MieScattering;
        [ProtoMember]
        public Vector3 MieColorScattering;
        // This is RayleighHeightSurface
        [ProtoMember]
        public float RayleighHeight;
        [ProtoMember]
        public float RayleighHeightSpace;
        [ProtoMember]
        public float RayleighTransitionModifier;
        [ProtoMember]
        public float MieHeight;
        [ProtoMember]
        public float MieG;
        [ProtoMember]
        public float Intensity;
        [ProtoMember]
        public float FogIntensity;
        [ProtoMember]
        public float SeaLevelModifier;
        [ProtoMember]
        public float AtmosphereTopModifier;

        [ProtoMember]
        public float Scale;

        public static MyAtmosphereSettings Defaults()
        {
            MyAtmosphereSettings settings;
            settings.RayleighScattering = new Vector3(20f, 7.5f, 10f);
            settings.MieScattering = 50f;
            settings.MieColorScattering = new Vector3(50.0f, 50.0f, 50.0f);
            settings.RayleighHeight = 10f;
            settings.RayleighHeightSpace = 10f;
            settings.RayleighTransitionModifier = 1f;
            settings.MieHeight = 50f;
            settings.MieG = 0.9998f;
            settings.Intensity = 1.0f;
            settings.FogIntensity = 0.0f;
            settings.SeaLevelModifier = 1.0f;
            settings.AtmosphereTopModifier = 1.0f;
            settings.Scale = 0.5f;
            return settings;
        }
    }

    public class MyRenderMessageUpdateAtmosphereSettings : MyRenderMessageBase
    {
        public uint ID;
        public MyAtmosphereSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateAtmosphereSettings; } }
    }
}
