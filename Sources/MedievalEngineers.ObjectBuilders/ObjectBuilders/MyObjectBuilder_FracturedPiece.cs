using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System.Collections.Generic;
using System.ComponentModel;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_FracturedPiece : MyObjectBuilder_EntityBase
    {
        [ProtoContract]
        public struct Shape
        {
            [ProtoMember]
            public string Name;
            [ProtoMember]
            public SerializableQuaternion Orientation;// = Quaternion.Identity;
            [ProtoMember, DefaultValue(false)]
            public bool Fixed;
        }
        [ProtoMember]
        public List<SerializableDefinitionId> BlockDefinitions = new List<SerializableDefinitionId>();

        [ProtoMember]
        public List<Shape> Shapes = new List<Shape>();
    }
}
