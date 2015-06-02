using VRageMath;
using ProtoBuf;
using VRage.ObjectBuilders;
using System.Collections.Generic;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_ToolbarItemTerminal : MyObjectBuilder_ToolbarItem
    {
        [ProtoMember]
        public string Action;

        [ProtoMember]
        public List<MyObjectBuilder_ToolbarItemActionParameter> Parameters = new List<MyObjectBuilder_ToolbarItemActionParameter>();

        public bool ShouldSerializeParameters()
        {
            return Parameters != null && Parameters.Count > 0;
        }
    }
}
