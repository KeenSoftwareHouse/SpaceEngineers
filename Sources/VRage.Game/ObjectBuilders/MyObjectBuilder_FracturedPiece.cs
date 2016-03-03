using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRage;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
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
