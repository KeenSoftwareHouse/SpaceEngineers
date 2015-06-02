using VRage.ObjectBuilders;
using System.Xml.Serialization;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.VRageData
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TransparentMaterials : MyObjectBuilder_Base
    {
        [XmlArrayItem("TransparentMaterial")]
        [ProtoMember]
        public MyObjectBuilder_TransparentMaterial[] Materials;
    }
}
