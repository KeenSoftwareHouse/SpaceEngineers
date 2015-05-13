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
            [ProtoMember(1)]
            public string Name;
            [ProtoMember(2)]
            public SerializableQuaternion Orientation;// = Quaternion.Identity;
            [ProtoMember(3), DefaultValue(false)]
            public bool Fixed;
        }
        [ProtoMember(1)]
        public List<SerializableDefinitionId> BlockDefinitions = new List<Common.ObjectBuilders.Definitions.SerializableDefinitionId>();

        [ProtoMember(2)]
        public List<ShapeB> Shapes = new List<ShapeB>();

        [ProtoMember(3)]
        public List<SerializableBlockOrientation> BlockOrientations = new List<SerializableBlockOrientation>();
    }
}
