using VRageMath;
using ProtoBuf;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.Game;
using VRage.Serialization;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public abstract class MyObjectBuilder_ToolbarItemTerminal : MyObjectBuilder_ToolbarItem
    {
        [ProtoMember]
        [XmlElement("Action")]
        [Nullable]
        public string _Action = null;

        [ProtoMember]
        public List<MyObjectBuilder_ToolbarItemActionParameter> Parameters = new List<MyObjectBuilder_ToolbarItemActionParameter>();

        public bool ShouldSerializeParameters()
        {
            return Parameters != null && Parameters.Count > 0;
        }
    }
}
