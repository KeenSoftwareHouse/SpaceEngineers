using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]    
    public class MyObjectBuilder_InventoryDefinition 
    {
        [ProtoMember]
        public float InventoryVolume = 0.4f;

        [ProtoMember]
        public float InventoryMass = float.MaxValue;

        [ProtoMember]
        public float InventorySizeX = 1.2f;

        [ProtoMember]
        public float InventorySizeY = 0.7f;

        [ProtoMember]
        public float InventorySizeZ = 0.4f;
    }
}
