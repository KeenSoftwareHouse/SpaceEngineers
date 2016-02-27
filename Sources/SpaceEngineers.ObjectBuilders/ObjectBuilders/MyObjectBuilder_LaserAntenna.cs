using ProtoBuf;
using VRage.Game;
using VRageMath;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LaserAntenna : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public byte State;
        [ProtoMember]
        public long? targetEntityId;
        [ProtoMember]
        public Vector3D? gpsTarget;
        [ProtoMember]
        public string gpsTargetName;
        [ProtoMember]
        public Vector3D LastTargetPosition;
        [ProtoMember]
        public Vector2 HeadRotation;
        [ProtoMember]
        public string LastKnownTargetName;
    }
}
