using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Medieval.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MechanicalSourceBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1), DefaultValue(10)]
        public float AngularImpulse = 10f;

        [ProtoMember(2), DefaultValue(null)]
        public string AngularImpulseSubBockName = null;

        [ProtoMember(3), DefaultValue(0)]
        public float AngularVelocityLimit = 0;

    }
}
