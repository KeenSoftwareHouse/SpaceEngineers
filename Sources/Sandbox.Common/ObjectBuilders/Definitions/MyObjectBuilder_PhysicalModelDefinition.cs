using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalModelDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember(2)]
        public string PhysicalMaterial;

        [ProtoMember(3)]
        public float Mass;
    }
}
