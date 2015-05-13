using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentSettings : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public float SunAzimuth;

        [ProtoMember(2)]
        public float SunElevation;

        [ProtoMember(3)]
        public float SunIntensity;

        [ProtoMember(4)]
        public float FogMultiplier;

        [ProtoMember(5)]
        public float FogDensity;

        [ProtoMember(6)]
        public SerializableVector3 FogColor;
    }
}
