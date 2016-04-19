using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition(LegacyName: "MyObjectBuilder_ToolbarItemConsumable")]
    public class MyObjectBuilder_ToolbarItemUsable : MyObjectBuilder_ToolbarItemDefinition
    {
        public SerializableDefinitionId defId
        {
            get { return base.DefinitionId; }
            set { base.DefinitionId = value; }
        }
        public bool ShouldSerializedefId() { return false; }
    }
}
