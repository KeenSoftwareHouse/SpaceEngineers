using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Radar : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public float DetectionRadius;

        [ProtoMember(3)] 
        public float MinimumSize;

        [ProtoMember(4)] 
        public float MaximumSize;

        [ProtoMember(5)] 
        public int TrackingLimit;

        [ProtoMember(2)]
        public bool BroadcastUsingAntennas;
    }
}
