using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SolarPanelDefinition : MyObjectBuilder_PowerProducerDefinition
    {
        [ProtoMember(1)]
        public Vector3 PanelOrientation = new Vector3(0, 0, 0);

        [ProtoMember(2)]
        public bool TwoSidedPanel = true;

        [ProtoMember(3)]
        public float PanelOffset = 1;
    }
}
