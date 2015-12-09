using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryBase : MyObjectBuilder_ComponentBase
    {
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public string InventoryId = null;
    }
}
