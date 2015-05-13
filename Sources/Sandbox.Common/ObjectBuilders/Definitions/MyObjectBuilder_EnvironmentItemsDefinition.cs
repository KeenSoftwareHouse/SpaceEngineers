using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.Diagnostics;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentItemsDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public int Channel;

        [ProtoMember(2)]
        public float MaxViewDistance;

        [ProtoMember(3)]
        public float SectorSize;

        [ProtoMember(4)]
        public string PhysicalMaterial;

        [ProtoMember(5)]
        public float ItemSize;
    }
}
