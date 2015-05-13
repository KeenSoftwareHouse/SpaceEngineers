using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConveyorSorterDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float PowerInput = 0.001f;

        [ProtoMember(2)]
        public Vector3 InventorySize;
    }
}
