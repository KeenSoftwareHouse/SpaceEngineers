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
        [ProtoMember]
        public SerializableVector3 SunDirection;

        [ProtoMember, ModdableContentFile("dds")]
        public string EnvironmentTexture;

        [ProtoMember]
        public MyOrientation EnvironmentOrientation;

        [ProtoMember]
        public bool EnableFog;

        [ProtoMember]
        public float FogNear;

        [ProtoMember]
        public float FogFar;

        [ProtoMember]
        public float FogMultiplier;

        [ProtoMember]
        public float FogBacklightMultiplier;

        [ProtoMember]
        public float FogDensity;

        [ProtoMember]
        public SerializableVector3 FogColor;

        [ProtoMember]
        public SerializableVector3 SunDiffuse = new SerializableVector3(200 / 255.0f, 200 / 255.0f, 200 / 255.0f);

        [ProtoMember]
        public float SunIntensity = 1.456f;

        [ProtoMember]
        public SerializableVector3 SunSpecular = new SerializableVector3(200 / 255.0f, 200 / 255.0f, 200 / 255.0f);

        [ProtoMember]
        public SerializableVector3 BackLightDiffuse = new SerializableVector3(200 / 255.0f, 200 / 255.0f, 200 / 255.0f);

        [ProtoMember]
        public float BackLightIntensity = 0.239f;

        [ProtoMember]
        public SerializableVector3 AmbientColor = new SerializableVector3(36 / 255.0f, 36 / 255.0f, 36 / 255.0f);

        [ProtoMember]
        public float AmbientMultiplier = 0.969f;

        [ProtoMember]
        public float EnvironmentAmbientIntensity = 0.500f;

        [ProtoMember]
        public SerializableVector3 BackgroundColor = new SerializableVector3(1, 1, 1);

        [ProtoMember]
        public string SunMaterial = "SunDisk";

        [ProtoMember]
        public float SunSizeMultiplier = 200;

        [ProtoMember]
        public float SmallShipMaxSpeed = 100;

        [ProtoMember]
        public float LargeShipMaxSpeed = 100;

        [ProtoMember]
        public float SmallShipMaxAngularSpeed = 36000;

        [ProtoMember]
        public float LargeShipMaxAngularSpeed = 18000;
    }
}
