using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ContainerDefinition : MyObjectBuilder_DefinitionBase
    {       
        [ProtoContract]
        public class DefaultComponentBuilder
        {
            [ProtoMember]
            [XmlAttribute("BuilderType"), DefaultValue(null)]
            public string BuilderType = null;

            [ProtoMember]
            [XmlAttribute("InstanceType"), DefaultValue(null)]
            public string InstanceType = null;

            [ProtoMember]
            [XmlAttribute("ForceCreate"),DefaultValue(false)]
            public bool ForceCreate = false;

            [ProtoMember]
            [XmlAttribute("SubtypeId"), DefaultValue(null)]
            public string SubtypeId = null;
        }

        [ProtoMember]
        [XmlArrayItem("Component")]
        public DefaultComponentBuilder[] DefaultComponents;

        [ProtoMember, DefaultValue(null)]
        public EntityFlags? Flags = null;
    }
}
