using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OxygenGeneratorDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember(1)]
        public float IceToOxygenRatio;
        [ProtoMember(2)]
        public float OxygenProductionPerSecond;
        [ProtoMember(3)]
        public string IdleSound;
        [ProtoMember(4)]
        public string GenerateSound;
    }
}
