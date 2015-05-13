using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AirVentDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float StandbyPowerConsumption;
        [ProtoMember(2)]
        public float OperationalPowerConsumption;
        [ProtoMember(3)]
        public float VentilationCapacityPerSecond;
        
        [ProtoMember(4)]
        public string PressurizeSound;
        [ProtoMember(5)]
        public string DepressurizeSound;
        [ProtoMember(6)]
        public string IdleSound;
    }
}
