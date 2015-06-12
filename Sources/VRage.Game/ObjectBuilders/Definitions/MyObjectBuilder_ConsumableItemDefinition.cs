using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConsumableItemDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoMember, DefaultValue(0)]
        public float Food = 0;

        [ProtoMember, DefaultValue(0)]
        public float Stamina = 0;

        [ProtoMember, DefaultValue(0)]
        public float Health = 0;

        [ProtoMember, DefaultValue(0)]
        public float StaminaRegen = 0;

        [ProtoMember, DefaultValue(0)]
        public float StaminaRegenTime = 0;

        [ProtoMember, DefaultValue(0)]
        public float HealthRegen = 0;

        [ProtoMember, DefaultValue(0)]
        public float HealthRegenTime = 0;
    }
}
