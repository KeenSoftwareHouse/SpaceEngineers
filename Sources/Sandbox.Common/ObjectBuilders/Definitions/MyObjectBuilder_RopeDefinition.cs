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
        [ProtoMember]
        public bool EnableRayCastRelease;

        [ProtoMember]
        public bool IsDefaultCreativeRope;

        [ProtoMember]
        public string ColorMetalTexture;

        [ProtoMember]
        public string NormalGlossTexture;

        [ProtoMember]
        public string AddMapsTexture;
    }
}
