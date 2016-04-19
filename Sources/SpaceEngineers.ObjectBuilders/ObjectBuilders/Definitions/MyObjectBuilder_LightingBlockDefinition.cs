using ProtoBuf;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LightingBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public SerializableBounds LightRadius = new SerializableBounds(2, 10, 2.8f);

        [ProtoMember]
        public SerializableBounds LightReflectorRadius = new SerializableBounds(2, 120, 120.0f);

        [ProtoMember]
        public SerializableBounds LightFalloff = new SerializableBounds(1, 3, 1.5f);

        [ProtoMember]
        public SerializableBounds LightIntensity = new SerializableBounds(0.5f, 5, 2);

	    [ProtoMember]
	    public string ResourceSinkGroup;

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
