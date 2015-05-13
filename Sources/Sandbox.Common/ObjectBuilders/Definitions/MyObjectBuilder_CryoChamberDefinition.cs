using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CryoChamberDefinition : MyObjectBuilder_CockpitDefinition
    {
        [ProtoMember(1)]
        [ModdableContentFile("dds")]
        public string OverlayTexture;

        [ProtoMember(2)]
        public float IdlePowerConsumption = 0.001f;
    }
}
