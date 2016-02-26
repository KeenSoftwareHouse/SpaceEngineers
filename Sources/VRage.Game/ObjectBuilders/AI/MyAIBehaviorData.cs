using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.AI
{
    [MyObjectBuilderDefinition]
    [ProtoContract]
    public class MyAIBehaviorData : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class CategorizedData
        {
            [ProtoMember]
            public string Category;

            [XmlArrayItem("Action")]
            [ProtoMember]
            public ActionData[] Descriptors;
        }

        [ProtoContract]
        public class ParameterData
        {
            [ProtoMember, XmlAttribute]
            public string Name;

            [ProtoMember, XmlAttribute]
            public string TypeFullName;

            [ProtoMember, XmlAttribute]
            public MyMemoryParameterType MemType;
        }

        [ProtoContract]
        public class ActionData
        {
            [ProtoMember, XmlAttribute]
            public string ActionName;

            [ProtoMember, XmlAttribute]
            public bool ReturnsRunning;

            [XmlArrayItem("Param")]
            [ProtoMember]
            public ParameterData[] Parameters;
        }

        [XmlArrayItem("AICategory")]
        [ProtoMember]
        public CategorizedData[] Entries;
    }
}
