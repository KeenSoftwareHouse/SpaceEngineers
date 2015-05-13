using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FracturedPiece : MyObjectBuilder_EntityBase
    {
        [ProtoContract]
        public struct Shape
        {
            [ProtoMember(1)]
            public string Name;
            [ProtoMember(2)]
            public SerializableQuaternion Orientation;// = Quaternion.Identity;
            [ProtoMember(3), DefaultValue(false)]
            public bool Fixed;
        }
        [ProtoMember(1)]
        public List<SerializableDefinitionId> BlockDefinitions = new List<SerializableDefinitionId>();

        [ProtoMember(2)]
        public List<Shape> Shapes = new List<Shape>();
    }
}
