using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlockNavigationDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class Triangle
        {
            [ProtoMember, XmlArrayItem("Point")]
            public SerializableVector3[] Points;
        }

        [ProtoMember, XmlArrayItem("Triangle")]
        public Triangle[] Triangles;

        [ProtoMember]
        public bool NoEntry = false;

        [ProtoMember]
        public SerializableVector3I Size = new SerializableVector3I(1, 1, 1);

        [ProtoMember]
        public SerializableVector3I Center = new SerializableVector3I(0, 0, 0);
    }
}
