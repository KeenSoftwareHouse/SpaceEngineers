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
        [ProtoMember]
        public int Channel;

        [ProtoMember]
        public float MaxViewDistance;

        [ProtoMember]
        public float SectorSize;

        [ProtoMember]
        public string PhysicalMaterial;

        [ProtoMember]
        public float ItemSize;
    }
}
