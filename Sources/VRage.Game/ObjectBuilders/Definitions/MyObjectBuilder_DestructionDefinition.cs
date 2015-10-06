using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DestructionDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember, DefaultValue(100f)]
        public float DestructionDamage = 100f;

        [ProtoMember, DefaultValue("Textures\\GUI\\Icons\\Fake.dds")]
        public string Icon = "Textures\\GUI\\Icons\\Fake.dds";
    }
}
