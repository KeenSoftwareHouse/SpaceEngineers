using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipDrillDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float SensorRadius;

        [ProtoMember]
        public float SensorOffset;

        [ProtoMember]
        public Vector3 InventorySize;
    }
}
