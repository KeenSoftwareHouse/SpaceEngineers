using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AirtightDoorGenericDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float PowerConsumptionIdle;
        [ProtoMember(2)]
        public float PowerConsumptionMoving;
        [ProtoMember(3)]
        public float OpeningSpeed;
        [ProtoMember(4)]
        public string Sound;
        [ProtoMember(5)]
        public float SubpartMovementDistance=2.5f;
    }
}
