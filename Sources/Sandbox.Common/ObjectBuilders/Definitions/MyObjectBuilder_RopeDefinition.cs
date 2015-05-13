using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RopeDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public bool EnableRayCastRelease;

        [ProtoMember(2)]
        public bool IsDefaultCreativeRope;
    }
}
