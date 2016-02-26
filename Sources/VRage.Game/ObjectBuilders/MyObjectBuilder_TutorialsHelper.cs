using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    public struct MyTutorialDescription
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        [XmlArrayItem("Tutorial")]
        public string[] UnlockedBy;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TutorialsHelper : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlArrayItem("Tutorial")]
        public MyTutorialDescription[] Tutorials;
    }
}