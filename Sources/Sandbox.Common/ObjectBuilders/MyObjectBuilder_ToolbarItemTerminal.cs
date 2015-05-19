using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;

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
