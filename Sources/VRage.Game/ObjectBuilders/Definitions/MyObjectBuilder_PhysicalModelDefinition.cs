﻿using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalModelDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember]
        public string PhysicalMaterial;

        [ProtoMember]
        public float Mass = 0;
    }
}
