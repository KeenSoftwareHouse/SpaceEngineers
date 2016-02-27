using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Meteor : MyObjectBuilder_EntityBase
    {
        [ProtoMember]
        public MyObjectBuilder_InventoryItem Item;

        [ProtoMember]
        public Vector3 LinearVelocity;

        [ProtoMember]
        public Vector3 AngularVelocity;

        [ProtoMember]
        public float Integrity = 100;
    }
}
