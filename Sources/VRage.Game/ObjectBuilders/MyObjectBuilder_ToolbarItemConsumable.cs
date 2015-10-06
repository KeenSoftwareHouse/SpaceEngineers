using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemConsumable : MyObjectBuilder_ToolbarItemDefinition
    {
        public SerializableDefinitionId defId
        {
            get { return base.DefinitionId; }
            set { base.DefinitionId = value; }
        }
        public bool ShouldSerializedefId() { return false; }
    }
}
