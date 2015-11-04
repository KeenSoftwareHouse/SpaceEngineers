using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using VRage;
using VRage.ObjectBuilders;
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

        [ProtoContract]
        public class MyMultiBlockPart
        {
            [ProtoMember]
            public SerializableDefinitionId MultiBlockDefinition;
            [ProtoMember]
            public int MultiBlockId;
        }

        [ProtoMember]
        public List<SerializableDefinitionId> BlockDefinitions = new List<SerializableDefinitionId>();

        [ProtoMember]
        public List<ShapeB> Shapes = new List<ShapeB>();

        [ProtoMember]
        public List<SerializableBlockOrientation> BlockOrientations = new List<SerializableBlockOrientation>();

        public bool CreatingFracturedBlock = false;

        [ProtoMember]
        public List<MyMultiBlockPart> MultiBlocks = new List<MyMultiBlockPart>();
    }
}
