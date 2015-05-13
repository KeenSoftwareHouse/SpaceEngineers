using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using System.ComponentModel;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public SerializableVector3 SunDirection;

        [ProtoMember(2), ModdableContentFile("dds")]
        public string EnvironmentTexture;

        [ProtoMember(3)]
        public MyOrientation EnvironmentOrientation;

        [ProtoMember(4)]
        public bool EnableFog;

        [ProtoMember(5)]
        public float FogNear;

        [ProtoMember(6)]
        public float FogFar;

        [ProtoMember(7)]
        public float FogMultiplier;

        [ProtoMember(8)]
        public float FogBacklightMultiplier;

        [ProtoMember(9)]
        public float FogDensity;

        [ProtoMember(10)]
        public SerializableVector3 FogColor;

        [ProtoMember(11)]
        public SerializableVector3 SunDiffuse = new SerializableVector3(200 / 255.0f, 200 / 255.0f, 200 / 255.0f);

        [ProtoMember(12)]
        public float SunIntensity = 1.456f;

        [ProtoMember(13)]
        public SerializableVector3 SunSpecular = new SerializableVector3(200 / 255.0f, 200 / 255.0f, 200 / 255.0f);

        [ProtoMember(14)]
        public SerializableVector3 BackLightDiffuse = new SerializableVector3(200 / 255.0f, 200 / 255.0f, 200 / 255.0f);

        [ProtoMember(15)]
        public float BackLightIntensity = 0.239f;

        [ProtoMember(16)]
        public SerializableVector3 AmbientColor = new SerializableVector3(36 / 255.0f, 36 / 255.0f, 36 / 255.0f);

        [ProtoMember(17)]
        public float AmbientMultiplier = 0.969f;

        [ProtoMember(18)]
        public float EnvironmentAmbientIntensity = 0.500f;

        [ProtoMember(19)]
        public SerializableVector3 BackgroundColor = new SerializableVector3(1, 1, 1);

        [ProtoMember(20)]
        public string SunMaterial = "SunDisk";

        [ProtoMember(21)]
        public float SunSizeMultiplier = 200;

        [ProtoMember(22)]
        public float SmallShipMaxSpeed = 100;

        [ProtoMember(23)]
        public float LargeShipMaxSpeed = 100;

        [ProtoMember(24)]
        public float SmallShipMaxAngularSpeed = 36000;

        [ProtoMember(25)]
        public float LargeShipMaxAngularSpeed = 18000;
    }
}
