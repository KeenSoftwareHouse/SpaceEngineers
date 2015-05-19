using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LightingBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public SerializableBounds LightRadius = new SerializableBounds(2, 10, 2.8f);

        [ProtoMember]
        public SerializableBounds LightFalloff = new SerializableBounds(1, 3, 1.5f);

        [ProtoMember]
        public SerializableBounds LightIntensity = new SerializableBounds(0.5f, 5, 2);

        [ProtoMember]
        public float RequiredPowerInput = 0.001f;

        [ProtoMember]
        public string LightGlare = "GlareLsLight";

        [ProtoMember]
        public SerializableBounds LightBlinkIntervalSeconds = new SerializableBounds(0.0f, 30.0f, 0);

        [ProtoMember]
        public SerializableBounds LightBlinkLenght = new SerializableBounds(0.0f, 100.0f, 10.0f);

        [ProtoMember]
        public SerializableBounds LightBlinkOffset = new SerializableBounds(0.0f, 100.0f, 0);

        [ProtoMember]
        public bool HasPhysics = false;
    }
}
