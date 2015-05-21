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
        [ProtoMember, DefaultValue(10)]
        public float AngularImpulse = 10f;

        [ProtoMember, DefaultValue(null)]
        public string AngularImpulseSubBockName = null;

        [ProtoMember, DefaultValue(0)]
        public float AngularVelocityLimit = 0;

    }
}
