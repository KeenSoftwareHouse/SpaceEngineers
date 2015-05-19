using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FracturedBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoContract]
        public struct ShapeB
        {
            [ProtoMember]
            public string Name;
            [ProtoMember]
            public SerializableQuaternion Orientation;// = Quaternion.Identity;
            [ProtoMember, DefaultValue(false)]
            public bool Fixed;
        }
        [ProtoMember]
        public List<SerializableDefinitionId> BlockDefinitions = new List<Common.ObjectBuilders.Definitions.SerializableDefinitionId>();

        [ProtoMember]
        public List<ShapeB> Shapes = new List<ShapeB>();

        [ProtoMember]
        public List<SerializableBlockOrientation> BlockOrientations = new List<SerializableBlockOrientation>();
    }
}
