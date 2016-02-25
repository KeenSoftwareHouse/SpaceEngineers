using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LaserAntennaDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;
        [ProtoMember]
        public float PowerInputIdle=0.001f;
        [ProtoMember]
        public float PowerInputTurning=0.01f;
        [ProtoMember]
        public float PowerInputLasing=2f;
        [ProtoMember]
        public float RotationRate = MathHelper.Pi / 20000.0f;
        [ProtoMember]
        public float MaxRange = 40000;
        [ProtoMember]
        public bool RequireLineOfSight = true;
        [ProtoMember]
        public int MinElevationDegrees = -180;
        [ProtoMember]
        public int MaxElevationDegrees = 180;
        [ProtoMember]
        public int MinAzimuthDegrees = -180;
        [ProtoMember]
        public int MaxAzimuthDegrees = 180;
    }
}
