using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DefinitionsToPreload : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember] 
        [XmlArrayItem("File")]
        public MyObjectBuilder_PreloadFileInfo[] DefinitionFiles;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PreloadFileInfo : MyObjectBuilder_Base
    {
        [ProtoMember]
        public string Name;

        [ProtoMember, DefaultValue(false)]
        public bool LoadOnDedicated;
    }
}
