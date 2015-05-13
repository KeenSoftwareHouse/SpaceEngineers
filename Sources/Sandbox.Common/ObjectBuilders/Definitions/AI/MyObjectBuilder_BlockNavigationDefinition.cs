using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlockNavigationDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class Triangle
        {
            [ProtoMember(1), XmlArrayItem("Point")]
            public SerializableVector3[] Points;
        }

        [ProtoMember(1), XmlArrayItem("Triangle")]
        public Triangle[] Triangles;

        [ProtoMember(2)]
        public bool NoEntry = false;

        [ProtoMember(3)]
        public SerializableVector3I Size = new SerializableVector3I(1, 1, 1);

        [ProtoMember(4)]
        public SerializableVector3I Center = new SerializableVector3I(0, 0, 0);
    }
}
