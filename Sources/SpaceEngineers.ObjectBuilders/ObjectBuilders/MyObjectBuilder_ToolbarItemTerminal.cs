using VRageMath;
using ProtoBuf;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.Game;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public abstract class MyObjectBuilder_ToolbarItemTerminal : MyObjectBuilder_ToolbarItem
    {
        [ProtoMember]
        [Nullable]
        public string Action;

        [ProtoMember]
        public List<MyObjectBuilder_ToolbarItemActionParameter> Parameters = new List<MyObjectBuilder_ToolbarItemActionParameter>();

        public bool ShouldSerializeParameters()
        {
            return Parameters != null && Parameters.Count > 0;
        }
    }
}
